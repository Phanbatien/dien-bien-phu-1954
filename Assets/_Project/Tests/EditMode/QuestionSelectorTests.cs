using System.Collections.Generic;
using NUnit.Framework;

namespace DBP.Quiz.Tests
{
    public class QuestionSelectorTests
    {
        private static Question Make(string id, string missionId, int level, string status = "approved", params string[] forms)
        {
            if (forms.Length == 0) forms = new[] { "field", "bunker" };
            return new Question
            {
                id = id, missionId = missionId, level = level, status = status, forms = forms,
                stem = "Câu " + id,
                options = new[]
                {
                    new QuestionOption { id = "a", text = "A" },
                    new QuestionOption { id = "b", text = "B" }
                },
                correct = "a"
            };
        }

        private static List<Question> Bank(int count, string mission = "M1", int level = 1)
        {
            var list = new List<Question>();
            for (int i = 1; i <= count; i++) list.Add(Make(mission + "-" + i.ToString("000"), mission, level));
            return list;
        }

        [Test]
        public void CallingWholeBank_NeverRepeats_ThenThrows()
        {
            var sel = new QuestionSelector(Bank(10), null, 1);
            var q = QuestionQuery.ForLevel("M1", QuizForm.Field, 1);
            var seen = new HashSet<string>();

            for (int i = 0; i < 10; i++) Assert.IsTrue(seen.Add(sel.Pick(q).id), "câu bị lặp ở lần " + i);

            Assert.Throws<QuestionUnavailableException>(() => sel.Pick(q));
        }

        [Test]
        public void TryPick_WhenEmpty_ReturnsFalseWithMessage()
        {
            var sel = new QuestionSelector(Bank(1), null, 1);
            var q = QuestionQuery.ForLevel("M1", QuizForm.Field, 1);
            Question result; string err;

            Assert.IsTrue(sel.TryPick(q, out result, out err));
            Assert.IsFalse(sel.TryPick(q, out result, out err));
            Assert.IsNull(result);
            StringAssert.Contains("Hết", err);
        }

        [Test]
        public void Drafts_AreIgnored_UnlessEnabled()
        {
            var bank = new List<Question> { Make("M1-001", "M1", 1, "draft"), Make("M1-002", "M1", 1, "approved") };
            var q = QuestionQuery.ForLevel("M1", QuizForm.Field, 1);

            var strict = new QuestionSelector(bank, null, 1);
            Assert.AreEqual(1, strict.PoolSize);
            Assert.AreEqual("M1-002", strict.Pick(q).id);

            var loose = new QuestionSelector(bank, new QuestionSelectorOptions { IncludeDrafts = true }, 1);
            Assert.AreEqual(2, loose.PoolSize);
        }

        [Test]
        public void Filters_ByMission_Level_AndForm()
        {
            var bank = new List<Question>
            {
                Make("M1-001", "M1", 1, "approved", "field"),
                Make("M1-002", "M1", 3, "approved", "bunker"),
                Make("M2-001", "M2", 1, "approved", "field")
            };
            var sel = new QuestionSelector(bank, null, 1);

            Assert.AreEqual("M1-001", sel.Pick(QuestionQuery.ForLevel("M1", QuizForm.Field, 1)).id);
            Assert.AreEqual("M1-002", sel.Pick(new QuestionQuery("M1", QuizForm.Bunker, 2, 5)).id);
            Assert.AreEqual("M2-001", sel.Pick(QuestionQuery.ForLevel("M2", QuizForm.Field, 1)).id);
        }

        [Test]
        public void MissionId_IsCaseInsensitive()
        {
            var sel = new QuestionSelector(new List<Question> { Make("M1-001", "M1", 1) }, null, 1);
            Assert.AreEqual("M1-001", sel.Pick(QuestionQuery.ForLevel("m1", QuizForm.Field, 1)).id);
        }

        [Test]
        public void QuestionUsedInField_IsNotShownAgainInBunker()
        {
            var sel = new QuestionSelector(new List<Question> { Make("M1-001", "M1", 1, "approved", "field", "bunker") }, null, 1);

            sel.Pick(QuestionQuery.ForLevel("M1", QuizForm.Field, 1));
            Assert.AreEqual(0, sel.Remaining(QuestionQuery.ForLevel("M1", QuizForm.Bunker, 1)));
        }

        [Test]
        public void SharedCampaignPool_IsUsedByAnyMission()
        {
            var sel = new QuestionSelector(new List<Question> { Make("C-001", "CAMPAIGN", 1) }, null, 1);
            Assert.AreEqual("C-001", sel.Pick(QuestionQuery.ForLevel("M3", QuizForm.Field, 1)).id);
        }

        [Test]
        public void Reserve_IsKeptAside_AndOnlyUsedWhenRequested()
        {
            var opts = new QuestionSelectorOptions { ReservePerMission = 2, ReserveMaxLevel = 2 };
            var sel = new QuestionSelector(Bank(6), opts, 7);
            var normal = QuestionQuery.ForLevel("M1", QuizForm.Field, 1);
            var reserve = QuestionQuery.ForLevel("M1", QuizForm.Field, 1, true);

            for (int i = 0; i < 4; i++) Assert.IsFalse(sel.IsReserved(sel.Pick(normal).id));
            Assert.Throws<QuestionUnavailableException>(() => sel.Pick(normal));

            Assert.IsTrue(sel.IsReserved(sel.Pick(reserve).id));
            Assert.IsTrue(sel.IsReserved(sel.Pick(reserve).id));
            Assert.Throws<QuestionUnavailableException>(() => sel.Pick(reserve));
        }

        [Test]
        public void ResetSession_MakesQuestionsAvailableAgain()
        {
            var sel = new QuestionSelector(Bank(2), null, 1);
            var q = QuestionQuery.ForLevel("M1", QuizForm.Field, 1);

            sel.Pick(q); sel.Pick(q);
            Assert.AreEqual(0, sel.Remaining(q));

            sel.ResetSession();
            Assert.AreEqual(2, sel.Remaining(q));
        }

        [Test]
        public void BrokenQuestions_AreRejected_NotCrashing()
        {
            var badCorrect = Make("M1-001", "M1", 1);
            badCorrect.correct = "z"; // không khớp id đáp án nào

            var bank = new List<Question>
            {
                badCorrect,
                Make("M1-002", "M1", 1),
                Make("M1-002", "M1", 1), // trùng mã
                null
            };
            var sel = new QuestionSelector(bank, null, 1);

            Assert.AreEqual(1, sel.PoolSize);
            Assert.AreEqual(3, sel.Rejected.Count);
        }

        [Test]
        public void SameSeed_GivesSameSequence()
        {
            var q = QuestionQuery.ForLevel("M1", QuizForm.Field, 1);
            var a = new QuestionSelector(Bank(10), null, 42);
            var b = new QuestionSelector(Bank(10), null, 42);

            for (int i = 0; i < 10; i++) Assert.AreEqual(a.Pick(q).id, b.Pick(q).id);
        }

        [Test]
        public void IsCorrectAnswer_ComparesById_NotPosition()
        {
            var q = Make("M1-001", "M1", 1);
            q.correct = "b";

            Assert.IsTrue(q.IsCorrectAnswer("b"));
            Assert.IsFalse(q.IsCorrectAnswer("a"));
        }

        [Test]
        public void Provider_ReturnsQuestion_AndNullWhenMissing()
        {
            var sel = new QuestionSelector(Bank(1), null, 1);
            var provider = new JsonQuestionProvider(sel, "M1");

            Assert.IsNotNull(provider.Next(1, QuizForm.Field));
            Assert.IsNull(provider.Next(1, QuizForm.Field));   // hết câu: null, không ném lỗi
            Assert.IsNull(provider.Next(5, QuizForm.Field));   // không có câu cấp 5
        }

        [Test]
        public void Provider_FollowsMissionId()
        {
            var bank = new List<Question> { Make("M1-001", "M1", 1), Make("M2-001", "M2", 1) };
            var provider = new JsonQuestionProvider(new QuestionSelector(bank, null, 1), "M2");

            Assert.AreEqual("M2-001", provider.Next(1, QuizForm.Field).id);
        }

        [Test]
        public void GddSampleJson_Parses_IgnoringExtraFields()
        {
            string json = @"{ 'schemaVersion': 1, 'questions': [ {
                'id': 'Q-M1-L2-006', 'missionId': 'M1', 'eventId': 'EVT-19540313', 'level': 2,
                'forms': ['bunker', 'field'], 'sceneTags': ['himlam'],
                'stem': 'Trận mở màn đánh vào cứ điểm nào?',
                'options': [ { 'id': 'a', 'text': 'Him Lam' }, { 'id': 'b', 'text': 'Đồi A1' } ],
                'correct': 'a', 'fact': 'Chiều 13/3/1954 ...',
                'source': { 'sourceId': 'SRC-001', 'page': '12' },
                'status': 'approved', 'revision': 1 } ] }".Replace('\'', '"');

            List<Question> list = QuizQuestionParser.Parse(json);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("M1", list[0].missionId);
            Assert.AreEqual("Trận mở màn đánh vào cứ điểm nào?", list[0].stem);
            Assert.AreEqual(2, list[0].options.Length);
            Assert.AreEqual("a", list[0].correct);
            Assert.IsTrue(list[0].IsApproved());
            Assert.IsTrue(list[0].HasForm(QuizForm.Field));
        }
    }
}
