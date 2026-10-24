using System.Collections.Generic;
using System.Linq;
using DBP.Quiz;
using NUnit.Framework;

namespace DBP.Tests
{
    /// T25: họp tác chiến trong hầm (BR-05, BR-06, BR-07, BR-31, BR-34).
    public class BunkerBriefingTests
    {
        static Question Q(string id, int level, string mission = "M1", params string[] forms) => new Question
        {
            id = id, level = level, missionId = mission, status = "approved",
            forms = forms.Length > 0 ? forms : new[] { "bunker", "field" }, stem = "?", correct = "a", fact = "f",
            options = new[] { new QuestionOption { id = "a", text = "A" }, new QuestionOption { id = "b", text = "B" } },
        };

        static List<Question> Bank() => new List<Question>
        {
            Q("B1", 1), Q("B2", 2), Q("B3", 3), Q("B4", 1), Q("B5", 2), Q("B6", 3),
            Q("HARD", 5), Q("FIELDONLY", 1, "M1", "field"), Q("OTHER", 1, "M2"),
        };

        [TearDown]
        public void TearDown() => BunkerBriefing.Clear();

        [Test]
        public void Create_Picks3To5_Level1To3_BunkerForm_ThisMission()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var b = BunkerBriefing.Create("M1", seed, Bank(), false);
                Assert.That(b.Questions.Count, Is.InRange(3, 5));
                Assert.IsTrue(b.Questions.All(q => q.level <= 3 && q.forms.Contains("bunker") && q.missionId == "M1"));
                Assert.AreEqual(b.Questions.Count, b.Questions.Select(q => q.id).Distinct().Count());
            }
        }

        [Test]
        public void MustAnswerAll_BeforeReady()
        {
            var b = BunkerBriefing.Create("M1", 1, Bank(), false);
            Assert.IsFalse(b.Ready);
            while (!b.Complete) b.Answer("a");
            Assert.IsTrue(b.Ready);
            Assert.IsNull(b.Answer("a"), "Mỗi câu chỉ nhận một đáp án (BR-07)");
        }

        [Test]
        public void Shortage_BlocksDeployment()
        {
            var b = BunkerBriefing.Create("M1", 1, new[] { Q("B1", 1), Q("B2", 2) }, false);
            Assert.IsTrue(b.HasShortage);
            while (!b.Complete) b.Answer("a");
            Assert.IsFalse(b.Ready, "Thiếu câu trước lượt: không cho bắt đầu (BR-31)");
        }

        [Test]
        public void Resolve_10_20_30_OnlyCorrect_OncePerProfile()
        {
            var b = BunkerBriefing.Create("M1", 3, Bank(), false);
            foreach (var q in b.Questions.ToList()) b.Answer(q.level == 1 ? "b" : "a"); // sai câu cấp 1
            int expected = b.Questions.Where(q => q.level > 1).Sum(q => q.level * 10);
            var rewarded = new List<string>();
            Assert.AreEqual(expected, b.PendingResolve(rewarded));

            int resolve = 100;
            Assert.AreEqual(expected, b.Settle(ref resolve, rewarded));
            Assert.AreEqual(100 + expected, resolve);
            Assert.AreEqual(0, b.Settle(ref resolve, rewarded), "Không thưởng lại khi quyết toán lần hai (BR-34)");
            Assert.AreEqual(0, b.PendingResolve(rewarded));
        }

        [Test]
        public void SharedSelector_FieldNeverRepeatsBunkerQuestions()
        {
            var b = BunkerBriefing.Create("M1", 5, Bank(), false);
            var asked = new HashSet<string>(b.Questions.Select(q => q.id));
            Question q2;
            while ((q2 = b.Selector.Next(1, 5, QuizForm.Field)) != null)
                Assert.IsFalse(asked.Contains(q2.id), $"{q2.id} đã hỏi trong hầm (BR-31)");
        }

        [Test]
        public void Take_OnlyForSameMission_AndOnlyOnce()
        {
            var b = BunkerBriefing.Create("M1", 1, Bank(), false);
            b.Commit();
            Assert.IsNull(BunkerBriefing.Take("M2"), "Buổi họp của màn khác bị bỏ");
            b.Commit();
            Assert.AreSame(b, BunkerBriefing.Take("M1"));
            Assert.IsNull(BunkerBriefing.Take("M1"), "Chơi lại không dùng lại buổi họp cũ (BR-33)");
        }
    }
}
