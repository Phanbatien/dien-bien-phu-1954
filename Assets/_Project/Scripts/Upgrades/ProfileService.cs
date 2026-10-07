using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DBP.Upgrades
{
    /// Hồ sơ trên máy (BR-22): chỉ lưu tên hiển thị, lớp, thời điểm đồng ý gửi thành tích, cùng tiến độ chơi.
    [Serializable]
    public class ProfileData
    {
        public string displayName = "";
        public string className = "";
        public string consentAt = "";
        public int resolve;
        public List<string> upgrades = new List<string>();
        public List<string> unlockedMissions = new List<string> { "M1" };
        /// Mã câu trong hầm đã được thưởng Quyết Tâm (BR-34: mỗi câu một lần trên mỗi hồ sơ).
        public List<string> rewardedQuestions = new List<string>();
    }

    /// Lưu/nạp hồ sơ (T20; BR-37) tại persistentDataPath/profiles/&lt;id&gt;/progress.json.
    /// Ghi file tạm rồi thay thế, giữ bản .bak: tắt máy giữa chừng không làm hỏng bản đã lưu.
    public static class ProfileService
    {
        /// Thư mục gốc. Test đổi sang thư mục tạm.
        public static string Root = Path.Combine(Application.persistentDataPath, "profiles");
        /// Hồ sơ đang chơi. Menu chọn hồ sơ (T27) đổi giá trị này rồi gọi Reload().
        public static string ProfileId = "default";

        static ProfileData data;

        public static string FilePath => Path.Combine(Root, ProfileId, "progress.json");
        public static ProfileData Data => data ??= Load();

        public static void Reload() => data = null;

        static ProfileData Load()
        {
            foreach (var path in new[] { FilePath, FilePath + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var loaded = JsonUtility.FromJson<ProfileData>(File.ReadAllText(path));
                    if (loaded != null) return loaded;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Profile] File hỏng, thử bản dự phòng: {path} ({e.Message})");
                }
            }
            return new ProfileData();
        }

        /// false = chưa lưu được; nơi gọi phải báo người chơi, không hiện "đã lưu" (BR-37).
        public static bool Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(Data, true));
                if (File.Exists(FilePath)) File.Replace(tmp, FilePath, FilePath + ".bak");
                else File.Move(tmp, FilePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Profile] Không lưu được hồ sơ: {e.Message}");
                return false;
            }
        }

        /// Thắng màn N mở màn N+1; thua không khóa lại màn đã mở (BR-32).
        public static bool OnMissionWon(string nextMissionId)
        {
            if (!string.IsNullOrEmpty(nextMissionId) && !Data.unlockedMissions.Contains(nextMissionId))
                Data.unlockedMissions.Add(nextMissionId);
            return Save();
        }

        public static bool IsUnlocked(string missionId) => Data.unlockedMissions.Contains(missionId);

        /// Mua nâng cấp: trừ điểm và ghi nâng cấp trong cùng một lần lưu (BR-35).
        /// Lưu hỏng thì hoàn tác trong bộ nhớ để không lệch với file.
        public static BuyResult Buy(string upgradeId, bool inBunker)
        {
            int before = Data.resolve;
            var result = UpgradeService.TryBuy(Data, upgradeId, inBunker);
            if (result == BuyResult.Ok && !Save())
            {
                Data.resolve = before;
                Data.upgrades.Remove(upgradeId);
                return BuyResult.SaveFailed;
            }
            return result;
        }
    }
}
