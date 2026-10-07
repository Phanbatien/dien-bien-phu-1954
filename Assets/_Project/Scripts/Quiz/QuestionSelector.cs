using System;
using System.Collections.Generic;
using System.Linq;

namespace DBP.Quiz
{
    /// Chọn câu cho một lượt chơi (T17; BR-31):
    /// chỉ câu đã duyệt, đúng màn (hoặc dùng chung "CAMPAIGN"), đúng cấp và hình thức;
    /// không lặp mã câu trong lượt, tính chung hầm, hòm, súng rơi và điện thoại.
    public class QuestionSelector : IQuestionProvider
    {
        public const string Campaign = "CAMPAIGN";

        readonly List<Question> pool;
        readonly HashSet<string> seen = new HashSet<string>();
        readonly Random rng;

        /// allowDraft = true chỉ dùng trong Editor/bản Development; bản nộp chỉ có câu approved (BR-28).
        public QuestionSelector(IEnumerable<Question> all, string missionId, int seed, bool allowDraft)
        {
            pool = all.Where(q => q != null
                                  && (allowDraft || q.status == "approved")
                                  && (string.IsNullOrEmpty(missionId) || q.missionId == missionId || q.missionId == Campaign))
                      .GroupBy(q => q.id).Select(g => g.First())
                      .ToList();
            rng = new Random(seed);
        }

        public int Count => pool.Count;
        public bool Seen(string questionId) => seen.Contains(questionId);

        public int Available(int minLevel, int maxLevel, QuizForm form) =>
            pool.Count(q => !seen.Contains(q.id) && Matches(q, minLevel, maxLevel, form));

        public Question Next(int minLevel, int maxLevel, QuizForm form)
        {
            var candidates = pool.Where(q => !seen.Contains(q.id) && Matches(q, minLevel, maxLevel, form)).ToList();
            if (candidates.Count == 0) return null;

            // Giữ lại câu dùng chung cho hình thức kia: ưu tiên câu chỉ dùng được ở hình thức đang hỏi.
            var exclusive = candidates.Where(q => q.forms.Length == 1).ToList();
            var from = exclusive.Count > 0 ? exclusive : candidates;
            var pick = from[rng.Next(from.Count)];
            seen.Add(pick.id); // đánh dấu đã gặp ngay khi hiển thị, kể cả nếu sau đó hết giờ
            return pick;
        }

        static bool Matches(Question q, int minLevel, int maxLevel, QuizForm form)
        {
            if (q.level < minLevel || q.level > maxLevel || q.forms == null) return false;
            var tag = form == QuizForm.Field ? "field" : "bunker";
            return Array.IndexOf(q.forms, tag) >= 0;
        }
    }
}
