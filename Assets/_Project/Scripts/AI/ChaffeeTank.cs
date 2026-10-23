using System.Collections.Generic;
using DBP.Core;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// <summary>
    /// Xe tăng Chaffee (T45, BR-17, BR-20):
    /// - Di chuyển theo lộ trình cố định (waypoints).
    /// - Miễn nhiễm hoàn toàn với đạn súng cá nhân (DamageType.Bullet); chỉ bị hạ bằng bộc phá, bazooka, pháo (BR-17, BR-20).
    /// - Có bộ binh hộ tống; khi xe tăng bị tiêu diệt, bộ binh vẫn tiếp tục chiến đấu.
    /// - Tháp pháo quay theo người chơi, bắn đạn nổ định kỳ 3 giây (DamageType.Explosive).
    /// - Khi bị phá hủy: dừng di chuyển, kích hoạt trigger Dead, sinh hiệu ứng destructionPrefab, sau 2 giây Destroy.
    /// - Đảm bảo luôn có ít nhất một điểm cấp vật tư chống tăng hoạt động (BR-17).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class ChaffeeTank : MonoBehaviour
    {
        [Header("Lộ trình cố định")]
        [Tooltip("Danh sách các mốc đường cố định theo thứ tự.")]
        [SerializeField] List<Transform> waypoints = new List<Transform>();
        [SerializeField] float speed = 5f;
        [SerializeField] float rotateSpeed = 60f;
        [SerializeField] bool loopWaypoints = false;

        [Header("Bộ binh hộ tống (BR-20)")]
        [Tooltip("Danh sách lính đi kèm bảo vệ xe tăng. Khi xe tăng bị hạ, lính vẫn tiếp tục tác chiến.")]
        [SerializeField] List<SoldierAI> escorts = new List<SoldierAI>();

        [Header("Tháp pháo & Hỏa lực")]
        [SerializeField] Transform turret;
        [SerializeField] Transform muzzle;
        [SerializeField] float turretRotateSpeed = 45f;
        [SerializeField] float fireInterval = 3f;
        [SerializeField] float fireDamage = 60f;
        [SerializeField] float fireRange = 50f;
        [SerializeField] float explosionRadius = 4f;
        [SerializeField] LayerMask fireMask = ~0;

        [Header("Phương án chống tăng (BR-17)")]
        [Tooltip("Các điểm cấp vật tư chống tăng (bộc phá, đạn chống tăng). Đảm bảo ít nhất 1 điểm hoạt động.")]
        [SerializeField] Transform[] antiTankSupplyPoints;

        [Header("VFX & SFX")]
        [SerializeField] GameObject destructionPrefab;
        [SerializeField] GameObject muzzleFlashPrefab;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip fireClip;
        [SerializeField] AudioClip destructionClip;
        [SerializeField] Animator animator;

        static readonly int AnimDead = Animator.StringToHash("Dead");

        Health health;
        NavMeshAgent agent;
        Transform player;
        int currentWaypointIndex;
        float nextFireAt;
        bool dead;

        public Health Health => health;
        AudioSource engineSound;
        public bool IsDead => dead;
        public IReadOnlyList<SoldierAI> Escorts => escorts;
        public IReadOnlyList<Transform> Waypoints => waypoints;

        void Awake()
        {
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            if (!animator) animator = GetComponentInChildren<Animator>();

            // Xe tăng miễn nhiễm với đạn súng bộ binh (BR-17, BR-20)
            health.Configure(TargetKind.Tank, health.Max, DamageType.Bullet);
        }

        void OnEnable()
        {
            health.Died += OnDied;
            EnsureAntiTankSupply();
        }

        void OnDisable()
        {
            health.Died -= OnDied;
        }

        void Start()
        {
            if (!dead) engineSound = CampaignAudio.AttachLoop("TankEngine", "tank_engine_loop", transform);
            var pGo = GameObject.FindGameObjectWithTag("Player");
            if (pGo) player = pGo.transform;

            EnsureAntiTankSupply();

            if (agent != null && agent.enabled)
            {
                agent.speed = speed;
                agent.angularSpeed = rotateSpeed;
                if (waypoints.Count > 0 && waypoints[0] != null)
                {
                    agent.SetDestination(waypoints[0].position);
                }
            }
        }

        void Update()
        {
            if (dead) return;

            MoveAlongWaypoints();
            AimTurret();

            if (Time.time >= nextFireAt && player != null)
            {
                float distToPlayer = Vector3.Distance(transform.position, player.position);
                if (distToPlayer <= fireRange)
                {
                    nextFireAt = Time.time + fireInterval;
                    FireShell();
                }
            }
        }

        /// <summary>
        /// Kích hoạt xe tăng: bật GameObject và đảm bảo phương án chống tăng (BR-17).
        /// </summary>
        public void Activate()
        {
            gameObject.SetActive(true);
            EnsureAntiTankSupply();
        }

        /// <summary>
        /// Đảm bảo ít nhất một điểm cấp vật tư chống tăng hoạt động (BR-17).
        /// </summary>
        public void EnsureAntiTankSupply()
        {
            if (antiTankSupplyPoints == null || antiTankSupplyPoints.Length == 0) return;

            bool anyActive = false;
            foreach (var pt in antiTankSupplyPoints)
            {
                if (pt != null && pt.gameObject.activeInHierarchy)
                {
                    anyActive = true;
                    break;
                }
            }

            // Nếu chưa có điểm nào mở, bật điểm hợp lệ đầu tiên
            if (!anyActive)
            {
                foreach (var pt in antiTankSupplyPoints)
                {
                    if (pt != null)
                    {
                        pt.gameObject.SetActive(true);
                        Debug.Log($"[ChaffeeTank] Đảm bảo điểm cấp vũ khí chống tăng hoạt động: {pt.name} (BR-17)");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Di chuyển theo các mốc đường cố định (NavMeshAgent hoặc MoveTowards).
        /// </summary>
        void MoveAlongWaypoints()
        {
            if (waypoints == null || waypoints.Count == 0) return;
            if (currentWaypointIndex >= waypoints.Count) return;

            Transform targetWp = waypoints[currentWaypointIndex];
            if (targetWp == null) return;

            if (agent != null && agent.enabled)
            {
                if (!agent.hasPath || agent.remainingDistance < 1f)
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex < waypoints.Count)
                    {
                        if (waypoints[currentWaypointIndex] != null)
                            agent.SetDestination(waypoints[currentWaypointIndex].position);
                    }
                    else if (loopWaypoints && waypoints.Count > 0)
                    {
                        currentWaypointIndex = 0;
                        if (waypoints[0] != null)
                            agent.SetDestination(waypoints[0].position);
                    }
                }
            }
            else
            {
                // Di chuyển trực tiếp bằng Transform khi không dùng NavMeshAgent
                Vector3 targetPos = targetWp.position;
                targetPos.y = transform.position.y;
                Vector3 toTarget = targetPos - transform.position;

                if (toTarget.sqrMagnitude > 0.36f) // Khoảng cách > 0.6m
                {
                    Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                }
                else
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex >= waypoints.Count && loopWaypoints)
                    {
                        currentWaypointIndex = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Xoay tháp pháo về hướng người chơi.
        /// </summary>
        void AimTurret()
        {
            if (!turret || !player) return;

            Vector3 toPlayer = player.position - turret.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer);
                turret.rotation = Quaternion.RotateTowards(turret.rotation, targetRot, turretRotateSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Bắn đạn pháo nổ (DamageType.Explosive, 60 sát thương).
        /// </summary>
        void FireShell()
        {
            Vector3 origin = muzzle != null ? muzzle.position : (turret != null ? turret.position : transform.position);

            if (audioSource && fireClip)
            {
                audioSource.PlayOneShot(fireClip);
            }
            else CampaignAudio.Effect("cannon_generic", origin);

            if (muzzleFlashPrefab && muzzle)
            {
                Instantiate(muzzleFlashPrefab, muzzle.position, muzzle.rotation);
            }
            else CombatEffects.Muzzle(origin, muzzle ? muzzle.rotation : transform.rotation, 2f);

            Vector3 aimDir = player ? (player.position + Vector3.up * 0.5f - origin).normalized : (turret ? turret.forward : transform.forward);

            if (Physics.Raycast(origin, aimDir, out var hit, fireRange, fireMask, QueryTriggerInteraction.Ignore))
            {
                CombatEffects.Explosion(hit.point, 1.5f);
                // Sát thương trúng đích
                var directTarget = hit.collider.GetComponentInParent<IDamageable>();
                directTarget?.TakeDamage(fireDamage, DamageType.Explosive, hit.point);

                // Sát thương nổ lan khu vực xung quanh
                Collider[] colliders = Physics.OverlapSphere(hit.point, explosionRadius);
                foreach (var col in colliders)
                {
                    if (col.transform.IsChildOf(transform)) continue;
                    var aoeTarget = col.GetComponentInParent<IDamageable>();
                    if (aoeTarget != null && aoeTarget != directTarget)
                    {
                        aoeTarget.TakeDamage(fireDamage * 0.5f, DamageType.Explosive, hit.point);
                    }
                }
            }
        }

        /// <summary>
        /// Khi xe tăng bị phá hủy: dừng di chuyển, kích hoạt trigger Dead, sinh destructionPrefab, sau 2s Destroy.
        /// Bộ binh hộ tống vẫn tiếp tục tác chiến độc lập (BR-20).
        /// </summary>
        void OnDied()
        {
            if (dead) return;
            dead = true;
            if (engineSound) engineSound.Stop();

            // Dừng di chuyển
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            if (animator != null)
            {
                animator.SetTrigger(AnimDead);
            }

            if (destructionPrefab != null)
            {
                Instantiate(destructionPrefab, transform.position, transform.rotation);
            }
            else CombatEffects.Explosion(transform.position, 2.5f);

            if (audioSource != null && destructionClip != null)
            {
                audioSource.PlayOneShot(destructionClip);
            }
            else CampaignAudio.Effect("explosion_04", transform.position);

            // Tắt va chạm để không chặn đường di chuyển sau khi nổ
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            // Tự hủy sau 2 giây (bộ binh escorts là đối tượng độc lập nên vẫn tiếp tục)
            Destroy(gameObject, 2f);
        }

        void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count == 0) return;

            Gizmos.color = Color.red;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.6f);

                if (i + 1 < waypoints.Count && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                }
                else if (loopWaypoints && waypoints.Count > 1 && waypoints[0] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
                }
            }
        }
    }
}
