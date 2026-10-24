using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DBP.Core;
namespace DBP.UI
{
    /// Khối dựng giao diện theo UITheme: bảng = khung gỗ + giấy da; nút bo tròn theo vai trò; chữ theo một thang cỡ,
    /// mực nâu khi nằm trên giấy, kem có bóng khi nằm trên nền tối.
    internal static class UIWidgets
    {
        public static readonly Color Ink = new Color32(20, 28, 26, 255);
        public static readonly Color PanelColor = new Color32(33, 44, 39, 250);
        public static readonly Color Gold = UITheme.Gold;
        public static readonly Color Paper = UITheme.Cream;
        static Font font;
        static Font Font => font ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); return rect;
        }
        public static RectTransform Fill(Transform parent, string name)
        {
            var r = Rect(parent, name, 0, 0, 0, 0); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }

        /// Không truyền màu: bảng gỗ + giấy da. Truyền màu: khối bo tròn màu đó (HUD, nền, thanh kẻ).
        public static Image Panel(Transform parent, string name, float x, float y, float w, float h, Color? color = null)
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            if (color == null)
            {
                image.sprite = UITheme.WoodSprite; image.type = Image.Type.Sliced; image.color = Color.white;
                var paper = Fill(image.transform, "Giay").gameObject.AddComponent<Image>();
                paper.rectTransform.offsetMin = new Vector2(12, 12); paper.rectTransform.offsetMax = new Vector2(-12, -12);
                paper.sprite = UITheme.PaperSprite; paper.type = Image.Type.Sliced; paper.raycastTarget = false;
                var shadow = image.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .45f); shadow.effectDistance = new Vector2(5, -6);
                return image;
            }
            image.color = color.Value;
            if (name == "Backdrop") { image.sprite = UITheme.VignetteSprite; image.color = new Color(1, 1, 1, color.Value.a); }
            else if (w >= 40 && h >= 40) { image.sprite = UITheme.RoundedSprite; image.type = Image.Type.Sliced; }
            return image;
        }

        public static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size = 24, Color? color = null)
        {
            var text = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = Font; text.fontSize = UITheme.Size(size); text.text = value;
            text.supportRichText = false; text.raycastTarget = false; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.lineSpacing = 1.08f;
            if (h >= text.fontSize * 4f) text.alignment = TextAnchor.UpperLeft; // đoạn văn dài: canh lề trên
            bool accent = color.HasValue && color.Value == Gold;
            if (OnPaper(parent, text.rectTransform))
            {
                // Trên giấy: mực nâu, tiêu đề đỏ nâu đậm
                text.color = accent ? UITheme.Heading : color.HasValue && color.Value != Paper ? Darken(color.Value) : UITheme.Ink;
                if (accent || text.fontSize >= 22) text.fontStyle = FontStyle.Bold;
            }
            else
            {
                text.color = color ?? Paper;
                if (accent || text.fontSize >= 32) text.fontStyle = FontStyle.Bold;
                var shadow = text.gameObject.AddComponent<Shadow>();
                bool small = text.fontSize <= 22;
                shadow.effectColor = new Color(0, 0, 0, small ? .55f : .75f); shadow.effectDistance = small ? new Vector2(1, -1) : new Vector2(2, -2);
            }
            return text;
        }

        static Color Darken(Color c) => new Color(c.r * 0.45f, c.g * 0.4f, c.b * 0.35f, 1f);

        /// Chữ nằm trong vùng giấy da của một bảng đã dựng trước nó (cùng cha) thì là chữ trên giấy.
        static bool OnPaper(Transform parent, RectTransform label)
        {
            if (!parent) return false;
            var corners = new Vector3[4]; label.GetWorldCorners(corners);
            var center = (corners[0] + corners[2]) * 0.5f;
            foreach (var img in parent.GetComponentsInChildren<Image>())
            {
                if (img.sprite != UITheme.PaperSprite) continue;
                var c = new Vector3[4]; img.rectTransform.GetWorldCorners(c);
                if (center.x >= c[0].x && center.x <= c[2].x && center.y >= c[0].y && center.y <= c[2].y) return true;
            }
            return false;
        }

        /// Vai trò nút theo tên: xanh = tiến lên/xác nhận, đỏ = rời bỏ, gỗ = các nút còn lại.
        static Color RoleColor(string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("deploy") || n.Contains("confirm") || n.Contains("begin") || n.Contains("resume") || n.StartsWith("buy")
                || n.Contains("next") || n.Contains("retry") || n.Contains("briefing") || n.Contains("epilogue") || n.Contains("prologue")) return UITheme.Green;
            if (n.Contains("abandon") || n.Contains("quit") || n.Contains("delete")) return UITheme.Red;
            if (n.StartsWith("mission") || n.Contains("leaderboard") || n.Contains("credits")) return UITheme.Amber;
            return UITheme.Wood;
        }

        public static Button Button(Transform parent, string name, string value, float x, float y, float w, float h, UnityAction click, bool enabled = true)
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            var role = RoleColor(name);
            image.sprite = UITheme.PillSprite(enabled ? role : UITheme.Gray); image.type = Image.Type.Sliced;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = enabled;
            var colors = button.colors; colors.highlightedColor = new Color(1.12f, 1.12f, 1.08f); colors.pressedColor = new Color(.85f, .85f, .85f);
            colors.disabledColor = new Color(1f, 1f, 1f, .75f); button.colors = colors;
            var shadow = image.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .4f); shadow.effectDistance = new Vector2(3, -4);
            var text = Label(image.transform, "Label", value, 18, 0, w - 36, h, h >= 60 ? 24 : 22, UITheme.Cream); text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            if (!enabled) text.color = new Color(.92f, .9f, .85f, .85f);
            button.onClick.AddListener(() => { CampaignAudio.Interface("ui_click"); click(); }); return button;
        }
        public static InputField Input(Transform parent, string name, string placeholder, float x, float y, float w)
        {
            var image = Rect(parent, name, x, y, w, 56).gameObject.AddComponent<Image>();
            image.sprite = UITheme.PaperSprite; image.type = Image.Type.Sliced;
            var outline = image.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.3f, .18f, .08f, .9f); outline.effectDistance = new Vector2(2, -2);
            var field = image.gameObject.AddComponent<InputField>(); field.targetGraphic = image;
            field.textComponent = Label(image.transform, "Value", "", 16, 0, w - 32, 56, 24);
            field.placeholder = Label(image.transform, "Placeholder", placeholder, 16, 0, w - 32, 56, 22);
            field.textComponent.color = UITheme.Ink; field.placeholder.color = new Color(.45f, .36f, .26f, .8f);
            ((Text)field.placeholder).fontStyle = FontStyle.Italic;
            field.characterLimit = 40; return field;
        }
        public static Toggle Toggle(Transform parent, string name, string label, bool value, float x, float y, float w, UnityAction<bool> change)
        {
            var rect = Rect(parent, name, x, y, w, 56); var toggle = rect.gameObject.AddComponent<Toggle>();
            var box = rect.gameObject.AddComponent<Image>(); box.color = Color.clear; // vùng bấm cả dòng
            var square = Rect(rect, "Box", 0, 11, 34, 34).gameObject.AddComponent<Image>();
            square.sprite = UITheme.PaperSprite; square.type = Image.Type.Sliced;
            var o = square.gameObject.AddComponent<Outline>(); o.effectColor = new Color(.3f, .18f, .08f, 1f); o.effectDistance = new Vector2(2, -2);
            var check = Rect(square.transform, "Check", 6, 6, 22, 22).gameObject.AddComponent<Image>();
            check.sprite = UITheme.PillSprite(UITheme.Green); check.type = Image.Type.Sliced;
            toggle.targetGraphic = square; toggle.graphic = check; toggle.isOn = value;
            Label(rect, "Label", label, 48, 0, w - 48, 56, 20);
            toggle.onValueChanged.AddListener(change); return toggle;
        }
        public static Slider Volume(Transform parent, float value, float x, float y, UnityAction<float> change)
        {
            var rect = Rect(parent, "VolumeSlider", x, y, 320, 44); var slider = rect.gameObject.AddComponent<Slider>();
            var track = Panel(rect, "Track", 0, 15, 320, 14, new Color32(60, 40, 24, 255));
            track.sprite = UITheme.PillSprite(UITheme.Wood); track.type = Image.Type.Sliced; track.color = Color.white;
            var fillArea = Rect(rect, "FillArea", 4, 18, 312, 8);
            var fill = Panel(fillArea, "Fill", 0, 0, 0, 8, UITheme.Gold); fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.sizeDelta = Vector2.zero; slider.fillRect = fill.rectTransform;
            var area = Rect(rect, "HandleArea", 12, 0, 296, 44);
            var handle = Panel(area, "Handle", 0, 6, 26, 32, Color.white); slider.handleRect = handle.rectTransform;
            handle.sprite = UITheme.PillSprite(UITheme.Amber); handle.type = Image.Type.Sliced;
            handle.rectTransform.pivot = new Vector2(.5f, .5f);
            handle.rectTransform.anchoredPosition = Vector2.zero;
            handle.rectTransform.sizeDelta = new Vector2(26, -12);
            slider.targetGraphic = handle; slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight; slider.value = value;
            slider.onValueChanged.AddListener(change); return slider;
        }
        public static Image Meter(Transform parent, string name, float x, float y, float w, Color color)
        {
            var background = Panel(parent, name, x, y, w, 10, new Color32(40, 30, 22, 230));
            var fill = Panel(background.transform, "Fill", 0, 0, w, 10, color);
            return fill;
        }
    }
}
