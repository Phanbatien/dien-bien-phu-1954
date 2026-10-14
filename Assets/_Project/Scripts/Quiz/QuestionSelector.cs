using System;
using System.Collections.Generic;
using System.Linq;

namespace DBP.Quiz
{
    /// Yêu cầu một câu hỏi: màn nào, hình thức nào, khoảng cấp nào.
    public sealed class QuestionQuery
    {
        public readonly string MissionId;
        public readonly QuizForm Form;
        public readonly int MinLevel;
        public readonly int MaxLevel;

        /// true = lấy từ nhóm câu dự trữ (nhánh dự phòng, ví dụ bảo hiểm sau 3 hòm sai liên tiếp).
        /// false = lấy từ nhóm câu thường, không đụng vào câu dự trữ.
        public readonly bool UseReserve;

        public QuestionQuery(string missionId, QuizForm form, int minLevel, int maxLevel, bool useReserve = false)
        {
            if (minLevel > maxLevel) throw new ArgumentException("minLevel không được lớn hơn maxLevel");
            MissionId = missionId;
            Form = form;
            MinLevel = minLevel;
            MaxLevel = maxLevel;
            UseReserve = useReserve;
        }

        public static QuestionQuery ForLevel(string missionId, QuizForm form, int level, bool useReserve = false)
        {
            return new QuestionQuery(missionId, form, level, level, useReserve);
        }
    }

    /// Ném ra khi ngân hàng không còn câu phù hợp (hàm Pick). TryPick thì trả false thay vì ném.
    public sealed class QuestionUnavailableException : Exception
    {
        public readonly QuestionQuery Query;

        public QuestionUnavailableException(QuestionQuery query, string message) : base(message)
        {
            Query = query;
        }
    }

    public sealed class QuestionSelectorOptions
    {
        /// Chỉ bật khi chơi thử trong Editor. Bản build luôn để false: chỉ câu "approved" được hiện.
        public bool IncludeDrafts = false;

        /// missionId của nhóm câu chiến dịch dùng chung (file questions/campaign.json).
        /// Cần xác nhận với Long/GDD; để null hoặc IncludeShared = false thì không dùng nhóm này.
        public string SharedMissionId = "CAMPAIGN";
        public bool IncludeShared = true;

        /// Số câu dành riêng cho nhánh dự phòng ở mỗi màn (0 = không dự trữ).
        public int ReservePerMission = 0;

        /// Câu dự trữ chỉ chọn từ các câu có cấp nhỏ hơn hoặc bằng giá trị này (câu dễ).
        public int ReserveMaxLevel = 2;
    }

    /// Bộ chọn câu hỏi (T17, BR-31). Thuần C#, không dùng UnityEngine nên test được ở EditMode.
    ///  - chỉ lấy câu approved (trừ khi bật IncludeDrafts);
    ///  - không lặp mã câu trong một lượt (dùng ở "field" rồi thì cũng không hiện lại ở "bunker");
    ///  - lọc theo màn, hình thức, khoảng cấp;
    ///  - có nhóm câu dự trữ cho nhánh dự phòng;
    ///  - hết câu: Pick ném QuestionUnavailableException, TryPick trả false kèm lý do.
    public sealed class QuestionSelector
    {
        private readonly List<Question> _pool = new List<Question>();
        private readonly List<string> _rejected = new List<string>();
        private readonly HashSet<string> _used = new HashSet<string>();
        private readonly HashSet<string> _reserved = new HashSet<string>();
        private readonly QuestionSelectorOptions _options;
        private readonly System.Random _rng;

        /// Các câu bị loại vì sai cấu trúc hoặc trùng mã (để in ra log / Validate).
        public IReadOnlyList<string> Rejected { get { return _rejected; } }
        public int PoolSize { get { return _pool.Count; } }
        public int UsedCount { get { return _used.Count; } }

        public QuestionSelector(IEnumerable<Question> bank, QuestionSelectorOptions options = null, int? seed = null)
        {
            _options = options ?? new QuestionSelectorOptions();
            _rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

            var seen = new HashSet<string>();
            if (bank != null)
            {
                foreach (Question q in bank)
                {
                    if (q == null) { _rejected.Add("(câu rỗng)"); continue; }
                    if (!_options.IncludeDrafts && !q.IsApproved()) continue; // bản nháp: bỏ qua, không phải lỗi

                    string reason;
                    if (!q.TryValidateStructure(out reason)) { _rejected.Add(q.id + ": " + reason); continue; }
                    if (!seen.Add(q.id)) { _rejected.Add(q.id + ": trùng mã"); continue; }

                    _pool.Add(q);
                }
            }

            BuildReserve();
        }

        /// Bắt đầu lượt chơi mới: xóa danh sách câu đã dùng và chọn lại nhóm dự trữ.
        public void ResetSession()
        {
            _used.Clear();
            BuildReserve();
        }

        public bool IsReserved(string id) { return id != null && _reserved.Contains(id); }
        public bool IsUsed(string id) { return id != null && _used.Contains(id); }

        public void MarkUsed(string id)
        {
            if (!string.IsNullOrEmpty(id)) _used.Add(id);
        }

        /// Số câu còn lại phù hợp với yêu cầu (chưa dùng).
        public int Remaining(QuestionQuery query)
        {
            return Candidates(query).Count();
        }

        public bool TryPick(QuestionQuery query, out Question question, out string error)
        {
            List<Question> candidates = Candidates(query).ToList();
            if (candidates.Count == 0)
            {
                question = null;
                error = Describe(query);
                return false;
            }

            question = candidates[_rng.Next(candidates.Count)];
            _used.Add(question.id);
            error = null;
            return true;
        }

        public Question Pick(QuestionQuery query)
        {
            Question q;
            string error;
            if (!TryPick(query, out q, out error)) throw new QuestionUnavailableException(query, error);
            return q;
        }

        private bool SameMission(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSharedMission(string missionId)
        {
            return _options.IncludeShared
                   && !string.IsNullOrEmpty(_options.SharedMissionId)
                   && SameMission(missionId, _options.SharedMissionId);
        }

        private IEnumerable<Question> Candidates(QuestionQuery r)
        {
            foreach (Question q in _pool)
            {
                if (_used.Contains(q.id)) continue;

                bool sameMission = SameMission(q.missionId, r.MissionId);
                bool shared = IsSharedMission(q.missionId) && !SameMission(r.MissionId, _options.SharedMissionId);
                if (!sameMission && !shared) continue;

                if (q.level < r.MinLevel || q.level > r.MaxLevel) continue;
                if (!q.HasForm(r.Form)) continue;
                if (r.UseReserve != _reserved.Contains(q.id)) continue;

                yield return q;
            }
        }

        private void BuildReserve()
        {
            _reserved.Clear();
            if (_options.ReservePerMission <= 0) return;

            var eligible = _pool.Where(q => !IsSharedMission(q.missionId)
                                            && q.level <= _options.ReserveMaxLevel
                                            && q.HasForm(QuizForm.Field));

            foreach (IGrouping<string, Question> group in eligible.GroupBy(q => q.missionId, StringComparer.OrdinalIgnoreCase))
            {
                foreach (Question q in group.OrderBy(x => _rng.Next()).Take(_options.ReservePerMission))
                {
                    _reserved.Add(q.id);
                }
            }
        }

        private string Describe(QuestionQuery r)
        {
            string level = r.MinLevel == r.MaxLevel ? "cấp " + r.MinLevel : "cấp " + r.MinLevel + "–" + r.MaxLevel;
            string form = r.Form == QuizForm.Field ? "ngoài trận" : "hầm";
            string kind = r.UseReserve ? "câu dự trữ" : "câu";
            return "Hết " + kind + " " + level + " (" + form + ") cho màn " + r.MissionId
                   + ". Đã dùng " + _used.Count + " câu trong lượt này.";
        }
    }
}
