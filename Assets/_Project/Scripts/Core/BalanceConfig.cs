using System;
using System.Collections.Generic;
using UnityEngine;

namespace DBP.Core
{
    /// Cân bằng độ khó (T65 – Đạt; BR-40):
    /// ScriptableObject lưu tham số balance có thể chỉnh trong Editor mà không cần rebuild.
    /// Thay đổi ở đây ảnh hưởng trực tiếp đến SoldierAI, Health, QuizCrate, v.v.
    [CreateAssetMenu(menuName = "DBP/Balance Config", fileName = "BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        public static BalanceConfig Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoLoad()
        {
            var cfg = Resources.Load<BalanceConfig>("BalanceConfig");
            if (cfg != null) Instance = cfg;
            else Debug.LogWarning("[Balance] Không tìm thấy BalanceConfig trong Resources/. Dùng giá trị mặc định.");
        }

        [Header("Lính bộ binh")]
        [Tooltip("Máu mặc định của lính (SoldierAI / TrenchSoldierAI).")]
        public float soldierHealth = 80f;
        [Tooltip("Sát thương mỗi viên đạn của lính.")]
        public float soldierDamage = 15f;
        [Tooltip("Giây/phát cho lính bắn.")]
        public float soldierFireInterval = 1.8f;
        [Tooltip("Giây nấp sau khi bị bắn.")]
        public float soldierSuppressDuration = 2.5f;

        [Header("Lô cốt (Bunker)")]
        public float bunkerHealth = 500f;
        public float bunkerDamage = 20f;
        public float bunkerFireInterval = 0.15f;

        [Header("Xe tăng Chaffee")]
        public float tankHealth = 800f;
        public float tankDamage = 60f;
        public float tankFireInterval = 3f;

        [Header("Hòm đồ — trọng số độ hiếm")]
        [Tooltip("Common:Uncommon:Rare:Legendary — phải cộng = 1.0")]
        public float rarityCommon    = 0.50f;
        public float rarityUncommon  = 0.30f;
        public float rarityRare      = 0.15f;
        public float rarityLegendary = 0.05f;

        [Header("Hòm — số lượng trong màn")]
        public int minCrates = 5;
        public int maxCrates = 7;

        [Header("Súng rơi từ địch")]
        [Tooltip("Xác suất rơi súng mặc định (0–1). Per-màn có thể ghi đè.")]
        public float dropChanceDefault = 0.08f;
        public int maxDropsPerRun = 5;

        [Header("Bảo hiểm hòm (BR-16)")]
        public int insuranceStreakThreshold = 3;
        public int insuranceLevel = 1;
        public float insuranceTimeLimit = 12f;

        [Header("Thời gian chơi mục tiêu ≤ 15 phút (BR-40)")]
        [Tooltip("Mục tiêu: người chơi hoàn thành mỗi màn trong thời gian này (giây).")]
        public float targetCompletionSeconds = 900f; // 15 phút

        public Rarity RollRarity(System.Random rng)
        {
            float r = (float)rng.NextDouble();
            if (r < rarityCommon)    return Rarity.Common;
            r -= rarityCommon;
            if (r < rarityUncommon)  return Rarity.Uncommon;
            r -= rarityUncommon;
            if (r < rarityRare)      return Rarity.Rare;
            return Rarity.Legendary;
        }

        void OnValidate()
        {
            float total = rarityCommon + rarityUncommon + rarityRare + rarityLegendary;
            if (Mathf.Abs(total - 1f) > 0.001f)
                Debug.LogWarning($"[BalanceConfig] Tổng trọng số độ hiếm = {total:F3} ≠ 1.0 (BR-29).");
        }
    }

    /// Enum Rarity phải dùng chung — định nghĩa ở đây nếu chưa có ở namespace Interaction.
    // (QuizCrate.cs đã define trong DBP.Interaction; dùng đó, không define lại ở đây.)
}
