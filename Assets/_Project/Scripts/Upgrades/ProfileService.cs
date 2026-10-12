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
            var store = new ProfileRepository(Path.Combine(Application.persistentDataPath, "profiles.json"));
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
        public static bool IsUnlocked(string missionId) => Data.unlockedMissions.Contains(missionId);
        public static BuyResult Buy(string upgradeId, bool inBunker) => HasProfile ? Repository.Buy(upgradeId, inBunker) : BuyResult.NotInBunker;
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
