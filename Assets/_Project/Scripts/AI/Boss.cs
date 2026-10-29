using System.Collections;
using DBP.Core;
using DBP.Missions;
using DBP.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DBP.AI
{
    /// Trùm mỗi màn: lính địch đứng xa người chơi nhất (ưu tiên sĩ quan) được nâng thành trùm khi vào màn.
    /// Trùm có mọi kỹ năng chỉ huy (OfficerLeader), to hơn, máu gấp 4 lần chỉ huy; hiện tên và thanh máu lớn
    /// trên đỉnh màn hình khi đã giao chiến hoặc đến gần trong 45 m.
    public sealed class Boss : MonoBehaviour
    {
        public static Boss Current { get; private set; }
        public string Title { get; private set; }
        const float HealthMul = 4f, ShowRange = 45f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            // Màn 6 (vô hạn) tự tạo trùm theo đợt (EndlessObjective)
            if (mode != LoadSceneMode.Single || !MissionController.Current || !s.name.StartsWith("M") || s.name.StartsWith("M6") || Current) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (!player) return;
            // Ứng viên: lính bộ binh địch có AI (SoldierAI, hoặc lính gác ở Màn 3), gồm cả lính đợt sau chưa bật
            Health pick = null; float best = -1f;
            foreach (var h in FindObjectsByType<Health>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!h.gameObject.scene.IsValid() || h.Kind != TargetKind.Infantry || h.IsDead) continue;
                if (!h.GetComponent<SoldierAI>() && !h.GetComponent<SentryAI>() && !h.GetComponent<TrenchSoldierAI>()) continue;
                float d = (h.transform.position - player.transform.position).sqrMagnitude + (h.GetComponent<OfficerLeader>() ? 1e6f : 0f);
                if (d > best) { best = d; pick = h; }
            }
            if (!pick) return;
            if (!pick.GetComponent<OfficerLeader>()) pick.gameObject.AddComponent<OfficerLeader>();
            pick.gameObject.AddComponent<Boss>().Title = TitleFor(s.name);
        }

        /// Nâng một lính thành trùm (có kỹ năng chỉ huy), dùng cho đợt trùm ở Màn 6.
        public static Boss Promote(GameObject soldier, string title)
        {
            if (!soldier.GetComponent<OfficerLeader>()) soldier.AddComponent<OfficerLeader>();
            var boss = soldier.AddComponent<Boss>();
            boss.Title = title;
            return boss;
        }

        static string TitleFor(string scene)
        {
            if (scene.StartsWith("M1")) return "TRÙM · Thiếu tá chỉ huy cứ điểm Him Lam";
            if (scene.StartsWith("M2")) return "TRÙM · Đại úy chỉ huy cứ điểm Độc Lập";
            if (scene.StartsWith("M3")) return "TRÙM · Trung tá chỉ huy đồi E – D";
            if (scene.StartsWith("M4")) return "TRÙM · Chỉ huy trưởng chiến hào";
            return "TRÙM · Tướng chỉ huy tập đoàn cứ điểm";
        }

        Health health;
        OfficerLeader officer;
        Transform player;
        GameObject panel;
        Image fill;
        Text label;
        bool rallied;

        void Awake()
        {
            Current = this;
            health = GetComponent<Health>();
            officer = GetComponent<OfficerLeader>();
            health.Died += OnDied;
        }

        void OnDestroy() { if (Current == this) Current = null; if (panel) Destroy(panel.transform.root.gameObject); }

        IEnumerator Start()
        {
            yield return null; // OfficerLeader.Start nhân máu x2 trước
            transform.localScale *= 1.25f;
            health.Configure(health.Kind, health.Max * HealthMul);
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
            BuildBar();
        }

        void BuildBar()
        {
            var go = new GameObject("BossHudCanvas", typeof(Canvas), typeof(CanvasScaler));
            var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 41;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            panel = Rect("TrumPanel", go.transform, new Vector2(0, -70), new Vector2(620, 22), new Color(0, 0, 0, 0.75f)).gameObject;
            fill = Rect("TrumMau", panel.transform, Vector2.zero, new Vector2(612, 14), new Color(0.85f, 0.15f, 0.1f));
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            label = new GameObject("TrumTen", typeof(RectTransform)).AddComponent<Text>();
            label.rectTransform.SetParent(panel.transform, false);
            label.rectTransform.anchoredPosition = new Vector2(0, 26); label.rectTransform.sizeDelta = new Vector2(800, 30);
            label.font = UITheme.Bold; label.fontSize = 22; label.color = UITheme.Gold; label.alignment = TextAnchor.MiddleCenter; label.text = Title;
            label.raycastTarget = false;
            var o = label.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, .9f); o.effectDistance = new Vector2(2, -2);
            panel.SetActive(false);
        }

        static Image Rect(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            img.rectTransform.SetParent(parent, false);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, parent.GetComponent<Canvas>() ? 1f : 0.5f);
            img.rectTransform.anchoredPosition = pos; img.rectTransform.sizeDelta = size;
            img.color = color; img.raycastTarget = false;
            return img;
        }

        void Update()
        {
            if (!panel) return;
            bool gameplay = CampaignUI.Current && CampaignUI.Current.ScreenState == CampaignScreen.Gameplay;
            bool near = player && (player.position - transform.position).sqrMagnitude < ShowRange * ShowRange;
            bool engaged = officer && officer.Fighting;
            panel.SetActive(gameplay && !health.IsDead && (near || engaged));
            if (engaged && !rallied) { rallied = true; EnemyAnimatorDriver.Play(this, "Rally"); } // vào trận: hô hào
            fill.fillAmount = health.Current / health.Max;
        }

        void OnDied()
        {
            CampaignNotices.Show("ĐÃ HẠ TRÙM! Quân địch mất chỉ huy.");
            if (panel) panel.SetActive(false);
        }
    }
}
