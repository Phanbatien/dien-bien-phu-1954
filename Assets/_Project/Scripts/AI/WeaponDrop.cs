using DBP.Core;
using DBP.Interaction;
using UnityEngine;

namespace DBP.AI
{
    /// T23 (BR-14): Xử lý tỉ lệ rơi súng khi lính địch bị tiêu diệt.
    /// Gắn trên prefab lính (yêu cầu Health).
    /// Khi lính chết, nếu chưa vượt trần rơi súng toàn màn (DropLimitTracker)
    /// và trúng tỉ lệ dropChance, sẽ sinh một DroppedWeapon tại vị trí lính.
    [RequireComponent(typeof(Health))]
    public class WeaponDrop : MonoBehaviour
    {
        [Header("Tỉ lệ & Giới hạn")]
        [Tooltip("Xác suất rơi súng khi lính bị hạ (0–1). Mặc định 0.08 (8%).")]
        [Range(0f, 1f)]
        [SerializeField] float dropChance = 0.08f;

        [Header("Thông tin vũ khí rơi")]
        [Tooltip("ID của vũ khí rơi ra (khớp với weapons.json)")]
        [SerializeField] string weaponId = "mat49";
        [Tooltip("Độ hiếm của súng rơi, ảnh hưởng cấp câu hỏi và thời gian trả lời")]
        [SerializeField] Rarity rarity = Rarity.Common;
        [Tooltip("Số đạn thưởng khi trả lời đúng")]
        [SerializeField] int dropAmmo = 10;

        [Header("Dự phòng khi trả lời sai")]
        [SerializeField] string fallbackAmmoType = "7.62x54R";
        [SerializeField] int fallbackAmmo = 10;

        [Header("Prefab (tùy chọn)")]
        [Tooltip("Prefab súng rơi. Nếu để trống sẽ tự tạo khối đại diện tại chỗ.")]
        [SerializeField] DroppedWeapon dropPrefab;

        Health health;
        bool hasDropped;

        public float DropChance => dropChance;
        public string WeaponId => weaponId;
        public int DropAmmo => dropAmmo;

        void Awake()
        {
            health = GetComponent<Health>();
        }

        void OnEnable()
        {
            if (health) health.Died += OnDied;
        }

        void OnDisable()
        {
            if (health) health.Died -= OnDied;
        }

        void OnDied()
        {
            if (hasDropped) return;
            hasDropped = true;

            // Kiểm tra giới hạn toàn màn (BR-14)
            if (!DropLimitTracker.CanDrop()) return;

            // Kiểm tra xác suất rơi súng
            if (Random.value >= dropChance) return;

            DropLimitTracker.RegisterDrop();
            SpawnDroppedWeapon();
        }

        void SpawnDroppedWeapon()
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.1f;
            Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            if (dropPrefab != null)
            {
                var instance = Instantiate(dropPrefab, spawnPos, spawnRot);
                instance.Configure(weaponId, rarity, dropAmmo, fallbackAmmoType, fallbackAmmo);
            }
            else
            {
                DroppedWeapon.Create(spawnPos, spawnRot, weaponId, rarity, dropAmmo, fallbackAmmoType, fallbackAmmo);
            }
        }
    }
}
