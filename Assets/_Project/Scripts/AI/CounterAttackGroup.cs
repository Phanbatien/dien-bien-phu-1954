using System.Collections.Generic;
using DBP.Core;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// <summary>
    /// Nhóm phản kích (T39, BR-20):
    /// - 4–8 lính, có thể chia nhóm yểm trợ + xung phong.
    /// - Rút khi mất HƠN một nửa lính (mất đúng 1/2 chưa rút).
    /// - Đẩy lùi tính một lần khi toàn bộ bị hạ hoặc đã rút.
    /// </summary>
    public class CounterAttackGroup : MonoBehaviour
    {
        [Header("Thành viên & Điểm rút")]
        [Tooltip("Danh sách lính trong nhóm. Tự thu thập trong Awake() nếu để trống.")]
        [SerializeField] List<SoldierAI> members = new List<SoldierAI>();
        [Tooltip("Điểm rút lui khi nhóm mất hơn 1/2 quân số (BR-20).")]
        [SerializeField] Transform retreatPoint;

        [Header("Chiến thuật (BR-20)")]
        [Tooltip("Nếu true: 1/3 nấp bắn yểm trợ, 2/3 xung phong tiến về người chơi.")]
        [SerializeField] bool hasSupport = true;

        [Header("Bán kính báo động khi kích hoạt")]
        [SerializeField] float alertBroadcastRadius = 20f;

        int initialCount;
        bool activated;
        Transform player;
        float nextChargeUpdate;

        public bool Retreated { get; private set; }

        /// <summary>
        /// Toàn bộ thành viên đã bị hạ hoặc đã rút lui (BR-20, BR-32).
        /// </summary>
        public bool AllNeutralized
        {
            get
            {
                if (Retreated) return true;
                if (members == null || members.Count == 0) return true;
                foreach (var m in members)
                {
                    if (m == null) continue;
                    var h = m.GetComponent<Health>();
                    if (h != null && !h.Neutralized) return false;
                }
                return true;
            }
        }

        public IReadOnlyList<SoldierAI> Members => members;
        public Transform RetreatPoint => retreatPoint;
        public bool HasSupport => hasSupport;

        void Awake()
        {
            if (members == null || members.Count == 0)
            {
                members = new List<SoldierAI>(GetComponentsInChildren<SoldierAI>(true));
            }
            initialCount = members.Count;
        }

        void Start()
        {
            var pGo = GameObject.FindGameObjectWithTag("Player");
            if (pGo) player = pGo.transform;
        }

        void OnEnable()
        {
            // Tự kích hoạt khi GameObject cha được bật (vd: đợt phản kích trong RepelWavesObjective)
            if (!activated)
            {
                Activate();
            }
        }

        /// <summary>
        /// Kích hoạt đợt phản kích: bật tất cả lính, phát tiếng súng báo động 20m, triển khai chiến thuật.
        /// </summary>
        public void Activate()
        {
            if (Retreated) return;
            activated = true;
            gameObject.SetActive(true);

            if (members == null || members.Count == 0)
            {
                members = new List<SoldierAI>(GetComponentsInChildren<SoldierAI>(true));
                initialCount = members.Count;
            }

            // Bật toàn bộ lính con
            foreach (var m in members)
            {
                if (m != null) m.gameObject.SetActive(true);
            }

            // Broadcast OnHeardShot đến tất cả SoldierAI trong vùng 20m
            BroadcastHeardShot(transform.position, alertBroadcastRadius);

            // Phân chia yểm trợ và xung phong (BR-20)
            ApplyTactics();
        }

        void Update()
        {
            if (!activated || Retreated) return;

            // Kiểm tra số lính đã chết
            int deadCount = 0;
            foreach (var m in members)
            {
                if (m == null)
                {
                    deadCount++;
                    continue;
                }
                var h = m.GetComponent<Health>();
                if (h == null || h.IsDead)
                {
                    deadCount++;
                }
            }

            // BR-20: Rút lui khi mất HƠN một nửa (mất đúng 1/2 chưa rút)
            if (initialCount > 0 && deadCount > (initialCount / 2f))
            {
                Retreat();
                return;
            }

            // Cập nhật hướng xung phong tiến về người chơi cho nhóm xung phong
            if (hasSupport && player != null && Time.time >= nextChargeUpdate)
            {
                nextChargeUpdate = Time.time + 0.6f;
                int supportCount = Mathf.Max(1, Mathf.RoundToInt(members.Count / 3f));
                for (int i = supportCount; i < members.Count; i++)
                {
                    var s = members[i];
                    if (s != null && s.IsCharging && !s.IsDead)
                    {
                        var agent = s.Agent ?? s.GetComponent<NavMeshAgent>();
                        if (agent != null && agent.enabled && agent.isOnNavMesh)
                        {
                            agent.SetDestination(player.position);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Phân chia lính theo chiến thuật yểm trợ + xung phong (BR-20).
        /// 1/3 nấp bắn yểm trợ, 2/3 xung phong (tiến về người chơi).
        /// </summary>
        void ApplyTactics()
        {
            if (!hasSupport || members == null || members.Count == 0) return;

            if (!player)
            {
                var pGo = GameObject.FindGameObjectWithTag("Player");
                if (pGo) player = pGo.transform;
            }

            int supportCount = Mathf.Max(1, Mathf.RoundToInt(members.Count / 3f));
            for (int i = 0; i < members.Count; i++)
            {
                var soldier = members[i];
                if (soldier == null) continue;

                if (i < supportCount)
                {
                    // 1/3 nấp bắn yểm trợ
                    soldier.IsCharging = false;
                    soldier.SetState(SoldierAI.State.Shoot);
                }
                else
                {
                    // 2/3 xung phong (NavMeshAgent tiến về người chơi)
                    soldier.IsCharging = true;
                    soldier.SetState(SoldierAI.State.Alerted);
                    var agent = soldier.Agent ?? soldier.GetComponent<NavMeshAgent>();
                    if (agent != null && agent.enabled && agent.isOnNavMesh && player != null)
                    {
                        agent.SetDestination(player.position);
                    }
                }
            }
        }

        /// <summary>
        /// Ra lệnh rút lui: đặt Retreated = true, chuyển Suppress, di chuyển về retreatPoint.
        /// Sau 5 giây Destroy group.
        /// </summary>
        void Retreat()
        {
            if (Retreated) return;
            Retreated = true;

            foreach (var m in members)
            {
                if (m == null) continue;
                var h = m.GetComponent<Health>();
                if (h != null)
                {
                    if (h.IsDead) continue;
                    h.Retreated = true; // Đánh dấu đã rút trên Health để tính Neutralized
                }

                m.IsCharging = false;
                m.SetState(SoldierAI.State.Suppress);

                var agent = m.Agent ?? m.GetComponent<NavMeshAgent>();
                if (agent != null && agent.enabled && agent.isOnNavMesh && retreatPoint != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(retreatPoint.position);
                }
            }

            // Sau 5 giây hủy nhóm
            Destroy(gameObject, 5f);
        }

        /// <summary>
        /// Phát tín hiệu tiếng súng đến tất cả SoldierAI trong bán kính radius.
        /// </summary>
        void BroadcastHeardShot(Vector3 origin, float radius)
        {
            var soldiers = FindObjectsOfType<SoldierAI>();
            foreach (var s in soldiers)
            {
                if (s != null && Vector3.Distance(origin, s.transform.position) <= radius)
                {
                    s.OnHeardShot(origin);
                }
            }
        }

        void OnDrawGizmos()
        {
            if (retreatPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(retreatPoint.position, 0.5f);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (retreatPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(retreatPoint.position, 0.5f);
                Gizmos.DrawWireSphere(retreatPoint.position, 0.8f);
                Gizmos.DrawLine(transform.position, retreatPoint.position);
            }
        }
    }
}
