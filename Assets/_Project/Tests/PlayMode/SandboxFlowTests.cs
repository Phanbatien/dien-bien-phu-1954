using System.Collections;
using System.Linq;
using DBP.Core;
using DBP.Interaction;
using DBP.Missions;
using DBP.Quiz;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Chạy scene Sandbox thật: vòng hòm → đứng yên → hết giờ → chuyển tiếp, và thắng/thua.
    public class SandboxFlowTests
    {
        System.IDisposable rankingProfileScope;
        string rankingTestFolder;
        [UnitySetUp] public IEnumerator IsolateRankingStore()
        {
            rankingTestFolder = System.IO.Path.Combine(Application.temporaryCachePath,"t57-compat-"+System.Guid.NewGuid().ToString("N"));
            rankingProfileScope = DBP.Upgrades.ProfileService.UseTemporaryStore(System.IO.Path.Combine(rankingTestFolder,"profiles.json"));
            yield return null;
        }
        [UnityTearDown] public IEnumerator RestoreRankingStore()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
            rankingProfileScope.Dispose();
            if(System.IO.Directory.Exists(rankingTestFolder))System.IO.Directory.Delete(rankingTestFolder,true);
        }

        [UnitySetUp]
        public IEnumerator LoadSandbox()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Crate_FreezesWorld_TimesOut_ThenResumesTogether()
        {
            var crate = Object.FindObjectsByType<QuizCrate>(FindObjectsSortMode.None).First(c => c.rarity == Rarity.Common);
            var interactor = Object.FindAnyObjectByType<Interactor>();
            Assert.IsTrue(crate.CanInteract(interactor));

            crate.Interact(interactor);
            Assert.IsTrue(WorldFreeze.IsFrozen, "Thế giới phải đứng yên khi câu hỏi hiện");
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(InputLock.Locked);
            Assert.IsFalse(crate.CanInteract(interactor), "Hòm chỉ dùng một lần (BR-12)");

            QuizResult? result = null;
            FieldQuizSession.Answered += r => result = r;
            yield return new WaitForSecondsRealtime(12.1f); // hòm thường: 12 giây
            Assert.IsTrue(result.HasValue);
            Assert.AreEqual(QuizOutcome.Timeout, result.Value.outcome);
            Assert.IsTrue(WorldFreeze.IsFrozen, "Trong 0,5 s chuyển tiếp chiến trường vẫn đứng yên (BR-12)");
            Assert.IsTrue(InputLock.Locked);

            // Bảng câu hỏi hiện kết quả 1,25 s (FieldQuizSession.CloseResult) rồi mới tới 0,5 s chuyển tiếp
            yield return new WaitForSecondsRealtime(1.25f + 0.7f);
            Assert.IsFalse(WorldFreeze.IsFrozen);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(InputLock.Locked, "Mở điều khiển cùng lúc chiến trường chạy lại");
            Assert.IsFalse(FieldQuizSession.Instance.IsOpen);
            Assert.IsNotNull(Object.FindAnyObjectByType<LootPile>(), "Băng đạn đầy: đạn thưởng nằm lại tại hòm (BR-36)");
        }

        [UnityTest]
        public IEnumerator KillingAllTargets_WinsOnce()
        {
            var mission = MissionController.Current;
            var objective = (DestroyTargetsObjective)mission.Objectives[0];
            yield return null;
            foreach (var t in objective.targets) t.TakeDamage(1000f, DamageType.Bullet, Vector3.zero);
            yield return null;
            yield return null;
            Assert.AreEqual(MissionOutcome.Won, mission.Attempt.Outcome);
            Assert.AreEqual(5, mission.Attempt.Kills);

            mission.Player.TakeDamage(1000f, DamageType.Bullet, Vector3.zero);
            yield return null;
            Assert.AreEqual(MissionOutcome.Won, mission.Attempt.Outcome, "Kết quả chỉ chốt một lần");
        }

        [UnityTest]
        public IEnumerator PlayerDeath_LosesMission()
        {
            var mission = MissionController.Current;
            mission.Player.TakeDamage(1000f, DamageType.Bullet, Vector3.zero);
            yield return null;
            yield return null;
            Assert.AreEqual(MissionOutcome.Lost, mission.Attempt.Outcome);
        }
    }
}
