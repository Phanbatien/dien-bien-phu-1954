using System;
using System.IO;
using System.Linq;
using DBP.Missions;
using DBP.Ranking;
using NUnit.Framework;
using UnityEngine;
namespace DBP.Tests
{
    public class LeaderboardTests
    {
        string folder, path;
        [SetUp] public void Setup() { folder = Path.Combine(Path.GetTempPath(), "t57-" + Guid.NewGuid().ToString("N")); path = Path.Combine(folder, "boards.json"); }
        [TearDown] public void Cleanup() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        public static RankedResult Result(string id = "a", string attempt = "run", int kills = 5, int score = 300)
            => new RankedResult { profileId = id, displayName = "Same name", className = "SE", attemptId = attempt, group = new RankingGroup { missionId = "M1", difficulty = 1, contentVersion = "content-1", rulesVersion = "rules-1", upgrades = "0/0/0/0" }, kills = kills, bunkers = 2, waves = 1, damage = 30, milliseconds = 10000, knowledge = score, outcome = MissionOutcome.Won, eligible = true, seed = 42 };
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void Combat_EachCriterionHasPriorityOverAllFollowingOnes(int criterion)
        {
            var better = Result(); var worse = Result("b");
            if (criterion == 0) { better.kills++; better.bunkers = 0; better.waves = 0; better.damage = 999; better.milliseconds = 999999; }
            if (criterion == 1) { better.bunkers++; better.waves = 0; better.damage = 999; better.milliseconds = 999999; }
            if (criterion == 2) { better.waves++; better.damage = 999; better.milliseconds = 999999; }
            if (criterion == 3) { better.damage = 29.999f; better.milliseconds = 999999; }
            if (criterion == 4) better.milliseconds--;
            Assert.Less(Leaderboard.CombatCompare(better,worse),0); Assert.Greater(Leaderboard.CombatCompare(worse,better),0);
        }
        [Test] public void TH10_LessDamageWinsEvenWhenSlower()
        {
            var a = Result(); var b = Result("b"); b.damage=50; a.milliseconds=99999;
            Assert.Less(Leaderboard.CombatCompare(a,b),0);
        }
        [Test] public void TH11_TiesUseCompetitionRanksAndStableIds_RegardlessOfInputOrder()
        {
            var a=Result("a"); var b=Result("b"); var c=Result("c",kills:4);
            foreach(var input in new[] { new[]{b,c,a}, new[]{a,b,c}, new[]{c,a,b} })
            {
                var rows=Leaderboard.Rank(input,false);
                CollectionAssert.AreEqual(new[]{1,1,3},rows.Select(r=>r.rank));
                CollectionAssert.AreEqual(new[]{"a","b","c"},rows.Select(r=>r.result.profileId));
            }
        }
        [Test] public void Knowledge_TiesIgnoreCombatAndTime()
        {
            var a=Result("a",kills:1); var b=Result("b",kills:999); b.milliseconds=1;
            Assert.AreEqual(0,Leaderboard.KnowledgeCompare(a,b));
            CollectionAssert.AreEqual(new[]{1,1,3},Leaderboard.Rank(new[]{Result("c",score:299),b,a},true).Select(r=>r.rank));
        }
        [Test] public void Store_IndependentBests_EqualEarlierRetained_ReloadAndDuplicateSafe()
        {
            var store=new LeaderboardRepository(path); var first=Result(attempt:"first");
            store.Record(first); store.Record(Result(attempt:"second",kills:4,score:500));
            store.Record(Result(attempt:"equal",score:300)); store.Record(first);
            store=new LeaderboardRepository(path);
            Assert.AreEqual("first",store.Rows(first.group,false).Single().result.attemptId);
            Assert.AreEqual("second",store.Rows(first.group,true).Single().result.attemptId);
            store.Record(Result(attempt:"third",kills:6,score:200));
            Assert.AreEqual("third",store.Rows(first.group,false).Single().result.attemptId);
            Assert.AreEqual(500,store.Rows(first.group,true).Single().result.knowledge);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void Store_EachGroupDimensionIsIsolated(int dimension)
        {
            var store=new LeaderboardRepository(path);var a=Result();var b=Result(attempt:"different");
            if(dimension==0)b.group.missionId="M2";if(dimension==1)b.group.difficulty=2;
            if(dimension==2)b.group.contentVersion="content-2";if(dimension==3)b.group.rulesVersion="rules-2";
            if(dimension==4)b.group.upgrades="1/0/0/0";
            store.Record(a);store.Record(b);Assert.AreEqual(2,store.Groups.Count);
            Assert.AreEqual("run",store.Rows(a.group,false).Single().result.attemptId);
            Assert.AreEqual("different",store.Rows(b.group,false).Single().result.attemptId);
        }
        [TestCase(MissionOutcome.Lost,true)] [TestCase(MissionOutcome.Abandoned,true)] [TestCase(MissionOutcome.Won,false)] [TestCase(MissionOutcome.None,true)]
        public void Store_RejectsIneligibleRuns(MissionOutcome outcome,bool eligible)
        {
            var a=Result();a.outcome=outcome;a.eligible=eligible;var store=new LeaderboardRepository(path);
            Assert.IsFalse(store.Record(a));Assert.IsEmpty(store.Groups);Assert.IsFalse(File.Exists(path));
        }
        [Test] public void Store_CopiesInputsAndOutputs_AndSeparatesSameNames()
        {
            var store=new LeaderboardRepository(path);var a=Result();var group=a.group.Copy();store.Record(a);store.Record(Result("b"));
            a.group.missionId="M5";a.kills=999;var row=store.Rows(group,false)[0];row.result.kills=888;
            store.Groups[0].missionId="M4";
            Assert.AreEqual(2,store.Rows(group,false).Count);Assert.AreEqual(5,store.Rows(group,false)[0].result.kills);
        }
        [Test] public void Store_WriteFailureRollsBackThenRetryPreservesBest()
        {
            var store=new LeaderboardRepository(path);var a=Result();store.Record(a);
            var better=Result(attempt:"better",kills:10);
            using(var locked=new FileStream(path+".tmp",FileMode.Create,FileAccess.Write,FileShare.None))
            {
                Assert.Throws<IOException>(()=>store.Record(better));
                Assert.AreEqual(5,store.Rows(a.group,false)[0].result.kills);
                Assert.AreEqual(5,new LeaderboardRepository(path).Rows(a.group,false)[0].result.kills);
            }
            Assert.IsTrue(store.Record(better));Assert.AreEqual(10,new LeaderboardRepository(path).Rows(a.group,false)[0].result.kills);
        }
        [Test] public void Store_CorruptMainRecoversBackup_AndPreservesItOnNextSave()
        {
            var store=new LeaderboardRepository(path);var a=Result();store.Record(a);store.Record(Result("b"));
            File.WriteAllText(path,"corrupt");store=new LeaderboardRepository(path);Assert.IsTrue(store.RecoveredBackup);
            Assert.AreEqual(1,store.Rows(a.group,false).Count);store.Record(Result("c"));
            File.WriteAllText(path,"corrupt again");Assert.AreEqual(1,new LeaderboardRepository(path).Rows(a.group,false).Count);
            File.WriteAllText(path+".bak","corrupt too");Assert.Throws<InvalidDataException>(()=>new LeaderboardRepository(path));
        }
        [Test] public void Store_BackupOnlyAndInvalidNumbers()
        {
            var store=new LeaderboardRepository(path);var a=Result();store.Record(a);store.Record(Result("b"));File.Delete(path);
            Assert.IsTrue(new LeaderboardRepository(path).RecoveredBackup);
            a.damage=float.NaN;Assert.Throws<ArgumentException>(()=>store.Record(a));
            a.damage=0;a.milliseconds=-1;Assert.Throws<ArgumentException>(()=>store.Record(a));
        }
        [Test] public void Clock_CountsRealTimeAndUnionsExclusions_AndStopsOnce()
        {
            var clock=new RankingClock();clock.Begin(10);clock.Exclude("bunker",11);clock.Exclude("cinematic",12);
            clock.Include("bunker",14);clock.Include("cinematic",15);clock.Include("cinematic",16);
            Assert.AreEqual(6000,clock.Stop(20));Assert.AreEqual(6000,clock.Stop(99));
        }
        [Test] public void Clock_RoundsMillisecondMidpointsUp_AndOpenExclusionStopsAtFinish()
        {
            var clock=new RankingClock();clock.Begin(0);Assert.AreEqual(1001,clock.Stop(1.0005));
            clock=new RankingClock();clock.Begin(0);clock.Exclude("tutorial",1);Assert.AreEqual(1000,clock.Stop(20));
        }
        [TestCase("{}")]
        [TestCase("{\"version\":1}")]
        [TestCase("{\"version\":99,\"combat\":[],\"knowledge\":[]}")]
        public void Store_SemanticCorruptionUsesGoodBackup(string broken)
        {
            var store=new LeaderboardRepository(path);var a=Result();store.Record(a);store.Record(Result("b"));
            File.WriteAllText(path,broken);store=new LeaderboardRepository(path);
            Assert.IsTrue(store.RecoveredBackup);Assert.AreEqual(1,store.Rows(a.group,false).Count);
        }
        [Test] public void Store_RewardAuditIsDetachedAndPersists()
        {
            var store=new LeaderboardRepository(path);var a=Result();a.rewards.Add(new RewardAudit {phase="resolved",weaponId="W-MAT49",ammo=20,leftoverAmmo=5});
            store.Record(a);a.rewards[0].ammo=999;
            var row=store.Rows(a.group,false)[0].result;row.rewards[0].ammo=888;
            Assert.AreEqual(20,new LeaderboardRepository(path).Rows(a.group,false)[0].result.rewards[0].ammo);
            Assert.AreEqual(5,store.Rows(a.group,true)[0].result.rewards[0].leftoverAmmo);
        }
    }
}
