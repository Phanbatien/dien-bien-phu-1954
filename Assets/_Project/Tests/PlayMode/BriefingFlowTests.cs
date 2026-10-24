using System;
using System.Collections;
using System.IO;
using System.Linq;
using DBP.Core;
using DBP.Missions;
using DBP.Quiz;
using DBP.UI;
using DBP.Upgrades;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DBP.Tests
{
    /// T25 trên luồng thật: hầm chuẩn bị → họp tác chiến → xuất trận → thắng → nhận Quyết Tâm đúng một lần.
    public class BriefingFlowTests
    {
        IDisposable scope;
        string folder;

        static T Find<T>(string name) where T : Component => Object.FindObjectsByType<T>(FindObjectsSortMode.None).First(c => c.name == name);
        static void Click(string name) { var b = Find<Button>(name); Assert.IsTrue(b.interactable, name); b.onClick.Invoke(); }

        static IEnumerator Frames()
        {
            yield return null; yield return null; yield return null;
            float deadline = Time.realtimeSinceStartup + 10;
            while (SceneTransition.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "briefing-" + Guid.NewGuid().ToString("N"));
            scope = ProfileService.UseTemporaryStore(Path.Combine(folder, "profiles.json"));
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return Frames();
            Click("NewProfile");
            Find<InputField>("DisplayName").text = "Briefing";
            Find<InputField>("ClassName").text = "SE";
            Click("ConfirmCreate");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox");
            yield return Frames();
            scope.Dispose();
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Time.timeScale = 1; InputLock.ResetAll(); BunkerBriefing.Clear();
        }

        static void Win(MissionController mission) =>
            mission.Setup("M1", "Him Lam", "M2", "M2_GiuDocLap", mission.Player, new System.Collections.Generic.List<Objective>(), true);

        [UnityTest]
        public IEnumerator DeployLockedUntilBriefed_CorrectAnswersPayResolveOnceOnWin()
        {
            Click("Mission1");
            yield return Frames();
            Assert.AreEqual(CampaignScreen.Preparation, CampaignUI.Current.ScreenState);
            Assert.IsFalse(Find<Button>("Deploy").interactable, "Chưa họp thì chưa được xuất trận (BR-06)");
            var briefing = CampaignUI.Current.Briefing;
            Assert.That(briefing.Questions.Count, Is.InRange(3, 5));
            int expected = briefing.Questions.Sum(q => q.level * 10);

            CampaignUI.Current.CompleteBriefingForTests(true);
            Assert.IsTrue(Find<Button>("Deploy").interactable);
            int before = ProfileService.Data.resolve;
            Click("Deploy");
            yield return Frames();
            var mission = MissionController.Current;
            Assert.AreSame(briefing, mission.Briefing);
            Assert.AreEqual(briefing.Questions.Count, mission.Attempt.Answers.Count(a => a.inBunker), "Câu trong hầm tính vào điểm kiến thức của lượt (BR-25)");
            Assert.AreEqual(before, ProfileService.Data.resolve, "Quyết Tâm chỉ là chờ, chưa cộng khi chưa thắng (BR-07)");

            Win(mission);
            yield return Frames();
            Assert.AreEqual(MissionOutcome.Won, mission.Attempt.Outcome);
            Assert.AreEqual(before + expected, ProfileService.Data.resolve);
            Assert.AreEqual(expected, mission.Attempt.ResolveGained);
            foreach (var q in briefing.Questions) Assert.Contains(q.id, ProfileService.Data.rewardedQuestions);
        }

        [UnityTest]
        public IEnumerator LosingRun_PaysNoResolve()
        {
            Click("Mission1");
            yield return Frames();
            CampaignUI.Current.CompleteBriefingForTests(true);
            int before = ProfileService.Data.resolve;
            Click("Deploy");
            yield return Frames();
            var mission = MissionController.Current;
            mission.Player.TakeDamage(9999f, DamageType.Bullet, Vector3.zero);
            yield return Frames();
            Assert.AreEqual(MissionOutcome.Lost, mission.Attempt.Outcome);
            Assert.AreEqual(before, ProfileService.Data.resolve, "Thua không nhận Quyết Tâm chờ (BR-33)");
            Assert.IsEmpty(ProfileService.Data.rewardedQuestions, "Chưa ghi dấu thưởng để lượt sau còn nhận được (BR-34)");
        }
    }
}
