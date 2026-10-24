using System;
using System.Collections;
using System.Reflection;
using DBP.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    public class SceneTransitionTests
    {
        Type Transition
        {
            get
            {
                var type = typeof(CampaignAudio).Assembly.GetType("DBP.Core.SceneTransition");
                Assert.NotNull(type, "T67 scene loads must fade."); return type;
            }
        }
        bool Load(string scene) => (bool)Transition.GetMethod("Load").Invoke(null, new object[] { scene });
        bool Busy => (bool)Transition.GetProperty("IsTransitioning").GetValue(null);
        float Alpha => (float)Transition.GetProperty("Alpha").GetValue(null);
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            Time.timeScale = 1; InputLock.ResetAll();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var type = typeof(CampaignAudio).Assembly.GetType("DBP.Core.SceneTransition");
            if (type != null)
                while ((bool)type.GetProperty("IsTransitioning").GetValue(null)) yield return null;
            Time.timeScale = 1; InputLock.ResetAll();
            yield return SceneManager.LoadSceneAsync("MainMenu");
        }
        [UnityTest] public IEnumerator PausedTransition_FadesAndLoadsOnce_ThenReleasesInput()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox");
            Time.timeScale = 0;
            Assert.IsTrue(Load("MainMenu"));
            Assert.IsFalse(Load("Sandbox"), "Repeated requests must not override the in-flight destination.");
            bool sawFade = false;
            float deadline = Time.realtimeSinceStartup + 8;
            while (Busy && Time.realtimeSinceStartup < deadline) { sawFade |= Alpha > .1f; yield return null; }
            Assert.IsFalse(Busy, "Paused transitions must finish.");
            Assert.IsTrue(sawFade, "Scene change must visibly fade.");
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            Assert.AreEqual(0, Alpha, .001f);
            Assert.IsFalse(InputLock.Locked, "Transition must release its own lock.");
        }
        [Test] public void InvalidScene_DoesNotStartTransitionOrAcquireInputLock()
        {
            Assert.IsFalse(Load("__missing_scene__"));
            Assert.IsFalse(Busy); Assert.AreEqual(0, Alpha); Assert.IsFalse(InputLock.Locked);
        }
        IEnumerator Finished()
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (Busy && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(Busy);
        }
        [UnityTest] public IEnumerator SandboxPausedRestart_DoesNotKeepOldPauseLock()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox"); yield return null;
            var debug = UnityEngine.Object.FindAnyObjectByType<DBP.DebugTools.DebugOverlay>();
            debug.GetType().GetMethod("SetPaused", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(debug, new object[] { true });
            Assert.IsTrue(InputLock.Locked);
            DBP.Missions.MissionController.Current.Restart(); yield return Finished();
            Assert.IsFalse(InputLock.Locked, "Old Sandbox pause lock must be removed during restart.");
        }
        [UnityTest] public IEnumerator SandboxResultRestart_DoesNotKeepOldResultLock()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox"); yield return null;
            DBP.Missions.MissionController.Current.Abandon();
            Assert.IsTrue(InputLock.Locked);
            DBP.Missions.MissionController.Current.Restart(); yield return Finished();
            Assert.IsFalse(InputLock.Locked, "Old Sandbox result lock must be removed during restart.");
        }
        [UnityTest] public IEnumerator LaterMissionTransition_RestoresGameplayCursor()
        {
            Assert.IsTrue(Load("M2_GiuDocLap")); yield return Finished(); yield return null;
            Assert.AreEqual(DBP.UI.CampaignScreen.Gameplay, DBP.UI.CampaignUI.Current.ScreenState);
            Assert.IsFalse(InputLock.Locked);
            // -batchmode không có cửa sổ nên Unity không khóa chuột thật; khi đó kiểm tra trạng thái game đã yêu cầu
            var lockState = Application.isBatchMode ? DBP.Player.FirstPersonController.RequestedLock : Cursor.lockState;
            Assert.AreEqual(CursorLockMode.Locked, lockState, "Gameplay cursor must lock after the transition releases input.");
        }
    }
}
