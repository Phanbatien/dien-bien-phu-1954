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
            Click("BeginMission"); yield return new WaitForSecondsRealtime(4f);
            yield return Shot("ui_8_hud");
            CampaignUI.Current.OpenSettings(); yield return null;
            yield return Shot("ui_9_cai_dat");
        }

        IEnumerator Shot(string name)
        {
            yield return null;
            // 1. Thế giới 3D (mọi camera theo thứ tự độ sâu)
            var rt = new RenderTexture(1600, 900, 24);
            foreach (var cam in Camera.allCameras.Where(c => c.targetTexture == null).OrderBy(c => c.depth))
            {
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            }
            var world = Read(rt);
            // 2. Giao diện: mọi canvas phủ màn hình vẽ chung một camera nền trong suốt, rồi ghép lên ảnh thế giới
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.isActiveAndEnabled).OrderBy(c => c.sortingOrder).ToArray();
            var uiRt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            var uiCam = new GameObject("UiShotCam").AddComponent<Camera>();
            uiCam.clearFlags = CameraClearFlags.SolidColor; uiCam.backgroundColor = new Color(0, 0, 0, 0);
            uiCam.cullingMask = 1 << 5; uiCam.targetTexture = uiRt; uiCam.orthographic = true;
            var layers = canvases.SelectMany(c => c.GetComponentsInChildren<Transform>(true)).Distinct().ToDictionary(x => x, x => x.gameObject.layer);
            foreach (var x in layers.Keys) x.gameObject.layer = 5;
            for (int i = 0; i < canvases.Length; i++)
            { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = uiCam; canvases[i].planeDistance = 10 - i; }
            Canvas.ForceUpdateCanvases();
            uiCam.Render();
            foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
            foreach (var kv in layers) if (kv.Key) kv.Key.gameObject.layer = kv.Value;
            Object.Destroy(uiCam.gameObject);
            var ui = Read(uiRt);
            var a = world.GetPixels(); var b = ui.GetPixels();
            for (int i = 0; i < a.Length; i++) a[i] = Color.Lerp(a[i], new Color(b[i].r, b[i].g, b[i].b, 1), b[i].a);
            world.SetPixels(a); world.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), world.EncodeToPNG());
            rt.Release(); uiRt.Release();
        }

        static Texture2D Read(RenderTexture rt)
        {
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = null;
            return tex;
        }
    }
}
