using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBP.Content;
using DBP.Interaction;
using DBP.Quiz;
using DBP.Upgrades;
using DBP.Weapons;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Test cho T11, T17, T20, T22, T23, T40.
    public class DataAndCrateTests
    {
        static Question Q(string id, int level, string status = "approved", string mission = "M1", params string[] forms) => new Question
        {
            id = id, level = level, status = status, missionId = mission,
            forms = forms.Length > 0 ? forms : new[] { "field" },
            stem = "Câu hỏi?", correct = "a", fact = "Giải thích.",
            source = new QuestionSource { sourceId = "SRC-1", page = "12" },
            options = new[] { new QuestionOption { id = "a", text = "A" }, new QuestionOption { id = "b", text = "B" } },
        };

        string tempDir;

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "dbp-test-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            ContentLoader.Root = Path.Combine(UnityEngine.Application.streamingAssetsPath, "content");
            ProfileService.Root = Path.Combine(UnityEngine.Application.persistentDataPath, "profiles");
            ProfileService.Reload();
        }

        // ── T11: đọc JSON ──
        [Test]
        public void Loader_ReadsGoodFile_ReportsMissingAndBroken()
        {
            ContentLoader.Root = tempDir;
            Directory.CreateDirectory(Path.Combine(tempDir, "questions"));
            File.WriteAllText(Path.Combine(tempDir, "questions", "m1.json"), "{\"questions\":[{\"id\":\"Q1\",\"level\":1}]}");
            File.WriteAllText(Path.Combine(tempDir, "questions", "m2.json"), "{ hỏng");

            var errors = new List<string>();
            var all = QuestionBank.LoadAll(errors);
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual("Q1", all[0].id);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("m2.json", errors[0]);

            Assert.IsFalse(ContentLoader.TryLoad<QuestionFile>("khong-co.json", out _, out var missing));
            StringAssert.Contains("Thiếu file", missing);
        }

        [Test]
        public void Loader_ShippedContentParses()
        {
            var errors = new List<string>();
            Assert.IsNotEmpty(QuestionBank.LoadAll(errors));
            Assert.IsEmpty(errors);
            Assert.IsTrue(ContentLoader.TryLoad<WeaponFileForTest>("weapons.json", out var w, out _));
            Assert.AreEqual(5, w.weapons.Length);
        }

        [System.Serializable]
        class WeaponFileForTest { public WeaponDef[] weapons; }

        // ── T17: chọn câu không lặp (BR-31) ──
        [Test]
        public void Selector_NeverRepeats_ThenRunsOut()
        {
            var s = new QuestionSelector(new[] { Q("A", 1), Q("B", 2), Q("C", 2) }, "M1", 42, false);
            var got = new HashSet<string>();
            for (int i = 0; i < 3; i++) Assert.IsTrue(got.Add(s.Next(1, 2, QuizForm.Field).id));
            Assert.IsNull(s.Next(1, 2, QuizForm.Field), "Hết câu thì trả null, không lặp");
        }

        [Test]
        public void Selector_FiltersDraft_Mission_Form_Level()
        {
            var bank = new[]
            {
                Q("draft", 1, "draft"),
                Q("other-mission", 1, mission: "M2"),
                Q("bunker-only", 1, forms: "bunker"),
                Q("level3", 3),
                Q("campaign", 1, mission: QuestionSelector.Campaign),
                Q("ok", 1),
            };
            var s = new QuestionSelector(bank, "M1", 1, allowDraft: false);
            var picked = new List<string>();
            Question q;
            while ((q = s.Next(1, 2, QuizForm.Field)) != null) picked.Add(q.id);
            CollectionAssert.AreEquivalent(new[] { "campaign", "ok" }, picked);
            Assert.AreEqual("bunker-only", s.Next(1, 1, QuizForm.Bunker).id);
        }

        [Test]
        public void Selector_PrefersQuestionsExclusiveToForm()
        {
            var s = new QuestionSelector(new[] { Q("both", 1, forms: new[] { "bunker", "field" }), Q("field-only", 1) }, "M1", 7, false);
            Assert.AreEqual("field-only", s.Next(1, 1, QuizForm.Field).id, "Giữ câu dùng chung cho hầm");
        }

        // ── T20: lưu hồ sơ an toàn (BR-37) ──
        [Test]
        public void Profile_SaveLoadRoundTrip()
        {
            ProfileService.Root = tempDir;
            ProfileService.Reload();
            ProfileService.Data.resolve = 250;
            ProfileService.Data.upgrades.Add("U-AMMO-1");
            Assert.IsTrue(ProfileService.Save());
            ProfileService.Reload();
            Assert.AreEqual(250, ProfileService.Data.resolve);
            Assert.Contains("U-AMMO-1", ProfileService.Data.upgrades);
        }

        [Test]
        public void Profile_CorruptFile_FallsBackToBackup()
        {
            ProfileService.Root = tempDir;
            ProfileService.Reload();
            ProfileService.Data.resolve = 100;
            ProfileService.Save();
            ProfileService.Data.resolve = 200;
            ProfileService.Save(); // bản 100 thành .bak
            File.WriteAllText(ProfileService.FilePath, "{ hỏng giữa chừng");
            ProfileService.Reload();
            Assert.AreEqual(100, ProfileService.Data.resolve);
        }

        [Test]
        public void Profile_BuyFailsToSave_RollsBack()
        {
            var blocked = Path.Combine(tempDir, "blocked");
            File.WriteAllText(blocked, "file chặn: không tạo được thư mục hồ sơ");
            ProfileService.Root = blocked;
            ProfileService.Reload();
            ProfileService.Data.resolve = 300;
            LogAssert.Expect(UnityEngine.LogType.Error, new Regex("Không lưu được hồ sơ"));
            Assert.AreEqual(BuyResult.SaveFailed, ProfileService.Buy("U-AMMO-1", true));
            Assert.AreEqual(300, ProfileService.Data.resolve);
            Assert.IsEmpty(ProfileService.Data.upgrades);
        }

        // ── T22: rút hòm theo seed (BR-13, BR-15) ──
        [Test]
        public void CrateRoll_SameSeedSamePoints_DistinctAndInRange()
        {
            var a = CrateRoll.PickPoints(new System.Random(99), 12, 5, 7);
            var b = CrateRoll.PickPoints(new System.Random(99), 12, 5, 7);
            CollectionAssert.AreEqual(a, b);
            Assert.That(a.Count, Is.InRange(5, 7));
            Assert.AreEqual(a.Count, a.Distinct().Count());
            Assert.IsTrue(a.All(i => i >= 0 && i < 12));
        }

        [Test]
        public void CrateRoll_RarityFollowsWeights()
        {
            var rng = new System.Random(1);
            var w = new[] { 0.55f, 0.25f, 0.15f, 0.05f };
            var counts = new int[4];
            for (int i = 0; i < 10000; i++) counts[(int)CrateRoll.RollRarity(rng, w)]++;
            for (int i = 0; i < 4; i++) Assert.AreEqual(w[i], counts[i] / 10000f, 0.02f);
        }

        // ── T23: bảo hiểm 3 lần sai (BR-16, Phụ lục A dòng 4) ──
        [Test]
        public void Pity_ThreeWrongCrates_NextIsPity_ThenResets()
        {
            var p = new PityTracker();
            p.Record(QuizOutcome.Wrong, false);
            p.Record(QuizOutcome.Timeout, false);
            // điện thoại đúng ở giữa: không gọi Record, chuỗi giữ nguyên
            p.Record(QuizOutcome.Wrong, false);
            Assert.IsTrue(p.Pending);
            p.Record(QuizOutcome.Wrong, true); // câu bảo hiểm sai vẫn đặt về 0
            Assert.IsFalse(p.Pending);
            Assert.AreEqual(0, p.WrongStreak);
        }

        [Test]
        public void Pity_CorrectBeforeThree_ResetsStreak()
        {
            var p = new PityTracker();
            p.Record(QuizOutcome.Wrong, false);
            p.Record(QuizOutcome.Wrong, false);
            p.Record(QuizOutcome.Correct, false);
            Assert.AreEqual(0, p.WrongStreak);
        }

        [Test]
        public void Rarity_CommonUsesLevels1To2()
        {
            Assert.AreEqual(1, RarityRules.MinLevel(Rarity.Common));
            Assert.AreEqual(2, RarityRules.MaxLevel(Rarity.Common));
            Assert.AreEqual(5, RarityRules.MaxLevel(Rarity.Legendary));
            Assert.AreEqual(Rarity.Rare, RarityRules.Parse("rare"));
        }

        // ── T40: Validate Content (BR-27–BR-30, Phụ lục A dòng 9) ──
        [Test]
        public void Validate_Field121Chars_AndDraft_AreNotShippable()
        {
            var longQ = Q("LONG", 1, forms: new[] { "bunker", "field" });
            longQ.stem = new string('a', 121);
            var errors = new List<string>();
            var warnings = new List<string>();
            ContentRules.CheckQuestions(new[] { longQ, Q("DRAFT", 1, "draft") }, errors, warnings);
            Assert.IsTrue(errors.Any(e => e.Contains("LONG") && e.Contains("121")));
            Assert.IsTrue(warnings.Any(w => w.Contains("DRAFT")));
        }

        [Test]
        public void Validate_VisibleLength_NormalizesUnicode()
        {
            string composed = "Điện Biên Phủ";
            string decomposed = composed.Normalize(System.Text.NormalizationForm.FormD);
            Assert.AreEqual(ContentRules.VisibleLength(composed), ContentRules.VisibleLength(decomposed));
            Assert.AreEqual(13, ContentRules.VisibleLength(decomposed));
        }

        [Test]
        public void Validate_QuestionErrors()
        {
            var noSource = Q("NOSRC", 2); noSource.source = null;
            var badCorrect = Q("BAD", 2); badCorrect.correct = "z";
            var dup = Q("NOSRC", 3);
            var errors = new List<string>();
            ContentRules.CheckQuestions(new[] { noSource, badCorrect, dup }, errors, new List<string>());
            Assert.IsTrue(errors.Any(e => e.Contains("NOSRC") && e.Contains("nguồn")));
            Assert.IsTrue(errors.Any(e => e.Contains("BAD") && e.Contains("correct")));
            Assert.IsTrue(errors.Any(e => e.Contains("trùng mã")));
        }

        [Test]
        public void Validate_Weapons_BannedAndAfter1954()
        {
            WeaponDef W(string id, int year) => new WeaponDef { id = id, name = id, ammoType = "x", availableFrom = year, sourceId = "S", stats = new WeaponStats() };
            var errors = new List<string>();
            ContentRules.CheckWeapons(new[] { W("W-K-54", 1954), W("W-NEW", 1960), W("W-OK", 1944) }, errors);
            Assert.IsTrue(errors.Any(e => e.Contains("W-K-54") && e.Contains("K-54")));
            Assert.IsTrue(errors.Any(e => e.Contains("W-NEW") && e.Contains("1960")));
            Assert.IsFalse(errors.Any(e => e.Contains("W-OK")));
        }

        [Test]
        public void Validate_WeightsMustSumToOne()
        {
            var errors = new List<string>();
            ContentRules.CheckWeights(new[] { 0.55f, 0.25f, 0.15f, 0.05f }, "ok", errors);
            Assert.IsEmpty(errors);
            ContentRules.CheckWeights(new[] { 0.5f, 0.25f, 0.15f, 0.05f }, "sai", errors);
            Assert.IsTrue(errors.Any(e => e.StartsWith("sai")));
        }

        [Test]
        public void Validate_Budget_ReportsShortfall()
        {
            var errors = new List<string>();
            var budget = new MissionBudget { missionId = "M1", maxCrates = 7, maxDrops = 3, phones = 1, bunkerQuestions = 5 };
            ContentRules.CheckBudget(new[] { Q("A", 1) }, budget, errors);
            Assert.IsTrue(errors.Any(e => e.Contains("cấp 5") && e.Contains("có 0, cần 10")));
        }
    }
}
