using System;
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
    /// Chạy scene thật cho T17, T22, T31.
    public class NewSystemsPlayTests
    {
        /// Giao diện giả trả lời ngay một đáp án (đúng hoặc sai).
        class AutoAnswerView : IQuizView
        {
            public bool answerCorrectly = true;
            public Question shown;
            public void Show(Question q, float limit, Action<string> onAnswer)
            {
                shown = q;
                onAnswer(answerCorrectly ? q.correct : q.options.First(o => o.id != q.correct).id);
            }
            public void SetRemaining(float s) { }
            public void Hide() { }
        }

        IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Phone_Correct_DestroysTarget_AfterWorldResumes()
        {
            yield return Load("Sandbox");
            var view = new AutoAnswerView { answerCorrectly = true };
            FieldQuizSession.Instance.View = view;
            var phone = UnityEngine.Object.FindAnyObjectByType<FieldPhone>();
            var interactor = UnityEngine.Object.FindAnyObjectByType<Interactor>();
            Assert.IsTrue(phone.CanInteract(interactor));

            phone.Interact(interactor);
            Assert.AreEqual(3, view.shown.level, "Điện thoại dùng câu cấp 3 (BR-38)");
            Assert.IsFalse(phone.target.IsDead, "Pháo chưa tới khi thế giới còn đứng yên");
            yield return new WaitForSecondsRealtime(0.6f); // 0,5 s chuyển tiếp
            yield return new WaitForSeconds(1.7f);         // đạn pháo bay
            Assert.IsTrue(phone.target.IsDead);
            Assert.IsFalse(phone.CanInteract(interactor), "Mỗi điện thoại dùng một lần");
        }

        [UnityTest]
        public IEnumerator Phone_Wrong_Misses()
        {
            yield return Load("Sandbox");
            FieldQuizSession.Instance.View = new AutoAnswerView { answerCorrectly = false };
            var phone = UnityEngine.Object.FindAnyObjectByType<FieldPhone>();
            phone.Interact(UnityEngine.Object.FindAnyObjectByType<Interactor>());
            yield return new WaitForSecondsRealtime(0.6f);
            yield return new WaitForSeconds(1.7f);
            Assert.IsFalse(phone.target.IsDead, "Sai: pháo trượt, không gây sát thương");
        }

        [UnityTest]
        public IEnumerator Mission_SpawnsCratesFromSeed_AndQuestionsNeverRepeat()
        {
            yield return Load("M1_HimLam");
            var spawner = UnityEngine.Object.FindAnyObjectByType<CrateSpawner>();
            Assert.That(spawner.Spawned.Count, Is.InRange(5, 7));
            Assert.That(spawner.points.Count, Is.InRange(10, 15));

            var view = new AutoAnswerView();
            FieldQuizSession.Instance.View = view;
            var interactor = UnityEngine.Object.FindAnyObjectByType<Interactor>();
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var crate in spawner.Spawned)
            {
                if (!crate.CanInteract(interactor)) continue;
                crate.Interact(interactor);
                if (crate.Used) Assert.IsTrue(seen.Add(view.shown.id), "Không lặp câu trong lượt (BR-31)");
                yield return new WaitForSecondsRealtime(0.6f);
            }
            Assert.IsFalse(WorldFreeze.IsFrozen);
        }

        [UnityTest]
        public IEnumerator EnemyDrops_RespectMissionLimit()
        {
            yield return Load("M1_HimLam");
            var mission = MissionController.Current;
            int drops = 0;
            for (int i = 0; i < 500; i++) if (mission.TryRollDrop()) drops++;
            Assert.AreEqual(mission.MaxEnemyDrops, drops, "Đạt giới hạn súng rơi thì không sinh thêm (BR-14)");
        }
    }
}
