using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace DBP.UI
{
    internal static class UIWidgets
    {
        public static readonly Color Ink = new Color32(20, 28, 26, 255);
        public static readonly Color PanelColor = new Color32(33, 44, 39, 250);
        public static readonly Color Gold = new Color32(219, 187, 111, 255);
        public static readonly Color Paper = new Color32(240, 233, 215, 255);
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
        public static Image Panel(Transform parent, string name, float x, float y, float w, float h, Color? color = null)
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>(); image.color = color ?? PanelColor; return image;
        }
        public static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size = 24, Color? color = null)
        {
            var text = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = Font; text.fontSize = size; text.color = color ?? Paper; text.text = value;
            text.supportRichText = false; text.raycastTarget = false; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        public static Button Button(Transform parent, string name, string value, float x, float y, float w, float h, UnityAction click, bool enabled = true)
        {
            var image = Panel(parent, name, x, y, w, h, new Color32(78, 96, 71, 255));
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = enabled;
            var colors = button.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.05f); colors.disabledColor = new Color(.45f, .45f, .45f, .8f); button.colors = colors;
            var text = Label(image.transform, "Label", value, 14, 0, w - 28, h, 23); text.alignment = TextAnchor.MiddleCenter;
            button.onClick.AddListener(click); return button;
        }
        public static InputField Input(Transform parent, string name, string placeholder, float x, float y, float w)
        {
            var image = Panel(parent, name, x, y, w, 56, new Color32(53, 65, 57, 255));
            var field = image.gameObject.AddComponent<InputField>(); field.targetGraphic = image;
            field.textComponent = Label(image.transform, "Value", "", 14, 0, w - 28, 56, 24);
            field.placeholder = Label(image.transform, "Placeholder", placeholder, 14, 0, w - 28, 56, 23, new Color(.65f,.68f,.62f));
            field.characterLimit = 40; return field;
        }
        public static Toggle Toggle(Transform parent, string name, string label, bool value, float x, float y, float w, UnityAction<bool> change)
        {
            var rect = Rect(parent, name, x, y, w, 56); var toggle = rect.gameObject.AddComponent<Toggle>();
            var box = Panel(rect, "Box", 0, 12, 30, 30, new Color32(64, 78, 66, 255));
            var check = Panel(box.transform, "Check", 6, 6, 18, 18, Gold);
            toggle.targetGraphic = box; toggle.graphic = check; toggle.isOn = value;
            Label(rect, "Label", label, 44, 0, w - 44, 56, 20);
            toggle.onValueChanged.AddListener(change); return toggle;
        }
        public static Slider Volume(Transform parent, float value, float x, float y, UnityAction<float> change)
        {
            var rect = Rect(parent, "VolumeSlider", x, y, 320, 44); var slider = rect.gameObject.AddComponent<Slider>();
            Panel(rect, "Track", 0, 18, 320, 8, new Color32(60, 72, 62, 255));
            var area = Rect(rect, "HandleArea", 10, 0, 300, 44);
            var handle = Panel(area, "Handle", 0, 9, 20, 26, Gold); slider.handleRect = handle.rectTransform;
            handle.rectTransform.pivot = new Vector2(.5f, .5f);
            handle.rectTransform.anchoredPosition = Vector2.zero;
            handle.rectTransform.sizeDelta = new Vector2(20, -18);
            slider.targetGraphic = handle; slider.direction = Slider.Direction.LeftToRight; slider.value = value;
            slider.onValueChanged.AddListener(change); return slider;
        }
        public static Image Meter(Transform parent, string name, float x, float y, float w, Color color)
        {
            var background = Panel(parent, name, x, y, w, 7, new Color32(64, 68, 61, 255));
            return Panel(background.transform, "Fill", 0, 0, w, 7, color);
        }
    }
}
