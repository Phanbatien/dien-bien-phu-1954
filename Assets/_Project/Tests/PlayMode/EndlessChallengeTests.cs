using System.Collections;
using System.Linq;
using DBP.AI;
using DBP.Core;
using DBP.Missions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Màn 6 – Thử thách vô hạn: địch tới theo đợt, diệt được tính điểm và lưu kỷ lục, không bao giờ "thắng".
    public class EndlessChallengeTests
    {
        [UnityTest] public IEnumerator Waves_SpawnFromAround_KillsCount_BestSaved_NeverWins()
        {
            PlayerPrefs.DeleteKey("dbp.endless.best");
            yield return SceneManager.LoadSceneAsync("M6_ThuThach"); yield return null; yield return null;
            var begin = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "BeginMission");
            if (begin) begin.onClick.Invoke();
            Time.timeScale = 1f;
            var mission = MissionController.Current;
            var endless = mission.Objectives.OfType<EndlessObjective>().Single();
            mission.Player.Configure(TargetKind.Player, 100000f);
            float end = Time.realtimeSinceStartup + 10f;
            while (endless.Wave == 0 && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(1, endless.Wave, "Đợt 1 phải tới");
            yield return null; yield return null;
            var wave = SoldierAI.All.Where(s => s && s.name.StartsWith("Dot1_")).ToList();
            Assert.GreaterOrEqual(wave.Count, 4, "Đợt 1 có ít nhất 4 lính");
            Assert.IsTrue(wave.All(s => Vector3.Distance(s.transform.position, mission.Player.transform.position) > 30f), "Địch xuất hiện ở xa, quanh trận địa");
            Assert.IsTrue(wave.All(s => s.CurrentState != SoldierAI.State.Idle), "Địch mới tới là tiến đánh ngay");
            Assert.IsFalse(wave.Any(s => s.GetComponent<OfficerLeader>()), "Đợt 1 chỉ có lính thường (sĩ quan từ đợt 2)");

            foreach (var s in wave) s.Health.TakeDamage(9999f, DamageType.Bullet, s.transform.position);
            yield return null; yield return null;
            Assert.GreaterOrEqual(mission.Attempt.Kills, wave.Count);
            Assert.AreEqual(mission.Attempt.Kills, EndlessObjective.Best, "Kỷ lục lưu trên máy");
            Assert.AreEqual(MissionOutcome.None, mission.Attempt.Outcome, "Thử thách vô hạn không bao giờ kết thúc bằng thắng");
            Assert.IsNotNull(GameObject.Find("ThuThachHud"));
        }
    }
}
