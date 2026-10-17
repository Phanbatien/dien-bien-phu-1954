using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DBP.Content;
using DBP.Missions;
using DBP.Quiz;
using DBP.Upgrades;
namespace DBP.Ranking
{
    public static class LeaderboardService
    {
        public const string RulesVersion = "br24-25-v2-t57-1";
        // Derived from the profile store, so temporary test profiles also isolate rankings.
        public static string StoragePath => Path.ChangeExtension(ProfileService.Repository.StoragePath,"leaderboards.json");
        public static LeaderboardRepository Open() => new LeaderboardRepository(StoragePath);
        public static RankingGroup Group(string missionId, int difficulty) => new RankingGroup
        {
            missionId = missionId, difficulty = difficulty, contentVersion = ContentVersion(), rulesVersion = RulesVersion,
            upgrades = string.Join("/",Enum.GetValues(typeof(UpgradeBranch)).Cast<UpgradeBranch>().Select(b=>UpgradeService.Level(ProfileService.Data,b)))
        };
        static string ContentVersion()
        {
            if (!Directory.Exists(ContentLoader.Root)) throw new IOException("Không tìm thấy nội dung để xác định bảng xếp hạng.");
            var files = Directory.GetFiles(ContentLoader.Root,"*.json",SearchOption.AllDirectories).OrderBy(f=>f,StringComparer.Ordinal).ToArray();
            if (files.Length == 0) throw new IOException("Không có nội dung để xác định bảng xếp hạng.");
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream,Encoding.UTF8,true))
                    foreach (var file in files) { var bytes = File.ReadAllBytes(file); writer.Write(file.Substring(ContentLoader.Root.Length).Replace('\\','/')); writer.Write(bytes.Length); writer.Write(bytes); }
                using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-","").ToLowerInvariant();
            }
        }
        public static void Capture(Attempt attempt, int difficulty)
        {
            try
            {
                attempt.RankingResult = new RankedResult { profileId = ProfileService.HasProfile ? ProfileService.Data.id : null,
                    displayName = ProfileService.Data.displayName, className = ProfileService.Data.className, attemptId = attempt.AttemptId,
                    seed = attempt.Seed, group = Group(attempt.MissionId,difficulty) };
            }
            catch (Exception e) when (e is IOException || e is InvalidDataException || e is UnauthorizedAccessException || e is ArgumentException)
            { attempt.RankEligible = false; attempt.RankingSaveError = e.Message; }
        }
        public static void Complete(Attempt attempt)
        {
            var r = attempt.RankingResult;
            if (r == null) return;
            r.outcome = attempt.Outcome; r.eligible = attempt.RankEligible;
            r.kills = attempt.Kills; r.bunkers = attempt.BunkersDestroyed; r.waves = attempt.WavesRepelled;
            r.damage = attempt.DamageTaken; r.milliseconds = attempt.RankingMilliseconds; r.weaponDrops = attempt.WeaponDrops;
            r.rewards = attempt.Rewards.Select(reward=>reward.Copy()).ToList();
            try { r.knowledge = KnowledgeScore.Total(attempt.Answers); TrySave(attempt); }
            catch (Exception e) when (e is ArgumentException || e is OverflowException) { attempt.RankingSaveError = e.Message; }
        }
        public static bool TrySave(Attempt attempt)
        {
            var result = attempt.RankingResult;
            if (result == null || result.outcome != MissionOutcome.Won || !result.eligible || string.IsNullOrWhiteSpace(result.profileId)) return false;
            try { Open().Record(result); attempt.RankingSaveError = null; return true; }
            catch (Exception e) when (e is IOException || e is InvalidDataException || e is UnauthorizedAccessException || e is ArgumentException)
            { attempt.RankingSaveError = e.Message; return false; }
        }
    }
}
