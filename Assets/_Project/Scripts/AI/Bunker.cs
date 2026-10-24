using System.Collections;
using DBP.Core;
using UnityEngine;

namespace DBP.AI
{
    /// Lô cốt (Bunker / Pillbox) — T15 – Đạt; BR-20:
    ///   • Quét hỏa lực theo cung cố định (yawMin → yawMax).
    ///   • Miễn nhiễm hoàn toàn với đạn súng cá nhân (Bullet); chỉ bị phá bằng Explosive hoặc Artillery.
    ///   • Bộc phá (Satchel): đặt gần, giữ E → kích nổ → gây sát thương Explosive đủ để phá lô cốt.
    ///   • Khi phá xong: ngừng bắn, nhô một lần hiệu ứng nổ, vô hiệu hóa.
    [RequireComponent(typeof(Health))]
    public class Bunker : MonoBehaviour
    {
        // ── Cài đặt ────────────────────────────────────────────────────────────────
        [Header("Hỏa lực")]
        [Tooltip("Điểm bắn (nòng súng máy).")]
        [SerializeField] Transform muzzle;
        [Tooltip("Góc yaw trái → phải tính từ forward của Bunker (độ).")]
        [SerializeField] float sweepHalfAngle = 45f;   // quét ±45° quanh forward
        [SerializeField] float sweepSpeed = 20f;        // độ/giây
        [SerializeField] float fireInterval = 0.15f;   // giây/phát
        [SerializeField] float fireDamage = 20f;
        [SerializeField] float fireRange = 60f;
        [SerializeField] LayerMask fireMask = ~0;

        [Header("Bộc phá")]
        [Tooltip("Tầm phát hiện bộc phá đã đặt (m). SatchelCharge phải ở trong tầm này.")]
        [SerializeField] float satchelDetectRadius = 5f;
        [Tooltip("Thời gian phát nổ bộc phá sau khi đặt (giây). Người chơi đặt xong → Bunker phát nổ tự động.")]
        [SerializeField] float satchelDelay = 3f;

        [Header("VFX / SFX")]
        [SerializeField] GameObject muzzleFlashPrefab; // T67 – Khôi gán
        [SerializeField] GameObject destructionPrefab; // VFX nổ khi phá xong
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip fireClip;
        [SerializeField] AudioClip destructionClip;

        [Header("Renderer để tắt khi phá")]
        [SerializeField] GameObject[] toDisableOnDestroyed;

        // ── Runtime ───────────────────────────────────────────────────────────────
        Health health;
        Transform player;
        bool destroyed;
        float sweepAngle;   // angle hiện tại trong cung [-half, +half]
        int sweepDir = 1;
        float nextFireAt;

        // ── Khởi tạo ──────────────────────────────────────────────────────────────
        void Awake()
        {
            health = GetComponent<Health>();
            // Lô cốt miễn với Bullet (BR-20)
            health.Configure(TargetKind.Bunker, health.Max, DamageType.Bullet);
        }

        void OnEnable()  => health.Died += OnDestroyed;
        void OnDisable() => health.Died -= OnDestroyed;

        void Start()
        {
            var pg = GameObject.FindGameObjectWithTag("Player");
            if (pg) player = pg.transform;
        }

        // ── Vòng lặp ──────────────────────────────────────────────────────────────
        void Update()
        {
            if (destroyed) return;
            Sweep();
            TryFire();
        }

        // ── Quét hỏa lực ──────────────────────────────────────────────────────────
        void Sweep()
        {
            sweepAngle += sweepDir * sweepSpeed * Time.deltaTime;
            if (sweepAngle > sweepHalfAngle)  { sweepAngle = sweepHalfAngle;  sweepDir = -1; }
            if (sweepAngle < -sweepHalfAngle) { sweepAngle = -sweepHalfAngle; sweepDir =  1; }

            if (muzzle)
                muzzle.localRotation = Quaternion.Euler(0f, sweepAngle, 0f);
        }

        // ── Bắn ───────────────────────────────────────────────────────────────────
        void TryFire()
        {
            if (!muzzle || Time.time < nextFireAt) return;

            var dir = muzzle.forward;
            if (muzzleFlashPrefab) Destroy(Instantiate(muzzleFlashPrefab, muzzle.position, muzzle.rotation), .1f);
            else CombatEffects.Muzzle(muzzle.position, muzzle.rotation, 1.3f);
            bool struck = Physics.Raycast(muzzle.position, dir, out var hit, fireRange, fireMask, QueryTriggerInteraction.Ignore);
            CombatEffects.Tracer(muzzle.position, struck ? hit.point : muzzle.position + dir * (fireRange), true);
            if (struck)
            {
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                dmg?.TakeDamage(fireDamage, DamageType.Bullet, hit.point);

                CombatEffects.Impact(hit.point, hit.normal, CombatEffects.IsMetal(hit.collider));
            }

            if (audioSource && fireClip) audioSource.PlayOneShot(fireClip);
            nextFireAt = Time.time + fireInterval;
        }

        // ── Bộc phá ───────────────────────────────────────────────────────────────
        /// Gọi bởi SatchelCharge khi người chơi kích nổ thành công.
        public void OnSatchelPlanted()
        {
            if (destroyed) return;
            StartCoroutine(SatchelDelay());
        }

        IEnumerator SatchelDelay()
        {
            yield return new WaitForSeconds(satchelDelay);
            // Bộc phá gây sát thương Explosive đủ phá lô cốt
            health.TakeDamage(health.Max + 1f, DamageType.Explosive, transform.position);
        }

        // ── Khi bị phá ────────────────────────────────────────────────────────────
        void OnDestroyed()
        {
            if (destroyed) return;
            destroyed = true;

            if (destructionPrefab)
                Destroy(Instantiate(destructionPrefab, transform.position, Quaternion.identity), 5f);
            else CombatEffects.Explosion(transform.position, 2f);

            if (audioSource && destructionClip)
                AudioSource.PlayClipAtPoint(destructionClip, transform.position);
            else CampaignAudio.Effect("explosion_01", transform.position);

            foreach (var go in toDisableOnDestroyed)
                if (go) go.SetActive(false);

            // Vô hiệu hoá collider vật lý (hào vẫn đi được)
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                if (col.isTrigger) continue; // giữ trigger để đếm phá
                col.enabled = false;
            }
        }

        // Hiển thị cung quét trong Editor
        void OnDrawGizmosSelected()
        {
            if (!muzzle) return;
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            var left  = Quaternion.Euler(0f, -sweepHalfAngle, 0f) * muzzle.forward;
            var right = Quaternion.Euler(0f,  sweepHalfAngle, 0f) * muzzle.forward;
            Gizmos.DrawRay(muzzle.position, left  * fireRange);
            Gizmos.DrawRay(muzzle.position, right * fireRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, satchelDetectRadius);
        }
    }
}
