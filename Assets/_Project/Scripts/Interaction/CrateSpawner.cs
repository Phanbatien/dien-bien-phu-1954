using System.Collections.Generic;
using DBP.Missions;
using UnityEngine;

namespace DBP.Interaction
{
    /// T22 (BR-15): Sinh hòm vũ khí theo seed từ Attempt của lượt chơi.
    /// Rút ngẫu nhiên 5-7 hòm từ danh sách 10-15 vị trí ứng viên không trùng.
    /// Cùng seed đảm bảo luôn sinh ra cùng vị trí và cùng độ hiếm.
    public class CrateSpawner : MonoBehaviour
    {
        public const int MIN_CRATES = 5;
        public const int MAX_CRATES = 7;
        public const int MIN_CANDIDATES = 10;
        public const int MAX_CANDIDATES = 15;

        [Header("Điểm spawn ứng viên (10–15 điểm)")]
        [SerializeField] List<Transform> candidatePoints = new List<Transform>();

        [Header("Prefab")]
        [Tooltip("Prefab của QuizCrate cần sinh ra")]
        [SerializeField] QuizCrate cratePrefab;

        bool spawned;

        public IReadOnlyList<Transform> CandidatePoints => candidatePoints;

        void Start()
        {
            Activate();
        }

        /// Kích hoạt sinh hòm ngẫu nhiên dựa trên seed của lượt chơi.
        public void Activate()
        {
            if (spawned) return;
            spawned = true;

            // Lấy seed từ Attempt (BR-33). Nếu chưa có thì fallback.
            int seed = MissionController.Current?.Attempt != null
                ? MissionController.Current.Attempt.Seed
                : System.Guid.NewGuid().GetHashCode();

            var rng = new System.Random(seed);

            // Lọc danh sách điểm hợp lệ
            var validPoints = new List<Transform>();
            if (candidatePoints != null)
            {
                foreach (var pt in candidatePoints)
                {
                    if (pt != null) validPoints.Add(pt);
                }
            }

            if (validPoints.Count == 0)
            {
                Debug.LogWarning("[CrateSpawner] Danh sách candidatePoints rỗng, không thể spawn hòm.");
                return;
            }

            // Shuffle vị trí theo thuật toán Fisher-Yates bằng rng
            for (int i = validPoints.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var temp = validPoints[i];
                validPoints[i] = validPoints[j];
                validPoints[j] = temp;
            }

            // Chọn ngẫu nhiên từ MIN_CRATES đến MAX_CRATES
            int targetCount = rng.Next(MIN_CRATES, MAX_CRATES + 1);
            int spawnCount = Mathf.Min(targetCount, validPoints.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                var point = validPoints[i];
                Rarity rarity = RollRarity(rng);

                QuizCrate crateInstance;
                if (cratePrefab != null)
                {
                    crateInstance = Instantiate(cratePrefab, point.position, point.rotation, transform);
                }
                else
                {
                    // Fallback nếu chưa gán prefab trong Editor
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = $"QuizCrate_{i + 1}";
                    go.transform.position = point.position;
                    go.transform.rotation = point.rotation;
                    go.transform.SetParent(transform);
                    crateInstance = go.AddComponent<QuizCrate>();
                }

                crateInstance.rarity = rarity;

                // Gán màu renderer theo độ hiếm (RarityRules)
                var renderers = crateInstance.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    r.material.color = RarityRules.Color(rarity);
                }
            }
        }

        /// Rút độ hiếm theo trọng số: Common=0.5, Uncommon=0.3, Rare=0.15, Legendary=0.05
        static Rarity RollRarity(System.Random rng)
        {
            double roll = rng.NextDouble();
            if (roll < 0.50) return Rarity.Common;
            if (roll < 0.80) return Rarity.Uncommon;
            if (roll < 0.95) return Rarity.Rare;
            return Rarity.Legendary;
        }

        void OnDrawGizmos()
        {
            if (candidatePoints == null) return;
            Gizmos.color = Color.yellow;
            foreach (var pt in candidatePoints)
            {
                if (pt != null)
                {
                    Gizmos.DrawSphere(pt.position, 0.4f);
                }
            }
        }
    }
}
