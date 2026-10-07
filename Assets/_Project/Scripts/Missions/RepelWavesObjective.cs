using System.Collections.Generic;
using DBP.Core;
using DBP.Player;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.Events;

namespace DBP.Missions
{
    /// Đẩy lùi các đợt phản kích theo thứ tự (M2, M4; BR-20).
    /// Một đợt tính là đẩy lùi khi mọi thành viên đã bị hạ hoặc đã rút; mỗi đợt chỉ tính một lần.
    public class RepelWavesObjective : Objective
    {
        [Tooltip("Mỗi phần tử là gốc của một đợt (đang tắt). Đợt cuối Màn 2 chứa xe tăng Chaffee.")]
        public List<GameObject> waves = new List<GameObject>();
        public float delayBetweenWaves = 4f;

        int index = -1;
        float nextWaveAt;
        readonly List<Health> members = new List<Health>();

        public override string Progress => $"Đợt {Mathf.Clamp(index + 1, 0, waves.Count)}/{waves.Count}";

        public override void Activate(MissionController mission)
        {
            base.Activate(mission);
            StartWave(0);
        }

        void StartWave(int i)
        {
            index = i;
            members.Clear();
            if (i >= waves.Count) return;
            waves[i].SetActive(true);
            members.AddRange(waves[i].GetComponentsInChildren<Health>(true));
        }

        protected override bool Check()
        {
            if (index >= waves.Count) return true;
            if (nextWaveAt > 0f)
            {
                if (Time.time < nextWaveAt) return false;
                nextWaveAt = 0f;
                StartWave(index + 1);
                return index >= waves.Count;
            }
            if (!AllNeutralized(members)) return false;

            Mission.Attempt.WavesRepelled++;
            if (index + 1 >= waves.Count)
            {
                index = waves.Count;
                return true;
            }
            nextWaveAt = Time.time + delayBetweenWaves;
            return false;
        }
    }
}
