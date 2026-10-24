using System;
using System.Collections.Generic;
using DBP.Core;
using UnityEngine;

namespace DBP.AI
{
    /// Sĩ quan Pháp chỉ huy một tốp lính (model Officer): lính trong bán kính chỉ huy bắn nhanh và chuẩn hơn,
    /// sĩ quan phát hiện người chơi thì hô cả tốp vào trận. Sĩ quan gục thì lính quanh đó hoảng loạn một lúc
    /// (nấp, bắn kém) — hạ sĩ quan trước là chiến thuật đáng thưởng.
    [RequireComponent(typeof(Health))]
    public class OfficerLeader : MonoBehaviour
    {
        public static readonly List<OfficerLeader> All = new List<OfficerLeader>();
        /// Sĩ quan gục: vị trí, bán kính ảnh hưởng.
        public static event Action<Vector3, float> Fallen;

        public float commandRadius = 18f;
        [Tooltip("Hệ số nhịp bắn và độ tản của lính trong bán kính chỉ huy (nhỏ hơn 1 = tốt hơn).")]
        public float fireBonus = 0.8f;

        Health health;
        SoldierAI soldier;
        bool shouted;

        void Awake() { health = GetComponent<Health>(); soldier = GetComponent<SoldierAI>(); }
        void OnEnable() { All.Add(this); health.Died += OnDied; }
        void OnDisable() { All.Remove(this); health.Died -= OnDied; }

        void Update()
        {
            if (shouted || !soldier || soldier.CurrentState == SoldierAI.State.Idle) return;
            shouted = true;
            SoldierAI.AlertAround(transform.position, commandRadius * 1.6f, soldier.LastKnownTarget);
        }

        void OnDied()
        {
            Fallen?.Invoke(transform.position, commandRadius * 1.2f);
            DBP.UI.CampaignNotices.Show("Sĩ quan địch đã gục — quân địch quanh đó rối loạn đội hình.");
        }

        /// Hệ số nhịp bắn/độ tản cho lính tại vị trí này (1 = không có sĩ quan gần).
        public static float BonusAt(Vector3 position, OfficerLeader except = null)
        {
            foreach (var o in All)
                if (o != except && o.health && !o.health.IsDead && (o.transform.position - position).sqrMagnitude <= o.commandRadius * o.commandRadius)
                    return o.fireBonus;
            return 1f;
        }
    }
}
