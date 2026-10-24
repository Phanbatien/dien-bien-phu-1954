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
    /// Chạy thật từng màn 3 giây với mọi đợt địch: AI, lô cốt, xe tăng, hoạt ảnh không được ném lỗi.
    /// Đặt biến môi trường SHOT_DIR để lưu ảnh chụp kiểm tra bằng mắt.
    public class MissionRuntimeSmokeTests
    {
        static readonly string[] Scenes = { "M1_HimLam", "M2_GiuDocLap", "M3_DoiED", "M4_LongHao", "M5_ChieuMungBay" };

        [UnityTest]
        public IEnumerator Mission_RunsWithAllEnemiesActive_NoErrors([ValueSource(nameof(Scenes))] string scene)
        {
            var dir = System.Environment.GetEnvironmentVariable("SHOT_DIR");
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null; yield return null;
            // Bỏ qua màn hướng dẫn (đặt timeScale = 0) để AI chạy
            var begin = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "BeginMission");
            if (begin) begin.onClick.Invoke();
            Time.timeScale = 1f;
            // Mở hết các đợt phản kích để chụp được lính và xe tăng
            foreach (var h in Resources.FindObjectsOfTypeAll<Health>())
                if (h.gameObject.scene.IsValid() && h.Kind != TargetKind.Player && !h.gameObject.activeInHierarchy)
                    for (var t = h.transform; t; t = t.parent) t.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(3f);

            var enemies = Object.FindObjectsByType<Health>(FindObjectsSortMode.None).Where(h => h.Kind != TargetKind.Player).ToArray();
            Assert.IsNotEmpty(enemies, $"{scene}: không có địch");
            Assert.IsTrue(enemies.Where(e => e.Kind == TargetKind.Infantry).All(e => e.GetComponent<UnityEngine.AI.NavMeshAgent>() == null || e.GetComponent<UnityEngine.AI.NavMeshAgent>().isOnNavMesh || e.IsDead),
                $"{scene}: có lính không đứng trên NavMesh");
            if (string.IsNullOrEmpty(dir)) yield break;
            var camGo = new GameObject("ShotCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55;
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            int idx = 0;
            foreach (var group in enemies.GroupBy(e => e.Kind))
            {
                var target = group.First().transform;
                var look = target.position + Vector3.up * (group.Key == TargetKind.Infantry ? 0.2f : 0.8f);
                var toPlayer = GameObject.FindGameObjectWithTag("Player").transform.position - target.position;
                toPlayer.y = 0;
                var dist = group.Key == TargetKind.Infantry ? 4.5f : 9f;
                camGo.transform.position = look + toPlayer.normalized * dist + Vector3.up * 1.2f;
                camGo.transform.LookAt(look);
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"run_{scene}_{idx++}_{group.Key}.png"), tex.EncodeToPNG());
                RenderTexture.active = null;
            }
            Object.Destroy(camGo);
            // Toàn cảnh từ camera người chơi (có hậu kỳ, núi xa, khói): nhìn trước rồi quay ngược lại
            var main = Camera.main;
            if (!main) yield break;
            var start = main.transform.rotation;
            for (int i = 0; i < 2; i++)
            {
                main.transform.rotation = Quaternion.Euler(-4f, start.eulerAngles.y + i * 180f, 0f);
                main.targetTexture = rt;
                main.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"run_{scene}_wide{i}.png"), tex.EncodeToPNG());
                RenderTexture.active = null;
                main.targetTexture = null;
            }
            main.transform.rotation = start;
        }
    }
}
