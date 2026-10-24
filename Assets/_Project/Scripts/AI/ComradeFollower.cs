using DBP.Core;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// Đồng đội bộ đội ta (mũ cối, quân phục xanh): giữ đội hình quanh người chơi, thỉnh thoảng nổ súng về phía
    /// địch. Chỉ để chiến trường có người cùng chiến đấu: không có máu, không va chạm, đạn không gây sát thương
    /// (không đổi cân bằng, địch không nhắm vào). Đứng nghỉ khi người chơi dừng.
    [RequireComponent(typeof(NavMeshAgent))]
    public class ComradeFollower : MonoBehaviour
    {
        [Tooltip("Vị trí trong đội hình so với người chơi (phải, lên trước).")]
        public Vector2 slot = new Vector2(-3f, -2f);
        public float fireRange = 45f;
        public Transform muzzle;

        NavMeshAgent agent;
        Transform player;
        float nextMoveAt, nextShotAt;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            nextShotAt = Time.time + Random.Range(2f, 5f);
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        void Update()
        {
            if (!player || !agent.isOnNavMesh) return;
            var flatFwd = player.forward; flatFwd.y = 0;
            if (flatFwd.sqrMagnitude < 0.01f) flatFwd = Vector3.forward;
            flatFwd.Normalize();
            var right = Vector3.Cross(Vector3.up, flatFwd);
            var target = player.position + right * slot.x + flatFwd * slot.y;

            if (Time.time >= nextMoveAt)
            {
                nextMoveAt = Time.time + 0.5f;
                float far = Vector3.Distance(transform.position, target);
                agent.speed = far > 8f ? 4.5f : 2.2f;
                if (far > 1.5f && NavMesh.SamplePosition(target, out var hit, 3f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
            }

            var enemy = NearestEnemy();
            if (enemy && agent.velocity.sqrMagnitude < 1f)
            {
                var dir = enemy.position - transform.position; dir.y = 0;
                if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
            }
            if (enemy && Time.time >= nextShotAt)
            {
                nextShotAt = Time.time + Random.Range(2.5f, 5f);
                var from = muzzle ? muzzle.position : transform.position + Vector3.up * 1.4f;
                var aim = enemy.position + Vector3.up * 1.1f + Random.insideUnitSphere * 1.2f;
                CombatEffects.Muzzle(from, Quaternion.LookRotation(aim - from));
                // Đạn chỉ để nhìn: dừng ở vật cản đầu tiên, không gây sát thương
                var end = Physics.Linecast(from, aim, out var h, ~0, QueryTriggerInteraction.Ignore) ? h.point : aim;
                CombatEffects.Tracer(from, end, false);
                CombatEffects.Impact(end, (from - end).normalized, false);
            }
        }

        Transform NearestEnemy()
        {
            Transform best = null; float bestD = fireRange * fireRange;
            foreach (var s in SoldierAI.All)
            {
                if (!s || s.IsDead) continue;
                float d = (s.transform.position - transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = s.transform; }
            }
            return best;
        }
    }
}
