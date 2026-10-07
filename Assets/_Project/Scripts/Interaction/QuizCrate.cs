using DBP.Quiz;
using DBP.Weapons;
using UnityEngine;

namespace DBP.Interaction
{
    public enum Rarity { Common, Uncommon, Rare, Legendary }

    /// Bảng độ hiếm → cấp câu, thời hạn (BR-15). T22–T23 (Đạt) thêm bảo hiểm 3 lần sai (BR-16).
    public static class RarityRules
    {
        public static int Level(Rarity rarity, System.Random rng) => rarity switch
        {
            Rarity.Common => rng.Next(1, 3),
            Rarity.Uncommon => 3,
            Rarity.Rare => 4,
            _ => 5,
        };

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
    }

    /// Hòm vũ khí: giữ E 1 giây → câu hỏi (T13; BR-08, BR-11, BR-12).
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

        bool used, locked;
        static readonly System.Random Rng = new System.Random();

        public string Prompt => $"Giữ E: mở hòm ({rarity})";
        public float HoldSeconds => 1f;
        public bool Used => used;

        public bool CanInteract(Interactor who)
        {
            var session = FieldQuizSession.Instance;
            return !used && !locked && who.Weapons != null && session && !session.IsOpen;
        }

        public void Interact(Interactor who)
        {
            var session = FieldQuizSession.Instance;
            if (!session.TryOpen(RarityRules.Level(rarity, Rng), RarityRules.TimeLimit(rarity), r => Resolve(r, who)))
            {
                if (!session.IsOpen) locked = true; // thiếu câu: khóa hòm lỗi trong lượt, không phát thưởng miễn phí (BR-31)
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

            // Hòm đã mở: tối màu đi (Khôi thay bằng hoạt ảnh mở nắp).
            if (TryGetComponent<Renderer>(out var r)) r.material.color = r.material.color * 0.4f;
        }
    }
}
