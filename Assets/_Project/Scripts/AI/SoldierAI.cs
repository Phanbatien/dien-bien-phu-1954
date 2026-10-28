using System.Collections;
using DBP.Core;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// AI lính (T07 – Đạt; BR-20), nâng cấp chiến thuật 24/10:
    ///   • Phát hiện → tìm chỗ nấp có tầm bắn (bao cát, mép hào) rồi mới bắn; mất tầm nhìn thì vòng sang chỗ khác (tạt sườn).
    ///   • Chỉ bắn khi thấy người chơi; bắn từng loạt ngắn rồi thụp xuống; độ chuẩn theo khoảng cách, người chơi chạy hay đứng.
    ///   • Nghe tiếng súng và báo đồng đội quanh mình; đạn sượt qua thì nấp; thấy lựu đạn thì chạy tránh.
    ///   • Có sĩ quan gần thì bắn tốt hơn; sĩ quan gục thì hoảng loạn một lúc.
    /// Trạng thái gốc:
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
        /// Vị trí người chơi lính biết gần nhất (thấy hoặc nghe).
        public Vector3 LastKnownTarget => lastKnown;

        public static readonly System.Collections.Generic.List<SoldierAI> All = new System.Collections.Generic.List<SoldierAI>();

        // Chiến thuật
        Vector3 lastKnown;
        Vector3 claimedSpot;
        bool hasSpot, fleeing;
        float lastSeenAt = -99f, acquiredAt = -99f, repositionAt, nextThinkAt, panicUntil;
        int burstLeft = 3;
        ComradeFollower allyFoe;     // đang nhắm đồng đội thay vì người chơi
        float nextMeleeAt;
        const float MeleeRange = 2.2f;
        Vector3 FoePos => allyFoe ? allyFoe.transform.position : player.position;
        const float ReactionSec = 0.55f;   // thấy rồi mới bóp cò, công bằng với người chơi
        const float LoseSightSec = 1.6f;   // không thấy quá lâu thì đổi chỗ

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
        }

        // ── Vòng lặp ──────────────────────────────────────────────────────────────
        void Update()
        {
            if (dead) return;
            if (Time.time >= nextThinkAt) { nextThinkAt = Time.time + 0.25f; Think(); }
            if (fleeing) { if (!agent.pathPending && agent.remainingDistance < 0.6f) fleeing = false; return; }

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
            if (IsCharging)
            {
                // Nhóm phản kích xung phong: lao tới, lách trái phải để khó ngắm
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= 6f) { SetState(State.Shoot); return; }
                var side = Vector3.Cross(Vector3.up, (player.position - transform.position).normalized) * Mathf.Sin(Time.time * 1.3f + GetInstanceID()) * 3f;
                if (agent.isOnNavMesh) agent.SetDestination(player.position + side);
                if (CanSeePlayer(false) && Time.time >= nextFireAt && dist < fireRange * 0.7f) { TryFire(); nextFireAt = Time.time + fireInterval * 1.4f; }
                return;
            }
            if (!hasSpot) PickFiringSpot();
            if (!hasSpot) { SetState(State.Shoot); return; }
            if (!agent.pathPending && agent.remainingDistance < 0.7f) { agent.ResetPath(); SetState(State.Shoot); }
        }

        // ── Shoot: đứng/ngồi sau bao cát, bắn định kỳ ────────────────────────────
        void TickShoot()
        {
            if (health != null && health.Retreated) return;
            if (!player) return;

            if (!IsCharging)
            {
                if (!agent.pathPending && agent.hasPath && agent.remainingDistance < 0.6f) agent.ResetPath();
            }
            else
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist > 6f)
                    agent.SetDestination(player.position);
                else
                    agent.ResetPath();
            }

            if (TryMelee()) return;
            bool sees = allyFoe ? SeesAlly(allyFoe, false) : CanSeePlayer(false);
            if (sees && Time.time - lastSeenAt > 1f) acquiredAt = Time.time;
            if (sees) { lastSeenAt = Time.time; lastKnown = FoePos; }
            FaceTarget(sees ? FoePos : lastKnown);

            if (!sees)
            {
                // Không thấy: không bắn vào tường. Chờ chút rồi vòng sang chỗ có tầm bắn (tạt sườn)
                if (Time.time - lastSeenAt > LoseSightSec && Time.time >= repositionAt && !IsCharging)
                {
                    repositionAt = Time.time + 4f;
                    hasSpot = false;
                    PickFiringSpot();
                    if (hasSpot) SetState(State.Alerted);
                }
                if (Time.time - lastSeenAt > 25f) { hasSpot = false; SetState(State.Idle); }
                return;
            }

            if (Time.time - acquiredAt < ReactionSec || Time.time < nextFireAt) return;
            TryFire();
            float officer = OfficerLeader.BonusAt(transform.position, GetComponent<OfficerLeader>());
            nextFireAt = Time.time + fireInterval * officer * Random.Range(0.8f, 1.2f);
            // Bắn 2–3 phát rồi thụp xuống nấp một lúc, không đứng lì giữa làn đạn
            if (--burstLeft <= 0 && !IsCharging)
            {
                burstLeft = Random.Range(2, 4);
                suppressEndsAt = Time.time + Random.Range(1.0f, 2.0f);
                SetState(State.Suppress);
            }
        }

        // ── Suppress: nấp thấp, không bắn ────────────────────────────────────────
        void TickSuppress()
        {
            if (health != null && health.Retreated) return;
            if (player) FaceTarget(lastKnown);
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
                if (hitAlly) allyFoe.TakeHit(fireDamage * (critAlly ? 2f : 1f), critAlly);
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
                dmg?.TakeDamage(fireDamage, DamageType.Bullet, hit.point);
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
            hasSpot = false;
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
                s.hasSpot = false;
                s.SetState(State.Alerted);
            }
        }

        // ── Chiến thuật ───────────────────────────────────────────────────────────
        /// 4 lần/giây: tránh lựu đạn đang lăn tới.
        void Think()
        {
            if (!agent || !agent.isOnNavMesh) return;
            PickFoe();
            foreach (var g in Grenade.Live)
            {
                if (!g) continue;
                var away = transform.position - g.transform.position; away.y = 0;
                float danger = g.BlastRadius + 1f;
                if (away.sqrMagnitude > danger * danger) continue;
                var target = transform.position + (away.sqrMagnitude > 0.01f ? away.normalized : -transform.forward) * (danger + 2f);
                if (NavMesh.SamplePosition(target, out var hit, 3f, NavMesh.AllAreas))
                {
                    fleeing = true; hasSpot = false;
                    agent.SetDestination(hit.position);
                    if (state == State.Idle) SetState(State.Alerted);
                }
                break;
            }
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
            float dmg = (stab ? 1f : 0.6f) * (crit ? 2f : 1f);
            if (allyFoe) allyFoe.TakeHit(40f * dmg, crit);
            else player.GetComponent<IDamageable>()?.TakeDamage(22f * dmg, DamageType.Bullet, player.position + Vector3.up);
            CombatEffects.Impact(FoePos + Vector3.up * 1.2f, -to.normalized, false);
            return true;
        }

        /// Chọn chỗ đứng bắn: thấy được người chơi, có vật che ngang hông, cách 10–28 m, gần mình, không trùng đồng đội.
        void PickFiringSpot()
        {
            if (!player || !agent || !agent.isOnNavMesh) return;
            var target = lastKnown == Vector3.zero ? player.position : lastKnown;
            var chest = target + Vector3.up * 1.2f;
            float best = float.MinValue; Vector3 bestPos = default;
            for (int i = 0; i < 14; i++)
            {
                float a = Random.value * Mathf.PI * 2f, r = Random.Range(3f, 13f);
                var p = transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                if (!NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas)) continue;
                p = hit.position;
                float d = Vector3.Distance(p, target);
                if (d < 6f || d > fireRange) continue;
                bool view = !Physics.Linecast(p + Vector3.up * 1.6f, chest, sightMask, QueryTriggerInteraction.Ignore);
                bool cover = Physics.Linecast(chest, p + Vector3.up * 0.6f, sightMask, QueryTriggerInteraction.Ignore);
                float score = (view ? 4f : -6f) + (cover ? 3f : 0f) - Mathf.Abs(d - 18f) * 0.12f - r * 0.15f;
                foreach (var s in All)
                    if (s != this && s.hasSpot && (s.claimedSpot - p).sqrMagnitude < 4f) score -= 3f;
                if (score > best) { best = score; bestPos = p; }
            }
            if (best <= -2f) return;
            claimedSpot = bestPos; hasSpot = true;
            agent.SetDestination(bestPos);
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

        /// Đạn của người chơi bay sát (dưới 2,5 m) thì nấp; nghe tiếng súng trong 35 m thì biết hướng.
        void OnPlayerShot(Vector3 from, Vector3 to)
        {
            if (dead) return;
            var me = transform.position + Vector3.up;
            var seg = to - from;
            float t = seg.sqrMagnitude > 0.01f ? Mathf.Clamp01(Vector3.Dot(me - from, seg) / seg.sqrMagnitude) : 0f;
            float miss = Vector3.Distance(me, from + seg * t);
            if ((from - me).sqrMagnitude < 35f * 35f)
            {
                lastKnown = from;
                if (state == State.Idle && !(GetComponent<SentryAI>() is SentryAI sentry && sentry.enabled)) Alert();
            }
            if (miss < 2.5f && state == State.Shoot && !IsCharging)
            {
                suppressEndsAt = Time.time + suppressDuration * (miss < 1f ? 1f : 0.6f);
                SetState(State.Suppress);
            }
        }

        void OnOfficerFallen(Vector3 at, float radius)
        {
            if (dead || (transform.position - at).sqrMagnitude > radius * radius) return;
            panicUntil = Time.time + 10f;
            suppressEndsAt = Time.time + 3f;
            SetState(State.Suppress);
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
