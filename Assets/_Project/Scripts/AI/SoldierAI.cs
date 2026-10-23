using System.Collections;
using DBP.Core;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// AI lính sau bao cát (T07 – Đạt; BR-20):
    ///   Idle → Alerted (phát hiện tiếng động/tầm nhìn) → Shoot (bắn trả) → Suppress (nấp khi bị áp chế).
    /// Ba trạng thái kiểm tra được bằng clip quay 30 giây:
    ///   1. Đứng sau bao cát, lộ đầu bắn trả người chơi.
    ///   2. Khi bị bắn (trúng đạn gần) → nấp thấp xuống dưới bao cát.
    ///   3. Sau ~2 giây không bị bắn → ngóc đầu lên bắn lại.
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class SoldierAI : MonoBehaviour
    {
        public enum State { Idle, Alerted, Shoot, Suppress }

        // ── Cài đặt ────────────────────────────────────────────────────────────────
        [Header("Nhân vật")]
        [Tooltip("Điểm quét mục tiêu (mắt lính).")]
        [SerializeField] Transform eyePoint;
        [Tooltip("Điểm bắn đạn (nòng súng).")]
        [SerializeField] Transform muzzle;

        [Header("Tầm nhìn")]
        [SerializeField] float sightRange = 30f;
        [SerializeField] float sightAngle = 80f;   // nửa góc (°)
        [SerializeField] LayerMask sightMask = ~0;

        [Header("Chiến đấu")]
        [SerializeField] float fireDamage = 15f;
        [SerializeField] float fireInterval = 1.8f; // giây/phát (lính bắn chậm)
        [SerializeField] float bulletSpread = 3f;   // độ lệch tối đa
        [SerializeField] float fireRange = 40f;

        [Header("Bao cát / Nấp")]
        [Tooltip("Cao (đứng) → thấp (nấp). AI hạ thấp CharacterController height.")]
        [SerializeField] float standHeight = 1.8f;
        [SerializeField] float crouchHeight = 1.0f;
        [SerializeField] float suppressDuration = 2.5f;  // giây nấp sau khi bị bắn

        [Header("Tuần tra (Lính gác T39)")]
        [SerializeField] Transform[] patrolPoints;
        [SerializeField] float patrolWaitSec = 2f;

        [Header("Nghe")]
        [SerializeField] float hearRange = 15f;

        [Header("Âm thanh")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip fireClip;
        [SerializeField] AudioClip alertClip;
        [SerializeField] AudioClip deathClip;

        [Header("Hoạt ảnh")]
        [SerializeField] Animator animator;
        static readonly int AnimShoot     = Animator.StringToHash("Shoot");
        static readonly int AnimCrouch    = Animator.StringToHash("Crouch");
        static readonly int AnimDead      = Animator.StringToHash("Dead");
        static readonly int AnimMove      = Animator.StringToHash("Speed");

        // ── Trạng thái runtime ─────────────────────────────────────────────────────
        Health health;
        NavMeshAgent agent;
        Transform player;

        State state = State.Idle;
        float nextFireAt;
        float suppressEndsAt;
        int patrolIndex;
        float patrolWaitEndsAt;
        bool dead;

        public Health Health => health;
        public NavMeshAgent Agent => agent;
        public State CurrentState => state;
        public bool IsDead => dead;
        public bool IsCharging { get; set; }

        // ── Khởi tạo ──────────────────────────────────────────────────────────────
        void Awake()
        {
            health = GetComponent<Health>();
            agent  = GetComponent<NavMeshAgent>();
            if (!eyePoint) eyePoint = transform;
            if (!muzzle)   muzzle   = eyePoint;
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died    += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died    -= OnDied;
        }

        void Start()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo) player = playerGo.transform;
            agent.height = standHeight;
            SetState(State.Idle);
        }

        // ── Vòng lặp ──────────────────────────────────────────────────────────────
        void Update()
        {
            if (dead) return;

            switch (state)
            {
                case State.Idle:     TickIdle();     break;
                case State.Alerted:  TickAlerted();  break;
                case State.Shoot:    TickShoot();    break;
                case State.Suppress: TickSuppress(); break;
            }

            if (animator) animator.SetFloat(AnimMove, agent.velocity.magnitude);
        }

        // ── Idle: tuần tra (nếu có điểm tuần tra) hoặc đứng chờ ──────────────────
        void TickIdle()
        {
            var sentry = GetComponent<SentryAI>();
            if (sentry != null && sentry.enabled) return;

            if (CanSeePlayer())
            {
                Alert();
                return;
            }

            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                if (!agent.hasPath || agent.remainingDistance < 0.5f)
                {
                    if (Time.time >= patrolWaitEndsAt)
                    {
                        agent.SetDestination(patrolPoints[patrolIndex].position);
                        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                        patrolWaitEndsAt = float.MaxValue;
                    }
                }
                else if (agent.remainingDistance < 0.5f)
                {
                    patrolWaitEndsAt = Time.time + patrolWaitSec;
                    agent.ResetPath();
                }
            }
        }

        // ── Alerted: tiến về vị trí bắn gần nhất rồi chuyển Shoot ────────────────
        void TickAlerted()
        {
            if (!player) return;
            float dist = Vector3.Distance(transform.position, player.position);
            float stopDist = IsCharging ? 6f : fireRange;
            if (dist <= stopDist)
            {
                SetState(State.Shoot);
                return;
            }
            agent.SetDestination(player.position);
        }

        // ── Shoot: đứng/ngồi sau bao cát, bắn định kỳ ────────────────────────────
        void TickShoot()
        {
            if (health != null && health.Retreated) return;
            if (!player) return;

            if (!IsCharging)
            {
                agent.ResetPath();
            }
            else
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist > 6f)
                    agent.SetDestination(player.position);
                else
                    agent.ResetPath();
            }

            FaceTarget(player.position);

            if (Time.time >= nextFireAt)
            {
                TryFire();
                nextFireAt = Time.time + fireInterval;
            }

            // Mất tầm nhìn quá lâu → về Idle
            if (!CanSeePlayer() && Time.time > nextFireAt + 3f)
                SetState(State.Idle);
        }

        // ── Suppress: nấp thấp, không bắn ────────────────────────────────────────
        void TickSuppress()
        {
            if (health != null && health.Retreated) return;
            if (Time.time >= suppressEndsAt)
            {
                SetState(State.Shoot);
            }
        }

        // ── Bắn ───────────────────────────────────────────────────────────────────
        void TryFire()
        {
            if (!muzzle || !player) return;

            if (animator) animator.SetTrigger(AnimShoot);
            Play(fireClip);
            CombatEffects.Muzzle(muzzle.position, muzzle.rotation);

            // Tia raycast với độ lệch ngẫu nhiên nhỏ (lính bắn không chuẩn)
            var spread = Random.insideUnitCircle * bulletSpread;
            var dir = (player.position + Vector3.up * 0.8f - muzzle.position).normalized;
            dir = Quaternion.Euler(spread.x, spread.y, 0f) * dir;

            if (Physics.Raycast(muzzle.position, dir, out var hit, fireRange, sightMask, QueryTriggerInteraction.Ignore))
            {
                CombatEffects.Impact(hit.point, hit.normal, CombatEffects.IsMetal(hit.collider));
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                dmg?.TakeDamage(fireDamage, DamageType.Bullet, hit.point);
            }
        }

        // ── Chuyển trạng thái ─────────────────────────────────────────────────────
        public void SetState(State next)
        {
            state = next;
            bool crouching = next == State.Suppress;
            agent.height = crouching ? crouchHeight : standHeight;
            if (animator) animator.SetBool(AnimCrouch, crouching);
        }

        void Alert()
        {
            Play(alertClip);
            SetState(State.Alerted);
        }

        // ── Sự kiện ───────────────────────────────────────────────────────────────
        /// Khi trúng đạn (bị áp chế): nấp ngay (BR-20).
        void OnDamaged(float _)
        {
            if (dead || state == State.Suppress) return;
            suppressEndsAt = Time.time + suppressDuration;
            SetState(State.Suppress);
        }

        /// Chết: ngã, mờ dần (T08; BR-21).
        void OnDied()
        {
            dead = true;
            agent.enabled = false;
            Play(deathClip);
            if (animator) animator.SetTrigger(AnimDead);

            // Tắt va chạm để không chặn đường
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            yield return new WaitForSeconds(2f); // chờ hoạt ảnh ngã

            var renderers = GetComponentsInChildren<Renderer>();
            float elapsed = 0f, fadeDuration = 1.5f;
            // Thu thập MaterialPropertyBlock hoặc Materials để fade alpha
            foreach (var rend in renderers)
            {
                foreach (var mat in rend.materials)
                {
                    // Chuyển sang Transparent nếu dùng Standard Shader
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

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - elapsed / fadeDuration;
                foreach (var rend in renderers)
                    foreach (var mat in rend.materials)
                    {
                        var c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                yield return null;
            }
            Destroy(gameObject);
        }

        // ── Tiện ích ──────────────────────────────────────────────────────────────
        bool CanSeePlayer()
        {
            if (!player || !eyePoint) return false;
            var toPlayer = player.position + Vector3.up * 0.8f - eyePoint.position;
            float dist = toPlayer.magnitude;
            if (dist > sightRange) return false;
            if (Vector3.Angle(eyePoint.forward, toPlayer) > sightAngle) return false;
            return !Physics.Raycast(eyePoint.position, toPlayer.normalized, dist, sightMask, QueryTriggerInteraction.Ignore);
        }

        void FaceTarget(Vector3 target)
        {
            var dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
        }

        void Play(AudioClip clip)
        {
            if (audioSource && clip) audioSource.PlayOneShot(clip);
        }

        // Lắng nghe tiếng súng của người chơi trong tầm nghe
        public void OnHeardShot(Vector3 origin)
        {
            var sentry = GetComponent<SentryAI>();
            if (sentry != null && sentry.enabled)
            {
                sentry.OnHeardShot(origin);
                return;
            }

            if (dead || state != State.Idle) return;
            if (Vector3.Distance(transform.position, origin) <= hearRange)
                Alert();
        }
    }
}
