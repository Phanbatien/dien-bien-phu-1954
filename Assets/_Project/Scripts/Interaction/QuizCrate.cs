using System;
using DBP.Missions;
using DBP.Quiz;
using DBP.Weapons;
using UnityEngine;

namespace DBP.Interaction
{
    public enum Rarity { Common, Uncommon, Rare, Legendary }

    /// Bảng độ hiếm → cấp câu, thời hạn, màu hòm (BR-15).
    public static class RarityRules
    {
        public static int MinLevel(Rarity rarity) => rarity switch
        {
            Rarity.Common => 1,
            Rarity.Uncommon => 3,
            Rarity.Rare => 4,
            _ => 5,
        };

        /// Hòm Thường rút trong tập câu cấp 1–2 còn hợp lệ.
        public static int MaxLevel(Rarity rarity) => rarity == Rarity.Common ? 2 : MinLevel(rarity);

        public static float TimeLimit(Rarity rarity) => rarity switch
        {
            Rarity.Common => 12f,
            Rarity.Uncommon => 15f,
            Rarity.Rare => 18f,
            _ => 20f,
        };

        public static Color Color(Rarity rarity) => rarity switch
        {
            Rarity.Common => new Color(0.55f, 0.4f, 0.25f),
            Rarity.Uncommon => new Color(0.2f, 0.45f, 0.75f),
            Rarity.Rare => new Color(0.75f, 0.2f, 0.15f),
            _ => new Color(0.95f, 0.8f, 0.2f),
        };

        /// Đọc độ hiếm ghi trong weapons.json ("common", "uncommon", "rare", "legendary").
        public static Rarity Parse(string text) => text switch
        {
            "uncommon" => Rarity.Uncommon,
            "rare" => Rarity.Rare,
            "legendary" => Rarity.Legendary,
            _ => Rarity.Common,
        };
    }

    /// Bảo hiểm sau ba lần sai (T23; BR-16). Chỉ tính hòm; súng rơi và điện thoại không tăng, không xóa chuỗi.
    public class PityTracker
    {
        public const int Threshold = 3;
        public int WrongStreak { get; private set; }
        public bool Pending => WrongStreak >= Threshold;

        /// wasPity: câu vừa rồi là câu bảo hiểm → chuỗi về 0 dù đúng, sai hay hết giờ.
        public void Record(QuizOutcome outcome, bool wasPity)
        {
            if (wasPity || outcome == QuizOutcome.Correct) WrongStreak = 0;
            else WrongStreak++;
        }
    }

    /// Phần thưởng gắn với một hòm, chốt trước khi hiện câu (BR-15).
    [Serializable]
    public class CrateReward
    {
        public Rarity rarity;
        public string weaponId;
        public string ammoType = "7.62x54R";
        public int ammo = 15;
        public int grenades;
    }

    /// Hòm vũ khí / súng rơi: giữ E 1 giây → câu hỏi (T13; BR-08, BR-11, BR-12).
    /// Đúng: nhận phần thưởng gắn với hòm. Sai/hết giờ: chỉ nhận gói đạn thường cho súng khởi đầu.
    /// Mỗi hòm chỉ dùng một lần trong lượt, bất kể kết quả.
    public class QuizCrate : MonoBehaviour, IInteractable
    {
        public Rarity rarity = Rarity.Common;
        [Header("Phần thưởng khi trả lời đúng (chốt trước khi hiện câu, BR-15)")]
        public string rewardWeaponId;
        public string rewardAmmoType = "7.62x54R";
        public int rewardAmmo = 15;
        public int rewardGrenades;
        [Header("Sai / hết giờ (BR-11)")]
        public string fallbackAmmoType = "7.62x54R";
        public int fallbackAmmo = 10;
        [Tooltip("Hòm: true. Súng rơi từ địch: false (không tính bảo hiểm, BR-16).")]
        public bool countsForPity = true;

        bool used, locked;

        public string Prompt => countsForPity ? $"Giữ E: mở hòm ({rarity})" : $"Giữ E: nhặt súng rơi ({rarity})";
        public float HoldSeconds => 1f;
        public bool Used => used;

        /// Tạo hòm hoặc súng rơi lúc chạy (CrateSpawner, WeaponDrop). Khôi thay khối hộp bằng prefab sau.
        public static QuizCrate Spawn(Vector3 position, Quaternion rotation, CrateReward reward, bool isCrate, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = isCrate ? $"Hom_{reward.rarity}" : $"SungRoi_{reward.weaponId}";
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = isCrate ? new Vector3(0.9f, 0.6f, 0.6f) : new Vector3(0.2f, 0.12f, 1f);
            go.transform.position += Vector3.up * go.transform.localScale.y / 2f;
            go.GetComponent<Renderer>().material.color = RarityRules.Color(reward.rarity);

            var crate = go.AddComponent<QuizCrate>();
            crate.rarity = reward.rarity;
            crate.rewardWeaponId = reward.weaponId;
            crate.rewardAmmoType = reward.ammoType;
            crate.rewardAmmo = reward.ammo;
            crate.rewardGrenades = reward.grenades;
            crate.countsForPity = isCrate;
            return crate;
        }

        public bool CanInteract(Interactor who)
        {
            var session = FieldQuizSession.Instance;
            return !used && !locked && who.Weapons != null && session && !session.IsOpen;
        }

        public void Interact(Interactor who)
        {
            var session = FieldQuizSession.Instance;
            var pity = countsForPity ? MissionController.Current?.Attempt.Pity : null;
            bool isPity = pity != null && pity.Pending;

            // Câu bảo hiểm: cấp 1, 12 giây; phần thưởng và độ hiếm của hòm giữ nguyên (BR-16).
            int min = isPity ? 1 : RarityRules.MinLevel(rarity);
            int max = isPity ? 1 : RarityRules.MaxLevel(rarity);
            float limit = isPity ? 12f : RarityRules.TimeLimit(rarity);

            if (!session.TryOpen(min, max, limit, r =>
                {
                    pity?.Record(r.outcome, isPity);
                    Resolve(r, who);
                }))
            {
                // Thiếu câu: khóa hòm lỗi trong lượt, không phát thưởng miễn phí, không hạ cấp (BR-31).
                // Bảo hiểm đang chờ vẫn giữ cho hòm sau.
                if (!session.IsOpen) locked = true;
                return;
            }
            used = true;
        }

        void Resolve(QuizResult result, Interactor who)
        {
            var inventory = who.Weapons.Inventory;
            Weapon dropped = null;
            string leftoverType = fallbackAmmoType;
            int leftoverAmmo, leftoverGrenades = 0;

            if (result.outcome == QuizOutcome.Correct)
            {
                leftoverAmmo = 0;
                var def = string.IsNullOrEmpty(rewardWeaponId) ? null : WeaponCatalog.Get(rewardWeaponId);
                if (def != null)
                {
                    var give = inventory.GiveWeapon(def, rewardAmmo);
                    dropped = give.Dropped;
                    leftoverType = def.ammoType;
                    leftoverAmmo = give.LeftoverAmmo;
                }
                else if (rewardAmmo > 0)
                {
                    leftoverType = rewardAmmoType;
                    leftoverAmmo = inventory.AddAmmo(rewardAmmoType, rewardAmmo);
                }
                if (rewardGrenades > 0) leftoverGrenades = inventory.AddGrenades(rewardGrenades);
            }
            else
            {
                leftoverAmmo = inventory.AddAmmo(fallbackAmmoType, fallbackAmmo);
            }

            if (dropped != null || leftoverAmmo > 0 || leftoverGrenades > 0)
                LootPile.At(transform).Add(dropped, leftoverType, leftoverAmmo, leftoverGrenades);

            // Đã mở: tối màu đi (Khôi thay bằng hoạt ảnh mở nắp).
            if (TryGetComponent<Renderer>(out var r)) r.material.color = r.material.color * 0.4f;
        }
    }
}
