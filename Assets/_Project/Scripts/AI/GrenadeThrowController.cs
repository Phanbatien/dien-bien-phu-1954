using UnityEngine;
using UnityEngine.InputSystem;
using DBP.Core;
using DBP.Weapons;

namespace DBP.AI
{
    /// Ném lựu đạn (T14 – Đạt): nhấn G → giơ tay 0.5 s → ném.
    /// Gắn cùng WeaponController / Interactor trên nhân vật người chơi.
    public class GrenadeThrowController : MonoBehaviour
    {
        [SerializeField] Grenade grenadePrefab;
        public Grenade Prefab => grenadePrefab;
        [Tooltip("Điểm xuất phát lựu đạn (tay phải của nhân vật).")]
        [SerializeField] Transform throwPoint;
        [SerializeField] Camera cam;
        [SerializeField] float throwForce = 12f;
        [SerializeField] float upwardAngle = 20f;   // độ ngóc lên so với tầm nhìn

        [Header("Kho lựu đạn")]
        [Tooltip("Lấy từ Inventory (BR-36). Nếu không có Inventory thì dùng trực tiếp.")]
        [SerializeField] int debugGrenadeCount = 2;  // chỉ dùng khi không có Inventory

        Inventory inventory; // null nếu không tìm thấy

        void Awake()
        {
            if (!cam) cam = Camera.main;
            // Inventory là lớp C# thường nằm trong WeaponController, không phải component.
            var weapons = GetComponentInParent<WeaponController>();
            if (!weapons) weapons = GetComponentInChildren<WeaponController>();
            inventory = weapons != null ? weapons.Inventory : null;
        }

        void Update()
        {
            if (InputLock.Locked) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (!keyboard.gKey.wasPressedThisFrame) return;

            TryThrow();
        }

        void TryThrow()
        {
            if (!grenadePrefab) return;

            // Kiểm tra số lựu đạn
            int count = inventory != null ? inventory.Grenades : debugGrenadeCount;
            if (count <= 0)
            {
                Debug.Log("[Grenade] Không còn lựu đạn.");
                return;
            }

            if (inventory != null) inventory.UseGrenade();
            else debugGrenadeCount--;
            var body = GetComponentInParent<DBP.Player.FirstPersonController>();
            if (body) EnemyAnimatorDriver.Play(body, "Throw");

            // Điểm và hướng ném
            var origin = throwPoint ? throwPoint.position : cam.transform.position + cam.transform.forward * 0.4f;
            var aimDir = cam.transform.forward;
            var throwDir = Quaternion.Euler(-upwardAngle, 0f, 0f) * aimDir;
            throwDir.Normalize();

            var g = Instantiate(grenadePrefab, origin, Quaternion.LookRotation(throwDir));
            if (g.TryGetComponent<Rigidbody>(out var rb))
                rb.AddForce(throwDir * throwForce, ForceMode.Impulse);
        }
    }
}
