using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DBP.AI;
using DBP.Core;
using DBP.Interaction;
using DBP.Missions;
using DBP.Player;
using DBP.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Quay trailer (T73 bản 3, 31/10) tự động từ game, 1080p 24 khung/giây: xung phong khỏi chiến hào, ngồi đại liên,
    /// bazooka bắn xe tăng, máy bay ném bom, đêm trên đồi, đánh trong hào, giữ hào đầu cầu, trùm, thử thách vô hạn.
    /// Chỉ chạy khi đặt TRAILER_DIR. Ghi kèm canh.txt (tên cảnh, khung đầu, số khung) để ghép bằng ffmpeg.
    public class EpicTrailerTests
    {
        const int Fps = 24, W = 1920, H = 1080;

        enum Mode { Follow, AirRaid, MountedGun, Bazooka, Boss, Crane }
        struct Shot { public string name, scene; public float seconds, back, height, preRoll; public Mode mode; public bool holdStart, inTrench; public Vector3 stand; }

        static readonly Shot[] Shots =
        {
            new Shot { name = "m1", scene = "M1_HimLam", seconds = 6.5f, back = 6f, height = 2.4f, preRoll = 1.5f },
            new Shot { name = "m2_daiLien", scene = "M2_GiuDocLap", seconds = 7f, mode = Mode.MountedGun, preRoll = 5f },
            new Shot { name = "m2_bazooka", scene = "M2_GiuDocLap", seconds = 7f, mode = Mode.Bazooka, preRoll = 3f },
            new Shot { name = "m2_maybay", scene = "M2_GiuDocLap", seconds = 9f, mode = Mode.AirRaid, preRoll = 1.5f },
            new Shot { name = "m3", scene = "M3_DoiED", seconds = 7f, back = 9f, height = 5f, preRoll = 3f },
            new Shot { name = "m4", scene = "M4_LongHao", seconds = 7f, back = 5f, height = 2.5f, preRoll = 2f, holdStart = true, inTrench = true, stand = new Vector3(0, 0, 5) },
            new Shot { name = "m5", scene = "M5_ChieuMungBay", seconds = 8f, back = 10f, height = 6f, preRoll = 6f, holdStart = true, stand = new Vector3(0, 0, 27) },
            new Shot { name = "m6_trum", scene = "M6_ThuThach", seconds = 6f, mode = Mode.Boss, preRoll = 2f },
            new Shot { name = "m6_voHan", scene = "M6_ThuThach", seconds = 10f, mode = Mode.Crane, preRoll = 5f },
        };

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.captureFramerate = 0; Time.timeScale = 1f;
            PlayerView.Set(false);
            yield return SceneManager.LoadSceneAsync("Sandbox");
            InputLock.ResetAll();
        }

        static void Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, args);

        static Health[] Foes(GameObject player) => Object.FindObjectsByType<Health>(FindObjectsSortMode.None)
            .Where(h => h.Kind == TargetKind.Infantry && !h.IsDead && h.gameObject != player).ToArray();

        [UnityTest, Timeout(3000000)] public IEnumerator CaptureEpicTrailer()
        {
            var dir = System.Environment.GetEnvironmentVariable("TRAILER_DIR");
            if (string.IsNullOrEmpty(dir)) Assert.Ignore("Đặt TRAILER_DIR để quay trailer.");
            Directory.CreateDirectory(dir);
            int frame = 0;
            var rt = new RenderTexture(W, H, 24);
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            var index = new System.Text.StringBuilder();
            foreach (var shot in Shots)
            {
                PlayerView.Set(false);
                yield return SceneManager.LoadSceneAsync(shot.scene); yield return null; yield return null;
                var begin = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "BeginMission");
                if (begin) begin.onClick.Invoke();
                Time.timeScale = 1f;
                var player = GameObject.FindGameObjectWithTag("Player");
                var body = player.GetComponent<FirstPersonController>();
                player.GetComponent<Health>().Configure(TargetKind.Player, 100000f);
                var endless = Object.FindAnyObjectByType<EndlessObjective>();
                // Bật mọi đợt địch ngay cho trận đông (Màn 6 tự sinh đợt, mẫu lính phải giữ ẩn)
                if (!endless)
                    foreach (var h in Resources.FindObjectsOfTypeAll<Health>())
                        if (h.gameObject.scene.IsValid() && h.Kind != TargetKind.Player && !h.gameObject.activeInHierarchy)
                            for (var t = h.transform; t; t = t.parent) t.gameObject.SetActive(true);
                yield return null; yield return null;

                // Đồng đội và người chơi vào vị trí
                bool freeCam = shot.mode != Mode.MountedGun && shot.mode != Mode.Bazooka;
                if (shot.holdStart || (shot.mode == Mode.Follow))
                {
                    var stand = shot.stand;
                    if (!shot.holdStart)
                    {
                        var near = Foes(player).OrderBy(h => (h.transform.position - player.transform.position).sqrMagnitude).FirstOrDefault();
                        if (near)
                        {
                            var toward = near.transform.position - player.transform.position; toward.y = 0; toward.Normalize();
                            stand = near.transform.position - toward * 22f;
                        }
                        else stand = player.transform.position;
                    }
                    if (NavMesh.SamplePosition(stand, out var hit, 8f, NavMesh.AllAreas)) stand = hit.position;
                    body.SnapTo(stand + Vector3.up * .1f, shot.holdStart ? 0f : player.transform.eulerAngles.y);
                    foreach (var c in ComradeFollower.All)
                    {
                        // Trong hào: xếp dọc lòng hào, không văng ra đồng
                        var spot = shot.inTrench ? stand + new Vector3(Random.Range(-.6f, .6f), 0, Random.Range(-6f, 3f)) : stand + Random.insideUnitSphere * 6f;
                        if (NavMesh.SamplePosition(spot, out var h2, shot.inTrench ? 1.5f : 5f, NavMesh.AllAreas)) c.GetComponent<NavMeshAgent>().Warp(h2.position);
                    }
                }
                // Lính vừa bật tiến đánh luôn
                SoldierAI.AlertAround(player.transform.position, 1000f, player.transform.position);
                if (endless) { Call(endless, "SpawnWave", 0); if (shot.mode == Mode.Crane) { yield return new WaitForSeconds(.5f); Call(endless, "SpawnWave", 0); Call(endless, "SpawnWave", 0); } }

                var cam = Camera.main;
                MountedGun gun = null; ChaffeeTank tank = null; Boss boss = null;
                if (shot.mode == Mode.MountedGun)
                {
                    gun = Object.FindAnyObjectByType<MountedGun>();
                    gun.Interact(player.GetComponent<Interactor>());
                }
                else if (shot.mode == Mode.Bazooka)
                {
                    tank = Object.FindAnyObjectByType<ChaffeeTank>();
                    player.GetComponentInChildren<WeaponController>().Inventory.GiveWeapon(WeaponCatalog.Get("W-BAZOOKA"), 8);
                    PlayerView.Set(true);
                }
                else
                {
                    // Máy quay bay tự do: hiện thân nhân vật một khung rồi tắt điều khiển camera của PlayerView
                    PlayerView.Set(true);
                    yield return null; yield return null;
                    player.GetComponent<PlayerView>().enabled = false;
                    var arms = Object.FindAnyObjectByType<FirstPersonArms>();
                    if (arms) arms.gameObject.SetActive(false);
                    cam.transform.SetParent(null, true);
                    cam.nearClipPlane = .3f;
                }
                if (shot.mode == Mode.AirRaid) AirRaid.Now();
                yield return new WaitForSeconds(shot.preRoll);
                if (shot.mode == Mode.Boss)
                {
                    var pick = SoldierAI.All.Where(s => s && !s.IsDead && s.isActiveAndEnabled).OrderBy(s => (s.transform.position - player.transform.position).sqrMagnitude).FirstOrDefault();
                    if (pick) { boss = Boss.Promote(pick.gameObject, "TRÙM"); pick.GetComponent<Health>().Configure(TargetKind.Infantry, 100000f); }
                    yield return null; yield return null;
                }

                int startFrame = frame;
                Time.captureFramerate = Fps;
                int count = Mathf.RoundToInt(shot.seconds * Fps);
                Vector3 camPos = cam.transform.position; Quaternion camRot = cam.transform.rotation;
                float sweepYaw = 0f;
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)count;
                    var foes = Foes(player);
                    var allies = ComradeFollower.All.Where(c => c && !c.IsDead).ToList();
                    var center = allies.Count > 0 ? allies.Aggregate(Vector3.zero, (s, c) => s + c.transform.position) / allies.Count : player.transform.position;
                    var closest = foes.OrderBy(h => (h.transform.position - center).sqrMagnitude).Take(4).ToList();
                    var foe = closest.Count > 0 ? closest.Aggregate(Vector3.zero, (s, x) => s + x.transform.position) / closest.Count : center + player.transform.forward * 20f;
                    var fwd = foe - center; fwd.y = 0; fwd = fwd.sqrMagnitude > .01f ? fwd.normalized : Vector3.forward;
                    var side = Vector3.Cross(Vector3.up, fwd);
                    Vector3 want = camPos, look = center;

                    switch (shot.mode)
                    {
                        case Mode.Follow:
                            want = center - fwd * shot.back * (1.1f - .3f * t) + side * (shot.inTrench ? Mathf.Lerp(-.3f, .3f, t) : Mathf.Lerp(-3f, 3f, t)) + Vector3.up * shot.height;
                            look = Vector3.Lerp(center, foe, .55f) + Vector3.up * 1.2f;
                            break;
                        case Mode.AirRaid:
                            var target = AirRaid.LastTarget;
                            var away = center - target; away.y = 0; away = away.sqrMagnitude > .01f ? away.normalized : -fwd;
                            want = target + away * Mathf.Lerp(34f, 26f, t) + Vector3.up * Mathf.Lerp(8f, 5f, t);
                            look = target + Vector3.up * Mathf.Lerp(14f, 3f, Mathf.Clamp01(t * 1.6f));
                            break;
                        case Mode.Crane:
                            want = player.transform.position + new Vector3(Mathf.Sin(t * 1.4f) * 12f, Mathf.Lerp(2.5f, 17f, t * t * (3f - 2f * t)), -Mathf.Lerp(8f, 20f, t));
                            look = player.transform.position + new Vector3(0, 1.5f, Mathf.Lerp(30f, 45f, t));
                            break;
                        case Mode.Boss:
                            if (boss)
                            {
                                var b = boss.transform;
                                float orbit = Mathf.Lerp(-35f, 35f, t) * Mathf.Deg2Rad;
                                // Góc thấp nhìn lên, khung lấy trọn người (theo khối hình thật của model)
                                var rs = b.GetComponentsInChildren<Renderer>();
                                var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
                                want = bb.center + (b.forward * Mathf.Cos(orbit) + b.right * Mathf.Sin(orbit)) * 4.6f + Vector3.down * (bb.extents.y * .6f);
                                look = bb.center + Vector3.up * (bb.extents.y * .15f);
                            }
                            break;
                        case Mode.MountedGun:
                            // Lia nòng qua trận địa, bắn từng loạt
                            sweepYaw = Mathf.Sin(t * Mathf.PI * 2f) * 28f;
                            var seat = gun.transform.parent ? gun.transform.parent.Find("Seat") : null;
                            body.SnapTo(seat ? seat.position : player.transform.position, sweepYaw);
                            if (i % 3 == 0 && (i / 24) % 3 != 2) gun.Shoot();
                            break;
                        case Mode.Bazooka:
                            var aim = tank ? tank.transform.position : player.transform.position + Vector3.forward * 50f;
                            var yaw = Quaternion.LookRotation(aim - player.transform.position).eulerAngles.y;
                            body.SnapTo(player.transform.position, yaw);
                            if (i == 24 || i == 96)
                            {
                                var muzzle = PlayerView.Muzzle ? PlayerView.Muzzle.position : player.transform.position + Vector3.up * 1.5f;
                                CombatEffects.Muzzle(muzzle, Quaternion.LookRotation(aim - muzzle), 2.5f);
                                RocketFlight.Launch(muzzle, aim + Vector3.up * 1.2f, true);
                                EnemyAnimatorDriver.Play(body, "Shoot");
                            }
                            break;
                    }
                    if (freeCam)
                    {
                        camPos = i == 0 ? want : Vector3.Lerp(camPos, want, .08f);
                        camRot = i == 0 ? Quaternion.LookRotation(look - want) : Quaternion.Slerp(camRot, Quaternion.LookRotation(look - camPos), .1f);
                        cam.transform.SetPositionAndRotation(camPos, camRot);
                    }
                    yield return null;
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(dir, $"f{frame++:D5}.jpg"), tex.EncodeToJPG(92));
                }
                Time.captureFramerate = 0;
                index.AppendLine($"{shot.name} {startFrame} {count}");
            }
            File.WriteAllText(Path.Combine(dir, "canh.txt"), index.ToString());
            Object.Destroy(tex); rt.Release();
            Assert.Greater(frame, 0);
        }
    }
}
