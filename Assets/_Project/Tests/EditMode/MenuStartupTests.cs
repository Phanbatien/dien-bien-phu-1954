using System.Collections;
using DBP.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    public class MenuStartupTests
    {
        [UnityTest]
        public IEnumerator PressPlayFromMainMenu_CreatesMenuWithoutReloadingScene()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
            yield return new EnterPlayMode();
            yield return null;
            yield return null;
            bool exists = CampaignUI.Current != null;
            bool menu = exists && CampaignUI.Current.ScreenState == CampaignScreen.Menu;
            yield return new ExitPlayMode();
            Assert.IsTrue(exists, "Pressing Play directly from MainMenu must create its UI.");
            Assert.IsTrue(menu, "The initial screen must be the profile menu.");
        }
    }
}
