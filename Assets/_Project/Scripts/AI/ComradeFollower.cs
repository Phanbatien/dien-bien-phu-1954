using System.Collections.Generic;
using DBP.Core;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// Đồng đội bộ đội ta (mũ cối, quân phục xanh), AI chiến đấu thật:
    ///   • Không có địch gần: giữ đội hình quanh người chơi.
    ///   • Có địch trong 40 m: tìm chỗ nấp có tầm bắn, bắn loạt ngắn rồi thụp xuống, mất tầm nhìn thì đổi chỗ.
    ///   • Địch áp sát dưới 2,2 m: đánh giáp lá cà (báng súng, lưỡi lê).
    ///   • Có máu riêng; hy sinh thì gục xuống tại chỗ. Không có va chạm nên đạn người chơi không trúng đồng đội.
    /// Địch do đồng đội hạ không tính vào thành tích người chơi (Health.TakeAllyDamage).
    [RequireComponent(typeof(NavMeshAgent))]
    public class ComradeFollower : MonoBehaviour
    {
        public static readonly List<ComradeFollower> All = new List<ComradeFollower>();

        [Tooltip("Vị trí trong đội hình so với người chơi (phải, lên trước).")]
        public Vector2 slot = new Vector2(-3f, -2f);
        public float fireRange = 40f;
        public float maxHealth = 100f;
        public float fireDamage = 18f;
        public float meleeDamage = 50f;
        public Transform muzzle;

        public float Health { get; private set; }
        public bool IsDead => Health <= 0f;
        public SoldierAI Target => target;

        NavMeshAgent agent;
        Transform player;
        SoldierAI target;
        float nextThinkAt, nextShotAt, crouchUntil, lastSeenAt, nextMeleeAt, nextSpotAt;
        int burst;
        bool hasSpot;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            Health = maxHealth;
            nextShotAt = Time.time + Random.Range(1f, 3f);
            burst = Random.Range(2, 4);
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        /// Trúng đạn/nổ. Chết thì dừng mọi việc, hoạt ảnh tự gục (EnemyAnimatorDriver).
        public void TakeHit(float amount)
        {
            if (IsDead) return;
            Health -= amount;
            crouchUntil = Time.time + 1.2f; // trúng đạn thì nấp ngay
            if (Health > 0f) return;
            Health = 0f;
            if (agent && agent.isOnNavMesh) agent.ResetPath();
            if (agent) agent.enabled = false;
            DBP.UI.CampaignNotices.Show("Một đồng chí đã hy sinh.");
        }

        void Update()
        {
            if (IsDead || !player || !agent.isOnNavMesh) return;
            if (Time.time >= nextThinkAt) { nextThinkAt = Time.time + 0.3f; Think(); }
            if (!target) { Follow(); return; }

            var to = target.transform.position - transform.position;
            float dist = to.magnitude;
            Face(target.transform.position);

            // Giáp lá cà
            if (dist < 2.2f)
            {
                agent.ResetPath();
                if (Time.time >= nextMeleeAt)
                {
                    nextMeleeAt = Time.time + 1.3f;
                    transform.position += to.normalized * 0.25f; // lao tới
                    var health = target.Health;
                    if (health) health.TakeAllyDamage(meleeDamage, DamageType.Bullet, target.transform.position + Vector3.up);
                    CombatEffects.Impact(target.transform.position + Vector3.up * 1.2f, -to.normalized, false);
                }
                return;
            }
            if (dist < 5f && Random.value < 0.01f) { agent.SetDestination(target.transform.position); return; } // xông lên đâm lê

            bool sees = Sees(target.transform);
            if (sees) lastSeenAt = Time.time;
            if ((!sees && Time.time - lastSeenAt > 1.5f || !hasSpot) && Time.time >= nextSpotAt)
            {
                nextSpotAt = Time.time + 3f;
                PickSpot(target.transform.position);
            }
            if (!sees || Time.time < crouchUntil || Time.time < nextShotAt) return;
            if (!agent.pathPending && agent.hasPath && agent.remainingDistance > 1f && agent.velocity.sqrMagnitude > 1f) return; // đang chạy tới chỗ nấp
            Fire();
        }

        void Think()
        {
            // Bám người chơi: không chạy quá xa
            if ((transform.position - player.position).sqrMagnitude > 35f * 35f) { target = null; hasSpot = false; return; }
            if (target && (target.IsDead || !target.isActiveAndEnabled)) { target = null; hasSpot = false; }
            float best = fireRange * fireRange;
            SoldierAI pick = target;
            foreach (var s in SoldierAI.All)
            {
                if (!s || s.IsDead || !s.isActiveAndEnabled) continue;
                float d = (s.transform.position - transform.position).sqrMagnitude;
                if (d < best && (d < 9f || Sees(s.transform))) { best = d; pick = s; }
            }
            if (pick != target) { target = pick; hasSpot = false; }
        }

        void Follow()
        {
            var fwd = player.forward; fwd.y = 0;
            fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, fwd);
            var goal = player.position + right * slot.x + fwd * slot.y;
            float far = Vector3.Distance(transform.position, goal);
            agent.speed = far > 8f ? 4.5f : 2.4f;
            if (far > 1.5f && NavMesh.SamplePosition(goal, out var hit, 3f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
        }

        void Fire()
        {
            nextShotAt = Time.time + Random.Range(0.9f, 1.6f);
            var from = muzzle ? muzzle.position : transform.position + Vector3.up * 1.4f;
            var aimAt = target.transform.position + Vector3.up * 1.1f;
            float dist = Vector3.Distance(from, aimAt);
            bool hit = Random.value < Mathf.Clamp(0.75f - dist / 80f, 0.25f, 0.7f);
            var end = hit ? aimAt : aimAt + Random.insideUnitSphere * 1.5f;
            if (Physics.Linecast(from, end, out var block, ~0, QueryTriggerInteraction.Ignore) && !block.collider.transform.IsChildOf(target.transform))
            { end = block.point; hit = false; }
            CombatEffects.Muzzle(from, Quaternion.LookRotation(end - from));
            CombatEffects.Tracer(from, end, false);
            CombatEffects.Impact(end, (from - end).normalized, false);
            CampaignAudio.Play(AudioCatalog.Weapon("W-MOSIN", Random.Range(0, 5)), from, true, 0.55f);
            if (hit && target.Health) target.Health.TakeAllyDamage(fireDamage, DamageType.Bullet, end);
            if (--burst <= 0) { burst = Random.Range(2, 4); crouchUntil = Time.time + Random.Range(1f, 2f); }
        }

        /// Chỗ nấp có tầm bắn tới mục tiêu, cách 8–25 m, không xa người chơi, không trùng đồng đội.
        void PickSpot(Vector3 enemy)
        {
            float best = float.MinValue; Vector3 bestPos = default;
            for (int i = 0; i < 10; i++)
            {
                var p = transform.position + Random.insideUnitSphere * 9f; p.y = transform.position.y;
                if (!NavMesh.SamplePosition(p, out var h, 2f, NavMesh.AllAreas)) continue;
                p = h.position;
                float d = Vector3.Distance(p, enemy);
                if (d < 5f || d > fireRange || (p - player.position).sqrMagnitude > 30f * 30f) continue;
                bool view = !Physics.Linecast(p + Vector3.up * 1.5f, enemy + Vector3.up * 1.2f, ~0, QueryTriggerInteraction.Ignore);
                bool cover = Physics.Linecast(enemy + Vector3.up * 1.2f, p + Vector3.up * 0.6f, ~0, QueryTriggerInteraction.Ignore);
                float score = (view ? 4f : -5f) + (cover ? 3f : 0f) - Mathf.Abs(d - 16f) * 0.1f;
                foreach (var c in All) if (c != this && (c.transform.position - p).sqrMagnitude < 4f) score -= 2f;
                if (score > best) { best = score; bestPos = p; }
            }
            if (best < -1f) return;
            hasSpot = true;
            agent.speed = 4.2f;
            agent.SetDestination(bestPos);
        }

        bool Sees(Transform t)
        {
            var from = transform.position + Vector3.up * 1.5f;
            var to = t.position + Vector3.up * 1.2f;
            return !Physics.Linecast(from, to, out var hit, ~0, QueryTriggerInteraction.Ignore) || hit.collider.transform.IsChildOf(t);
        }

        void Face(Vector3 p)
        {
            var dir = p - transform.position; dir.y = 0;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
        }
    }
}
