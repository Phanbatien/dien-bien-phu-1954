using System.Collections.Generic;
using DBP.AI;
using DBP.Core;
using DBP.Missions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DBP.UI
{
    /// Bản đồ nhỏ góc phải dưới: camera nhìn thẳng từ trên cao (bán kính 55 m) xoay theo hướng người chơi.
    /// Mũi tên vàng = mình, chấm xanh = đồng đội, chấm đỏ = lính địch, ô vuông đỏ = lô cốt/xe tăng/ổ súng máy,
    /// ngôi sao vàng = mục tiêu hiện tại (nằm ngoài bản đồ thì dạt ra mép). Chỉ hiện khi đang chiến đấu.
    public sealed class MiniMap : MonoBehaviour
    {
        const float Radius = 55f;     // mét thể hiện từ tâm tới mép
        const float Size = 250f;      // điểm ảnh trên màn hình 1600×900
        const int TextureSize = 256;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && MissionController.Current && !FindAnyObjectByType<MiniMap>())
                new GameObject("MiniMap").AddComponent<MiniMap>();
        }

        Camera mapCam;
        RenderTexture rt;
        Transform player;
        RectTransform root, markers;
        Image playerArrow, objective;
        readonly List<Image> pool = new List<Image>();
        static Sprite dot, square, arrow, star;
        bool fogWas;

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
            rt = new RenderTexture(TextureSize, TextureSize, 16) { name = "BanDoNho" };
            mapCam = new GameObject("MapCamera").AddComponent<Camera>();
            mapCam.transform.SetParent(transform, false);
            mapCam.orthographic = true; mapCam.orthographicSize = Radius;
            mapCam.clearFlags = CameraClearFlags.SolidColor; mapCam.backgroundColor = new Color(0.22f, 0.18f, 0.12f);
            mapCam.targetTexture = rt; mapCam.nearClipPlane = 1f; mapCam.farClipPlane = 400f;
            mapCam.depth = -10; mapCam.allowHDR = false; mapCam.allowMSAA = false;
            RenderPipelineManager.beginCameraRendering += BeginCam;
            RenderPipelineManager.endCameraRendering += EndCam;
            BuildUi();
        }

        void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCam;
            RenderPipelineManager.endCameraRendering -= EndCam;
            if (rt) rt.Release();
        }

        // Bản đồ nhìn từ trên cao không có sương mù
        void BeginCam(ScriptableRenderContext c, Camera cam) { if (cam == mapCam) { fogWas = RenderSettings.fog; RenderSettings.fog = false; } }
        void EndCam(ScriptableRenderContext c, Camera cam) { if (cam == mapCam) RenderSettings.fog = fogWas; }

        void BuildUi()
        {
            var canvasGo = new GameObject("MiniMapCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 45;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            root = new GameObject("Khung", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvasGo.transform, false);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(1, 0);
            root.anchoredPosition = new Vector2(-36, 200); root.sizeDelta = new Vector2(Size + 24, Size + 24);
            var frame = root.gameObject.AddComponent<Image>(); frame.sprite = UITheme.WoodSprite; frame.type = Image.Type.Sliced; frame.raycastTarget = false;

            var mapRect = Child(root, "BanDo", Vector2.zero, new Vector2(Size, Size));
            var mask = mapRect.gameObject.AddComponent<Image>(); mask.sprite = UITheme.RoundedSprite; mask.type = Image.Type.Sliced; mask.raycastTarget = false;
            mapRect.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var raw = Child(mapRect, "Anh", Vector2.zero, new Vector2(Size, Size)).gameObject.AddComponent<RawImage>();
            raw.texture = rt; raw.color = new Color(0.95f, 0.92f, 0.82f); raw.raycastTarget = false;
            markers = Child(mapRect, "DauHieu", Vector2.zero, new Vector2(Size, Size));

            objective = Marker(Star(), new Color(1f, 0.85f, 0.25f), 18);
            playerArrow = Marker(Arrow(), new Color(1f, 0.82f, 0.3f), 20);
            playerArrow.rectTransform.anchoredPosition = Vector2.zero;
            var label = new GameObject("Bac", typeof(RectTransform)).AddComponent<Text>();
            label.rectTransform.SetParent(root, false); label.rectTransform.sizeDelta = new Vector2(Size, 24);
            label.rectTransform.anchoredPosition = new Vector2(0, -(Size + 24) / 2f - 14);
            label.font = UITheme.Bold; label.fontSize = 16; label.alignment = TextAnchor.MiddleCenter; label.color = UITheme.Cream;
            label.text = "BẢN ĐỒ  •  ▲ mình  ● ta  ● địch";
            label.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, .7f);
        }

        static RectTransform Child(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchoredPosition = pos; r.sizeDelta = size; return r;
        }

        Image Marker(Sprite sprite, Color color, float size)
        {
            var img = Child(markers, "DauHieu", Vector2.zero, new Vector2(size, size)).gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = false;
            var o = img.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, .8f); o.effectDistance = new Vector2(1, -1);
            return img;
        }

        void LateUpdate()
        {
            bool show = player && CampaignUI.Current && CampaignUI.Current.ScreenState == CampaignScreen.Gameplay;
            if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
            mapCam.enabled = show;
            if (!show) return;

            float yaw = player.eulerAngles.y;
            mapCam.transform.SetPositionAndRotation(player.position + Vector3.up * 150f, Quaternion.Euler(90f, yaw, 0f));
            int used = 0;
            foreach (var c in ComradeFollower.All)
                if (c && !c.IsDead) Place(used++, c.transform.position, dot, new Color(0.35f, 0.85f, 0.35f), 10, yaw, false);
            foreach (var h in FindHealths())
            {
                if (!h || h.IsDead || h.Kind == TargetKind.Player || !h.gameObject.activeInHierarchy) continue; // lính chết đã bị xóa
                bool big = h.Kind != TargetKind.Infantry;
                Place(used++, h.transform.position, big ? square : dot, big ? new Color(0.95f, 0.3f, 0.2f) : new Color(0.95f, 0.22f, 0.18f), big ? 14 : 10, yaw, false);
            }
            for (int i = used; i < pool.Count; i++) pool[i].enabled = false;

            var target = MissionController.Current ? MissionController.Current.CurrentObjective : null;
            objective.enabled = target;
            if (target) objective.rectTransform.anchoredPosition = ToMap(target.transform.position, yaw, true);
            playerArrow.transform.SetAsLastSibling();
        }

        // Danh sách Health làm mới mỗi giây (không tìm mỗi khung)
        Health[] healths = new Health[0];
        float nextScan;
        Health[] FindHealths()
        {
            if (Time.unscaledTime >= nextScan) { nextScan = Time.unscaledTime + 1f; healths = FindObjectsByType<Health>(FindObjectsSortMode.None); }
            return healths;
        }

        void Place(int i, Vector3 world, Sprite sprite, Color color, float size, float yaw, bool clamp)
        {
            while (pool.Count <= i) pool.Add(Marker(dot, Color.white, 10));
            var img = pool[i];
            var pos = ToMap(world, yaw, clamp);
            bool inside = pos.magnitude <= Size * 0.5f;
            img.enabled = inside;
            if (!inside) return;
            img.sprite = sprite; img.color = color; img.rectTransform.sizeDelta = new Vector2(size, size);
            img.rectTransform.anchoredPosition = pos;
        }

        Vector2 ToMap(Vector3 world, float yaw, bool clamp)
        {
            var rel = world - player.position;
            var local = Quaternion.Euler(0, -yaw, 0) * rel;
            var p = new Vector2(local.x, local.z) * (Size * 0.5f / Radius);
            float edge = Size * 0.5f - 10f;
            if (clamp && p.magnitude > edge) p = p.normalized * edge;
            return p;
        }

        // ───────── Hình dấu hiệu vẽ bằng code ─────────
        static Sprite Arrow() => arrow ? arrow : arrow = Shape("MuiTen", (x, y) => y > 0.1f && Mathf.Abs(x) < (0.95f - y) * 0.55f || y <= 0.1f && y > -0.85f && Mathf.Abs(x) < 0.5f * (y + 0.85f) && Mathf.Abs(x) > (0.1f - y) * 0.4f);
        static Sprite Star() => star ? star : star = Shape("Sao", (x, y) =>
        {
            float a = Mathf.Atan2(y, x), r = new Vector2(x, y).magnitude;
            float spike = 0.45f + 0.5f * Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2.5f + Mathf.PI / 4f)), 3f);
            return r < spike;
        });
        static Sprite Circle() => Shape("Cham", (x, y) => x * x + y * y < 0.8f);
        static Sprite Square() => Shape("Vuong", (x, y) => Mathf.Abs(x) < 0.75f && Mathf.Abs(y) < 0.75f);

        static Sprite Shape(string name, System.Func<float, float, bool> inside)
        {
            const int n = 32; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = name, hideFlags = HideFlags.DontSave };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                int hits = 0; // khử răng cưa 2×2
                for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++)
                    if (inside((x + 0.25f + sx * 0.5f) / n * 2f - 1f, (y + 0.25f + sy * 0.5f) / n * 2f - 1f)) hits++;
                px[y * n + x] = new Color(1, 1, 1, hits / 4f);
            }
            t.SetPixels(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f));
        }

        void Awake() { dot = dot ? dot : Circle(); square = square ? square : Square(); }
    }
}
