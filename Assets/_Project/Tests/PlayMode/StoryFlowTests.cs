using System;
using System.Collections;
using System.IO;
using System.Linq;
using DBP.Core;
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
    /// Mở đầu 90 giây, nhật ký, kết thúc, Nguồn & Credits (T34, T61).
    public class StoryFlowTests
    {
        IDisposable scope;
        string folder;

        static T Find<T>(string name) where T : Component => Object.FindObjectsByType<T>(FindObjectsSortMode.None).First(c => c.name == name);
        static void Click(string name) { var b = Find<Button>(name); Assert.IsTrue(b.interactable, name); b.onClick.Invoke(); }

        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "story-" + Guid.NewGuid().ToString("N"));
            scope = ProfileService.UseTemporaryStore(Path.Combine(folder, "profiles.json"));
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null; yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox"); yield return null;
            scope.Dispose();
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Time.timeScale = 1; InputLock.ResetAll();
        }

        [Test] public void Prologue_Lasts90Seconds_AndEveryDeckHasText()
        {
            Assert.AreEqual(6, CampaignUI.Prologue.Length, "6 khung × 15 giây = 90 giây");
            foreach (var slide in CampaignUI.Prologue.Concat(CampaignUI.Diary).Concat(CampaignUI.Epilogue))
            {
                Assert.IsNotEmpty(slide.heading);
                Assert.IsNotEmpty(slide.body);
            }
        }

        [UnityTest] public IEnumerator Prologue_PlaysThroughAndReturnsToMenu()
        {
            Click("PlayPrologue");
            Assert.AreEqual(CampaignScreen.Story, CampaignUI.Current.ScreenState);
            for (int i = 0; i < CampaignUI.Prologue.Length; i++)
            {
                Assert.AreEqual(CampaignUI.Prologue[i].heading, Find<Text>("StoryHeading").text);
                Click("NextSlide"); yield return null;
            }
            Assert.AreEqual(CampaignScreen.Menu, CampaignUI.Current.ScreenState);
        }

        [UnityTest] public IEnumerator Credits_ListsSources()
        {
            Click("OpenCredits"); yield return null;
            Assert.AreEqual(CampaignScreen.Credits, CampaignUI.Current.ScreenState);
            var sources = Find<Text>("Sources").text;
            StringAssert.DoesNotContain("Không đọc được", sources);
            StringAssert.StartsWith("•", sources);
            Click("CreditsBack");
            Assert.AreEqual(CampaignScreen.Menu, CampaignUI.Current.ScreenState);
        }
    }
}
