using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DBP.UI
{
    /// Scene-owned feedback with a bounded queue and a real-time lifetime.
    public sealed class CampaignNotices : MonoBehaviour
    {
        static CampaignNotices current;
        readonly Queue<string> pending = new Queue<string>();
        CanvasGroup group;
        Text label;
        GameObject card;
        RectTransform accent;
        Coroutine showing;
        public static void Show(string message)
        {
            if (!Application.isPlaying || string.IsNullOrWhiteSpace(message)) return;
            if (!current) current = new GameObject("CampaignNotices").AddComponent<CampaignNotices>();
            if (current.pending.Count == 2) current.pending.Dequeue();
            current.pending.Enqueue(message);
            if (current.showing == null) current.showing = current.StartCoroutine(current.Display());
        }
        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 600;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600,900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            group = gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            var panel = UIWidgets.Panel(transform,"NoticeCard",0,0,720,84,UIWidgets.Ink); card = panel.gameObject;
            var rect = panel.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,1);
            rect.anchoredPosition = new Vector2(0,-210);
            accent = UIWidgets.Panel(rect,"NoticeAccent",0,0,5,84,UIWidgets.Gold).rectTransform;
            label = UIWidgets.Label(rect,"NoticeText","",24,10,672,64,23,UIWidgets.Paper);
            foreach (var graphic in GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
            card.SetActive(false);
        }
        void LateUpdate()
        {
            // Leave results navigation and leaderboard condition text unobstructed.
            bool gameplay = !CampaignUI.Current || CampaignUI.Current.ScreenState == CampaignScreen.Gameplay;
            float width = gameplay ? 720 : 640, height = gameplay ? 84 : 64;
            var rect = (RectTransform)card.transform;
            rect.anchoredPosition = gameplay ? new Vector2(0,-210) : new Vector2(416,-110);
            rect.sizeDelta = new Vector2(width,height);
            label.rectTransform.sizeDelta = new Vector2(width-48,height-20);
            label.fontSize = gameplay ? 23 : 20;
            accent.sizeDelta = new Vector2(5,height);
        }
        IEnumerator Display()
        {
            while (pending.Count > 0)
            {
                label.text = pending.Dequeue(); card.SetActive(true);
                yield return Fade(0,1); yield return new WaitForSecondsRealtime(2.5f); yield return Fade(1,0);
                card.SetActive(false);
            }
            showing = null;
        }
        IEnumerator Fade(float from, float to)
        {
            group.alpha = from; float elapsed = 0;
            while (elapsed < .15f)
            {
                elapsed += Time.unscaledDeltaTime; group.alpha = Mathf.Lerp(from,to,elapsed/.15f); yield return null;
            }
            group.alpha = to;
        }
        void OnDestroy() { if (current == this) current = null; }
    }
}
