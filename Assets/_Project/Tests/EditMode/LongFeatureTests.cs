using System;
using System.IO;
using DBP.Quiz;
using DBP.Upgrades;
using NUnit.Framework;
namespace DBP.Tests
{
    public class LongFeatureTests
    {
        string folder, path;
        [SetUp] public void SetUp() { folder = Path.Combine(Path.GetTempPath(), "dbp-tests-" + Guid.NewGuid().ToString("N")); path = Path.Combine(folder, "profiles.json"); }
        [TearDown] public void TearDown() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        [TestCase(3, QuizForm.Field, 6, 15, 360)]
        [TestCase(3, QuizForm.Bunker, 0, 0, 300)]
        [TestCase(1, QuizForm.Field, 6, 12, 125)]
        [TestCase(1, QuizForm.Field, 0, 12, 100)]
        [TestCase(1, QuizForm.Field, 12, 12, 150)]
        [TestCase(1, QuizForm.Field, -1, 12, 100)]
        [TestCase(5, QuizForm.Field, 20, 12, 750)]
        [TestCase(1, QuizForm.Field, 0.12, 12, 101)]
        public void Score_BR25(int level, QuizForm form, double remaining, double limit, int expected) => Assert.AreEqual(expected, KnowledgeScore.Calculate(level, form, QuizOutcome.Correct, remaining, limit));
        [Test] public void Score_WrongAndTimeoutGiveZero()
        {
            Assert.AreEqual(0, KnowledgeScore.Calculate(5, QuizForm.Field, QuizOutcome.Wrong, 12, 12));
            Assert.AreEqual(0, KnowledgeScore.Calculate(1, QuizForm.Field, QuizOutcome.Timeout, 0, 12));
        }
        [Test] public void Score_RoundsEachQuestionBeforeAdding()
        {
            var answer = new QuizResult { level = 1, timeLimit = 12, remaining = 0.12f, outcome = QuizOutcome.Correct };
            Assert.AreEqual(202, KnowledgeScore.Total(new[] { answer, answer }));
        }
        [Test] public void Score_RejectsInvalidInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => KnowledgeScore.Calculate(0, QuizForm.Bunker, QuizOutcome.Correct));
            Assert.Throws<ArgumentOutOfRangeException>(() => KnowledgeScore.Calculate(6, QuizForm.Bunker, QuizOutcome.Correct));
            Assert.Throws<ArgumentOutOfRangeException>(() => KnowledgeScore.Calculate(1, QuizForm.Field, QuizOutcome.Correct, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => KnowledgeScore.Calculate(1, QuizForm.Field, QuizOutcome.Correct, double.NaN, 12));
        }
        [Test] public void Profiles_SameNamesStaySeparateAcrossReload()
        {
            var store = new ProfileRepository(path);
            var first = store.Create("Long", "SE", false); first.resolve = 400; store.Save(); store.Unlock("M2");
            var second = store.Create("Long", "SE", true);
            Assert.AreNotEqual(first.id, second.id);
            store = new ProfileRepository(path);
            Assert.AreEqual(2, store.Profiles.Count); Assert.AreEqual(second.id, store.Active.id);
            Assert.AreEqual(0, store.Active.resolve); Assert.IsFalse(store.Active.unlockedMissions.Contains("M2"));
            Assert.IsNotEmpty(store.Active.consentAt);
            store.Select(first.id); Assert.AreEqual(400, store.Active.resolve); Assert.IsEmpty(store.Active.consentAt);
            Assert.Contains("M2", store.Active.unlockedMissions);
        }
        [Test] public void Profiles_PurchaseIsPersistentAndNeverChargedTwice()
        {
            var store = new ProfileRepository(path); store.Create("Test", "", false).resolve = 500; store.Save();
            Assert.AreEqual(BuyResult.Ok, store.Buy("U-AMMO-1", true));
            Assert.AreEqual(BuyResult.AlreadyOwned, store.Buy("U-AMMO-1", true));
            store = new ProfileRepository(path); Assert.AreEqual(400, store.Active.resolve);
            Assert.AreEqual(1, store.Active.upgrades.Count);
            Assert.AreEqual(BuyResult.NotInBunker, store.Buy("U-AMMO-2", false));
            Assert.AreEqual(400, store.Active.resolve);
        }
        [Test] public void Profiles_BackupRecoveryAndInterruptedTemporaryFile()
        {
            var store = new ProfileRepository(path); store.Create("Saved", "", false); store.SetVolume(0.4f);
            File.WriteAllText(path + ".tmp", "incomplete");
            Assert.AreEqual(0.4f, new ProfileRepository(path).Active.volume);
            File.WriteAllText(path, "broken"); store = new ProfileRepository(path);
            Assert.IsTrue(store.RecoveredBackup); Assert.AreEqual("Saved", store.Active.displayName);
            store.Save(); Assert.AreEqual("Saved", new ProfileRepository(path).Active.displayName);
        }
        [Test] public void Profiles_SaveFailureRollsBackPurchase()
        {
            var store = new ProfileRepository(path); store.Create("Test", "", false).resolve = 500; store.Save();
            using (var locked = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                Assert.Throws<IOException>(() => store.Buy("U-AMMO-1", true));
            Assert.AreEqual(500, store.Active.resolve); Assert.IsEmpty(store.Active.upgrades);
            Assert.AreEqual(500, new ProfileRepository(path).Active.resolve);
        }
        [Test] public void Profiles_ConsentAndVolumePersist()
        {
            var store = new ProfileRepository(path); store.Create("Test", "", false);
            store.SetConsent(true); store.SetVolume(0.35f); store = new ProfileRepository(path);
            Assert.IsNotEmpty(store.Active.consentAt); Assert.AreEqual(0.35f, store.Active.volume);
            store.SetConsent(false); Assert.IsEmpty(new ProfileRepository(path).Active.consentAt);
        }
        [Test] public void Profiles_RejectBlankNameAndKeepOriginalOnCorruption()
        {
            var store = new ProfileRepository(path); Assert.Throws<ArgumentException>(() => store.Create(" ", "", false));
            Directory.CreateDirectory(folder); File.WriteAllText(path, "corrupt");
            Assert.Throws<InvalidDataException>(() => new ProfileRepository(path)); Assert.AreEqual("corrupt", File.ReadAllText(path));
        }
    }
}
