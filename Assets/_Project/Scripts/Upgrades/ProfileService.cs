using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace DBP.Upgrades
{
    [Serializable]
    public class ProfileData
    {
        public string id;
        public string displayName;
        public string className;
        public string consentAt;
        public float volume = 0.8f;
        public int resolve;
        public List<string> upgrades = new List<string>();
        public List<string> unlockedMissions = new List<string> { "M1" };
        // BR-34: track bunker questions rewarded once for each profile.
        public List<string> rewardedQuestions = new List<string>();
    }
    public static class ProfileService
    {
        static ProfileRepository repository;
        static ProfileData guest;
        public static ProfileRepository Repository => repository ??= Open();
        public static ProfileData Data => Repository.Active ?? (guest ??= new ProfileData());
        public static bool HasProfile => Repository.Active != null;
        static ProfileRepository Open()
        {
            var store = new ProfileRepository(Path.Combine(Application.persistentDataPath, "profiles.json"),
                Path.Combine(Application.persistentDataPath, "profiles"));
            if (!store.Exists && PlayerPrefs.HasKey("dbp.profile.v1"))
            {
                var legacy = JsonUtility.FromJson<ProfileData>(PlayerPrefs.GetString("dbp.profile.v1"));
                if (legacy != null) store.ImportLegacy(legacy);
            }
            return store;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { repository = null; guest = null; }
        public static void Save() { if (HasProfile) Repository.Save(); }
        public static void OnMissionWon(string nextMissionId)
        {
            if (HasProfile) Repository.Unlock(nextMissionId);
            else if (!string.IsNullOrEmpty(nextMissionId) && !Data.unlockedMissions.Contains(nextMissionId)) Data.unlockedMissions.Add(nextMissionId);
        }
        public static bool IsUnlocked(string missionId) => UnlockAllForPlaytest || Data.unlockedMissions.Contains(missionId);

        /// Chơi thử: mở mọi màn để cả nhóm góp ý một lượt. Bản build mặc định bật, Editor mặc định tắt
        /// (để test luật mở màn vẫn đúng). Đổi trong màn Cài đặt; không ghi vào hồ sơ.
        public static bool UnlockAllForPlaytest
        {
            get => UnityEngine.PlayerPrefs.GetInt("dbp.unlockAll", UnityEngine.Application.isEditor ? 0 : 1) == 1;
            set { UnityEngine.PlayerPrefs.SetInt("dbp.unlockAll", value ? 1 : 0); UnityEngine.PlayerPrefs.Save(); }
        }
        public static BuyResult Buy(string upgradeId, bool inBunker)
        {
            if (!HasProfile) return BuyResult.NotInBunker;
            try { return Repository.Buy(upgradeId, inBunker); }
            catch (IOException) { return BuyResult.SaveFailed; }
            catch (UnauthorizedAccessException) { return BuyResult.SaveFailed; }
        }
#if UNITY_EDITOR
        // Tests and capture tools must never change the user's real profiles.
        public static IDisposable UseTemporaryStore(string path)
        {
            var previous = repository;
            repository = new ProfileRepository(path);
            return new RestoreStore(() => repository = previous);
        }
        sealed class RestoreStore : IDisposable
        {
            readonly Action restore;
            public RestoreStore(Action action) { restore = action; }
            public void Dispose() => restore();
        }
#endif
    }
}
