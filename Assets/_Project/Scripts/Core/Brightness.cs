using DBP.Missions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DBP.Core
{
    /// Độ sáng màn hình (lưu trên máy): phím B chuyển 4 mức, hoặc kéo thanh trong Cài đặt.
    /// Áp bằng một Volume toàn cục chỉnh độ phơi sáng; màn đêm (Màn 3) được cộng thêm để vẫn thấy địch.
    public sealed class Brightness : MonoBehaviour
    {
        const string Key = "dbp.brightness";
        static readonly float[] Levels = { 0.15f, 0.4f, 0.65f, 0.9f };
        static readonly string[] Names = { "Tối", "Vừa", "Sáng", "Rất sáng" };

        public static float Level
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(Key, 0.4f));
            set { PlayerPrefs.SetFloat(Key, Mathf.Clamp01(value)); PlayerPrefs.Save(); if (current) current.Apply(); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || current) return;
            new GameObject("Brightness").AddComponent<Brightness>();
        }

        static Brightness current;
        Volume volume;
        ColorAdjustments color;
        bool night;

        void Awake()
        {
            current = this;
            night = SceneManager.GetActiveScene().name.StartsWith("M3");
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 5;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            color = profile.Add<ColorAdjustments>(true);
            volume.profile = profile;
            Apply();
        }

        void OnDestroy() { if (current == this) current = null; }

        void Apply()
        {
            // 0 → −0,3 EV, 1 → +1,4 EV; đêm cộng 0,9 EV
            color.postExposure.Override(Mathf.Lerp(-0.3f, 1.4f, Level) + (night ? 0.9f : 0f));
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.bKey.wasPressedThisFrame || !MissionController.Current) return;
            int i = 0; float best = 9f;
            for (int k = 0; k < Levels.Length; k++) if (Mathf.Abs(Levels[k] - Level) < best) { best = Mathf.Abs(Levels[k] - Level); i = k; }
            i = (i + 1) % Levels.Length;
            Level = Levels[i];
            DBP.UI.CampaignNotices.Show($"Độ sáng: {Names[i]} (phím B)");
        }
    }
}
