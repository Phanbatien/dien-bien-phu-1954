using System;
using System.Collections.Generic;
using System.Linq;
using DBP.Missions;
using UnityEngine;
namespace DBP.Ranking
{
    [Serializable]
    public sealed class RankingGroup : IEquatable<RankingGroup>
    {
        public string missionId;
        public int difficulty;
        public string contentVersion, rulesVersion, upgrades;
        public RankingGroup Copy() => (RankingGroup)MemberwiseClone();
        public bool Equals(RankingGroup other) => other != null && missionId == other.missionId && difficulty == other.difficulty && contentVersion == other.contentVersion && rulesVersion == other.rulesVersion && upgrades == other.upgrades;
        public override bool Equals(object obj) => Equals(obj as RankingGroup);
        public override int GetHashCode() => JsonUtility.ToJson(this).GetHashCode();
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(missionId) || difficulty < 1 || difficulty > 5 || string.IsNullOrWhiteSpace(contentVersion) || string.IsNullOrWhiteSpace(rulesVersion) || string.IsNullOrWhiteSpace(upgrades))
                throw new ArgumentException("Thiếu điều kiện so sánh bảng xếp hạng.");
        }
    }
    [Serializable]
    public sealed class RewardAudit
    {
        public string phase, sourceId, weaponId, ammoType, displacedWeaponId, questionId;
        public bool isCrate;
        public int rarity, ammo, grenades, leftoverAmmo, leftoverGrenades;
        public Quiz.QuizOutcome outcome;
        public Vector3 position;
        public RewardAudit Copy() => (RewardAudit)MemberwiseClone();
    }
    [Serializable]
    public sealed class RankedResult
    {
        public string profileId, displayName, className, attemptId;
        public RankingGroup group;
        public int kills, bunkers, waves, knowledge, seed, weaponDrops;
        public float damage;
        public long milliseconds;
        public MissionOutcome outcome;
        public bool eligible;
        public List<RewardAudit> rewards = new List<RewardAudit>();
        // All public records and rows are detached copies, never live profile/attempt objects.
        public RankedResult Copy() { var copy = (RankedResult)MemberwiseClone(); copy.group = group?.Copy(); copy.rewards = rewards?.Select(r=>r.Copy()).ToList(); return copy; }
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(profileId) || string.IsNullOrWhiteSpace(attemptId) || string.IsNullOrWhiteSpace(displayName) || group == null || kills < 0 || bunkers < 0 || waves < 0 || knowledge < 0 || milliseconds < 0 || float.IsNaN(damage) || float.IsInfinity(damage) || damage < 0 || weaponDrops < 0 || rewards == null || rewards.Any(r=>r == null) || outcome != MissionOutcome.Won || !eligible)
                throw new ArgumentException("Thành tích xếp hạng không hợp lệ.");
            group.Validate();
        }
    }
    public sealed class RankedRow
    {
        public int rank;
        public RankedResult result;
    }
    public static class Leaderboard
    {
        // Negative means the first record is better. IDs affect display order, never rank.
        public static int CombatCompare(RankedResult a, RankedResult b)
        {
            int c = b.kills.CompareTo(a.kills); if (c != 0) return c;
            c = b.bunkers.CompareTo(a.bunkers); if (c != 0) return c;
            c = b.waves.CompareTo(a.waves); if (c != 0) return c;
            c = a.damage.CompareTo(b.damage); return c != 0 ? c : a.milliseconds.CompareTo(b.milliseconds);
        }
        public static int KnowledgeCompare(RankedResult a, RankedResult b) => b.knowledge.CompareTo(a.knowledge);
        public static List<RankedRow> Rank(IEnumerable<RankedResult> records, bool knowledge)
        {
            Comparison<RankedResult> compare = knowledge ? KnowledgeCompare : CombatCompare;
            var sorted = records.Select(r => r.Copy()).ToList();
            sorted.Sort((a,b) => { int c = compare(a,b); return c != 0 ? c : StringComparer.Ordinal.Compare(a.profileId,b.profileId); });
            var rows = new List<RankedRow>(); int rank = 1;
            for (int i=0; i<sorted.Count; i++)
            {
                if (i > 0 && compare(sorted[i-1],sorted[i]) != 0) rank = i+1;
                rows.Add(new RankedRow { rank = rank, result = sorted[i] });
            }
            return rows;
        }
    }
}
