using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
namespace DBP.Upgrades
{
    // A complete atomic snapshot keeps balance and purchases consistent (BR-22/35).
    public sealed class ProfileRepository
    {
        [Serializable]
        sealed class Document
        {
            public int version = 1;
            public string activeId;
            public List<ProfileData> profiles = new List<ProfileData>();
        }
        readonly string path;
        Document document;
        public bool Exists => File.Exists(path);
        public bool RecoveredBackup { get; private set; }
        public IReadOnlyList<ProfileData> Profiles => document.profiles;
        public ProfileData Active => document.profiles.Find(p => p.id == document.activeId);
        public ProfileRepository(string filePath, string legacyRoot = null)
        {
            path = filePath;
            if (!File.Exists(path) && !File.Exists(path + ".bak"))
            {
                document = new Document();
                ImportDirectory(legacyRoot);
                return;
            }
            try { document = Read(path); }
            catch (Exception e) when (e is IOException || e is ArgumentException)
            {
                if (!File.Exists(path + ".bak")) throw new InvalidDataException("Không đọc được hồ sơ. Dữ liệu gốc được giữ nguyên.", e);
                document = Read(path + ".bak");
                RecoveredBackup = true;
            }
        }
        static Document Read(string file)
        {
            var result = JsonUtility.FromJson<Document>(File.ReadAllText(file, Encoding.UTF8));
            if (result == null || result.version != 1 || result.profiles == null) throw new InvalidDataException("Định dạng hồ sơ không hợp lệ.");
            var ids = new HashSet<string>();
            foreach (var p in result.profiles)
                if (p == null || string.IsNullOrWhiteSpace(p.id) || !ids.Add(p.id) || p.upgrades == null || p.unlockedMissions == null || p.resolve < 0 || float.IsNaN(p.volume) || p.volume < 0 || p.volume > 1)
                    throw new InvalidDataException("Hồ sơ không hợp lệ.");
            if (result.profiles.Count > 0 && !ids.Contains(result.activeId)) throw new InvalidDataException("Hồ sơ đang chọn không tồn tại.");
            return result;
        }
        public ProfileData Create(string name, string className, bool consent)
        {
            name = (name ?? "").Trim(); className = (className ?? "").Trim();
            if (name.Length == 0 || name.Length > 40 || className.Length > 40) throw new ArgumentException("Tên bắt buộc; tên và lớp tối đa 40 ký tự.");
            var profile = new ProfileData { id = Guid.NewGuid().ToString("N"), displayName = name, className = className, consentAt = consent ? DateTime.UtcNow.ToString("o") : "" };
            Change(() => { document.profiles.Add(profile); document.activeId = profile.id; });
            return profile;
        }
        public void ImportLegacy(ProfileData legacy)
        {
            if (document.profiles.Count != 0) return;
            legacy.id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(legacy.displayName)) legacy.displayName = "Hồ sơ cũ";
            legacy.className ??= ""; legacy.consentAt ??= "";
            legacy.upgrades ??= new List<string>(); legacy.unlockedMissions ??= new List<string> { "M1" };
            legacy.rewardedQuestions ??= new List<string>();
            Change(() => { document.profiles.Add(legacy); document.activeId = legacy.id; });
        }
        void ImportDirectory(string root)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;
            var folders = Directory.GetDirectories(root);
            Array.Sort(folders, (a, b) =>
            {
                bool aDefault = Path.GetFileName(a) == "default", bDefault = Path.GetFileName(b) == "default";
                return aDefault != bDefault ? (aDefault ? -1 : 1) : StringComparer.Ordinal.Compare(a, b);
            });
            var profiles = new List<ProfileData>();
            foreach (var folder in folders)
            {
                var file = Path.Combine(folder, "progress.json");
                if (!File.Exists(file) && !File.Exists(file + ".bak")) continue;
                var profile = ReadLegacy(file);
                profile.id = Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(profile.displayName)) profile.displayName = "Hồ sơ cũ";
                profile.className ??= ""; profile.consentAt ??= "";
                profile.upgrades ??= new List<string>();
                profile.unlockedMissions ??= new List<string> { "M1" };
                profile.rewardedQuestions ??= new List<string>();
                profiles.Add(profile);
            }
            if (profiles.Count == 0) return;
            // Read every old profile before saving one atomic snapshot. Original files stay intact.
            Change(() => { document.profiles.AddRange(profiles); document.activeId = profiles[0].id; });
        }
        static ProfileData ReadLegacy(string file)
        {
            Exception lastError = null;
            foreach (var candidate in new[] { file, file + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var profile = JsonUtility.FromJson<ProfileData>(File.ReadAllText(candidate, Encoding.UTF8));
                    if (profile == null || profile.resolve < 0 || float.IsNaN(profile.volume) || profile.volume < 0 || profile.volume > 1)
                        throw new InvalidDataException("Hồ sơ cũ không hợp lệ.");
                    return profile;
                }
                catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException) { lastError = e; }
            }
            throw new InvalidDataException("Không đọc được hồ sơ cũ. Dữ liệu gốc được giữ nguyên: " + file, lastError);
        }
        public void Select(string id)
        {
            if (!document.profiles.Exists(p => p.id == id)) throw new ArgumentException("Không tìm thấy hồ sơ.");
            Change(() => document.activeId = id);
        }
        public void SetConsent(bool value) => Change(() => Required().consentAt = value ? DateTime.UtcNow.ToString("o") : "");
        public void SetVolume(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Change(() => Required().volume = Mathf.Clamp01(value));
        }
        public void Unlock(string missionId)
        {
            if (string.IsNullOrEmpty(missionId)) return;
            Change(() => { if (!Required().unlockedMissions.Contains(missionId)) Required().unlockedMissions.Add(missionId); });
        }
        public BuyResult Buy(string upgradeId, bool inBunker)
        {
            BuyResult result = BuyResult.UnknownId;
            Change(() => result = UpgradeService.TryBuy(Required(), upgradeId, inBunker));
            return result;
        }
        ProfileData Required() => Active ?? throw new InvalidOperationException("Hãy chọn hồ sơ trước.");
        void Change(Action change)
        {
            string before = JsonUtility.ToJson(document);
            try { change(); Save(); }
            catch { document = JsonUtility.FromJson<Document>(before); throw; }
        }
        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary = path + ".tmp";
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(document, true));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(path)) File.Replace(temporary, path, RecoveredBackup ? null : path + ".bak");
            else File.Move(temporary, path);
            RecoveredBackup = false;
        }
    }
}
