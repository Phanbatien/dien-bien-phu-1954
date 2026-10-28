using System.Collections;
using System.Linq;
using DBP.AI;
using DBP.Core;
using DBP.Missions;
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
    }
}
