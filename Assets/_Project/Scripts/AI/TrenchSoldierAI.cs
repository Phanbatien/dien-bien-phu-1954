using System.Collections;
using System.Collections.Generic;
using DBP.Core;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// <summary>
    /// AI cận chiến trong hào (Task T52, BR-20):
    /// - Khi người chơi đến gần (< 8m): ưu tiên ném lựu đạn (grenadeRange = 8m, cooldown = 5s, số lượng = 2 quả).
    /// - Khi bị áp chế / bị bắn trúng hoặc quá khoảng thời gian coverChangeInterval = 4s: NavMeshAgent đổi chỗ nấp sang coverPoint khác.
    /// - State machine độc lập gồm: HaoCover (nấp góc hào), HaoShoot (bắn), HaoGrenade (ném lựu đạn), HaoChange (đổi chỗ nấp).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class TrenchSoldierAI : MonoBehaviour
    {
        public enum State
        {
            HaoCover,    // Nấp thấp sau góc hào / bao cát
            HaoShoot,    // Ngóc đầu lên bắn trả người chơi
            HaoGrenade,  // Ném lựu đạn khi người chơi áp sát (< 8m)
            HaoChange    // Di chuyển nhanh sang điểm nấp khác trong hào
        }

        // ── Cấu hình nhân vật & vũ khí ──────────────────────────────────────────
        [Header("Nhân vật")]
        [Tooltip("Điểm mắt để quét tầm nhìn.")]
        [SerializeField] Transform eyePoint;
        [Tooltip("Nòng súng / điểm bắn và xuất phát đạn.")]
        [SerializeField] Transform muzzle;
        [Tooltip("Điểm ném lựu đạn (nếu để trống sẽ dùng muzzle hoặc eyePoint).")]
        [SerializeField] Transform throwPoint;

        [Header("Tầm nhìn")]
        [SerializeField] float sightRange = 30f;
        [SerializeField] float sightAngle = 80f;   // Nửa góc nhìn (độ)
        [SerializeField] LayerMask sightMask = ~0;

        [Header("Chiến đấu (Bắn)")]
        [SerializeField] float fireDamage = 15f;
        [SerializeField] float fireInterval = 1.5f; // Giây/phát bắn
        [SerializeField] float bulletSpread = 2.5f; // Độ lệch đạn tối đa
        [SerializeField] float fireRange = 25f;     // Cự ly bắn hiệu quả trong hào
        [SerializeField] int burstShots = 3;        // Số phát bắn mỗi đợt trước khi nấp lại

        [Header("Lựu đạn cận chiến (BR-20)")]
        [Tooltip("Prefab lựu đạn ném bởi AI.")]
        [SerializeField] Grenade grenadePrefab;
        [Tooltip("Khoảng cách kích hoạt ném lựu đạn (mặc định 8m theo BR-20).")]
        [SerializeField] float grenadeRange = 8f;
        [Tooltip("Thời gian hồi giữa 2 lần ném lựu đạn.")]
        [SerializeField] float grenadeCooldown = 5f;
        [Tooltip("Số lượng lựu đạn mang theo.")]
        [SerializeField] int grenadeCount = 2;
        [Tooltip("Lực ném lựu đạn.")]
        [SerializeField] float throwForce = 11f;
        [Tooltip("Góc ngóc lên khi ném để tạo quỹ đạo vòng cung.")]
        [SerializeField] float upwardAngle = 18f;

        [Header("Nấp & Cơ động trong hào (BR-20)")]
        [Tooltip("Các điểm nấp an toàn dọc chiến hào.")]
        [SerializeField] List<Transform> coverPoints = new List<Transform>();
        [Tooltip("Khoảng thời gian tối đa nấp trước khi đổi góc hoặc bắn (giây).")]
        [SerializeField] float coverChangeInterval = 4f;
        [Tooltip("Chiều cao NavMeshAgent khi đứng bắn.")]
        [SerializeField] float standHeight = 1.8f;
        [Tooltip("Chiều cao NavMeshAgent khi nấp thấp.")]
        [SerializeField] float crouchHeight = 1.0f;
        [Tooltip("Tốc độ di chuyển đổi vị trí trong hào.")]
        [SerializeField] float moveSpeed = 3.5f;

        [Header("Âm thanh")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip fireClip;
        [SerializeField] AudioClip throwClip;
        [SerializeField] AudioClip alertClip;
        [SerializeField] AudioClip deathClip;

        [Header("Hoạt ảnh (Animator)")]
        [SerializeField] Animator animator;
        static readonly int AnimShoot  = Animator.StringToHash("Shoot");
        static readonly int AnimCrouch = Animator.StringToHash("Crouch");
        static readonly int AnimDead   = Animator.StringToHash("Dead");
        static readonly int AnimMove   = Animator.StringToHash("Speed");
        static readonly int AnimThrow  = Animator.StringToHash("Throw");

        // ── Trạng thái runtime ──────────────────────────────────────────────────
        Health health;
        NavMeshAgent agent;
        Transform player;

        State state = State.HaoCover;
        float nextFireAt;
        float nextGrenadeAt;
        float stateTimer;
        int currentBurstCount;
        Transform currentCoverPoint;
        bool dead;

        public State CurrentState => state;
        public int RemainingGrenades => grenadeCount;

        // ── Khởi tạo ────────────────────────────────────────────────────────────
        void Awake()
        {
            health = GetComponent<Health>();
            agent  = GetComponent<NavMeshAgent>();

            if (!eyePoint)   eyePoint   = transform;
            if (!muzzle)     muzzle     = eyePoint;
            if (!throwPoint) throwPoint = muzzle;

            if (agent)
            {
                agent.speed = moveSpeed;
                agent.height = crouchHeight;
            }
        }

        void OnEnable()
        {
            if (health)
            {
                health.Damaged += OnDamaged;
                health.Died    += OnDied;
            }
        }

        void OnDisable()
        {
            if (health)
            {
                health.Damaged -= OnDamaged;
                health.Died    -= OnDied;
            }
        }

        void Start()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo) player = playerGo.transform;

            // Nếu có coverPoints, chọn điểm gần nhất khởi đầu
            if (coverPoints != null && coverPoints.Count > 0)
            {
                currentCoverPoint = GetNearestCoverPoint();
                if (currentCoverPoint != null)
                {
                    agent.Warp(currentCoverPoint.position);
                }
            }

            SetState(State.HaoCover);
        }

        // ── Vòng lặp cập nhật ───────────────────────────────────────────────────
        void Update()
        {
            if (dead) return;

            // Cập nhật tốc độ cho Animator
            if (animator && agent)
                animator.SetFloat(AnimMove, agent.velocity.magnitude);

            // Kiểm tra cận chiến: Nếu người chơi áp sát < grenadeRange (8m), ưu tiên ném lựu đạn (BR-20)
            if (state != State.HaoGrenade && ShouldThrowGrenade())
            {
                SetState(State.HaoGrenade);
                return;
            }

            switch (state)
            {
                case State.HaoCover:
                    TickHaoCover();
                    break;
                case State.HaoShoot:
                    TickHaoShoot();
                    break;
                case State.HaoGrenade:
                    TickHaoGrenade();
                    break;
                case State.HaoChange:
                    TickHaoChange();
                    break;
            }
        }

        // ── State 1: HaoCover (Nấp thấp sau góc hào) ──────────────────────────
        void TickHaoCover()
        {
            stateTimer += Time.deltaTime;

            // Trong lúc nấp, nếu thấy người chơi trong tầm bắn sau 1.5s quan sát -> ngóc đầu bắn
            if (stateTimer >= 1.5f && player != null)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= fireRange && CanSeePlayer())
                {
                    SetState(State.HaoShoot);
                    return;
                }
            }

            // Bị áp chế hoặc ở một chỗ quá coverChangeInterval (4s) -> đổi điểm nấp
            if (stateTimer >= coverChangeInterval)
            {
                SetState(State.HaoChange);
            }
        }

        // ── State 2: HaoShoot (Đứng lên bắn theo loạt) ─────────────────────────
        void TickHaoShoot()
        {
            if (!player)
            {
                SetState(State.HaoCover);
                return;
            }

            FaceTarget(player.position);

            if (Time.time >= nextFireAt)
            {
                TryFire();
                nextFireAt = Time.time + fireInterval;
                currentBurstCount++;

                // Bắn hết loạt (burstShots) thì nấp lại để tránh hỏa lực phản kích
                if (currentBurstCount >= burstShots)
                {
                    currentBurstCount = 0;
                    // Xác suất 50% đổi điểm nấp, 50% nấp tại chỗ
                    if (Random.value < 0.5f && coverPoints.Count > 1)
                        SetState(State.HaoChange);
                    else
                        SetState(State.HaoCover);
                    return;
                }
            }

            // Mất tầm nhìn người chơi -> quay lại nấp
            if (!CanSeePlayer())
            {
                SetState(State.HaoCover);
            }
        }

        // ── State 3: HaoGrenade (Ném lựu đạn khi < 8m) ─────────────────────────
        void TickHaoGrenade()
        {
            if (!player)
            {
                SetState(State.HaoCover);
                return;
            }

            FaceTarget(player.position);
            ExecuteThrowGrenade();

            // Ném xong lập tức đổi điểm nấp để tránh vụ nổ và tránh đòn trả đũa
            if (coverPoints.Count > 1)
                SetState(State.HaoChange);
            else
                SetState(State.HaoCover);
        }

        // ── State 4: HaoChange (Di chuyển đổi chỗ nấp) ─────────────────────────
        void TickHaoChange()
        {
            // Kiểm tra đã đến điểm nấp mới chưa
            if (!agent.pathPending && agent.remainingDistance <= 0.6f)
            {
                SetState(State.HaoCover);
            }
        }

        // ── Chuyển trạng thái FSM ──────────────────────────────────────────────
        void SetState(State next)
        {
            state = next;
            stateTimer = 0f;

            switch (state)
            {
                case State.HaoCover:
                    agent.ResetPath();
                    agent.height = crouchHeight;
                    if (animator) animator.SetBool(AnimCrouch, true);
                    break;

                case State.HaoShoot:
                    agent.ResetPath();
                    agent.height = standHeight;
                    currentBurstCount = 0;
                    if (animator) animator.SetBool(AnimCrouch, false);
                    break;

                case State.HaoGrenade:
                    agent.ResetPath();
                    agent.height = standHeight;
                    if (animator) animator.SetBool(AnimCrouch, false);
                    break;

                case State.HaoChange:
                    agent.height = standHeight;
                    if (animator) animator.SetBool(AnimCrouch, false);
                    MoveToNewCover();
                    break;
            }
        }

        // ── Xử lý hành động ───────────────────────────────────────────────────
        bool ShouldThrowGrenade()
        {
            if (grenadeCount <= 0 || !grenadePrefab || !player) return false;
            if (Time.time < nextGrenadeAt) return false;

            float dist = Vector3.Distance(transform.position, player.position);
            return dist <= grenadeRange && CanSeePlayer();
        }

        void ExecuteThrowGrenade()
        {
            if (grenadeCount <= 0 || !grenadePrefab) return;

            grenadeCount--;
            nextGrenadeAt = Time.time + grenadeCooldown;

            if (animator) animator.SetTrigger(AnimThrow);
            PlaySound(throwClip);

            var origin = throwPoint ? throwPoint.position : (transform.position + Vector3.up * 1.5f);
            var toPlayer = (player.position + Vector3.up * 0.5f) - origin;
            var aimDir = toPlayer.normalized;

            // Tạo góc ngóc lên upwardAngle để lựu đạn bay vòng cung
            var rightAxis = Vector3.Cross(Vector3.up, aimDir).normalized;
            if (rightAxis.sqrMagnitude < 0.01f) rightAxis = transform.right;
            var throwDir = Quaternion.AngleAxis(-upwardAngle, rightAxis) * aimDir;

            var g = Instantiate(grenadePrefab, origin, Quaternion.LookRotation(throwDir));
            if (g.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.AddForce(throwDir * throwForce, ForceMode.Impulse);
            }
        }

        void TryFire()
        {
            if (!muzzle || !player) return;

            if (animator) animator.SetTrigger(AnimShoot);
            PlaySound(fireClip);

            // Bắn raycast có độ tản mát
            var spread = Random.insideUnitCircle * bulletSpread;
            var targetPos = player.position + Vector3.up * 0.8f;
            var dir = (targetPos - muzzle.position).normalized;
            dir = Quaternion.Euler(spread.x, spread.y, 0f) * dir;

            if (Physics.Raycast(muzzle.position, dir, out var hit, fireRange, sightMask, QueryTriggerInteraction.Ignore))
            {
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                damageable?.TakeDamage(fireDamage, DamageType.Bullet, hit.point);
            }
        }

        void MoveToNewCover()
        {
            var nextCover = ChooseNextCoverPoint();
            if (nextCover != null)
            {
                currentCoverPoint = nextCover;
                agent.SetDestination(nextCover.position);
            }
            else
            {
                // Nếu không có coverPoints hợp lệ, nấp tại chỗ
                SetState(State.HaoCover);
            }
        }

        Transform ChooseNextCoverPoint()
        {
            if (coverPoints == null || coverPoints.Count == 0) return null;
            if (coverPoints.Count == 1) return coverPoints[0];

            // Chọn ngẫu nhiên một điểm nấp khác với điểm hiện tại
            var candidates = new List<Transform>();
            foreach (var cp in coverPoints)
            {
                if (cp != null && cp != currentCoverPoint)
                    candidates.Add(cp);
            }

            if (candidates.Count == 0) return currentCoverPoint;
            return candidates[Random.Range(0, candidates.Count)];
        }

        Transform GetNearestCoverPoint()
        {
            if (coverPoints == null || coverPoints.Count == 0) return null;
            Transform best = null;
            float minDst = float.MaxValue;
            foreach (var cp in coverPoints)
            {
                if (!cp) continue;
                float d = Vector3.Distance(transform.position, cp.position);
                if (d < minDst)
                {
                    minDst = d;
                    best = cp;
                }
            }
            return best;
        }

        // ── Sự kiện nhận sát thương & Chết ─────────────────────────────────────
        /// Khi bị trúng đạn (áp chế): lập tức đổi chỗ nấp ngẫu nhiên trong coverPoints (BR-20).
        void OnDamaged(float _)
        {
            if (dead) return;

            // Đổi chỗ nấp ngay lập tức khi bị bắn trúng
            if (coverPoints != null && coverPoints.Count > 1)
            {
                SetState(State.HaoChange);
            }
            else
            {
                SetState(State.HaoCover);
            }
        }

        void OnDied()
        {
            if (dead) return;
            dead = true;

            if (agent) agent.enabled = false;
            PlaySound(deathClip);

            if (animator) animator.SetTrigger(AnimDead);

            // Vô hiệu hóa colliders để không cản trở di chuyển trong hào
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            StartCoroutine(FadeOutRoutine());
        }

        IEnumerator FadeOutRoutine()
        {
            yield return new WaitForSeconds(2f);

            var renderers = GetComponentsInChildren<Renderer>();
            float elapsed = 0f;
            float fadeDuration = 1.5f;

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

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
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
            if (dist > sightRange) return false;
            if (Vector3.Angle(eyePoint.forward, toPlayer) > sightAngle) return false;
            return !Physics.Raycast(eyePoint.position, toPlayer.normalized, dist, sightMask, QueryTriggerInteraction.Ignore);
        }

        void FaceTarget(Vector3 target)
        {
            var dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }

        void PlaySound(AudioClip clip)
        {
            if (audioSource && clip) audioSource.PlayOneShot(clip);
        }

        // Vẽ Gizmos hỗ trợ Level Design trong Editor
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, grenadeRange);

            if (coverPoints != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var cp in coverPoints)
                {
                    if (cp) Gizmos.DrawWireSphere(cp.position, 0.4f);
                }
            }
        }
    }
}
