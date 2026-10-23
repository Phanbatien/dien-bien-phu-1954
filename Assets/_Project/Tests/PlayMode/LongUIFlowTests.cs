using System;
using System.Collections;
using System.IO;
using System.Linq;
using DBP.Core;
using DBP.Missions;
using DBP.Player;
using DBP.Quiz;
using DBP.UI;
using DBP.Upgrades;
using DBP.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace DBP.Tests
{
    public class LongUIFlowTests
    {
        IDisposable scope;
        string folder;
        float originalVolume;
        static T Find<T>(string name) where T : Component => Object.FindObjectsByType<T>(FindObjectsSortMode.None).First(c => c.name == name);
        static void Click(string name) { var b = Find<Button>(name); Assert.IsTrue(b.interactable, name); b.onClick.Invoke(); }
        static IEnumerator Frames()
        {
            yield return null; yield return null; yield return null;
            float deadline = Time.realtimeSinceStartup + 10;
            while (SceneTransition.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(SceneTransition.IsTransitioning, "Scene transition must finish.");
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "long-ui-" + Guid.NewGuid().ToString("N"));
            originalVolume = AudioListener.volume;
            scope = ProfileService.UseTemporaryStore(Path.Combine(folder, "profiles.json"));
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return Frames();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox"); yield return Frames();
            scope.Dispose(); AudioListener.volume = originalVolume;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Time.timeScale = 1; InputLock.ResetAll();
        }
        static void Create(string name)
        {
            Click("NewProfile"); Find<InputField>("DisplayName").text = name;
            Find<InputField>("ClassName").text = "SE"; Click("ConfirmCreate");
        }
        [UnityTest] public IEnumerator Menu_CreatesDistinctProfiles_ConsentVolumeAndMissionLocks()
        {
            Assert.IsFalse(Find<Button>("Mission1").interactable);
            Create("Long"); var first = ProfileService.Data.id;
            Assert.IsTrue(Find<Button>("Mission1").interactable); Assert.IsFalse(Find<Button>("Mission2").interactable);
            Create("Long"); Assert.AreNotEqual(first, ProfileService.Data.id); Assert.AreEqual(2, ProfileService.Repository.Profiles.Count);
            Find<Toggle>("Consent").isOn = true; Assert.IsNotEmpty(ProfileService.Data.consentAt);
            Find<Slider>("VolumeSlider").value = .3f; Assert.AreEqual(.3f, AudioListener.volume, .001f);
            Click("Profile_" + first); Assert.IsEmpty(ProfileService.Data.consentAt); Assert.AreEqual(.8f, AudioListener.volume, .001f);
            CampaignUI.Current.SelectMission(4); Assert.AreEqual(CampaignScreen.Menu, CampaignUI.Current.ScreenState);
            yield return Frames();
        }
        [UnityTest] public IEnumerator Menu_MouseInputAndEscapePause_WorkThroughInputSystem()
        {
            Create("Mouse test");
            var settings = InputSystem.settings;
            var oldBackground = settings.backgroundBehavior;
            var oldEditor = settings.editorInputBehaviorInPlayMode;
            var mouse = InputSystem.AddDevice<Mouse>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                // Menu creation rebuilds the Canvas; wait for its final screen layout before aiming.
                yield return Frames();
                Canvas.ForceUpdateCanvases();
                var rect = Find<Button>("Mission1").GetComponent<RectTransform>();
                var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
                yield return Frames();
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
                yield return Frames();
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
                yield return Frames();
                Assert.AreEqual(CampaignScreen.Preparation, CampaignUI.Current.ScreenState, "Mouse release must open preparation.");
                Click("Deploy"); yield return Frames(); Click("BeginMission"); yield return Frames();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); yield return Frames();
                Assert.AreEqual(CampaignScreen.Pause, CampaignUI.Current.ScreenState);
                Assert.IsTrue(InputLock.Locked); Assert.AreEqual(0f, Time.timeScale);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return Frames(); Click("Resume"); yield return Frames();
                Assert.AreEqual(CampaignScreen.Gameplay, CampaignUI.Current.ScreenState);
                Assert.IsFalse(InputLock.Locked);
            }
            finally
            {
                InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(keyboard);
                settings.backgroundBehavior = oldBackground; settings.editorInputBehaviorInPlayMode = oldEditor;
            }
        }
        [UnityTest] public IEnumerator Menu_LongNamesRemainStoredAndProfileListFits()
        {
            string name = new string('W', 40), className = new string('W', 40);
            var profile = ProfileService.Repository.Create(name, className, false);
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return Frames();
            Assert.AreEqual(name, ProfileService.Data.displayName);
            Assert.AreEqual(className, ProfileService.Data.className);
            var label = Find<Button>("Profile_" + profile.id).GetComponentInChildren<Text>();
            Assert.LessOrEqual(label.preferredHeight, label.rectTransform.rect.height + 1, "Profile label must fit its button.");
        }
        [UnityTest] public IEnumerator Shop_InsufficientFunds_DuplicateClick_AndDeploymentModifiers()
        {
            Create("Shop test"); Click("Mission1"); Assert.IsFalse(Find<Button>("Buy0").interactable);
            Click("BackToMenu"); ProfileService.Data.resolve = 1200; ProfileService.Save(); Click("Mission1");
            var buy = Find<Button>("Buy0"); buy.onClick.Invoke(); buy.onClick.Invoke();
            Assert.AreEqual(1100, ProfileService.Data.resolve); Assert.AreEqual(1, ProfileService.Data.upgrades.Count);
            Click("Buy0"); Click("Buy0"); Assert.AreEqual(600, ProfileService.Data.resolve);
            Assert.IsFalse(Find<Button>("Buy0").interactable);
            Click("Buy1"); Click("Deploy"); yield return Frames();
            Assert.AreEqual("M1_HimLam", SceneManager.GetActiveScene().name);
            var weapons = Object.FindAnyObjectByType<WeaponController>();
            Assert.AreEqual(130, weapons.Inventory.ReservePercent); Assert.AreEqual(.9f, weapons.ReloadMultiplier, .001f);
            Assert.AreEqual(CampaignScreen.Tutorial, CampaignUI.Current.ScreenState); Assert.IsTrue(InputLock.Locked);
            var before = ProfileService.Data.resolve; CampaignUI.Current.Purchase("U-STAMINA-1"); Assert.AreEqual(before, ProfileService.Data.resolve);
        }
        [UnityTest] public IEnumerator Tutorial_HudTracksDamageAmmoStamina_ResultsAndFreshReplay()
        {
            Create("Mission test"); Click("Mission1"); Click("Deploy"); yield return Frames();
            Assert.AreEqual(0, Time.timeScale); Click("BeginMission"); yield return Frames();
            Assert.IsFalse(InputLock.Locked); Assert.AreEqual(1, Time.timeScale);
            var mission = MissionController.Current; var attemptId = mission.Attempt.AttemptId;
            mission.Player.TakeDamage(25, DamageType.Bullet, Vector3.zero);
            var weapons = Object.FindAnyObjectByType<WeaponController>(); weapons.Inventory.Current.TryConsume();
            var player = Object.FindAnyObjectByType<FirstPersonController>(); player.Stamina.Tick(1f, true);
            mission.Attempt.Answers.Add(new QuizResult { level = 3, remaining = 6, timeLimit = 15, outcome = QuizOutcome.Correct });
            yield return Frames();
            StringAssert.Contains("75", Find<Text>("HealthText").text);
            StringAssert.Contains(weapons.Inventory.Current.InMag + " /", Find<Text>("AmmoText").text);
            StringAssert.Contains("360", Find<Text>("ScoreText").text);
            StringAssert.Contains("Còn 4 mục tiêu", Find<Text>("ObjectiveText").text);
            Assert.Less(Find<RectTransform>("StaminaMeter").GetChild(0).GetComponent<RectTransform>().sizeDelta.x, 490);
            mission.Player.TakeDamage(999, DamageType.Bullet, Vector3.zero); yield return Frames();
            Assert.AreEqual(CampaignScreen.Results, CampaignUI.Current.ScreenState);
            StringAssert.Contains("360", Find<Text>("KnowledgeResult").text); Assert.IsTrue(InputLock.Locked);
            Click("Replay"); yield return Frames(); Assert.AreEqual(CampaignScreen.Preparation, CampaignUI.Current.ScreenState);
            Click("Deploy"); yield return Frames(); Assert.AreNotEqual(attemptId, MissionController.Current.Attempt.AttemptId);
            Assert.IsEmpty(MissionController.Current.Attempt.Answers);
        }
        [UnityTest] public IEnumerator WinningMission_UnlocksNextOnlyForActiveProfile()
        {
            Create("Winner"); string winner = ProfileService.Data.id; Create("Winner"); string other = ProfileService.Data.id;
            Click("Profile_" + winner); Click("Mission1"); Click("Deploy"); yield return Frames(); Click("BeginMission");
            var mission = MissionController.Current;
            // Exercise the real controller's completion path without requiring a human to traverse the greybox.
            mission.Setup("M1", "Him Lam", "M2", "M2_GiuDocLap", mission.Player, new System.Collections.Generic.List<Objective>(), true);
            yield return Frames(); Assert.AreEqual(MissionOutcome.Won, mission.Attempt.Outcome);
            Assert.IsTrue(ProfileService.IsUnlocked("M2")); Click("ResultMenu"); yield return Frames();
            Assert.IsTrue(Find<Button>("Mission2").interactable); Click("Profile_" + other);
            Assert.IsFalse(Find<Button>("Mission2").interactable);
        }
        [UnityTest] public IEnumerator WinningMission_SaveFailureStillShowsResults_AndRetrySavesOnce()
        {
            Create("Save failure"); Click("Mission1"); Click("Deploy"); yield return Frames(); Click("BeginMission");
            var mission = MissionController.Current;
            int wins = 0; mission.onWon.AddListener(() => wins++);
            using (var locked = new FileStream(Path.Combine(folder, "profiles.json.tmp"), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                mission.Setup("M1", "Him Lam", "M2", "M2_GiuDocLap", mission.Player, new System.Collections.Generic.List<Objective>(), true);
                yield return Frames();
                Assert.AreEqual(MissionOutcome.Won, mission.Attempt.Outcome);
                Assert.AreEqual(CampaignScreen.Results, CampaignUI.Current.ScreenState, "Save failure must not strand the player in combat.");
                Assert.AreEqual(1, wins);
                Assert.IsFalse(ProfileService.IsUnlocked("M2"));
                Assert.IsTrue(mission.Attempt.SaveFailed);
                StringAssert.Contains("Chưa lưu", Find<Text>("ResultNote").text);
                Assert.IsTrue(Find<Button>("RetrySave").interactable);
                Assert.IsTrue(Find<Button>("ResultMenu").interactable);
            }
            Click("RetrySave");
            Assert.IsTrue(ProfileService.IsUnlocked("M2")); Assert.AreEqual(1, wins);
            Assert.IsFalse(mission.Attempt.SaveFailed);
            Assert.Contains("M2", new ProfileRepository(Path.Combine(folder, "profiles.json")).Active.unlockedMissions);
            Click("ResultMenu"); yield return Frames(); Assert.AreEqual(CampaignScreen.Menu, CampaignUI.Current.ScreenState);
        }
    }
}
