using System.Collections;
using System.Linq;
using DBP.AI;
using DBP.Core;
using DBP.Missions;
using DBP.Interaction;
using DBP.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Hai phe 10–20 người đánh nhau thật; địch do đồng đội hạ không tính công người chơi; máy bay ném bom;
    /// bản đồ nhỏ; cài đặt nhạc.
    public class ArmyBattleTests
    {
        static readonly string[] Missions = { "M1_HimLam", "M2_GiuDocLap", "M3_DoiED", "M4_LongHao", "M5_ChieuMungBay" };

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Sandbox");
            InputLock.ResetAll();
        }

        static IEnumerator Begin(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene); yield return null; yield return null;
            var begin = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "BeginMission");
            if (begin) begin.onClick.Invoke();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest] public IEnumerator EachSide_Has10To20Soldiers([ValueSource(nameof(Missions))] string scene)
        {
            yield return Begin(scene);
            int allies = ComradeFollower.All.Count;
            int enemies = Resources.FindObjectsOfTypeAll<Health>().Count(h => h.gameObject.scene.IsValid() && h.Kind == TargetKind.Infantry);
            Assert.That(allies, Is.InRange(10, 20), $"{scene}: đồng đội");
            Assert.That(enemies, Is.InRange(10, 20), $"{scene}: lính địch (kể cả đợt phản kích chưa xuất hiện)");
            Assert.IsNotNull(Object.FindAnyObjectByType<MiniMap>(), "Có bản đồ nhỏ");
            Assert.IsNotNull(Object.FindAnyObjectByType<AirRaid>(), "Có máy bay ném bom");
        }

        [UnityTest] public IEnumerator AlliesFightAndKill_ButKillsAreNotCreditedToPlayer()
        {
            yield return Begin("M1_HimLam");
            var player = GameObject.FindGameObjectWithTag("Player");
            player.GetComponent<Health>().Configure(TargetKind.Player, 100000f); // người chơi đứng xem, không chết
            // Đưa cả tốp đồng đội tới gần lính địch đầu tiên để chắc chắn giao chiến trong thời gian test
            var enemy = SoldierAI.All.First(s => s.isActiveAndEnabled);
            var toEnemy = (enemy.transform.position - player.transform.position).normalized;
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
            player.transform.position = enemy.transform.position - toEnemy * 22f; cc.enabled = true;
            foreach (var c in ComradeFollower.All)
            {
                var agent = c.GetComponent<UnityEngine.AI.NavMeshAgent>();
                agent.Warp(player.transform.position + Random.insideUnitSphere.normalized * 3f);
            }
            int killsBefore = MissionController.Current.Attempt.Kills;
            Time.timeScale = 2f;
            float end = Time.realtimeSinceStartup + 20f;
            bool engaged = false;
            while (Time.realtimeSinceStartup < end && !Object.FindObjectsByType<Health>(FindObjectsSortMode.None).Any(h => h.IsDead && h.LastHitByAlly))
            {
                engaged |= ComradeFollower.All.Any(c => c.Target);
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.IsTrue(engaged, "Đồng đội phải chọn được mục tiêu là lính địch");
            var allyKills = Resources.FindObjectsOfTypeAll<Health>().Count(h => h.gameObject.scene.IsValid() && h.IsDead && h.LastHitByAlly);
            Assert.Greater(allyKills, 0, "Đồng đội phải hạ được ít nhất một lính địch");
            Assert.AreEqual(killsBefore, MissionController.Current.Attempt.Kills, "Địch do đồng đội hạ không tính công người chơi");
        }

        [UnityTest] public IEnumerator AirRaid_FliesOverAndDropsBombs()
        {
            yield return Begin("M2_GiuDocLap");
            GameObject.FindGameObjectWithTag("Player").GetComponent<Health>().Configure(TargetKind.Player, 100000f);
            AirRaid.Now();
            yield return null; yield return null;
            Assert.IsNotNull(GameObject.Find("MayBayPhap"), "Máy bay xuất hiện");
            Time.timeScale = 3f;
            float end = Time.realtimeSinceStartup + 6f;
            bool bombSeen = false;
            while (Time.realtimeSinceStartup < end) { bombSeen |= GameObject.Find("Bom"); yield return null; }
            Time.timeScale = 1f;
            Assert.IsTrue(bombSeen, "Máy bay phải thả bom");
        }

        [Test] public void MusicSettings_PersistAndClamp()
        {
            float vol = MusicSettings.Volume; int track = MusicSettings.Track; bool on = MusicSettings.Enabled;
            try
            {
                MusicSettings.Volume = 1.7f; Assert.AreEqual(1f, MusicSettings.Volume);
                MusicSettings.Track = MusicSettings.TrackNames.Length; Assert.AreEqual(0, MusicSettings.Track, "Chọn quá bản cuối thì quay về đầu");
                MusicSettings.Track = -1; Assert.AreEqual(MusicSettings.TrackNames.Length - 1, MusicSettings.Track);
                MusicSettings.Track = 3; Assert.AreEqual("mission_03", MusicSettings.ClipFor("M1_HimLam").name);
                MusicSettings.Track = 0; Assert.AreEqual("mission_02", MusicSettings.ClipFor("M2_GiuDocLap").name);
                MusicSettings.Enabled = false; Assert.IsFalse(MusicSettings.Enabled);
            }
            finally { MusicSettings.Volume = vol; MusicSettings.Track = track; MusicSettings.Enabled = on; }
        }

        [UnityTest] public IEnumerator ZoneObjectives_HaveRedFlags_AndOldMarkersHidden([ValueSource(nameof(Missions))] string scene)
        {
            yield return Begin(scene);
            yield return null;
            int zones = MissionController.Current.Objectives.Count(o => o is ReachZoneObjective r && r.zone || o is CaptureZoneObjective c && c.zone);
            var flags = Object.FindObjectsByType<ObjectiveFlag>(FindObjectsSortMode.None);
            Assert.AreEqual(zones, flags.Length, $"{scene}: mỗi vùng mục tiêu một lá cờ");
            Assert.IsFalse(Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Any(r => r.name == "Marker" && r.enabled), "Cột xanh cũ phải ẩn");
            var dir = System.Environment.GetEnvironmentVariable("SHOT_DIR");
            if (flags.Length == 0 || string.IsNullOrEmpty(dir)) yield break;
            var flag = flags[0].transform;
            var cam = Camera.main; cam.transform.SetParent(null, true);
            var arms = Object.FindAnyObjectByType<DBP.Player.FirstPersonArms>(); if (arms) arms.gameObject.SetActive(false);
            cam.transform.position = flag.position + new Vector3(4.5f, 3.2f, -5f);
            cam.transform.LookAt(flag.position + Vector3.up * 3.6f);
            yield return new WaitForSeconds(0.5f);
            var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null; rt.Release();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"co_{scene}.png"), tex.EncodeToPNG());
        }

        [UnityTest] public IEnumerator M3_HillsHaveLadders_ThatReachTheTop()
        {
            yield return Begin("M3_DoiED");
            var ladders = Object.FindObjectsByType<DBP.Player.Ladder>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(ladders.Length, 4, "Mỗi đồi E, D có 2 thang");
            foreach (var hill in new[] { "DoiE", "DoiD" })
            {
                var top = GameObject.Find(hill).GetComponent<Collider>().bounds.max.y;
                Assert.IsTrue(ladders.Any(l => l.TopY >= top && Vector3.Distance(l.transform.position, GameObject.Find(hill).transform.position) < 20f), $"{hill}: thang phải chạm tới đỉnh bục");
            }
            var foot = ladders[0].transform.position;
            Assert.IsNotNull(DBP.Player.Ladder.At(foot), "Đứng ở chân thang thì vào vùng trèo");
        }

        [UnityTest] public IEnumerator M3_AiCanPathUpTheLadders_AndCrossM5ByBridge()
        {
            yield return Begin("M3_DoiED");
            var ladder = Object.FindObjectsByType<DBP.Player.Ladder>(FindObjectsSortMode.None)[0];
            var link = ladder.GetComponent<Unity.AI.Navigation.NavMeshLink>();
            Assert.IsNotNull(link, "Thang có liên kết NavMesh cho AI");
            var foot = ladder.transform.TransformPoint(link.startPoint);
            var top = ladder.transform.TransformPoint(link.endPoint);
            var path = new UnityEngine.AI.NavMeshPath();
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(foot, out var a, 2f, UnityEngine.AI.NavMesh.AllAreas), "Chân thang nằm trên NavMesh");
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(top, out var b, 2f, UnityEngine.AI.NavMesh.AllAreas), "Đầu thang nằm trên mặt bục");
            UnityEngine.AI.NavMesh.CalculatePath(a.position, b.position, UnityEngine.AI.NavMesh.AllAreas, path);
            Assert.AreEqual(UnityEngine.AI.NavMeshPathStatus.PathComplete, path.status, "AI tìm được đường trèo thang lên bục");
            Assert.IsTrue(ComradeFollower.All.All(c => c.GetComponent<LadderClimber>()), "Đồng đội biết trèo thang");

            yield return Begin("M5_ChieuMungBay");
            var river = GameObject.Find("SongNamRom").GetComponent<Collider>().bounds;
            UnityEngine.AI.NavMesh.SamplePosition(new Vector3(-25f, 0, river.min.z - 6f), out a, 3f, UnityEngine.AI.NavMesh.AllAreas);
            UnityEngine.AI.NavMesh.SamplePosition(new Vector3(-25f, 0, river.max.z + 6f), out b, 3f, UnityEngine.AI.NavMesh.AllAreas);
            UnityEngine.AI.NavMesh.CalculatePath(a.position, b.position, UnityEngine.AI.NavMesh.AllAreas, path);
            Assert.AreEqual(UnityEngine.AI.NavMeshPathStatus.PathComplete, path.status, "Qua được sông");
            var bridge = GameObject.Find("CauMuongThanh").GetComponent<Collider>().bounds;
            for (int i = 1; i < path.corners.Length; i++)
            {
                var mid = (path.corners[i - 1] + path.corners[i]) * 0.5f;
                if (mid.z > river.min.z + 0.5f && mid.z < river.max.z - 0.5f)
                    Assert.That(mid.x, Is.InRange(bridge.min.x - 0.5f, bridge.max.x + 0.5f), "AI phải đi qua cầu, không lội sông");
            }
        }

        [UnityTest] public IEnumerator M5_RiverHasWalkableBanks()
        {
            yield return Begin("M5_ChieuMungBay");
            var river = GameObject.Find("SongNamRom").GetComponent<Collider>().bounds;
            var terrain = Object.FindAnyObjectByType<Terrain>();
            Assert.IsNotNull(terrain, "Màn 5 có địa hình thật");
            // Đi từ giữa sông lên mỗi bờ: mặt bước (đáy đất hoặc mặt nước) không dốc quá 45°
            foreach (int side in new[] { -1, 1 })
            {
                float prev = float.NaN;
                for (float d = 0f; d <= river.extents.z + 4f; d += .25f)
                {
                    var p = new Vector3(-20f, 0, river.center.z + side * d);
                    float h = Mathf.Max(terrain.SampleHeight(p) + terrain.transform.position.y, river.max.y);
                    if (!float.IsNaN(prev)) Assert.Less(Mathf.Abs(h - prev), .25f, $"Bờ {(side < 0 ? "nam" : "bắc")} dốc quá tại {p.z:0.0}");
                    prev = h;
                }
                Assert.AreEqual(0f, prev, .4f, "Lên tới mặt bờ");
            }
        }

        [UnityTest] public IEnumerator Enemies_ChargeInsteadOfHolding_OfficerIsTougher()
        {
            yield return Begin("M1_HimLam");
            var player = GameObject.FindGameObjectWithTag("Player");
            player.GetComponent<Health>().Configure(TargetKind.Player, 100000f);
            var enemy = SoldierAI.All.First(s => s.isActiveAndEnabled && !s.GetComponent<OfficerLeader>());
            float normalMax = enemy.Health.Max;
            enemy.Health.Configure(TargetKind.Infantry, 100000f); // đồng đội không hạ kịp trong lúc đo
            var toEnemy = (enemy.transform.position - player.transform.position).normalized;
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
            player.transform.position = enemy.transform.position - toEnemy * 32f; cc.enabled = true;
            SoldierAI.AlertAround(enemy.transform.position, 1f, player.transform.position);
            float end = Time.realtimeSinceStartup + 9f;
            while (Time.realtimeSinceStartup < end && enemy && !enemy.IsDead) yield return null;
            Assert.IsTrue(enemy && !enemy.IsDead);
            float dist = Vector3.Distance(enemy.transform.position, player.transform.position);
            Assert.Less(dist, 26f, "Địch thấy quân ta phải tiến lên vào tầm bắn, không đứng yên ở xa");
            Assert.Greater(dist, 6f, "Vào tầm bắn thì đứng lại bắn, không lao vào chỗ chết");

            var officer = Object.FindObjectsByType<OfficerLeader>(FindObjectsSortMode.None).FirstOrDefault(o => !o.GetComponent<Boss>());
            if (officer)
            {
                var oh = officer.GetComponent<Health>();
                SoldierAI.AlertAround(officer.transform.position, 1f, player.transform.position);
                yield return null; yield return null;
                if (!oh.IsDead) Assert.AreEqual(OfficerLeader.DodgeChance, oh.DodgeChance, "Giao chiến thì chỉ huy biết né đạn");
                Assert.AreEqual(2f, officer.GetComponent<SoldierAI>().DamageMul, "Chỉ huy sát thương gấp đôi");
                Assert.Greater(oh.Max, normalMax, "Chỉ huy máu trâu hơn");
            }
        }

        [UnityTest] public IEnumerator EveryMission_HasOneBoss_WithBigHealthBar()
        {
            foreach (var scene in new[] { "M1_HimLam", "M2_GiuDocLap", "M3_DoiED", "M4_LongHao", "M5_ChieuMungBay" })
            {
                yield return Begin(scene);
                yield return null; yield return null;
                var bosses = Object.FindObjectsByType<Boss>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.AreEqual(1, bosses.Length, $"{scene}: mỗi màn đúng 1 trùm");
                var boss = bosses[0];
                Assert.IsNotNull(boss.GetComponent<OfficerLeader>(), "Trùm có kỹ năng chỉ huy");
                Assert.IsTrue(boss.Title.StartsWith("TRÙM"));
                if (!boss.isActiveAndEnabled) continue; // trùm thuộc đợt sau, chưa xuất hiện
                var normal = Object.FindObjectsByType<Health>(FindObjectsSortMode.None).First(h => h.Kind == TargetKind.Infantry && !h.GetComponent<OfficerLeader>());
                Assert.Greater(boss.GetComponent<Health>().Max, normal.Max * 4f, "Trùm máu rất trâu");
                Assert.IsNotNull(GameObject.Find("BossHudCanvas"), "Có thanh máu trùm");
            }
        }

        /// Màn 2–4: địch không đứng yên — đợt phản kích tiến đánh ngay; lính gác / lính hào thấy bộ đội thì giao chiến.
        [UnityTest] public IEnumerator Maps234_EnemiesEngageAllies_InsteadOfStandingStill()
        {
            yield return Begin("M2_GiuDocLap");
            float end = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < end && !SoldierAI.All.Any(s => s && s.isActiveAndEnabled && s.CurrentState != SoldierAI.State.Idle)) yield return null;
            Assert.IsTrue(SoldierAI.All.Any(s => s && s.isActiveAndEnabled && s.CurrentState != SoldierAI.State.Idle), "M2: đợt phản kích phải tiến đánh");

            foreach (var (scene, kind) in new[] { ("M3_DoiED", typeof(SentryAI)), ("M4_LongHao", typeof(TrenchSoldierAI)) })
            {
                yield return Begin(scene);
                var enemy = (MonoBehaviour)Object.FindObjectsByType(kind, FindObjectsSortMode.None).Cast<MonoBehaviour>()
                    .First(e => e.isActiveAndEnabled && !e.GetComponent<Health>().IsDead);
                var ally = ComradeFollower.All.First();
                var p = enemy.transform.position + enemy.transform.forward * 8f;
                if (UnityEngine.AI.NavMesh.SamplePosition(p, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas)) p = hit.position;
                ally.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(p);
                end = Time.realtimeSinceStartup + 4f;
                while (Time.realtimeSinceStartup < end && !(enemy.TryGetComponent<SoldierAI>(out var s0) && s0.isActiveAndEnabled)) yield return null;
                Assert.IsTrue(enemy.TryGetComponent<SoldierAI>(out var s) && s.isActiveAndEnabled, $"{scene}: thấy bộ đội thì chuyển sang tiến đánh");
                yield return null; yield return null;
                Assert.AreNotEqual(SoldierAI.State.Idle, s.CurrentState, $"{scene}: không đứng yên");
                Assert.IsFalse(enemy.enabled, $"{scene}: AI cũ nhường quyền");
            }
        }

        [UnityTest] public IEnumerator Crates_GlowWithColoredBeam()
        {
            yield return Begin("M1_HimLam");
            var crates = Object.FindObjectsByType<QuizCrate>(FindObjectsSortMode.None).Where(c => !c.Used).ToArray();
            Assert.IsNotEmpty(crates);
            foreach (var c in crates)
            {
                var beam = c.transform.Find("CotSangHom");
                Assert.IsNotNull(beam, "Hòm phải có cột sáng");
                Assert.AreEqual(RarityRules.Color(c.rarity).r, beam.GetComponent<LineRenderer>().startColor.r, 0.01f, "Màu cột sáng theo độ khó");
            }
        }
    }
}
