using DBP.Core;
using DBP.Upgrades;
using UnityEngine;
using W = DBP.UI.UIWidgets;

namespace DBP.UI
{
    /// Màn Cài đặt (từ menu chính và màn tạm dừng): âm lượng chung, nhạc nền bật/tắt, âm lượng nhạc, chọn bản nhạc.
    public sealed partial class CampaignUI
    {
        CampaignScreen settingsReturn = CampaignScreen.Menu;

        public void OpenSettings()
        {
            settingsReturn = ScreenState == CampaignScreen.Pause ? CampaignScreen.Pause : CampaignScreen.Menu;
            Show(CampaignScreen.Settings);
        }

        void SettingsPage()
        {
            W.Label(page, "Title", "Cài đặt", 360, 120, 900, 76, 46);
            W.Panel(page, "SettingsPanel", 360, 210, 880, 540);
            float y = 250;
            if (ProfileService.HasProfile)
            {
                W.Label(page, "MasterLabel", "Âm lượng chung", 400, y, 300, 48, 24);
                W.Volume(page, ProfileService.Data.volume, 760, y + 2, value => Guard(() => { ProfileService.Repository.SetVolume(value); AudioListener.volume = value; }), "VolumeSlider");
                y += 80;
            }
            W.Label(page, "BrightnessLabel", "Độ sáng (phím B)", 400, y, 340, 48, 24);
            W.Volume(page, Brightness.Level, 760, y + 2, value => Brightness.Level = value, "BrightnessSlider");
            y += 70;
            W.Toggle(page, "MusicOn", "Bật nhạc nền", MusicSettings.Enabled, 400, y, 600, value => MusicSettings.Enabled = value);
            y += 70;
            W.Label(page, "MusicVolumeLabel", "Âm lượng nhạc nền", 400, y, 340, 48, 24);
            W.Volume(page, MusicSettings.Volume, 760, y + 2, value => MusicSettings.Volume = value, "MusicVolumeSlider");
            y += 80;
            W.Label(page, "TrackLabel", "Bản nhạc", 400, y, 300, 56, 24);
            W.Button(page, "PrevTrack", "◀", 660, y, 64, 56, () => { MusicSettings.Track--; Show(CampaignScreen.Settings); });
            W.Label(page, "TrackName", MusicSettings.TrackNames[MusicSettings.Track], 740, y, 360, 56, 24).alignment = TextAnchor.MiddleCenter;
            W.Button(page, "NextTrack", "▶", 1116, y, 64, 56, () => { MusicSettings.Track++; Show(CampaignScreen.Settings); });
            y += 90;
            if (!mission)
            {
                W.Toggle(page, "UnlockAll", "Mở khóa tất cả màn (chơi thử, không cần thắng màn trước)", ProfileService.UnlockAllForPlaytest, 400, y - 30, 800, value => ProfileService.UnlockAllForPlaytest = value);
                y += 40;
            }
            W.Label(page, "MusicHint", "Khi đang giao tranh, nhạc tự mạnh lên. Cài đặt nhạc lưu trên máy, dùng chung mọi hồ sơ.", 400, y, 800, 60, 20);
            W.Button(page, "SettingsBack", "← Quay lại", 360, 760, 420, 66, () => Show(settingsReturn));
        }
    }
}
