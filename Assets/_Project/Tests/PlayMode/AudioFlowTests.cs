using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DBP.Core;
using DBP.Upgrades;
using DBP.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DBP.Tests
{
    public class AudioFlowTests
    {
        IDisposable scope;
        string folder;
        float volume;
        static AudioSource[] Voices => Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "audio-" + Guid.NewGuid().ToString("N"));
            scope = ProfileService.UseTemporaryStore(Path.Combine(folder, "profiles.json"));
            volume = AudioListener.volume;
            Time.timeScale = 1; AudioListener.pause = false; InputLock.ResetAll();
            yield return SceneManager.LoadSceneAsync("Sandbox");
            yield return null; yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; AudioListener.pause = false; InputLock.ResetAll();
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            scope.Dispose(); AudioListener.volume = volume;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [UnityTest] public IEnumerator SwitchingWeapons_UsesApprovedAForMat49_AndBForType50()
        {
            var controller = Object.FindAnyObjectByType<WeaponController>();
            Assert.NotNull(controller);
            var mat = new Weapon(WeaponCatalog.Get("W-MAT49"));
            var type = new Weapon(WeaponCatalog.Get("W-TYPE50"));
            Call(controller, "Fire", mat);
            Assert.IsTrue(Voices.Any(s => s.clip && s.clip.name == "mat49_A"), "MAT-49 must play approved candidate A");
            Call(controller, "Fire", type);
            Assert.IsTrue(Voices.Any(s => s.clip && s.clip.name == "type50_B"), "Kiểu 50 must play approved candidate B after switching");
            Assert.AreEqual(mat.Def.stats.mag - 1, mat.InMag);
            Assert.AreEqual(type.Def.stats.mag - 1, type.InMag);
            yield return null;
        }

        [UnityTest] public IEnumerator EveryMission_LoadsItsApprovedMusic_AndMenuStopsMissionMusic()
        {
            var scenes = new[] { "M1_HimLam", "M2_GiuDocLap", "M3_DoiED", "M4_LongHao", "M5_ChieuMungBay" };
            for (int i = 0; i < scenes.Length; i++)
            {
                yield return SceneManager.LoadSceneAsync(scenes[i]); yield return null; yield return null;
                var music = Voices.SingleOrDefault(s => s.name == "MissionMusic");
                Assert.NotNull(music, scenes[i]);
                Assert.NotNull(music.clip);
                Assert.AreEqual($"mission_{i + 1:00}", music.clip.name);
                Assert.IsTrue(music.loop);
                Assert.IsFalse(music.ignoreListenerPause);
                Assert.AreEqual(AudioDataLoadState.Loaded, music.clip.loadState);
                Assert.AreEqual(1, Voices.Count(s => s.name == "MissionMusic"), "No duplicate music across scene changes");
            }
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            Assert.IsFalse(Voices.Any(s => s.name == "MissionMusic" && s.clip));
        }

        [UnityTest] public IEnumerator ImportedSounds_AreReadable_AndAirplaneIsAudioOnly()
        {
            foreach (var name in new[] { "mat49_A", "type50_B", "mosin_shot_01", "step_mud02", "airplane_loop", "tank_engine_loop", "cannon_generic", "explosion_01", "ui_click" })
            {
                var clip = Resources.Load<AudioClip>("Audio/" + name);
                Assert.NotNull(clip, name);
                Assert.Greater(clip.length, .05f);
                Assert.IsTrue(clip.LoadAudioData(), name);
            }
            Assert.IsEmpty(Resources.LoadAll<GameObject>("Audio"), "Airplane audio must not introduce an aircraft model");
            yield return null;
        }

        [UnityTest] public IEnumerator PauseAndQuizFreeze_StopWorldAudio_ButAllowUI_AndResume()
        {
            var owner = new GameObject("AudioLoopOwner");
            var loop = CampaignAudio.AttachLoop("TestEngine", "tank_engine_loop", owner.transform);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.IsTrue(loop.isPlaying, "Loop must actually play in the editor");
            Time.timeScale = 0;
            yield return null; yield return null;
            Assert.IsTrue(CampaignAudio.Current.WorldPaused);
            int pausedAt = loop.timeSamples;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(pausedAt, loop.timeSamples);
            var click = CampaignAudio.Interface("ui_click");
            Assert.NotNull(click);
            Assert.IsTrue(click.ignoreListenerPause);
            Assert.IsFalse(click.ignoreListenerVolume, "UI must obey the profile's master volume");
            Assert.IsNull(CampaignAudio.Effect("mat49_A", Vector3.zero));
            Time.timeScale = 1;
            yield return null; yield return null;
            Assert.IsFalse(CampaignAudio.Current.WorldPaused);
            Assert.IsTrue(loop.isPlaying);
            var freeze = Object.FindAnyObjectByType<WorldFreeze>();
            freeze.Freeze();
            Assert.IsTrue(AudioListener.pause);
            var reward = CampaignAudio.Interface("ui_reward");
            Assert.IsTrue(reward.ignoreListenerPause);
            Assert.IsNull(CampaignAudio.Effect("mat49_A", Vector3.zero));
            freeze.Resume();
            yield return new WaitForSecondsRealtime(.65f);
            Assert.IsFalse(WorldFreeze.IsFrozen);
            Assert.IsFalse(AudioListener.pause);
            Assert.IsTrue(loop.isPlaying);
            Object.Destroy(owner);
        }

        [UnityTest] public IEnumerator ManyShots_KeepVoiceCountBounded_AndUnknownWeaponsDoNotBorrowApprovedSounds()
        {
            for (int i = 0; i < 100; i++) CampaignAudio.Effect("mat49_A", Vector3.zero);
            Assert.AreEqual(CampaignAudio.VoiceLimit, Voices.Count(s => s.name.StartsWith("WorldVoice", StringComparison.Ordinal)));
            Assert.IsNull(AudioCatalog.Weapon("W-UNKNOWN"));
            Assert.AreEqual("mosin_shot_01", AudioCatalog.Weapon("W-MOSIN-SCOPE", 0).name);
            Assert.AreEqual("step_wood02", AudioCatalog.Step("WoodPlank", 1).name);
            Assert.AreEqual("step_stone01", AudioCatalog.Step("Concrete", 0).name);
            Assert.AreEqual("step_mud02", AudioCatalog.Step("Ground", 0).name);
            yield return null;
        }

        [UnityTest] public IEnumerator GrenadeExplosion_PlaysAtBlastPosition_AndSurvivesGrenadeDestruction()
        {
            var go = new GameObject("AudioGrenade");
            go.transform.position = new Vector3(500, 50, 500);
            var grenade = go.AddComponent<DBP.AI.Grenade>();
            grenade.Explode();
            var source = Voices.FirstOrDefault(s => s.clip && s.clip.name == "explosion_02");
            Assert.NotNull(source);
            Assert.AreEqual(go.transform.position, source.transform.position);
            Assert.AreEqual(1f, source.spatialBlend);
            yield return null;
            Assert.IsTrue(go == null);
            Assert.IsTrue(source != null, "Effect must live independently of the destroyed grenade");
        }
    }
}
