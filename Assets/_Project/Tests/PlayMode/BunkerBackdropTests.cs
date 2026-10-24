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
    /// Hầm chữ A hiện sau màn chuẩn bị và được gỡ khi rời đi. Đặt SHOT_DIR để chụp ảnh kiểm tra.
    public class BunkerBackdropTests
    {
        IDisposable scope;
        string folder;

        static T Find<T>(string name) where T : Component => Object.FindObjectsByType<T>(FindObjectsSortMode.None).First(c => c.name == name);
        static void Click(string name) { var b = Find<Button>(name); Assert.IsTrue(b.interactable, name); b.onClick.Invoke(); }
        static IEnumerator Until(Func<bool> done)
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (!done() && Time.realtimeSinceStartup < deadline) yield return null;
        }

        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "bunker-" + Guid.NewGuid().ToString("N"));
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

        [UnityTest] public IEnumerator Preparation_ShowsBunker_AndLeavingUnloadsIt()
        {
            Click("NewProfile"); Find<InputField>("DisplayName").text = "Hầm"; Click("ConfirmCreate");
            Click("Mission1");
            yield return Until(() => BunkerBackdrop.Loaded);
            Assert.IsTrue(BunkerBackdrop.Loaded, "Hầm chữ A phải được nạp khi vào màn chuẩn bị.");
            Assert.IsNotNull(Object.FindAnyObjectByType<LampFlicker>(), "Hầm phải có đèn dầu.");

            var dir = Environment.GetEnvironmentVariable("SHOT_DIR");
            if (!string.IsNullOrEmpty(dir))
            {
                // Ảnh camera hầm (giao diện phủ lên trên không nằm trong ảnh này)
                var cam = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c => c.name == "CameraPoint");
                var rt = new RenderTexture(1280, 720, 24);
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
                RenderTexture.active = null; cam.targetTexture = null; rt.Release();
                File.WriteAllBytes(Path.Combine(dir, "bunker_interior.png"), tex.EncodeToPNG());
                Object.Destroy(tex);
            }

            Click("BackToMenu");
            yield return Until(() => !BunkerBackdrop.Loaded);
            Assert.IsFalse(BunkerBackdrop.Loaded, "Rời màn chuẩn bị thì gỡ hầm.");
        }
    }
}
