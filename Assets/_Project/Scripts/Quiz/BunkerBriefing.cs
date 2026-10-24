using System.Collections.Generic;
using System.Linq;

namespace DBP.Quiz
{
    /// Họp tác chiến trong hầm trước mỗi lượt (T25; BR-05, BR-06, BR-07, BR-34):
    /// 3–5 câu cấp 1–3, không giới hạn thời gian, phải trả lời hết mới xuất trận.
    /// Câu đúng tạo Quyết Tâm *chờ*; chỉ cộng vào hồ sơ khi thắng màn, mỗi mã câu một lần trên mỗi hồ sơ.
    /// Dùng chung bộ chọn câu với chiến trường để không lặp câu trong cùng lượt (BR-31).
    public class BunkerBriefing
    {
        public const int MinQuestions = 3, MaxQuestions = 5;

        /// Buổi họp đã xong, đang chờ màn chơi nhận (mang qua lúc đổi scene).
        public static BunkerBriefing Pending { get; private set; }

        public string MissionId { get; }
        public QuestionSelector Selector { get; }
        public readonly List<Question> Questions = new List<Question>();
        public readonly List<QuizResult> Results = new List<QuizResult>();

        public bool Complete => Results.Count >= Questions.Count;
        /// Thiếu câu trước lượt: không cho bắt đầu, báo lỗi nội dung (BR-31).
        public bool HasShortage => Questions.Count < MinQuestions;
        public bool Ready => Complete && !HasShortage;
        public Question Current => Complete ? null : Questions[Results.Count];

        BunkerBriefing(string missionId, QuestionSelector selector)
        {
            MissionId = missionId;
            Selector = selector;
        }

        public static BunkerBriefing Create(string missionId, int seed, IEnumerable<Question> bank, bool allowDraft)
        {
            var briefing = new BunkerBriefing(missionId, new QuestionSelector(bank, missionId, seed, allowDraft));
            int count = new System.Random(seed).Next(MinQuestions, MaxQuestions + 1);
            for (int i = 0; i < count; i++)
            {
                var q = briefing.Selector.Next(1, 3, QuizForm.Bunker);
                if (q == null) break;
                briefing.Questions.Add(q);
            }
            return briefing;
        }

        /// Mỗi câu chỉ nhận một đáp án cuối cùng (BR-07).
        public QuizResult? Answer(string optionId)
        {
            var q = Current;
            if (q == null) return null;
            var result = new QuizResult
            {
                questionId = q.id,
                level = q.level,
                outcome = optionId == q.correct ? QuizOutcome.Correct : QuizOutcome.Wrong,
                inBunker = true,
            };
            Results.Add(result);
            return result;
        }

        /// Quyết Tâm theo cấp câu trong hầm: 10 / 20 / 30 (BR-34).
        public static int ResolveFor(int level) => level * 10;

        /// Quyết Tâm chờ: câu đúng mà hồ sơ chưa từng được thưởng.
        public int PendingResolve(ICollection<string> alreadyRewarded) =>
            Results.Where(r => r.outcome == QuizOutcome.Correct && !alreadyRewarded.Contains(r.questionId))
                   .Select(r => r.questionId).Distinct()
                   .Sum(id => ResolveFor(Results.First(r => r.questionId == id).level));

        /// Quyết toán khi thắng: cộng điểm và ghi dấu đã thưởng trên cùng đối tượng hồ sơ (người gọi lưu một lần).
        public int Settle(ref int resolve, ICollection<string> rewarded)
        {
            int gained = 0;
            foreach (var r in Results.Where(r => r.outcome == QuizOutcome.Correct))
            {
                if (rewarded.Contains(r.questionId)) continue;
                rewarded.Add(r.questionId);
                gained += ResolveFor(r.level);
            }
            resolve += gained;
            return gained;
        }

        /// Gọi khi bấm Xuất trận: giao buổi họp cho màn chơi sắp nạp.
        public void Commit() => Pending = this;

        /// Màn chơi nhận buổi họp của mình; buổi họp của màn khác thì bỏ.
        public static BunkerBriefing Take(string missionId)
        {
            var pending = Pending;
            Pending = null;
            return pending != null && pending.MissionId == missionId ? pending : null;
        }

        public static void Clear() => Pending = null;
    }
}
