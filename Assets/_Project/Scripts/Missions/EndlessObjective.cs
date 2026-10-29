using System.Collections;
using System.Collections.Generic;
using DBP.AI;
using DBP.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace DBP.Missions
{
    /// Thử thách vô hạn (Màn 6): không bao giờ hoàn thành — địch kéo tới thành đợt từ mọi phía cho tới khi người chơi gục.
    /// Đợt n có 4 + 2n lính (tối đa 30 lính còn sống cùng lúc), đợt chẵn có sĩ quan chỉ huy, cứ 5 đợt có một trùm.
    /// Đợt mới tới khi chỉ còn ≤ 2 lính hoặc sau 45 giây. Bảng góc trên trái: đợt, số địch đã diệt, kỷ lục (lưu trên máy).
    public sealed class EndlessObjective : Objective
    {
        const string BestKey = "dbp.endless.best";
        const int MaxAlive = 30;

        [SerializeField] Transform templates;

        public static int Best => PlayerPrefs.GetInt(BestKey, 0);
        public int Wave { get; private set; }
        public int Alive { get { int n = 0; foreach (var s in spawned) if (s && !s.IsDead) n++; return n; } }

        readonly List<SoldierAI> spawned = new List<SoldierAI>();
        float nextWaveAt, lastWaveAt;
        Transform player;
        Text hud;

        protected override bool Check() => false;
        public override string Progress => $"Đợt {Wave} · đã diệt {(Mission ? Mission.Attempt.Kills : 0)}";

        public override void Activate(MissionController mission)
        {
            base.Activate(mission);
            player = mission.Player ? mission.Player.transform : null;
            nextWaveAt = Time.time + 4f;
            BuildHud();
        }

        void Update()
        {
            if (!Active || !Mission || !player || Mission.PlayerDead) return;
            spawned.RemoveAll(s => !s);
            int alive = Alive;
            if (Time.time >= nextWaveAt || Wave > 0 && alive <= 2 && Time.time - lastWaveAt > 8f) SpawnWave(alive);
            int kills = Mission.Attempt.Kills;
            if (kills > Best) { PlayerPrefs.SetInt(BestKey, kills); PlayerPrefs.Save(); }
            if (hud)
            {
                hud.text = $"THỬ THÁCH VÔ HẠN\nĐợt {Wave}   ·   Đã diệt {kills}   ·   Kỷ lục {Best}";
                hud.transform.parent.gameObject.SetActive(CampaignUI.Current && CampaignUI.Current.ScreenState == CampaignScreen.Gameplay);
            }
        }

        void SpawnWave(int alive)
        {
            Wave++;
            lastWaveAt = Time.time;
            nextWaveAt = Time.time + 45f;
            if (!templates || templates.childCount == 0) return;
            int count = Mathf.Min(4 + Wave * 2, MaxAlive - alive);
            var fresh = new List<SoldierAI>();
            for (int i = 0; i < count; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                var p = player.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Random.Range(45f, 60f);
                if (!NavMesh.SamplePosition(p, out var hit, 8f, NavMesh.AllAreas)) continue;
                var template = templates.GetChild(Random.Range(0, templates.childCount)).gameObject;
                var go = Instantiate(template, hit.position, Quaternion.LookRotation(player.position - hit.position));
                go.name = $"Dot{Wave}_Linh{i}";
                go.SetActive(true);
                if (i == 0 && Wave % 5 == 0) Boss.Promote(go, $"TRÙM ĐỢT {Wave} · Chỉ huy phản kích");
                else if (i == 0 && Wave % 2 == 0) go.AddComponent<OfficerLeader>();
                var soldier = go.GetComponent<SoldierAI>();
                if (soldier) { spawned.Add(soldier); fresh.Add(soldier); }
            }
            CampaignNotices.Show(Wave % 5 == 0 ? $"Đợt {Wave}: TRÙM cùng {count - 1} lính địch đang kéo tới!" : $"Đợt {Wave}: {count} lính địch đang kéo tới!");
            StartCoroutine(Charge(fresh));
        }

        /// Lính mới chờ Start xong (đặt trạng thái rảnh) rồi mới báo động tiến về phía người chơi.
        IEnumerator Charge(List<SoldierAI> soldiers)
        {
            yield return null; yield return null;
            foreach (var s in soldiers)
                if (s && player) SoldierAI.AlertAround(s.transform.position, .5f, player.position);
        }

        void BuildHud()
        {
            var go = new GameObject("ThuThachHud", typeof(Canvas), typeof(CanvasScaler));
            var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 39;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var panel = new GameObject("Bang", typeof(RectTransform)).AddComponent<Image>();
            panel.rectTransform.SetParent(go.transform, false);
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(.5f, 1f);
            panel.rectTransform.anchoredPosition = new Vector2(0, -130); panel.rectTransform.sizeDelta = new Vector2(560, 64);
            panel.color = new Color(0, 0, 0, .55f); panel.raycastTarget = false;
            hud = new GameObject("Chu", typeof(RectTransform)).AddComponent<Text>();
            hud.rectTransform.SetParent(panel.transform, false);
            hud.rectTransform.anchorMin = Vector2.zero; hud.rectTransform.anchorMax = Vector2.one; hud.rectTransform.sizeDelta = Vector2.zero;
            hud.font = UITheme.Bold; hud.fontSize = 22; hud.color = UITheme.Gold; hud.alignment = TextAnchor.MiddleCenter; hud.raycastTarget = false;
        }
    }
}
