using System;
using System.Collections;
using System.Collections.Generic;
using DBP.Core;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// Sĩ quan Pháp chỉ huy một tốp lính (model Officer): lính trong bán kính chỉ huy bắn nhanh và chuẩn hơn,
    /// sĩ quan phát hiện người chơi thì hô cả tốp vào trận. Sĩ quan gục thì lính quanh đó bắn loạn một lúc
    /// — hạ sĩ quan trước là chiến thuật đáng thưởng.
    /// Từ 28/10 chỉ huy mạnh hơn: to hơn, máu và sát thương gấp đôi, né đạn, và khi giao chiến dùng kỹ năng:
    /// gọi viện binh, đặt mìn, quăng bộc phá, gọi máy bay ném bom; vẫn xung phong và đâm dao như lính (SoldierAI).
    [RequireComponent(typeof(Health))]
    public class OfficerLeader : MonoBehaviour
    {
        public static readonly List<OfficerLeader> All = new List<OfficerLeader>();
        /// Sĩ quan gục: vị trí, bán kính ảnh hưởng.
        public static event Action<Vector3, float> Fallen;

        public float commandRadius = 18f;
        [Tooltip("Hệ số nhịp bắn và độ tản của lính trong bán kính chỉ huy (nhỏ hơn 1 = tốt hơn).")]
        public float fireBonus = 0.8f;

        public const float DodgeChance = 0.35f;
        const float BlastPower = 1.7f;
        const int MaxReinforce = 2, MaxMines = 3;

        Health health;
        SoldierAI soldier;
        bool shouted;
        float engagedAt, nextReinforce, nextMine, nextBlast, nextAirRaid;
        int reinforced, mines;

        void Awake() { health = GetComponent<Health>(); soldier = GetComponent<SoldierAI>(); }
        void OnEnable() { All.Add(this); health.Died += OnDied; Health.AnyDodged += OnDodged; }
        void OnDisable() { All.Remove(this); health.Died -= OnDied; Health.AnyDodged -= OnDodged; }

        void Start()
        {
            transform.localScale *= 1.15f;
            health.Configure(health.Kind, health.Max * 2f);
            if (soldier) soldier.DamageMul = 2f;
        }

        void Update()
        {
            bool fighting = soldier && !soldier.IsDead && soldier.CurrentState != SoldierAI.State.Idle && !health.Retreated;
            health.DodgeChance = fighting ? DodgeChance : 0f; // chỉ né khi đang giao chiến (biết bị nhắm bắn)
            if (!fighting) return;
            if (!shouted)
            {
                shouted = true;
                engagedAt = Time.time;
                nextReinforce = Time.time + 6f; nextMine = Time.time + 4f; nextBlast = Time.time + 9f; nextAirRaid = Time.time + 15f;
                SoldierAI.AlertAround(transform.position, commandRadius * 1.6f, soldier.LastKnownTarget);
            }
            var foe = soldier.FoePosition;
            float dist = Vector3.Distance(transform.position, foe);

            if (reinforced < MaxReinforce && Time.time >= nextReinforce) { nextReinforce = Time.time + 25f; Reinforce(); }
            if (mines < MaxMines && Time.time >= nextMine && dist > 6f && dist < 30f) { nextMine = Time.time + 12f; PlaceMine(foe); }
            if (Time.time >= nextBlast && dist > 7f && dist < 24f && SoldierAI.SafeToBlast(foe, soldier))
            {
                nextBlast = Time.time + 14f;
                if (SoldierAI.Lob(this, foe, BlastPower)) DBP.UI.CampaignNotices.Show("Chỉ huy địch quăng bộc phá!");
            }
            if (Time.time >= nextAirRaid) { nextAirRaid = Time.time + 75f; DBP.Missions.AirRaid.Now(); DBP.UI.CampaignNotices.Show("Chỉ huy địch gọi máy bay yểm trợ!"); }
        }

        /// Né: lách ngang 1,5 m.
        void OnDodged(Health h, Vector3 _)
        {
            if (h != health || !soldier || !soldier.Agent || !soldier.Agent.isOnNavMesh) return;
            soldier.Agent.Move(transform.right * (UnityEngine.Random.value < 0.5f ? -1.5f : 1.5f));
        }

        /// Gọi 2 lính viện binh (nhân bản lính thường còn nguyên máu gần nhất) xuất hiện sau lưng rồi xung phong.
        void Reinforce()
        {
            SoldierAI template = null; float best = float.MaxValue;
            foreach (var s in SoldierAI.All)
            {
                if (!s || s.IsDead || s.GetComponent<OfficerLeader>() || s.Health.Current < s.Health.Max) continue;
                float d = (s.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; template = s; }
            }
            if (!template) return;
            reinforced++;
            for (int i = 0; i < 2; i++)
            {
                var p = transform.position - transform.forward * 3f + transform.right * (i == 0 ? -2f : 2f);
                if (!NavMesh.SamplePosition(p, out var hit, 4f, NavMesh.AllAreas)) continue;
                var clone = Instantiate(template.gameObject, hit.position, transform.rotation);
                clone.name = "VienBinh";
                CombatEffects.Explosion(hit.position, 0.4f);
                StartCoroutine(AlertNextFrame(hit.position));
            }
            DBP.UI.CampaignNotices.Show("Chỉ huy địch gọi viện binh!");
        }

        IEnumerator AlertNextFrame(Vector3 at)
        {
            yield return null; yield return null; // chờ Start của lính mới đặt Idle xong
            SoldierAI.AlertAround(at, 1.5f, soldier ? soldier.FoePosition : at);
        }

        /// Đặt mìn giữa mình và quân ta, lệch ngang một chút để khó đoán.
        void PlaceMine(Vector3 foe)
        {
            var p = Vector3.Lerp(transform.position, foe, UnityEngine.Random.Range(0.4f, 0.65f)) + transform.right * UnityEngine.Random.Range(-2f, 2f);
            if (!NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas)) return;
            mines++;
            Landmine.Place(hit.position);
            DBP.UI.CampaignNotices.Show("Chỉ huy địch đặt mìn — coi chừng bước chân!");
        }

        void OnDied()
        {
            Fallen?.Invoke(transform.position, commandRadius * 1.2f);
            DBP.UI.CampaignNotices.Show("Chỉ huy địch đã gục — quân địch quanh đó rối loạn đội hình.");
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
