using System.Collections;
using System.IO;
using System.Linq;
using DBP.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Quay khung hình cho trailer (T73). Không chạy cùng bộ test thường: chỉ chạy khi đặt TRAILER_DIR
    ///   -runTests -testPlatform PlayMode -testFilter TrailerCaptureTests
    /// rồi ghép bằng ffmpeg (xem docs/demo.md).
    public class TrailerCaptureTests
    {
        const int Fps = 24;
        static readonly (string scene, float seconds, float height, float radius)[] Shots =
        {
            ("M1_HimLam", 9f, 2.2f, 7f), ("M2_GiuDocLap", 9f, 4f, 12f), ("M3_DoiED", 8f, 2.5f, 8f),
            ("M4_LongHao", 8f, 1.8f, 6f), ("M5_ChieuMungBay", 10f, 5f, 14f),
        };

        [UnityTest] public IEnumerator CaptureTrailerFrames()
        {
            var dir = System.Environment.GetEnvironmentVariable("TRAILER_DIR");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("Đặt TRAILER_DIR để quay trailer.");
            Directory.CreateDirectory(dir);
            int frame = 0;
            var rt = new RenderTexture(1280, 720, 24);
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foreach (var shot in Shots)
            {
                yield return SceneManager.LoadSceneAsync(shot.scene);
                yield return null; yield return null;
                var begin = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "BeginMission");
                if (begin) begin.onClick.Invoke();
                Time.timeScale = 1f;
                foreach (var h in Resources.FindObjectsOfTypeAll<Health>())
                    if (h.gameObject.scene.IsValid() && h.Kind != TargetKind.Player && !h.gameObject.activeInHierarchy)
                        for (var t = h.transform; t; t = t.parent) t.gameObject.SetActive(true);
                // Người chơi không chết giữa cảnh quay
                var player = GameObject.FindGameObjectWithTag("Player");
                var playerHealth = player.GetComponent<Health>();
                yield return new WaitForSeconds(2f); // để lính chạy, súng nổ

                var enemies = Object.FindObjectsByType<Health>(FindObjectsSortMode.None).Where(h => h.Kind != TargetKind.Player).ToArray();
                var focus = enemies.Length > 0 ? enemies.Aggregate(Vector3.zero, (s, e) => s + e.transform.position) / enemies.Length : player.transform.position + player.transform.forward * 20f;
                var cam = Camera.main;
                var arms = Object.FindAnyObjectByType<DBP.Player.FirstPersonArms>();
                if (arms) arms.gameObject.SetActive(false); // camera bay tự do, không mang tay theo
                cam.transform.SetParent(null, true);
                var start = player.transform.position;
                var toFocus = focus - start; toFocus.y = 0;
                float baseAngle = Mathf.Atan2(-toFocus.z, -toFocus.x);

                Time.captureFramerate = Fps;
                int count = Mathf.RoundToInt(shot.seconds * Fps);
                for (int i = 0; i < count; i++)
                {
                    if (playerHealth && playerHealth.Current < 50f) playerHealth.Heal(100f);
                    float t = i / (float)count;
                    // Lướt chậm từ sau lưng người chơi, vòng nửa cung về phía trận địa
                    float a = baseAngle + Mathf.Lerp(-0.35f, 0.35f, t);
                    var pos = Vector3.Lerp(start, focus, 0.15f + 0.2f * t) + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * shot.radius;
                    pos.y = Mathf.Max(pos.y, (Terrain.activeTerrain ? Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y : start.y)) + shot.height;
                    cam.transform.position = pos;
                    cam.transform.rotation = Quaternion.LookRotation(focus + Vector3.up * 1.2f - pos);
                    yield return null;
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(dir, $"f{frame++:D5}.jpg"), tex.EncodeToJPG(88));
                }
                Time.captureFramerate = 0;
            }
            Object.Destroy(tex); rt.Release();
            Assert.Greater(frame, 0);
        }
    }
}
