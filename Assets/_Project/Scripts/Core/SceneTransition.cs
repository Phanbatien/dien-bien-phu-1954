using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DBP.Core
{
    /// Unscaled fades work from pause/results; only one destination is accepted.
    public sealed class SceneTransition : MonoBehaviour
    {
        static SceneTransition current;
        CanvasGroup shade;
        bool ownsLock;
        public static bool IsTransitioning => current && current.ownsLock;
        public static float Alpha => current ? current.shade.alpha : 0;
        public static bool Load(string scene)
        {
            if (!Application.isPlaying || IsTransitioning || string.IsNullOrEmpty(scene)
                || !Application.CanStreamedLevelBeLoaded(scene)) return false;
            if (!current) current = new GameObject("SceneTransition").AddComponent<SceneTransition>();
            current.ownsLock = true; InputLock.Push(); current.shade.blocksRaycasts = true;
            current.StartCoroutine(current.Change(scene)); return true;
        }
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue; gameObject.AddComponent<GraphicRaycaster>();
            shade = gameObject.AddComponent<CanvasGroup>(); shade.alpha = 0; shade.blocksRaycasts = false;
            var cover = new GameObject("Fade", typeof(RectTransform), typeof(Image)); cover.transform.SetParent(transform, false);
            var rect = cover.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; cover.GetComponent<Image>().color = Color.black;
        }
        IEnumerator Fade(float from, float to)
        {
            float elapsed = 0;
            while (elapsed < .12f)
            {
                elapsed += Time.unscaledDeltaTime; shade.alpha = Mathf.Lerp(from, to, elapsed / .12f); yield return null;
            }
            shade.alpha = to;
        }
        IEnumerator Change(string scene)
        {
            try
            {
                yield return Fade(0, 1);
                Time.timeScale = 1;
                var load = SceneManager.LoadSceneAsync(scene); if (load != null) yield return load;
                yield return Fade(1, 0);
            }
            finally { Release(); }
        }
        void Release()
        {
            if (shade) { shade.alpha = 0; shade.blocksRaycasts = false; }
            if (ownsLock) { ownsLock = false; InputLock.Pop(); }
            if (!InputLock.Locked && DBP.Missions.MissionController.Current
                && (!DBP.UI.CampaignUI.Current || DBP.UI.CampaignUI.Current.ScreenState == DBP.UI.CampaignScreen.Gameplay))
                DBP.Player.FirstPersonController.LockCursor();
        }
        void OnDestroy() { Release(); if (current == this) current = null; }
    }
}
