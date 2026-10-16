using System;
using System.Collections;
using System.Collections.Generic;
using DBP.Core;
using UnityEngine;

namespace DBP.AI
{
    /// <summary>
    /// Ổ đề kháng Màn 5 (Task T59):
    /// - Nhóm địch cố thủ tại một cứ điểm, không di chuyển cơ động (không dùng NavMeshAgent).
    /// - Khi người chơi vào tầm engageRange (25m) -> bắt đầu bắn.
    /// - Lính thường trang bị súng: KHÔNG miễn nhiễm đạn (nhận sát thương Bullet bình thường).
    /// - Cơ chế bắn theo loạt: bắn 3 phát liên tiếp -> nghỉ 2 giây -> lặp lại chu kỳ.
    /// - Khi máu <= 0: chạy hoạt ảnh ngã/chết (phối hợp cùng EnemyDeathHandler nếu có) hoặc tự mờ dần (fade out).
    /// - Quản lý danh sách lính hỗ trợ: List&lt;SoldierAI&gt; defenders.
    /// - Ổ đề kháng được coi là bị xóa sổ hoàn toàn (Cleared) khi bản thân ổ súng và tất cả lính defenders bị tiêu diệt.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class ResistanceNest : MonoBehaviour
    {
        public static event Action<ResistanceNest> NestCleared;
        public event Action OnCleared;

        [Header("Tầm chiến đấu")]
        [Tooltip("Cự ly giao chiến (m). Khi người chơi vào tầm này ổ đề kháng sẽ khai hỏa.")]
        [SerializeField] float engageRange = 25f;
        [Tooltip("Nửa góc quét tầm nhìn (độ).")]
        [SerializeField] float sightAngle = 75f;
        [SerializeField] LayerMask sightMask = ~0;

        [Header("Điểm bắn & Tầm nhìn")]
        [Tooltip("Điểm mắt quan sát.")]
        [SerializeField] Transform eyePoint;
        [Tooltip("Nòng súng máy / súng trường.")]
        [SerializeField] Transform muzzle;

        [Header("Hỏa lực bắn theo loạt")]
        [Tooltip("Sát thương mỗi viên đạn.")]
        [SerializeField] float fireDamage = 18f;
        [Tooltip("Độ tản mát của đạn.")]
        [SerializeField] float bulletSpread = 2f;
        [Tooltip("Số phát bắn trong một loạt (mặc định 3 phát).")]
        [SerializeField] int burstShots = 3;
        [Tooltip("Thời gian giãn cách giữa các phát trong cùng một loạt (giây).")]
        [SerializeField] float shotInterval = 0.22f;
        [Tooltip("Thời gian nghỉ giữa các loạt bắn (giây).")]
        [SerializeField] float cooldownBetweenBursts = 2.0f;

        [Header("Danh sách lính bảo vệ ổ đề kháng")]
        [Tooltip("Các lính bộ binh phối hợp phòng thủ ổ đề kháng.")]
        [SerializeField] List<SoldierAI> defenders = new List<SoldierAI>();

        [Header("Hiệu ứng & Âm thanh")]
        [SerializeField] GameObject muzzleFlashPrefab;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip fireClip;
        [SerializeField] AudioClip alertClip;
        [SerializeField] AudioClip deathClip;

        [Header("Hoạt ảnh (Animator)")]
        [SerializeField] Animator animator;
        static readonly int AnimShoot = Animator.StringToHash("Shoot");
        static readonly int AnimDead  = Animator.StringToHash("Dead");

        // ── Runtime variables ───────────────────────────────────────────────────
        Health health;
        Transform player;
        bool isFiringBurst;
        bool dead;
        bool clearedNotified;
        float nextBurstAllowedTime;

        public bool IsNestDead => dead || (health != null && health.IsDead);
        public IReadOnlyList<SoldierAI> Defenders => defenders;

        /// <summary>
        /// Ổ đề kháng bị xóa sổ khi cả hỏa điểm chính lẫn toàn bộ lính defenders đều bị hạ.
        /// </summary>
        public bool IsCleared
        {
            get
            {
                if (!IsNestDead) return false;
                return AreAllDefendersDown();
            }
        }

        void Awake()
        {
            health = GetComponent<Health>();
            if (!eyePoint) eyePoint = transform;
            if (!muzzle)   muzzle   = eyePoint;

            // Ổ đề kháng là lính thường: nhận sát thương đạn bình thường (không miễn Bullet)
            health.Configure(TargetKind.Infantry, health.Max);
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died    += OnDied;
            Health.AnyDied += OnAnyGlobalDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died    -= OnDied;
            Health.AnyDied -= OnAnyGlobalDied;
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        void Update()
        {
            if (dead || player == null) return;

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= engageRange && CanSeePlayer())
            {
                FaceTarget(player.position);

                if (!isFiringBurst && Time.time >= nextBurstAllowedTime)
                {
                    StartCoroutine(FireBurstRoutine());
                }
            }
        }

        // ── Chuỗi bắn theo loạt (3 phát liên tiếp -> nghỉ 2s) ───────────────────
        IEnumerator FireBurstRoutine()
        {
            isFiringBurst = true;

            for (int i = 0; i < burstShots; i++)
            {
                if (dead || player == null) break;

                // Bắn 1 phát
                ShootSingleBullet();

                // Chờ trước phát kế tiếp trong loạt
                yield return new WaitForSeconds(shotInterval);
            }

            // Nghỉ 2 giây trước khi cho phép loạt tiếp theo
            nextBurstAllowedTime = Time.time + cooldownBetweenBursts;
            isFiringBurst = false;
        }

        void ShootSingleBullet()
        {
            if (!muzzle || player == null) return;

            if (animator) animator.SetTrigger(AnimShoot);
            PlaySound(fireClip);

            if (muzzleFlashPrefab)
            {
                var flash = Instantiate(muzzleFlashPrefab, muzzle.position, muzzle.rotation);
                Destroy(flash, 0.1f);
            }

            // Raycast bắn trúng mục tiêu với độ lệch tản mát
            var spread = UnityEngine.Random.insideUnitCircle * bulletSpread;
            var targetPos = player.position + Vector3.up * 0.8f;
            var dir = (targetPos - muzzle.position).normalized;
            dir = Quaternion.Euler(spread.x, spread.y, 0f) * dir;

            if (Physics.Raycast(muzzle.position, dir, out var hit, engageRange + 5f, sightMask, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponentInParent<IDamageable>();
                target?.TakeDamage(fireDamage, DamageType.Bullet, hit.point);
            }
        }

        // ── Kiểm tra điều kiện phòng thủ ───────────────────────────────────────
        public bool AreAllDefendersDown()
        {
            if (defenders == null || defenders.Count == 0) return true;

            foreach (var d in defenders)
            {
                if (d == null) continue;
                var h = d.GetComponent<Health>();
                if (h != null && !h.Neutralized) return false;
            }

            return true;
        }

        void CheckClearCondition()
        {
            if (clearedNotified) return;

            if (IsCleared)
            {
                clearedNotified = true;
                OnCleared?.Invoke();
                NestCleared?.Invoke(this);
            }
        }

        void OnAnyGlobalDied(Health _)
        {
            CheckClearCondition();
        }

        // ── Sự kiện nhận sát thương & Chết ─────────────────────────────────────
        void OnDamaged(float _)
        {
            // Có thể thêm âm thanh rên la hoặc báo động cho defenders nếu cần
        }

        void OnDied()
        {
            if (dead) return;
            dead = true;
            StopAllCoroutines();

            PlaySound(deathClip);

            // Kiểm tra xem đã có EnemyDeathHandler xử lý chưa
            var deathHandler = GetComponent<EnemyDeathHandler>();
            if (deathHandler == null)
            {
                // Tự chạy hoạt ảnh chết và làm mờ dần nếu không gắn EnemyDeathHandler
                if (animator) animator.SetTrigger(AnimDead);

                foreach (var col in GetComponentsInChildren<Collider>())
                    col.enabled = false;

                StartCoroutine(FadeOutAndDestroy());
            }

            CheckClearCondition();
        }

        IEnumerator FadeOutAndDestroy()
        {
            yield return new WaitForSeconds(2f);

            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                foreach (var mat in rend.materials)
                {
                    mat.SetFloat("_Mode", 3f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                }
            }

            float elapsed = 0f;
            float duration = 1.5f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / duration);
                foreach (var rend in renderers)
                {
                    foreach (var mat in rend.materials)
                    {
                        var c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                }
                yield return null;
            }

            Destroy(gameObject);
        }

        // ── Tiện ích ──────────────────────────────────────────────────────────
        bool CanSeePlayer()
        {
            if (!player || !eyePoint) return false;
            var toPlayer = player.position + Vector3.up * 0.8f - eyePoint.position;
            float dist = toPlayer.magnitude;
            if (dist > engageRange) return false;
            if (Vector3.Angle(eyePoint.forward, toPlayer) > sightAngle) return false;
            return !Physics.Raycast(eyePoint.position, toPlayer.normalized, dist, sightMask, QueryTriggerInteraction.Ignore);
        }

        void FaceTarget(Vector3 target)
        {
            var dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
        }

        void PlaySound(AudioClip clip)
        {
            if (audioSource && clip) audioSource.PlayOneShot(clip);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, engageRange);

            if (defenders != null)
            {
                Gizmos.color = Color.magenta;
                foreach (var d in defenders)
                {
                    if (d) Gizmos.DrawLine(transform.position, d.transform.position);
                }
            }
        }
    }
}
