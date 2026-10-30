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
    /// Từ 28/10 chỉ huy mạnh hơn (giảm sức mạnh 31/10): to hơn, máu gấp đôi, sát thương +40%, né 20% đạn, và khi giao chiến dùng kỹ năng:
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

        public const float DodgeChance = 0.2f, DamageBonus = 1.4f;
        const float BlastPower = 1.3f;
        static float nextAirRaidAny; // cả màn chỉ một lệnh gọi máy bay mỗi 2 phút, dù có nhiều chỉ huy
        const int MaxReinforce = 1, MaxMines = 2;

        Health health;
        SoldierAI soldier;
        SentryAI sentry;
        TrenchSoldierAI trench;
        NavMeshAgent agent;
        Transform player;
        bool shouted;
        float engagedAt, nextReinforce, nextMine, nextBlast, nextAirRaid;
        int reinforced, mines;

        void Awake() { health = GetComponent<Health>(); soldier = GetComponent<SoldierAI>(); sentry = GetComponent<SentryAI>(); trench = GetComponent<TrenchSoldierAI>(); agent = GetComponent<NavMeshAgent>(); }
        void OnEnable() { All.Add(this); health.Died += OnDied; Health.AnyDodged += OnDodged; }
        void OnDisable() { All.Remove(this); health.Died -= OnDied; Health.AnyDodged -= OnDodged; }

        void Start()
        {
            transform.localScale *= 1.15f;
            health.Configure(health.Kind, health.Max * 2f);
            if (soldier) soldier.DamageMul = DamageBonus;
            if (sentry) sentry.DamageMul = DamageBonus;
            if (trench) trench.DamageMul = DamageBonus;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        /// Đang giao chiến: lính đã báo động, lính gác (Màn 3) chuyển sang chiến đấu, hoặc lính hào (Màn 4) đang bắn / quân ta trong 30 m.
        public bool Fighting => !health.IsDead && !health.Retreated &&
            ((soldier && soldier.isActiveAndEnabled && soldier.CurrentState != SoldierAI.State.Idle) || (sentry && sentry.CurrentState == SentryAI.State.Alert) ||
             (trench && player && (trench.CurrentState != TrenchSoldierAI.State.HaoCover || (player.position - transform.position).sqrMagnitude < 30f * 30f)));

        void Update()
        {
            if (!soldier) soldier = GetComponent<SoldierAI>(); // lính gác / lính hào được giao cho SoldierAI khi giao chiến
            bool fighting = Fighting && player;
            health.DodgeChance = fighting ? DodgeChance : 0f; // chỉ né khi đang giao chiến (biết bị nhắm bắn)
            if (!fighting) return;
            if (!shouted)
            {
                shouted = true;
                engagedAt = Time.time;
                nextReinforce = Time.time + 6f; nextMine = Time.time + 4f; nextBlast = Time.time + 12f; nextAirRaid = Time.time + 40f;
                SoldierAI.AlertAround(transform.position, commandRadius * 1.6f, player.position);
            }
            var foe = soldier && soldier.isActiveAndEnabled ? soldier.FoePosition : player.position;
            float dist = Vector3.Distance(transform.position, foe);

            if (reinforced < MaxReinforce && Time.time >= nextReinforce) { nextReinforce = Time.time + 25f; Reinforce(); }
            if (mines < MaxMines && Time.time >= nextMine && dist > 6f && dist < 30f) { nextMine = Time.time + 18f; PlaceMine(foe); }
            if (Time.time >= nextBlast && dist > 7f && dist < 24f && SoldierAI.SafeToBlast(foe, soldier))
            {
                nextBlast = Time.time + 22f;
                if (SoldierAI.Lob(this, foe, BlastPower)) DBP.UI.CampaignNotices.Show("Chỉ huy địch quăng bộc phá!");
            }
            if (Time.time >= nextAirRaid && Time.time >= nextAirRaidAny) { nextAirRaid = nextAirRaidAny = Time.time + 120f; DBP.Missions.AirRaid.Now(); Gesture(); DBP.UI.CampaignNotices.Show("Chỉ huy địch gọi máy bay yểm trợ!"); }
        }

        /// Né: lách ngang 1,5 m.
        void OnDodged(Health h, Vector3 _)
        {
            if (h != health || !agent || !agent.isOnNavMesh) return;
            agent.Move(transform.right * (UnityEngine.Random.value < 0.5f ? -1.5f : 1.5f));
        }

        /// Gọi 2 lính viện binh (nhân bản lính thường còn nguyên máu gần nhất) xuất hiện sau lưng rồi xung phong.
        void Reinforce()
        {
            // Mẫu: lính thường (SoldierAI hoặc lính gác) còn nguyên máu gần nhất
            Health template = null; float best = float.MaxValue;
            foreach (var h in FindObjectsByType<Health>(FindObjectsSortMode.None))
            {
                if (h.Kind != TargetKind.Infantry || h.IsDead || h.Current < h.Max || h.GetComponent<OfficerLeader>()) continue;
                if (!h.GetComponent<SoldierAI>() && !h.GetComponent<SentryAI>() && !h.GetComponent<TrenchSoldierAI>()) continue;
                float d = (h.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; template = h; }
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
            Gesture();
            DBP.UI.CampaignNotices.Show("Chỉ huy địch gọi viện binh!");
        }

        IEnumerator AlertNextFrame(Vector3 at)
        {
            yield return null; yield return null; // chờ Start của lính mới đặt Idle xong
            SoldierAI.AlertAround(at, 1.5f, player ? player.position : at);
        }

        /// Đặt mìn giữa mình và quân ta, lệch ngang một chút để khó đoán.
        void PlaceMine(Vector3 foe)
        {
            var p = Vector3.Lerp(transform.position, foe, UnityEngine.Random.Range(0.4f, 0.65f)) + transform.right * UnityEngine.Random.Range(-2f, 2f);
            if (!NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas)) return;
            mines++;
            Landmine.Place(hit.position);
            EnemyAnimatorDriver.Play(this, "Command");
            DBP.UI.CampaignNotices.Show("Chỉ huy địch đặt mìn — coi chừng bước chân!");
        }

        /// Ra lệnh: chỉ huy chỉ tay, trùm giơ hai tay hô hào.
        void Gesture() => EnemyAnimatorDriver.Play(this, GetComponent<Boss>() ? "Rally" : "Command");

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
