using System;
using System.Collections.Generic;
using DBP.Missions;
using UnityEngine;

namespace DBP.Interaction
{
    /// Phép rút ngẫu nhiên thuần, tách riêng để test (BR-13, BR-15).
    public static class CrateRoll
    {
        /// Rút n điểm khác nhau, n trong [min, max] và không quá số điểm có.
        public static List<int> PickPoints(System.Random rng, int pointCount, int min, int max)
        {
            int n = Math.Min(pointCount, rng.Next(min, max + 1));
            var idx = new List<int>();
            for (int i = 0; i < pointCount; i++) idx.Add(i);
            for (int i = 0; i < n; i++)
            {
                int j = rng.Next(i, pointCount);
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }
            return idx.GetRange(0, n);
        }

        /// Rút độ hiếm theo trọng số [Thường, Khá, Hiếm, Rất hiếm].
        public static Rarity RollRarity(System.Random rng, float[] weights)
        {
            double total = 0;
            foreach (var w in weights) total += Math.Max(0f, w);
            double r = rng.NextDouble() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                r -= Math.Max(0f, weights[i]);
                if (r < 0) return (Rarity)i;
            }
            return Rarity.Common;
        }
    }

    /// Sinh hòm vũ khí khi vào màn (T22; BR-13): rút 5–7 trong 10–15 điểm theo seed của lượt,
    /// rồi rút độ hiếm và phần thưởng. Cùng seed → cùng cách phân bổ, để tái hiện khi tìm lỗi.
    public class CrateSpawner : MonoBehaviour
    {
        [Tooltip("10–15 điểm có thể đặt hòm")]
        public List<Transform> points = new List<Transform>();
        public int minCrates = 5;
        public int maxCrates = 7;
        [Tooltip("Trọng số Thường, Khá, Hiếm, Rất hiếm. Tổng phải bằng 1 (Validate Content kiểm).")]
        public float[] rarityWeights = { 0.55f, 0.25f, 0.15f, 0.05f };
        [Tooltip("Danh mục phần thưởng; mỗi độ hiếm có trọng số > 0 phải có ít nhất một dòng.")]
        public List<CrateReward> rewards = new List<CrateReward>();

        public readonly List<QuizCrate> Spawned = new List<QuizCrate>();

        /// Số điểm bị bỏ vì không có chỗ trống (để kiểm tra trong Editor/test).
        public int RejectedPoints { get; private set; }

        void Start()
        {
            var mission = MissionController.Current;
            int seed = mission ? mission.Attempt.Seed : Environment.TickCount;
            var rng = new System.Random(seed);
            int target = rng.Next(minCrates, maxCrates + 1);

            // Duyệt các điểm theo thứ tự ngẫu nhiên của seed; điểm nào không đặt được (bị chôn, dính vật cản,
            // quá dốc) thì bỏ qua và lấy điểm kế tiếp, tới khi đủ số hòm hoặc hết điểm.
            foreach (int i in CrateRoll.PickPoints(rng, points.Count, points.Count, points.Count))
            {
                if (Spawned.Count >= target) break;
                var p = points[i];
                if (!p) continue;
                if (!CratePlacement.TryFindSpot(p.position, p.rotation, out var ground))
                {
                    RejectedPoints++;
                    Debug.LogWarning($"[CrateSpawner] Bỏ điểm {p.name}: không có chỗ trống trên mặt đất trong bán kính 3 m.");
                    continue;
                }

                var rarity = CrateRoll.RollRarity(rng, rarityWeights);
                var options = rewards.FindAll(r => r.rarity == rarity);
                if (options.Count == 0)
                {
                    Debug.LogError($"[CrateSpawner] Không có phần thưởng cho độ hiếm {rarity} (BR-29).");
                    continue;
                }
                var reward = options[rng.Next(options.Count)];
                Spawned.Add(QuizCrate.Spawn(ground, p.rotation, reward, true, transform));
            }
            if (Spawned.Count < minCrates)
                Debug.LogWarning($"[CrateSpawner] Chỉ đặt được {Spawned.Count}/{minCrates} hòm: cần thêm điểm hợp lệ trong màn.");
        }
    }
}
