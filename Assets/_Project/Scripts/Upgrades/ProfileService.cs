using System;
using System.Collections.Generic;
using UnityEngine;

namespace DBP.Upgrades
{
    /// Phần hồ sơ mà luồng màn và nâng cấp cần. T20 (Long) mở rộng thành profile.json/progress.json đầy đủ (BR-22, BR-37).
    [Serializable]
    public class ProfileData
    {
        public int resolve;
        public List<string> upgrades = new List<string>();
        public List<string> unlockedMissions = new List<string> { "M1" };
    }

    // ponytail: lưu tạm bằng PlayerPrefs để màn chơi chạy được. T20 (Long) thay Load/Save bằng
    // ghi file tạm rồi đổi tên trong persistentDataPath/profiles/<id>/; giữ nguyên các hàm public.
    public static class ProfileService
    {
        const string Key = "dbp.profile.v1";
        static ProfileData data;

        public static ProfileData Data => data ??= Load();

        static ProfileData Load()
        {
            var json = PlayerPrefs.GetString(Key, "");
            return string.IsNullOrEmpty(json) ? new ProfileData() : JsonUtility.FromJson<ProfileData>(json);
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        /// Thắng màn N mở màn N+1; thua không khóa lại màn đã mở (BR-32).
        public static void OnMissionWon(string nextMissionId)
        {
            if (!string.IsNullOrEmpty(nextMissionId) && !Data.unlockedMissions.Contains(nextMissionId))
                Data.unlockedMissions.Add(nextMissionId);
            Save();
        }

        public static bool IsUnlocked(string missionId) => Data.unlockedMissions.Contains(missionId);

        public static BuyResult Buy(string upgradeId, bool inBunker)
        {
            var result = UpgradeService.TryBuy(Data, upgradeId, inBunker);
            if (result == BuyResult.Ok) Save();
            return result;
        }
    }
}
