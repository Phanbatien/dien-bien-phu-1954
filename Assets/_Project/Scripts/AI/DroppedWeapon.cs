using DBP.Interaction;
using DBP.Quiz;
using DBP.Weapons;
using UnityEngine;

namespace DBP.AI
{
    /// T23 (BR-08, BR-14, BR-16): Súng rơi từ lính địch.
    /// Nằm trên chiến trường, người chơi giữ E 1 giây để mở câu hỏi.
    /// Trả lời đúng: nhận súng (hoặc đổi thành đạn nếu trùng); súng cũ rơi ra tại chỗ (LootPile).
    /// Trả lời sai/hết giờ: chỉ nhận lượng đạn fallback.
    /// Sau khi tương tác xong: súng biến mất (tắt Renderer và Collider).
    /// Lưu ý BR-16: Súng rơi không tính vào InsuranceTracker.
    public class DroppedWeapon : MonoBehaviour, IInteractable
    {
        [Header("Thông tin súng rơi")]
        [SerializeField] string weaponId = "mat49";
        [SerializeField] Rarity rarity = Rarity.Common;
        [SerializeField] int rewardAmmo = 15;

        [Header("Phần thưởng khi sai / hết giờ")]
        [SerializeField] string fallbackAmmoType = "7.62x54R";
        [SerializeField] int fallbackAmmo = 10;

        bool used;
        bool locked;
        static readonly System.Random Rng = new System.Random();

        public string WeaponId => weaponId;
        public Rarity Rarity => rarity;
        public int RewardAmmo => rewardAmmo;
        public bool Used => used;

        public string Prompt => $"Giữ E: nhặt súng {weaponId} ({rarity})";
        public float HoldSeconds => 1f;

        void Start()
        {
            ApplyColor();
        }

        public void Configure(string id, Rarity r, int ammo, string fallbackType, int fallbackCount)
        {
            weaponId = id;
            rarity = r;
            rewardAmmo = ammo;
            fallbackAmmoType = fallbackType;
            fallbackAmmo = fallbackCount;
            ApplyColor();
        }

        public bool CanInteract(Interactor who)
        {
            var session = FieldQuizSession.Instance;
            return !used && !locked && who != null && who.Weapons != null && session && !session.IsOpen;
        }

        public void Interact(Interactor who)
        {
            var session = FieldQuizSession.Instance;
            if (!session) return;

            int level = RarityRules.Level(rarity, Rng);
            float limit = RarityRules.TimeLimit(rarity);

            if (!session.TryOpen(level, limit, r => Resolve(r, who)))
            {
                if (!session.IsOpen) locked = true; // Thiếu câu: khóa lại trong lượt (BR-31)
                return;
            }
            used = true;
        }

        void Resolve(QuizResult result, Interactor who)
        {
            var inventory = who.Weapons.Inventory;
            Weapon dropped = null;
            string leftoverType = fallbackAmmoType;
            int leftoverAmmo = 0;

            if (result.outcome == QuizOutcome.Correct)
            {
                var def = string.IsNullOrEmpty(weaponId) ? null : WeaponCatalog.Get(weaponId);
                if (def != null)
                {
                    var give = inventory.GiveWeapon(def, rewardAmmo);
                    dropped = give.Dropped;
                    leftoverType = def.ammoType;
                    leftoverAmmo = give.LeftoverAmmo;
                }
                else if (rewardAmmo > 0)
                {
                    leftoverType = fallbackAmmoType;
                    leftoverAmmo = inventory.AddAmmo(fallbackAmmoType, rewardAmmo);
                }
            }
            else
            {
                leftoverAmmo = inventory.AddAmmo(fallbackAmmoType, fallbackAmmo);
            }

            if (dropped != null || leftoverAmmo > 0)
            {
                LootPile.At(transform).Add(dropped, leftoverType, leftoverAmmo, 0);
            }

            // Tắt hiển thị và va chạm sau khi đã giải quyết xong
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                // Tránh tắt renderer của LootPile con nếu vừa được spawn
                if (r.GetComponent<LootPile>() == null) r.enabled = false;
            }
            foreach (var c in GetComponentsInChildren<Collider>())
            {
                if (c.GetComponent<LootPile>() == null) c.enabled = false;
            }
        }

        void ApplyColor()
        {
            if (TryGetComponent<Renderer>(out var r))
            {
                r.material.color = RarityRules.Color(rarity);
            }
        }

        /// Tạo một đối tượng súng rơi dạng primitive cube trên chiến trường.
        public static DroppedWeapon Create(Vector3 position, Quaternion rotation, string id, Rarity rarity, int ammo, string fallbackType, int fallbackAmmo)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"DroppedWeapon_{id}";
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = new Vector3(0.5f, 0.2f, 0.2f);
            var dropped = go.AddComponent<DroppedWeapon>();
            dropped.Configure(id, rarity, ammo, fallbackType, fallbackAmmo);
            return dropped;
        }
    }
}
