using System.Collections;
using System.IO;
using System.Linq;
using DBP.AI;
using DBP.Core;
using DBP.Missions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Quay trailer hào hùng (T73 bản 2): camera bám tốp đồng đội xung phong vào lính địch, máy bay ném bom,
    /// đêm pháo sáng, giáp lá cà trong hào, chiều tà Màn 5. Chỉ chạy khi đặt TRAILER_DIR.
    public class EpicTrailerTests
    {
        const int Fps = 24;

        struct Shot { public string scene; public float seconds, back, height; public bool airRaid; }

        static readonly Shot[] Shots =
        {
            new Shot { scene = "M1_HimLam", seconds = 7f, back = 6f, height = 2.2f },
            new Shot { scene = "M2_GiuDocLap", seconds = 12f, back = 12f, height = 7f, airRaid = true },
            new Shot { scene = "M2_GiuDocLap", seconds = 6f, back = 5f, height = 1.8f },
            new Shot { scene = "M3_DoiED", seconds = 6f, back = 9f, height = 5f },
            new Shot { scene = "M4_LongHao", seconds = 6f, back = 7f, height = 3.2f },
            new Shot { scene = "M5_ChieuMungBay", seconds = 8f, back = 13f, height = 8f },
        };

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.captureFramerate = 0; Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Sandbox");
            InputLock.ResetAll();
        }

        [UnityTest] public IEnumerator CaptureEpicTrailer()
        {
            var dir = System.Environment.GetEnvironmentVariable("TRAILER_DIR");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("Đặt TRAILER_DIR để quay trailer.");
            Directory.CreateDirectory(dir);
            int frame = 0;
            var rt = new RenderTexture(1280, 720, 24);
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foreach (var shot in Shots)
            {
                yield return SceneManager.LoadSceneAsync(shot.scene); yield return null; yield return null;
                var begin = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "BeginMission");
                if (begin) begin.onClick.Invoke();
                Time.timeScale = 1f;
                var player = GameObject.FindGameObjectWithTag("Player");
                player.GetComponent<Health>().Configure(TargetKind.Player, 100000f);
                foreach (var h in Resources.FindObjectsOfTypeAll<Health>())
                    if (h.gameObject.scene.IsValid() && h.Kind != TargetKind.Player && !h.gameObject.activeInHierarchy)
                        for (var t = h.transform; t; t = t.parent) t.gameObject.SetActive(true);
                yield return null;

                // Đưa hai phe vào thế giáp trận: người chơi và đồng đội cách lính địch gần nhất khoảng 25 m
                var enemy = SoldierAI.All.Where(s => s && s.isActiveAndEnabled).OrderBy(s => (s.transform.position - player.transform.position).sqrMagnitude).FirstOrDefault();
                if (enemy)
                {
                    var toward = (enemy.transform.position - player.transform.position); toward.y = 0; toward.Normalize();
                    var stand = enemy.transform.position - toward * 25f;
                    if (NavMesh.SamplePosition(stand, out var hit, 6f, NavMesh.AllAreas)) stand = hit.position;
                    var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
                    player.transform.position = stand + Vector3.up * 0.1f; player.transform.rotation = Quaternion.LookRotation(toward); cc.enabled = true;
                    foreach (var c in ComradeFollower.All)
                        if (NavMesh.SamplePosition(stand + Random.insideUnitSphere * 5f, out var h2, 4f, NavMesh.AllAreas)) c.GetComponent<NavMeshAgent>().Warp(h2.position);
                }
                var arms = Object.FindAnyObjectByType<DBP.Player.FirstPersonArms>();
                if (arms) arms.gameObject.SetActive(false); // camera bay tự do, không mang tay theo
                var cam = Camera.main; cam.transform.SetParent(null, true);
                cam.nearClipPlane = 0.3f; // không để mũ, vai đồng đội sát ống kính che hình
                if (shot.airRaid) AirRaid.Now();
                yield return new WaitForSeconds(1.5f); // để hai bên giáp nhau, súng nổ

                Time.captureFramerate = Fps;
                int count = Mathf.RoundToInt(shot.seconds * Fps);
                Vector3 camPos = cam.transform.position; Quaternion camRot = cam.transform.rotation;
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)count;
                    var allies = ComradeFollower.All.Where(c => c && !c.IsDead).ToList();
                    var center = allies.Count > 0 ? allies.Aggregate(Vector3.zero, (s, c) => s + c.transform.position) / allies.Count : player.transform.position;
                    var foes = SoldierAI.All.Where(s => s && !s.IsDead && s.isActiveAndEnabled).ToList();
                    var foe = foes.Count > 0 ? foes.OrderBy(s => (s.transform.position - center).sqrMagnitude).Take(4).Aggregate(Vector3.zero, (s, x) => s + x.transform.position) / Mathf.Min(4, foes.Count) : center + player.transform.forward * 20f;
                    var fwd = foe - center; fwd.y = 0; fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
                    var side = Vector3.Cross(Vector3.up, fwd);
                    // Camera lướt chéo từ sau lưng tốp xung kích, hạ dần độ cao và trượt ngang
                    var want = center - fwd * shot.back * (1.1f - 0.3f * t) + side * Mathf.Lerp(-3f, 3f, t) + Vector3.up * shot.height;
                    var look = Vector3.Lerp(center, foe, 0.55f) + Vector3.up * 1.2f;
                    if (shot.airRaid)
                    {
                        // Nhìn thẳng vào điểm bom rơi từ 45 m: máy bay lướt qua khung hình, bom nổ giữa hình
                        var target = AirRaid.LastTarget;
                        var away = center - target; away.y = 0; away = away.sqrMagnitude > 0.01f ? away.normalized : -fwd;
                        want = target + away * Mathf.Lerp(34f, 26f, t) + Vector3.up * Mathf.Lerp(8f, 5f, t);
                        look = target + Vector3.up * Mathf.Lerp(14f, 3f, Mathf.Clamp01(t * 1.6f));
                    }
                    camPos = i == 0 ? want : Vector3.Lerp(camPos, want, 0.08f);
                    camRot = i == 0 ? Quaternion.LookRotation(look - want) : Quaternion.Slerp(camRot, Quaternion.LookRotation(look - camPos), 0.1f);
                    cam.transform.SetPositionAndRotation(camPos, camRot);
                    yield return null;
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(dir, $"f{frame++:D5}.jpg"), tex.EncodeToJPG(90));
                }
                Time.captureFramerate = 0;
            }
            Object.Destroy(tex); rt.Release();
            Assert.Greater(frame, 0);
        }
    }
}
