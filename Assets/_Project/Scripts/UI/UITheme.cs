using System.Collections.Generic;
using UnityEngine;

namespace DBP.UI
{
    /// Phong cách giao diện chung: khung gỗ vân dọc + giấy da cũ mép cháy sém, nút bo tròn đổ bóng,
    /// chữ mực nâu trên giấy, chữ kem có bóng trên nền tối. Mọi texture vẽ bằng code (không cần ảnh ngoài),
    /// dùng cho cả UGUI (UIWidgets) lẫn IMGUI (bảng câu hỏi).
    public static class UITheme
    {
        public static readonly Color Ink = new Color32(42, 26, 14, 255);        // chữ trên giấy
        public static readonly Color Heading = new Color32(122, 40, 22, 255);   // tiêu đề trên giấy
        public static readonly Color Cream = new Color32(246, 236, 212, 255);   // chữ trên nền tối
        public static readonly Color Gold = new Color32(232, 196, 112, 255);
        public static readonly Color Green = new Color32(88, 150, 62, 255);
        public static readonly Color Red = new Color32(178, 58, 42, 255);
        public static readonly Color Amber = new Color32(214, 150, 46, 255);
        public static readonly Color Wood = new Color32(132, 84, 46, 255);
        public static readonly Color Gray = new Color32(120, 112, 100, 255);

        /// Thang cỡ chữ duy nhất của game: mọi cỡ được làm tròn về một bậc để chữ đồng đều.
        public static int Size(int requested) =>
            requested >= 40 ? 44 : requested >= 30 ? 32 : requested >= 25 ? 26 : requested >= 21 ? 22 : 18;

        static Texture2D paper, wood, vignette, rounded;
        static readonly Dictionary<Color, Texture2D> pills = new Dictionary<Color, Texture2D>();
        static Sprite paperSprite, woodSprite, vignetteSprite, roundedSprite;
        static readonly Dictionary<Color, Sprite> pillSprites = new Dictionary<Color, Sprite>();

        public static Texture2D PaperTexture => paper ? paper : paper = MakePaper();
        public static Texture2D WoodTexture => wood ? wood : wood = MakeWood();
        public static Texture2D RoundedTexture => rounded ? rounded : rounded = MakeRounded(64, 16, Color.white, Color.white, 0, Color.clear);
        public static Texture2D PillTexture(Color c)
        {
            if (pills.TryGetValue(c, out var t) && t) return t;
            return pills[c] = MakeRounded(128, 30, Color.Lerp(c, Color.white, 0.28f), Color.Lerp(c, Color.black, 0.22f), 3, Color.Lerp(c, Color.black, 0.6f));
        }

        public static Sprite PaperSprite => paperSprite ? paperSprite : paperSprite = Slice(PaperTexture, 48);
        public static Sprite WoodSprite => woodSprite ? woodSprite : woodSprite = Slice(WoodTexture, 40);
        public static Sprite RoundedSprite => roundedSprite ? roundedSprite : roundedSprite = Slice(RoundedTexture, 20);
        public static Sprite VignetteSprite => vignetteSprite ? vignetteSprite : vignetteSprite = Sprite.Create(vignette ? vignette : vignette = MakeVignette(), new Rect(0, 0, 256, 256), new Vector2(.5f, .5f));
        public static Sprite PillSprite(Color c)
        {
            if (pillSprites.TryGetValue(c, out var s) && s) return s;
            return pillSprites[c] = Slice(PillTexture(c), 34);
        }

        static Sprite Slice(Texture2D t, int border) =>
            Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));

        static Texture2D New(int w, int h, string name) =>
            new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };

        /// Giấy da: nền kem lốm đốm, sợi giấy mờ, mép sẫm như bị hơ lửa.
        static Texture2D MakePaper()
        {
            const int n = 256; var t = New(n, n, "GiayDa"); var px = new Color[n * n];
            var baseCol = new Color(0.93f, 0.86f, 0.70f);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float blot = Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 0.7f + Mathf.PerlinNoise(x * 0.07f + 30, y * 0.07f + 11) * 0.3f;
                float fiber = Mathf.PerlinNoise(x * 0.35f + 5, y * 0.35f) * 0.025f;
                float edge = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y)) / 40f;
                float burn = 1f - Mathf.Clamp01(edge + Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.35f);
                var c = baseCol * (0.94f + blot * 0.09f - fiber);
                c = Color.Lerp(c, new Color(0.55f, 0.38f, 0.2f), burn * burn * 0.85f);
                c.a = 1f;
                px[y * n + x] = c;
            }
            t.SetPixels(px); t.Apply(false, true); return t;
        }

        /// Khung gỗ: ván dọc có vân, khe ván sẫm, mép vát sáng-tối, góc bo nhẹ.
        static Texture2D MakeWood()
        {
            const int n = 256; var t = New(n, n, "KhungGo"); var px = new Color[n * n];
            var light = new Color(0.62f, 0.4f, 0.22f); var dark = new Color(0.36f, 0.21f, 0.11f);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float plank = (x % 64) / 64f;
                float grain = Mathf.PerlinNoise(x * 0.08f + Mathf.Floor(x / 64f) * 7, y * 0.012f) * 0.7f + Mathf.Sin((x + Mathf.PerlinNoise(y * 0.02f, x * 0.02f) * 30f) * 0.5f) * 0.15f;
                var c = Color.Lerp(dark, light, Mathf.Clamp01(grain));
                if (plank < 0.03f || plank > 0.97f) c *= 0.55f;                         // khe giữa hai ván
                float edge = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y));
                if (edge < 3) c *= 0.5f; else if (edge < 6) c = Color.Lerp(c, Color.white, 0.12f); // viền vát
                float corner = CornerAlpha(x, y, n, n, 14);
                c.a = corner;
                px[y * n + x] = c;
            }
            t.SetPixels(px); t.Apply(false, true); return t;
        }

        /// Hình chữ nhật bo tròn có gradient dọc, viền và vệt sáng trên (nút bấm).
        static Texture2D MakeRounded(int size, int radius, Color top, Color bottom, int outline, Color outlineColor)
        {
            int w = size, h = size / 2 < radius * 2 + 2 ? radius * 2 + 4 : size / 2;
            if (outline == 0) h = size;
            var t = New(w, h, "BoTron"); var px = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float a = CornerAlpha(x, y, w, h, radius);
                float v = y / (float)(h - 1);
                var c = Color.Lerp(bottom, top, v);
                if (outline > 0)
                {
                    float inner = CornerAlpha(x - outline, y - outline, w - outline * 2, h - outline * 2, radius - outline);
                    if (y > h * 0.55f && y < h - outline - 2) c = Color.Lerp(c, Color.white, 0.12f); // vệt sáng nửa trên
                    c = Color.Lerp(outlineColor, c, inner);
                }
                c.a = a;
                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply(false, true); return t;
        }

        static float CornerAlpha(int x, int y, int w, int h, int r)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return 0f;
            float cx = Mathf.Clamp(x + 0.5f, r, w - r), cy = Mathf.Clamp(y + 0.5f, r, h - r);
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
            return Mathf.Clamp01(r - d + 0.5f);
        }

        /// Nền toàn màn hình: nâu sẫm, sáng nhẹ ở giữa, tối dần ra mép (như đèn dầu trong hầm).
        static Texture2D MakeVignette()
        {
            const int n = 256; var t = New(n, n, "NenToi"); var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n * 0.5f, n * 0.55f)) / (n * 0.7f);
                float k = Mathf.Clamp01(1f - d * d) * 0.6f + Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.08f;
                px[y * n + x] = new Color(0.1f + k * 0.16f, 0.075f + k * 0.11f, 0.05f + k * 0.06f, 1f);
            }
            t.SetPixels(px); t.Apply(false, true); return t;
        }

        // ───────── IMGUI ─────────

        /// Kiểu nút IMGUI bo tròn theo màu, có trạng thái rê chuột/nhấn.
        public static GUIStyle ImguiButton(GUIStyle baseStyle, Color color)
        {
            var s = new GUIStyle(baseStyle);
            s.normal.background = PillTexture(color);
            s.hover.background = PillTexture(Color.Lerp(color, Color.white, 0.15f));
            s.active.background = PillTexture(Color.Lerp(color, Color.black, 0.15f));
            s.focused.background = s.hover.background;
            s.onNormal.background = s.normal.background;
            s.border = new RectOffset(34, 34, 30, 30);
            s.normal.textColor = s.hover.textColor = s.active.textColor = s.focused.textColor = Cream;
            s.fontStyle = FontStyle.Bold;
            return s;
        }

        static GUIStyle woodStyle, paperStyle;

        /// Vẽ khung gỗ + giấy da (IMGUI), cắt 9 mảnh để mép không bị kéo giãn.
        public static void ImguiPanel(Rect r)
        {
            if (Event.current.type != EventType.Repaint) return;
            woodStyle ??= new GUIStyle { normal = { background = WoodTexture }, border = new RectOffset(40, 40, 40, 40) };
            paperStyle ??= new GUIStyle { normal = { background = PaperTexture }, border = new RectOffset(48, 48, 48, 48) };
            var old = GUI.color; GUI.color = Color.white;
            woodStyle.Draw(r, false, false, false, false);
            paperStyle.Draw(new Rect(r.x + 14, r.y + 14, r.width - 28, r.height - 28), false, false, false, false);
            GUI.color = old;
        }
    }
}
