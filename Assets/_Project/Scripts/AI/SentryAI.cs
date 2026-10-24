using DBP.Core;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// <summary>
    /// Lính gác (T39, BR-20):
    /// - Chu trình: Patrol (tuần tra) → Suspicious (nghi ngờ) → Alert (báo động).
    /// - Khi phát hiện hoặc nghi ngờ, dừng lại nhìn về phía nguồn âm thanh/hình ảnh 2s.
    /// - Khi báo động: đánh thức CounterAttackGroup trong phạm vi 30m và chuyển sang chiến đấu.
    /// </summary>
    public class SentryAI : MonoBehaviour
    {
        public enum State { Patrol, Suspicious, Alert }

        [Header("Trạng thái hiện tại")]
        [SerializeField] State currentState = State.Patrol;

        [Header("Tuần tra")]
        [Tooltip("Các mốc tuần tra theo chu trình kín.")]
        [SerializeField] Transform[] waypoints;
        [SerializeField] float patrolSpeed = 2.2f;
        [SerializeField] float patrolWaitSec = 2f;

        [Header("Tầm phát hiện")]
        [Tooltip("Bán kính nghe tiếng súng của người chơi (m).")]
        [SerializeField] float hearRange = 20f;
        [Tooltip("Tầm nhìn tối đa (m).")]
        [SerializeField] float sightRange = 15f;
        [Tooltip("Nửa góc nón tầm nhìn (°).")]
        [SerializeField] float sightAngle = 60f;
        [Tooltip("Điểm đặt mắt (mặc định lấy transform).")]
        [SerializeField] Transform eyePoint;
        [SerializeField] LayerMask sightMask = ~0;

        [Header("Nghi ngờ")]
        [Tooltip("Thời gian dừng quan sát khi nghe tiếng hoặc thoáng thấy người (giây).")]
        [SerializeField] float suspiciousDuration = 2f;

        [Header("Báo động")]
        [Tooltip("Bán kính gọi các nhóm phản kích hỗ trợ (m).")]
        [SerializeField] float alertNearbyRadius = 30f;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip alertClip;

        [Header("Chiến đấu dự phòng (khi không kèm SoldierAI)")]
        [SerializeField] Transform muzzle;
        [SerializeField] float fireDamage = 15f;
        [SerializeField] float fireInterval = 1.8f;
        [SerializeField] float fireRange = 30f;
        [SerializeField] AudioClip fireClip;

        Health health;
        NavMeshAgent agent;
        SoldierAI soldierAI;
        Animator animator;
        Transform player;

        int waypointIndex;
        float waypointWaitEndsAt;
        Vector3 suspiciousLookPos;
        float suspiciousTimer;
        float nextFireAt;

        static readonly int AnimMove = Animator.StringToHash("Speed");
        static readonly int AnimShoot = Animator.StringToHash("Shoot");

        public State CurrentState => currentState;
        public float HearRange => hearRange;
        public float SightRange => sightRange;
        public float SightAngle => sightAngle;

        void Awake()
        {
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            soldierAI = GetComponent<SoldierAI>();
            animator = GetComponentInChildren<Animator>();

            if (!eyePoint) eyePoint = transform;
            if (!muzzle) muzzle = eyePoint;
        }

        void Start()
        {
            var pGo = GameObject.FindGameObjectWithTag("Player");
            if (pGo) player = pGo.transform;

            currentState = State.Patrol;
            if (agent != null && agent.enabled)
            {
                agent.speed = patrolSpeed;
            }
        }

        void Update()
        {
            if (health != null && health.IsDead) return;

            switch (currentState)
            {
                case State.Patrol:
                    TickPatrol();
                    break;
                case State.Suspicious:
                    TickSuspicious();
                    break;
                case State.Alert:
                    TickAlertCombat();
                    break;
            }

            if (animator && agent && agent.enabled)
            {
                animator.SetFloat(AnimMove, agent.velocity.magnitude);
            }
        }

        // ── 1. Tuần tra ───────────────────────────────────────────────────────────
        void TickPatrol()
        {
            // Kiểm tra nhìn thấy người chơi
            if (CanSeePlayer())
            {
                BecomeSuspicious(player.position);
                return;
            }

            // Đi theo các waypoints tuần tra
            if (waypoints != null && waypoints.Length > 0 && agent != null && agent.enabled)
            {
                if (!agent.hasPath || agent.remainingDistance < 0.5f)
                {
                    if (Time.time >= waypointWaitEndsAt)
                    {
                        if (waypoints[waypointIndex] != null)
                        {
                            agent.isStopped = false;
                            agent.speed = patrolSpeed;
                            agent.SetDestination(waypoints[waypointIndex].position);
                        }
                        waypointIndex = (waypointIndex + 1) % waypoints.Length;
                        waypointWaitEndsAt = float.MaxValue;
                    }
                    else if (waypointWaitEndsAt == float.MaxValue)
                    {
                        waypointWaitEndsAt = Time.time + patrolWaitSec;
                        agent.ResetPath();
                    }
                }
            }
        }

        // ── 2. Nghi ngờ ───────────────────────────────────────────────────────────
        void TickSuspicious()
        {
            // Dừng lại và quay mặt về phía nghi ngờ
            if (agent != null && agent.enabled)
            {
                agent.ResetPath();
            }

            FaceTarget(suspiciousLookPos);

            suspiciousTimer -= Time.deltaTime;
            if (suspiciousTimer <= 0f)
            {
                // Sau 2s kiểm tra lại: nếu vẫn thấy người → Alert; nếu không → về Patrol
                if (CanSeePlayer())
                {
                    EnterAlert();
                }
                else
                {
                    currentState = State.Patrol;
                    waypointWaitEndsAt = 0f; // Tiếp tục di chuyển ngay
                }
            }
        }

        // ── 3. Báo động & Chiến đấu ───────────────────────────────────────────────
        void EnterAlert()
        {
            currentState = State.Alert;

            if (audioSource && alertClip)
            {
                audioSource.PlayOneShot(alertClip);
            }

            // Báo động cho các nhóm xung quanh
            AlertNearby();

            // Nếu có SoldierAI trên cùng đối tượng, chuyển giao quyền điều khiển
            if (soldierAI != null)
            {
                soldierAI.enabled = true;
                soldierAI.SetState(SoldierAI.State.Shoot);
                // Vô hiệu hóa SentryAI để tránh xung đột vòng lặp Update
                enabled = false;
            }
        }

        void TickAlertCombat()
        {
            // Chiến đấu dự phòng nếu không gắn SoldierAI
            if (!player) return;

            FaceTarget(player.position);

            if (Time.time >= nextFireAt && CanSeePlayer())
            {
                nextFireAt = Time.time + fireInterval;
                TryFire();
            }
        }

        void TryFire()
        {
            if (!muzzle || !player) return;

            if (animator) animator.SetTrigger(AnimShoot);
            if (audioSource && fireClip) audioSource.PlayOneShot(fireClip);

            var dir = (player.position + Vector3.up * 0.8f - muzzle.position).normalized;
            bool struck = Physics.Raycast(muzzle.position, dir, out var hit, fireRange, sightMask, QueryTriggerInteraction.Ignore);
            CombatEffects.Tracer(muzzle.position, struck ? hit.point : muzzle.position + dir * (fireRange), true);
            if (struck)
            {
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                dmg?.TakeDamage(fireDamage, DamageType.Bullet, hit.point);
            }
        }

        // ── Kích hoạt nghi ngờ & báo động ─────────────────────────────────────────
        public void OnHeardShot(Vector3 origin)
        {
            if (health != null && health.IsDead) return;

            if (currentState == State.Patrol)
            {
                if (Vector3.Distance(transform.position, origin) <= hearRange)
                {
                    BecomeSuspicious(origin);
                }
            }
        }

        void BecomeSuspicious(Vector3 targetPos)
        {
            currentState = State.Suspicious;
            suspiciousLookPos = targetPos;
            suspiciousTimer = suspiciousDuration;

            if (agent != null && agent.enabled)
            {
                agent.ResetPath();
            }
        }

        /// <summary>
        /// Tìm tất cả CounterAttackGroup trong 30m và gọi Activate() (BR-20).
        /// Đồng thời báo động các SoldierAI lân cận.
        /// </summary>
        public void AlertNearby()
        {
            // Tìm và kích hoạt CounterAttackGroup trong bán kính 30m
            var groups = FindObjectsOfType<CounterAttackGroup>();
            foreach (var grp in groups)
            {
                if (grp != null && Vector3.Distance(transform.position, grp.transform.position) <= alertNearbyRadius)
                {
                    grp.Activate();
                }
            }

            // Đánh thức lính lân cận trong 30m
            var soldiers = FindObjectsOfType<SoldierAI>();
            foreach (var s in soldiers)
            {
                if (s != null && s.gameObject != gameObject && Vector3.Distance(transform.position, s.transform.position) <= alertNearbyRadius)
                {
                    s.OnHeardShot(transform.position);
                }
            }
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
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
        }

        // ── Gizmos ────────────────────────────────────────────────────────────────
        void OnDrawGizmos()
        {
            DrawGizmosInternal();
        }

        void DrawGizmosInternal()
        {
            Vector3 origin = eyePoint ? eyePoint.position : transform.position + Vector3.up * 1.5f;

            // 1. Arc tầm nhìn (màu vàng)
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.8f);
            Vector3 forward = transform.forward;
            Vector3 leftDir = Quaternion.Euler(0, -sightAngle, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, sightAngle, 0) * forward;

            Gizmos.DrawRay(origin, leftDir * sightRange);
            Gizmos.DrawRay(origin, rightDir * sightRange);

            int segments = 20;
            Vector3 prevPoint = origin + leftDir * sightRange;
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-sightAngle, sightAngle, (float)i / segments);
                Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;
                Vector3 nextPoint = origin + dir * sightRange;
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }

            // 2. Vòng tròn tầm nghe (màu xanh dương nhạt)
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, hearRange);

            // 3. Đường tuần tra
            if (waypoints != null && waypoints.Length > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] == null) continue;
                    Gizmos.DrawSphere(waypoints[i].position, 0.3f);
                    int next = (i + 1) % waypoints.Length;
                    if (waypoints[next] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
                    }
                }
            }
        }
    }
}
