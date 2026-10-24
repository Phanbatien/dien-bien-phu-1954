using System.Collections;
using System.Linq;
using DBP.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Trên màn thật: mọi hòm sinh ra phải đứng trên mặt đất, không chạm vật cản và mở được.
    public class MissionCrateSpawnTests
    {
        /// Độ lún sâu nhất của đáy hòm vào Terrain, đo tại 9 điểm trên đáy (vách hào dốc sẽ cho số lớn).
        static float TerrainPenetration(Transform crate)
        {
            float bottom = crate.position.y - crate.lossyScale.y / 2f, worst = 0f;
            foreach (var terrain in Terrain.activeTerrains)
                for (int i = -1; i <= 1; i++)
                    for (int k = -1; k <= 1; k++)
                    {
                        var p = crate.position + crate.rotation * new Vector3(i * crate.lossyScale.x / 2f, 0, k * crate.lossyScale.z / 2f);
                        worst = Mathf.Max(worst, terrain.SampleHeight(p) + terrain.transform.position.y - bottom);
                    }
            return worst;
        }

        static readonly string[] Missions = { "M1_HimLam", "M2_GiuDocLap", "M3_DoiED", "M4_LongHao", "M5_ChieuMungBay" };

        [UnityTest]
        public IEnumerator AllMissions_CratesOnGround_NotInsideObstacles([ValueSource(nameof(Missions))] string mission)
        {
            yield return SceneManager.LoadSceneAsync(mission);
            yield return null;
            yield return null;
            var spawner = Object.FindAnyObjectByType<CrateSpawner>();
            Assert.That(spawner.Spawned.Count, Is.InRange(5, 7), $"{mission}: số hòm đặt được (bỏ {spawner.RejectedPoints} điểm)");
            Physics.SyncTransforms();
            foreach (var crate in spawner.Spawned)
            {
                var col = crate.GetComponent<Collider>();
                var b = col.bounds;
                // Có mặt đất ngay dưới đáy hòm
                var down = Physics.RaycastAll(b.center, Vector3.down, b.extents.y + 0.3f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => h.collider != col).ToArray();
                Assert.IsNotEmpty(down, $"{mission}/{crate.name} ở {crate.transform.position}: lơ lửng hoặc bị chôn");
                // Không chạm vật cản (trừ mặt đất ngay dưới)
                var half = crate.transform.lossyScale * 0.49f; // kích thước thật của hòm (không dùng hộp bao theo trục)
                var hits = Physics.OverlapBox(crate.transform.position + Vector3.up * 0.05f, half, crate.transform.rotation, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => c != col && c.bounds.max.y > b.min.y + 0.1f && !(c is TerrainCollider)).Select(c => c.name).ToArray();
                Assert.LessOrEqual(TerrainPenetration(crate.transform), 0.1f, $"{mission}/{crate.name} lún vào địa hình");
                Assert.IsEmpty(hits, $"{mission}/{crate.name} ở {crate.transform.position} dính: {string.Join(", ", hits)}");
            }

            // Mọi điểm khai báo (bao trùm mọi seed): đặt thử từng điểm, điểm nào đặt được thì vị trí phải hợp lệ.
            foreach (var crate in spawner.Spawned) crate.gameObject.SetActive(false);
            int ok = 0;
            foreach (var p in spawner.points)
            {
                if (!CratePlacement.TryFindSpot(p.position, p.rotation, out var ground)) continue;
                var probe = QuizCrate.Spawn(ground, p.rotation, new CrateReward(), true, null);
                Physics.SyncTransforms();
                var half = probe.transform.lossyScale * 0.49f;
                var hits = Physics.OverlapBox(probe.transform.position + Vector3.up * 0.05f, half, probe.transform.rotation, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => c.gameObject != probe.gameObject && c.bounds.max.y > ground.y + 0.1f && !(c is TerrainCollider)).Select(c => c.name).ToArray();
                Assert.LessOrEqual(TerrainPenetration(probe.transform), 0.1f, $"{mission}/{p.name} → {ground} lún vào địa hình/vách hào");
                Assert.IsEmpty(hits, $"{mission}/{p.name} → {ground} dính: {string.Join(", ", hits)}");
                Object.DestroyImmediate(probe.gameObject);
                ok++;
            }
            Assert.GreaterOrEqual(ok, spawner.minCrates, $"{mission}: chỉ {ok}/{spawner.points.Count} điểm đặt được hòm");
        }
    }
}
