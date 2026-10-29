using System.Collections;
using DBP.Core;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// AI lính (T07 – Đạt; BR-20), đổi lối đánh 28/10 theo góp ý: thấy quân ta thì tiến lên đánh, không nấp, không bỏ chạy.
    ///   • Còn xa thì tiến tới (lách trái phải, vừa đi vừa bắn); vào tầm bắn riêng của mỗi lính (12–20 m) thì dừng lại đứng bắn.
    ///   • Quân ta áp sát dưới 6 m thì lao vào đâm; mất tầm nhìn thì tiến tiếp cho tới khi thấy.
    ///   • Cách 8–22 m thỉnh thoảng ném lựu đạn (không ném khi lính phe mình đứng gần chỗ nổ).
    ///   • Áp sát dưới 2,2 m thì đâm lưỡi lê / đấm báng súng.
    ///   • Nghe tiếng súng và báo đồng đội quanh mình; có sĩ quan gần thì bắn tốt hơn; sĩ quan gục thì bắn loạn một lúc.
    /// Trạng thái: Idle → Alerted/Shoot (xung phong). Suppress chỉ còn dùng cho lệnh rút lui của CounterAttackGroup.
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
        int patrolIndex;
        float patrolWaitEndsAt;
        bool dead;

        public Health Health => health;
        public NavMeshAgent Agent => agent;
        public State CurrentState => state;
        public bool IsDead => dead;
        public bool IsCharging { get; set; }
        /// Hệ số sát thương (chỉ huy địch = 2).
        public float DamageMul { get; set; } = 1f;
        public Vector3 FoePosition => player ? FoePos : transform.position;
        /// Vị trí người chơi lính biết gần nhất (thấy hoặc nghe).
        public Vector3 LastKnownTarget => lastKnown;

        public static readonly System.Collections.Generic.List<SoldierAI> All = new System.Collections.Generic.List<SoldierAI>();

        // Chiến thuật
        Vector3 lastKnown;
        float lastSeenAt = -99f, acquiredAt = -99f, nextThinkAt, panicUntil, nextGrenadeAt, baseSpeed;
        int grenades = 2;
        float holdRange;
        const float RushRange = 6f;
        static Grenade grenadePrefab;
        ComradeFollower allyFoe;     // đang nhắm đồng đội thay vì người chơi
        float nextMeleeAt;
        const float MeleeRange = 2.2f;
        Vector3 FoePos => allyFoe ? allyFoe.transform.position : player.position;
        const float ReactionSec = 0.55f;   // thấy rồi mới bóp cò, công bằng với người chơi

        // ── Khởi tạo ──────────────────────────────────────────────────────────────
        void Awake()
        {
            health = GetComponent<Health>();
            agent  = GetComponent<NavMeshAgent>();
            if (!eyePoint) eyePoint = transform;
            if (!muzzle)   muzzle   = eyePoint;
            baseSpeed = agent.speed;
            LadderClimber.Ensure(this);
            holdRange = Random.Range(12f, 20f);
            nextGrenadeAt = Time.time + Random.Range(3f, 8f);
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died    += OnDied;
            All.Add(this);
            WeaponController.ShotFired += OnPlayerShot;
            OfficerLeader.Fallen += OnOfficerFallen;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died    -= OnDied;
            All.Remove(this);
            WeaponController.ShotFired -= OnPlayerShot;
            OfficerLeader.Fallen -= OnOfficerFallen;
        }

        void Start()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo) player = playerGo.transform;
            agent.height = standHeight;
            SetState(State.Idle);
            started = true;
            // Lính xuất hiện giữa trận (đợt phản kích Màn 2, viện binh, Màn 6) hoặc được giao quyền từ lính gác / lính hào:
            // tiến đánh ngay, không đứng chờ thấy người trong tầm 30 m.
            bool sentryControlled = TryGetComponent<SentryAI>(out var sentry) && sentry.enabled;
            if (alertOnStart || (Time.timeSinceLevelLoad > 2f && !sentryControlled)) Alert();
        }

        bool started, alertOnStart;

        /// Lính gác (Màn 3) / lính hào (Màn 4) chuyển sang lối đánh SoldierAI khi giao chiến: tiến vào tầm bắn,
        /// bắn cả bộ đội, áp sát thì đâm; bộ đội cũng chọn được họ làm mục tiêu (ComradeFollower nhắm SoldierAI).
        public static SoldierAI TakeOver(MonoBehaviour from, Transform eye, Transform gun, float damageMul, Vector3 target)
        {
            if (!from.TryGetComponent<SoldierAI>(out var s)) s = from.gameObject.AddComponent<SoldierAI>();
            Transform Raised(Transform t, string name)
            {
                if (t && t != from.transform) return t;
                var child = from.transform.Find(name);
                if (!child) { child = new GameObject(name).transform; child.SetParent(from.transform, false); child.localPosition = Vector3.up * 1.55f; }
                return child;
            }
            s.eyePoint = Raised(eye, "Mat");
            s.muzzle = Raised(gun, "Mat");
            s.DamageMul = Mathf.Max(s.DamageMul, damageMul);
            s.lastKnown = target;
            from.enabled = false;
            s.enabled = true;
            if (s.started) s.Alert(); else s.alertOnStart = true;
            return s;
        }

        // ── Vòng lặp ──────────────────────────────────────────────────────────────
        void Update()
        {
            if (dead) return;
            if (Time.time >= nextThinkAt) { nextThinkAt = Time.time + 0.25f; Think(); }

            if (state == State.Idle) TickIdle();
            else if (state != State.Suppress) TickCharge();

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

        // ── Xung phong: vừa chạy vừa bắn, ném lựu đạn, áp sát thì đâm ────────────
        void TickCharge()
        {
            if (health != null && health.Retreated) return;
            if (!player) return;
            if (TryMelee()) return;

            bool sees = allyFoe ? SeesAlly(allyFoe, false) : CanSeePlayer(false);
            if (sees && Time.time - lastSeenAt > 1f) acquiredAt = Time.time;
            if (sees) { lastSeenAt = Time.time; lastKnown = FoePos; }
            FaceTarget(FoePos);
            if (!sees) return;

            if (TryGrenade(Vector3.Distance(transform.position, FoePos))) return;
            if (Time.time - acquiredAt < ReactionSec || Time.time < nextFireAt) return;
            TryFire();
            float officer = OfficerLeader.BonusAt(transform.position, GetComponent<OfficerLeader>());
            nextFireAt = Time.time + fireInterval * officer * Random.Range(0.8f, 1.2f);
        }

        /// Di chuyển (4 lần/giây): xa thì tiến chữ chi, trong tầm thì đứng bắn, dưới 6 m thì lao thẳng tăng tốc để đâm.
        void Advance()
        {
            if (state == State.Idle || state == State.Suppress || (health && health.Retreated) || !player) return;
            var goal = FoePos;
            var to = goal - transform.position; to.y = 0;
            float dist = to.magnitude;
            if (dist <= MeleeRange) return;
            bool sees = Time.time - lastSeenAt < 0.5f;
            // Trong tầm bắn và đang thấy: đứng lại bắn; quá gần thì lao vào đâm
            if (sees && dist > RushRange && dist <= holdRange) { if (agent.hasPath) agent.ResetPath(); return; }
            var side = dist > 8f ? Vector3.Cross(Vector3.up, to.normalized) * Mathf.Sin(Time.time * 1.3f + GetInstanceID()) * 3f : Vector3.zero;
            agent.speed = dist <= RushRange ? Mathf.Max(baseSpeed, 5.5f) : baseSpeed;
            agent.SetDestination(goal + side);
        }

        /// Ném lựu đạn khi cách 8–22 m, còn lựu đạn và không có lính phe mình gần chỗ nổ.
        bool TryGrenade(float dist)
        {
            if (grenades <= 0 || dist < 8f || dist > 22f || Time.time < nextGrenadeAt) return false;
            nextGrenadeAt = Time.time + Random.Range(7f, 12f);
            if (Random.value > 0.45f || !SafeToBlast(FoePos, this)) return false;
            if (!Lob(this, FoePos)) return false;
            grenades--;
            return true;
        }

        /// Đạn địch trúng gì: người chơi thì mất máu; lính / công sự phe địch thì bỏ qua (không bắn nhầm nhau,
        /// và địch chết vì đạn đồng bọn không bị tính công cho người chơi).
        public static void EnemyBulletHit(IDamageable target, float amount, Vector3 point)
        {
            if (target is Health h && h.Kind != TargetKind.Player) return;
            target?.TakeDamage(amount, DamageType.Bullet, point);
        }

        /// Không có lính phe mình (còn sống) trong 6 m quanh chỗ nổ.
        public static bool SafeToBlast(Vector3 at, SoldierAI except)
        {
            foreach (var s in All)
                if (s != except && !s.dead && (s.transform.position - at).sqrMagnitude < 6f * 6f) return false;
            return true;
        }

        /// Ném một quả lựu đạn (dùng prefab lựu đạn của người chơi) theo vòng cung rơi đúng chỗ.
        /// power > 1 là bộc phá: nổ to và mạnh hơn.
        public static bool Lob(Component thrower, Vector3 target, float power = 1f)
        {
            if (!grenadePrefab)
            {
                var hand = FindAnyObjectByType<GrenadeThrowController>(FindObjectsInactive.Include);
                if (hand) grenadePrefab = hand.Prefab;
                if (!grenadePrefab) return false;
            }
            EnemyAnimatorDriver.Play(thrower, "Throw");
            var t = thrower.transform;
            var origin = t.position + Vector3.up * 1.6f + t.forward * 0.5f;
            const float flight = 1.1f;
            var v = (target - origin) / flight - 0.5f * Physics.gravity * flight;
            var g = Instantiate(grenadePrefab, origin, Quaternion.identity);
            g.CreditPlayer = false;
            if (power > 1f) g.Boost(power);
            if (g.TryGetComponent<Rigidbody>(out var rb)) rb.linearVelocity = v;
            return true;
        }

        // ── Bắn ───────────────────────────────────────────────────────────────────
        void TryFire()
        {
            if (!muzzle || !player) return;
            EnemyAnimatorDriver.Play(this, "Shoot");

            if (animator) animator.SetTrigger(AnimShoot);
            Play(fireClip);
            CombatEffects.Muzzle(muzzle.position, muzzle.rotation);

            // Tia raycast với độ lệch ngẫu nhiên nhỏ (lính bắn không chuẩn)
            if (allyFoe)
            {
                // Bắn đồng đội: đồng đội không có va chạm, tính trúng theo độ tản và khoảng cách
                var aim = allyFoe.transform.position + Vector3.up * 1.1f;
                float dist = Vector3.Distance(muzzle.position, aim);
                bool hitAlly = Random.value < Mathf.Clamp(0.65f - dist / 80f, 0.2f, 0.6f) * bulletSpread / Mathf.Max(0.5f, CurrentSpread());
                var end = hitAlly ? aim : aim + Random.insideUnitSphere * 1.5f;
                CombatEffects.Tracer(muzzle.position, end, true);
                CombatEffects.Impact(end, (muzzle.position - end).normalized, false);
                bool critAlly = hitAlly && Random.value < 0.1f;
                if (hitAlly) allyFoe.TakeHit(fireDamage * DamageMul * (critAlly ? 2f : 1f), critAlly);
                return;
            }
            var spread = Random.insideUnitCircle * CurrentSpread();
            var dir = (player.position + Vector3.up * 0.8f - muzzle.position).normalized;
            dir = Quaternion.Euler(spread.x, spread.y, 0f) * dir;

            bool struck = Physics.Raycast(muzzle.position, dir, out var hit, fireRange, sightMask, QueryTriggerInteraction.Ignore);

            CombatEffects.Tracer(muzzle.position, struck ? hit.point : muzzle.position + dir * (fireRange), true);

            if (struck)
            {
                CombatEffects.Impact(hit.point, hit.normal, CombatEffects.IsMetal(hit.collider));
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                EnemyBulletHit(dmg, fireDamage * DamageMul, hit.point);
            }
        }

        // ── Chuyển trạng thái ─────────────────────────────────────────────────────
        public void SetState(State next)
        {
            state = next;
            bool crouching = next == State.Suppress;
            // Nhóm phản kích có thể ra lệnh ngay khi đợt được bật, trước cả Awake của lính.
            if (!agent) agent = GetComponent<NavMeshAgent>();
            if (agent) agent.height = crouching ? crouchHeight : standHeight;
            if (animator) animator.SetBool(AnimCrouch, crouching);
        }

        void Alert()
        {
            if (player) lastKnown = player.position;
            Play(alertClip);
            SetState(State.Alerted);
            AlertAround(transform.position, 20f, lastKnown, this); // gọi đồng đội quanh mình
        }

        /// Báo động cho lính đang rảnh quanh một điểm (tiếng hô, tiếng súng).
        public static void AlertAround(Vector3 origin, float radius, Vector3 target, SoldierAI except = null)
        {
            foreach (var s in All.ToArray())
            {
                if (s == except || s.dead || s.state != State.Idle) continue;
                var sentry = s.GetComponent<SentryAI>();
                if (sentry != null && sentry.enabled) continue;
                if ((s.transform.position - origin).sqrMagnitude > radius * radius) continue;
                s.lastKnown = target;
                s.SetState(State.Alerted);
            }
        }

        // ── Chiến thuật ───────────────────────────────────────────────────────────
        /// 4 lần/giây: chọn mục tiêu và cập nhật đường xung phong.
        void Think()
        {
            if (!agent || !agent.isOnNavMesh) return;
            PickFoe();
            Advance();
        }

        /// Nhắm người chơi hay đồng đội: ưu tiên ai thấy được và gần hơn rõ rệt. Đang rảnh mà thấy đồng đội thì báo động.
        void PickFoe()
        {
            if (!player) return;
            if (allyFoe && (allyFoe.IsDead || !allyFoe.isActiveAndEnabled)) allyFoe = null;
            bool seesPlayer = CanSeePlayer(state == State.Idle);
            float playerDist = Vector3.Distance(transform.position, player.position);
            ComradeFollower best = null; float bestDist = fireRange;
            foreach (var c in ComradeFollower.All)
            {
                if (!c || c.IsDead) continue;
                float d = Vector3.Distance(transform.position, c.transform.position);
                if (d < bestDist && SeesAlly(c, state == State.Idle)) { bestDist = d; best = c; }
            }
            allyFoe = best && (!seesPlayer || bestDist < playerDist * 0.7f) ? best : null;
            if (state == State.Idle && allyFoe) { lastKnown = allyFoe.transform.position; Alert(); }
        }

        bool SeesAlly(ComradeFollower c, bool useFov)
        {
            if (!eyePoint) return false;
            var to = c.transform.position + Vector3.up * 1.2f - eyePoint.position;
            if (to.magnitude > (useFov ? sightRange : fireRange)) return false;
            if (useFov && Vector3.Angle(eyePoint.forward, to) > sightAngle) return false;
            return !Physics.Raycast(eyePoint.position, to.normalized, to.magnitude, sightMask, QueryTriggerInteraction.Ignore);
        }

        /// Giáp lá cà khi địch áp sát: báng súng/lưỡi lê.
        bool TryMelee()
        {
            var to = FoePos - transform.position; to.y = 0;
            if (to.magnitude > MeleeRange) return false;
            agent.ResetPath();
            FaceTarget(FoePos);
            if (Time.time < nextMeleeAt) return true;
            nextMeleeAt = Time.time + 1.5f;
            bool stab = Random.value < 0.6f, crit = Random.value < 0.2f;
            MeleeAnimator.Play(this, stab);
            transform.position += to.normalized * 0.2f;
            float dmg = (stab ? 1f : 0.6f) * (crit ? 2f : 1f) * DamageMul;
            if (allyFoe) allyFoe.TakeHit(40f * dmg, crit);
            else player.GetComponent<IDamageable>()?.TakeDamage(22f * dmg, DamageType.Bullet, player.position + Vector3.up);
            CombatEffects.Impact(FoePos + Vector3.up * 1.2f, -to.normalized, false);
            return true;
        }

        float CurrentSpread()
        {
            float d = player ? Vector3.Distance(transform.position, player.position) : 20f;
            float s = bulletSpread * (0.6f + d / 30f);
            var cc = player ? player.GetComponent<CharacterController>() : null;
            if (cc && new Vector2(cc.velocity.x, cc.velocity.z).magnitude > 3f) s *= 1.7f; // khó trúng người đang chạy
            if (Time.time - acquiredAt < 2f) s *= 1.5f;                                      // phát đầu chưa ngắm kỹ
            if (Time.time < panicUntil) s *= 1.8f;
            return s * OfficerLeader.BonusAt(transform.position, GetComponent<OfficerLeader>());
        }

        /// Nghe tiếng súng của người chơi trong 35 m thì biết hướng và xung phong tới.
        void OnPlayerShot(Vector3 from, Vector3 to)
        {
            if (dead) return;
            if ((from - (transform.position + Vector3.up)).sqrMagnitude < 35f * 35f)
            {
                lastKnown = from;
                if (state == State.Idle && !(GetComponent<SentryAI>() is SentryAI sentry && sentry.enabled)) Alert();
            }
        }

        void OnOfficerFallen(Vector3 at, float radius)
        {
            if (dead || (transform.position - at).sqrMagnitude > radius * radius) return;
            panicUntil = Time.time + 10f; // bắn loạn hơn nhưng vẫn xung phong
        }

        // ── Sự kiện ───────────────────────────────────────────────────────────────
        /// Trúng đạn khi đang rảnh: biết bị tấn công và xung phong (không nấp).
        void OnDamaged(float _)
        {
            if (dead || state != State.Idle) return;
            Alert();
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
        bool CanSeePlayer() => CanSeePlayer(true);

        /// useFov = false khi đã giao chiến: lính biết người chơi ở đâu, chỉ cần không bị che.
        bool CanSeePlayer(bool useFov)
        {
            if (!player || !eyePoint) return false;
            var toPlayer = player.position + Vector3.up * 0.8f - eyePoint.position;
            float dist = toPlayer.magnitude;
            if (dist > (useFov ? sightRange : fireRange)) return false;
            if (useFov && Vector3.Angle(eyePoint.forward, toPlayer) > sightAngle) return false;
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
