using UnityEngine;

namespace DBP.Core
{
    /// Cài đặt nhạc nền (lưu trên máy, dùng chung mọi hồ sơ): bật/tắt, âm lượng nhạc riêng với âm lượng chung,
    /// bản nhạc (0 = theo từng màn, 1–5 = cố định một bản).
    public static class MusicSettings
    {
        public static readonly string[] TrackNames =
        {
            "Theo từng màn",
            "Mở màn Him Lam",
            "Giữ Độc Lập",
            "Đêm đồi E, D",
            "Trong lòng hào",
            "Chiều mùng Bảy",
        };

        const string KeyOn = "dbp.music.on", KeyVolume = "dbp.music.volume", KeyTrack = "dbp.music.track";

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(KeyOn, 1) == 1;
            set { PlayerPrefs.SetInt(KeyOn, value ? 1 : 0); Changed(); }
        }

        public static float Volume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVolume, 0.7f));
            set { PlayerPrefs.SetFloat(KeyVolume, Mathf.Clamp01(value)); Changed(); }
        }

        public static int Track
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(KeyTrack, 0), 0, TrackNames.Length - 1);
            set { PlayerPrefs.SetInt(KeyTrack, (value % TrackNames.Length + TrackNames.Length) % TrackNames.Length); Changed(); }
        }

        /// Bản nhạc cho scene theo cài đặt; menu chính dùng bản đã chọn hoặc nhạc Màn 1.
        public static AudioClip ClipFor(string scene)
        {
            if (Track > 0) return AudioCatalog.Clip($"mission_{Track:00}");
            var clip = AudioCatalog.Music(scene);
            return clip ? clip : scene == "MainMenu" ? AudioCatalog.Clip("mission_01") : null;
        }

        static void Changed()
        {
            PlayerPrefs.Save();
            CampaignAudio.Current?.ApplyMusicSettings();
        }
    }
}
