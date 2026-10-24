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
    /// Chụp ảnh các màn hình giao diện (menu, hồ sơ, hầm chuẩn bị, cốt truyện, credits, HUD) để duyệt bằng mắt.
    /// Chỉ chạy khi đặt SHOT_DIR.
    public class UiShotTests
    {
        IDisposable scope;
        string folder, dir;

        static T Find<T>(string name) where T : Component => Object.FindObjectsByType<T>(FindObjectsSortMode.None).First(c => c.name == name);
        static void Click(string name) => Find<Button>(name).onClick.Invoke();

        [UnitySetUp] public IEnumerator Setup()
        {
            dir = Environment.GetEnvironmentVariable("SHOT_DIR");
            folder = Path.Combine(Application.temporaryCachePath, "uishot-" + Guid.NewGuid().ToString("N"));
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

        [UnityTest] public IEnumerator CaptureScreens()
        {
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("Đặt SHOT_DIR để chụp giao diện.");
            yield return Shot("ui_1_menu");
            Click("NewProfile"); yield return null;
            Find<InputField>("DisplayName").text = "Nguyễn Văn An"; Find<InputField>("ClassName").text = "SE1801";
            yield return Shot("ui_2_tao_ho_so");
            Click("ConfirmCreate"); yield return null;
            yield return Shot("ui_3_menu_co_ho_so");
            Click("Mission1");
            float t = Time.realtimeSinceStartup + 5; while (!BunkerBackdrop.Loaded && Time.realtimeSinceStartup < t) yield return null;
            yield return null; yield return null;
            yield return Shot("ui_4_ham_chuan_bi");
            Click("BackToMenu"); yield return null;
            Click("PlayPrologue"); yield return null;
            yield return Shot("ui_5_mo_dau");
            Click("SkipStory"); yield return null;
            Click("OpenCredits"); yield return null;
            yield return Shot("ui_6_credits");
            CampaignUI.Current.SelectMission(0); yield return null; Click("BackToMenu"); yield return null;
            yield return SceneManager.LoadSceneAsync("M1_HimLam"); yield return null; yield return null; yield return null;
            yield return Shot("ui_7_huong_dan");
            Click("BeginMission"); yield return new WaitForSecondsRealtime(1f);
            yield return Shot("ui_8_hud");
        }

        IEnumerator Shot(string name)
        {
            yield return null;
            var rt = new RenderTexture(1600, 900, 24);
            foreach (var cam in Camera.allCameras.OrderBy(c => c.depth))
            {
                var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old;
            }
            var canvas = CampaignUI.Current ? CampaignUI.Current.GetComponentInChildren<Canvas>() : null;
            if (canvas)
            {
                var layers = canvas.GetComponentsInChildren<Transform>(true).ToDictionary(x => x, x => x.gameObject.layer);
                foreach (var x in layers.Keys) x.gameObject.layer = 5;
                var uiCam = new GameObject("UiShotCam").AddComponent<Camera>();
                uiCam.clearFlags = Camera.allCameras.Length > 0 ? CameraClearFlags.Depth : CameraClearFlags.SolidColor;
                uiCam.backgroundColor = Color.black; uiCam.cullingMask = 1 << 5; uiCam.targetTexture = rt; uiCam.orthographic = true;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = uiCam; canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                uiCam.Render();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                foreach (var kv in layers) if (kv.Key) kv.Key.gameObject.layer = kv.Value;
                Object.Destroy(uiCam.gameObject);
            }
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            RenderTexture.active = null; rt.Release();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        }
    }
}
