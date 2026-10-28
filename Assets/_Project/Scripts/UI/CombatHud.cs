using System.Collections.Generic;
using DBP.AI;
using DBP.Core;
using DBP.Missions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DBP.UI
{
    /// Thanh máu trên đầu lính (đỏ = địch, xanh = ta; trong 35 m, hiện khi đã trúng đòn hoặc ở gần) và số sát thương
    /// bay lên mỗi lần trúng đòn (trắng = thường, vàng to "CHÍ MẠNG!" = trúng đầu, đỏ = đồng đội bị thương).
    public sealed class CombatHud : MonoBehaviour
    {
        const float BarRange = 35f, NearRange = 12f, ShowAfterHit = 4f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && MissionController.Current && !FindAnyObjectByType<CombatHud>())
                new GameObject("CombatHud").AddComponent<CombatHud>();
        }

        struct Bar { public RectTransform root; public Image fill; }
        struct Popup { public Text text; public Vector3 world; public float born; public float rise; }

        RectTransform canvas;
        readonly List<Bar> bars = new List<Bar>();
        readonly List<Popup> popups = new List<Popup>();
        readonly Dictionary<Object, float> lastHit = new Dictionary<Object, float>();
        Health[] healths = new Health[0];
        float nextScan;

        void Awake()
        {
            var go = new GameObject("CombatHudCanvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 40;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvas = (RectTransform)go.transform;
            Health.AnyDamaged += OnDamaged;
            Health.AnyDodged += OnDodged;
            ComradeFollower.AnyHurt += OnAllyHurt;
        }

        void OnDestroy()
        {
            Health.AnyDamaged -= OnDamaged;
            Health.AnyDodged -= OnDodged;
            ComradeFollower.AnyHurt -= OnAllyHurt;
        }

        void OnDodged(Health h, Vector3 point) => Spawn(point, "NÉ!", new Color(0.6f, 0.85f, 1f), 28);

        void OnDamaged(Health h, float amount, Vector3 point, bool crit)
        {
            if (!h || h.Kind == TargetKind.Player) return;
            lastHit[h] = Time.time;
            Spawn(point, crit ? $"CHÍ MẠNG! {amount:0}" : $"{amount:0}", crit ? new Color(1f, 0.82f, 0.2f) : Color.white, crit ? 34 : 24);
        }

        void OnAllyHurt(ComradeFollower c, float amount, bool crit)
        {
            if (!c) return;
            lastHit[c] = Time.time;
            Spawn(c.transform.position + Vector3.up * 1.9f, (crit ? "CHÍ MẠNG! " : "") + $"-{amount:0}", new Color(1f, 0.4f, 0.35f), crit ? 28 : 20);
        }

        void Spawn(Vector3 world, string value, Color color, int size)
        {
            var t = new GameObject("SoSatThuong", typeof(RectTransform)).AddComponent<Text>();
            t.rectTransform.SetParent(canvas, false); t.rectTransform.sizeDelta = new Vector2(260, 50);
            t.font = UITheme.Bold; t.fontSize = size; t.color = color; t.alignment = TextAnchor.MiddleCenter; t.text = value;
            t.raycastTarget = false;
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, .85f); o.effectDistance = new Vector2(2, -2);
            popups.Add(new Popup { text = t, world = world + Random.insideUnitSphere * 0.25f, born = Time.unscaledTime, rise = Random.Range(0.9f, 1.3f) });
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            bool show = cam && CampaignUI.Current && CampaignUI.Current.ScreenState == CampaignScreen.Gameplay;
            canvas.gameObject.SetActive(show);
            if (!show) return;
            float scale = canvas.rect.height / Screen.height;

            // Số sát thương bay lên rồi mờ dần trong 1 giây
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                float age = Time.unscaledTime - p.born;
                if (age > 1.1f || !p.text) { if (p.text) Destroy(p.text.gameObject); popups.RemoveAt(i); continue; }
                var sp = cam.WorldToScreenPoint(p.world + Vector3.up * age * p.rise);
                p.text.enabled = sp.z > 0;
                p.text.rectTransform.anchoredPosition = ToCanvas(sp, scale);
                var c = p.text.color; c.a = Mathf.Clamp01(1.4f - age * 1.3f); p.text.color = c;
            }

            // Thanh máu
            if (Time.unscaledTime >= nextScan) { nextScan = Time.unscaledTime + 1f; healths = FindObjectsByType<Health>(FindObjectsSortMode.None); }
            int used = 0;
            var eye = cam.transform.position;
            foreach (var h in healths)
            {
                if (!h || h.IsDead || h.Kind == TargetKind.Player || !h.gameObject.activeInHierarchy) continue;
                if (h.Current >= h.Max && !Near(h, h.transform.position, eye)) continue;
                float top = h.TryGetComponent<Collider>(out var col) ? col.bounds.max.y + 0.35f : h.transform.position.y + 2.2f;
                Draw(used++, new Vector3(h.transform.position.x, top, h.transform.position.z), h.Current / h.Max, new Color(0.9f, 0.2f, 0.15f), cam, scale, h, eye);
            }
            foreach (var c in ComradeFollower.All)
            {
                if (!c || c.IsDead) continue;
                if (c.Health >= c.maxHealth && !Near(c, c.transform.position, eye)) continue;
                Draw(used++, c.transform.position + Vector3.up * 2.05f, c.Health / c.maxHealth, new Color(0.35f, 0.85f, 0.3f), cam, scale, c, eye);
            }
            for (int i = used; i < bars.Count; i++) bars[i].root.gameObject.SetActive(false);
        }

        bool Near(Object who, Vector3 pos, Vector3 eye) =>
            (pos - eye).sqrMagnitude < NearRange * NearRange || lastHit.TryGetValue(who, out var t) && Time.time - t < ShowAfterHit;

        void Draw(int i, Vector3 world, float frac, Color color, Camera cam, float scale, Object who, Vector3 eye)
        {
            while (bars.Count <= i) bars.Add(NewBar());
            var bar = bars[i];
            var sp = cam.WorldToScreenPoint(world);
            bool visible = sp.z > 0 && sp.z < BarRange;
            bar.root.gameObject.SetActive(visible);
            if (!visible) return;
            bar.root.anchoredPosition = ToCanvas(sp, scale);
            float w = Mathf.Lerp(64f, 34f, sp.z / BarRange);
            bar.root.sizeDelta = new Vector2(w, 8);
            bar.fill.rectTransform.sizeDelta = new Vector2((w - 2) * Mathf.Clamp01(frac), 6);
            bar.fill.color = color;
        }

        Bar NewBar()
        {
            var root = new GameObject("ThanhMau", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvas, false);
            var bg = root.gameObject.AddComponent<Image>(); bg.color = new Color(0, 0, 0, 0.65f); bg.raycastTarget = false;
            var fill = new GameObject("Mau", typeof(RectTransform)).AddComponent<Image>();
            fill.rectTransform.SetParent(root, false);
            fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = fill.rectTransform.pivot = new Vector2(0, 0.5f);
            fill.rectTransform.anchoredPosition = new Vector2(1, 0); fill.raycastTarget = false;
            return new Bar { root = root, fill = fill };
        }

        Vector2 ToCanvas(Vector3 screen, float scale) =>
            new Vector2((screen.x - Screen.width * 0.5f) * scale, (screen.y - Screen.height * 0.5f) * scale);
    }
}
