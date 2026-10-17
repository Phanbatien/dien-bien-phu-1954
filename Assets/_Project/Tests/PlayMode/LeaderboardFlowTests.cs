using System;
using System.Collections;
using System.IO;
using System.Linq;
using DBP.Core;
using DBP.Missions;
using DBP.UI;
using DBP.Ranking;
using DBP.Quiz;
using DBP.Upgrades;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace DBP.Tests
{
    public class LeaderboardFlowTests
    {
        IDisposable profiles;
        string folder;
        static T Find<T>(string name) where T:Component => Object.FindObjectsByType<T>(FindObjectsSortMode.None).First(c=>c.name==name);
        static void Click(string name) => Find<Button>(name).onClick.Invoke();
        static IEnumerator Frames() { yield return null;yield return null;yield return null; }
        [UnitySetUp] public IEnumerator Setup()
        {
            folder=Path.Combine(Application.temporaryCachePath,"t57-ui-"+Guid.NewGuid().ToString("N"));
            profiles=ProfileService.UseTemporaryStore(Path.Combine(folder,"profiles.json"));
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return Frames();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return Frames();profiles.Dispose();
            if(Directory.Exists(folder))Directory.Delete(folder,true);Time.timeScale=1;InputLock.ResetAll();
        }
        [UnityTest] public IEnumerator Menu_OpensEmptyBoards_SwitchesTabsAndReturns()
        {
            Assert.IsTrue(Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.name=="OpenLeaderboard"),"T57 needs a leaderboard entry from MainMenu.");
            Click("OpenLeaderboard");yield return Frames();
            StringAssert.Contains("Chưa có",Find<Text>("LeaderboardEmpty").text);
            Click("KnowledgeTab");yield return Frames();StringAssert.Contains("KIẾN THỨC",Find<Text>("LeaderboardTitle").text);
            Click("LeaderboardBack");yield return Frames();Assert.AreEqual(CampaignScreen.Menu,CampaignUI.Current.ScreenState);
        }
        static void CreateProfile() { ProfileService.Repository.Create("T57 test", "SE", false); }
        static IEnumerator Deploy()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return Frames();
            Click("Mission1");Click("Deploy");yield return Frames();Click("BeginMission");yield return Frames();
        }
        static void Win(MissionController mission) => mission.Setup("M1","Him Lam","M2","M2_GiuDocLap",mission.Player,new System.Collections.Generic.List<Objective>(),true);
        [UnityTest] public IEnumerator WinningRun_SavesLaunchProfileAndUpgrades_AndReturnsFromBoardToResults()
        {
            CreateProfile();var owner=ProfileService.Data.id;yield return Deploy();var mission=MissionController.Current;
            var launch=mission.Attempt.RankingResult.group.Copy();
            ProfileService.Repository.Create("Other", "", true);ProfileService.Data.upgrades.Add("U-AMMO-1");
            mission.Attempt.Kills=12;mission.Attempt.Answers.Add(new QuizResult {level=3,inBunker=true,outcome=QuizOutcome.Correct});
            Win(mission);yield return Frames();
            var row=LeaderboardService.Open().Rows(launch,false).Single().result;
            Assert.AreEqual(owner,row.profileId);Assert.AreEqual("0/0/0/0",row.group.upgrades);Assert.AreEqual(300,row.knowledge);
            Assert.AreEqual(CampaignScreen.Results,CampaignUI.Current.ScreenState);
            Click("ResultLeaderboard");yield return Frames();Assert.AreEqual(CampaignScreen.Leaderboard,CampaignUI.Current.ScreenState);
            StringAssert.Contains("12",Find<Text>("LeaderboardStats0").text);
            Click("KnowledgeTab");yield return Frames();StringAssert.Contains("300",Find<Text>("LeaderboardStats0").text);
            Click("LeaderboardBack");yield return Frames();Assert.AreEqual(CampaignScreen.Results,CampaignUI.Current.ScreenState);
        }
        [UnityTest] public IEnumerator WinningRun_RankingWriteFailureCanRetryWithoutDuplicating()
        {
            CreateProfile();yield return Deploy();var mission=MissionController.Current;var group=mission.Attempt.RankingResult.group.Copy();
            Directory.CreateDirectory(folder);
            using(var locked=new FileStream(LeaderboardService.StoragePath+".tmp",FileMode.Create,FileAccess.Write,FileShare.None))
            {
                Win(mission);yield return Frames();Assert.IsNotEmpty(mission.Attempt.RankingSaveError);
                StringAssert.Contains("Chưa lưu",Find<Text>("RankingResultNote").text);
                Assert.IsEmpty(LeaderboardService.Open().Rows(group,false));
            }
            Click("RetryRankingSave");yield return Frames();Assert.IsNull(mission.Attempt.RankingSaveError);
            Assert.AreEqual(1,LeaderboardService.Open().Rows(group,false).Count);
            Assert.IsTrue(LeaderboardService.TrySave(mission.Attempt));Assert.AreEqual(1,LeaderboardService.Open().Rows(group,true).Count);
        }
        [UnityTest] public IEnumerator InvalidWin_AndAbandon_AreNotRanked()
        {
            CreateProfile();yield return Deploy();var mission=MissionController.Current;var group=mission.Attempt.RankingResult.group.Copy();
            mission.Attempt.RankEligible=false;Win(mission);yield return Frames();Assert.IsEmpty(LeaderboardService.Open().Rows(group,false));
            Click("ResultMenu");yield return Frames();yield return Deploy();mission=MissionController.Current;mission.Abandon();yield return Frames();
            Assert.IsEmpty(LeaderboardService.Open().Groups);
        }
        [UnityTest] public IEnumerator Board_ManyRowsKeepsGlobalRanks_AndSwitchesConditionGroup()
        {
            var store=LeaderboardService.Open();RankingGroup group=null;
            for(int i=0;i<9;i++)
            {
                var r=new RankedResult {profileId="p"+i,displayName=new string('W',40),className=new string('W',40),attemptId="run"+i,
                    group=new RankingGroup {missionId="M1",difficulty=1,contentVersion="content",rulesVersion="rules",upgrades="0/0/0/0"},
                    outcome=MissionOutcome.Won,eligible=true,kills=10-i,knowledge=100};
                group=r.group;store.Record(r);
            }
            var alternate=new RankedResult {profileId="alt",displayName="Alternate",attemptId="alternate",group=group.Copy(),outcome=MissionOutcome.Won,eligible=true};
            alternate.group.difficulty=2;store.Record(alternate);
            Click("OpenLeaderboard");yield return Frames();Click("NextRankPage");yield return Frames();
            StringAssert.StartsWith("7",Find<Text>("LeaderboardRank0").text);
            var longName=Find<Text>("LeaderboardName0");
            Assert.LessOrEqual(longName.preferredHeight,longName.rectTransform.rect.height+1,"Name and class must fit the row.");
            Click("NextRankGroup");yield return Frames();StringAssert.Contains("Độ khó 2",Find<Text>("LeaderboardConditions").text);
            Assert.AreEqual("1",Find<Text>("LeaderboardRank0").text);
        }
        [UnityTest] public IEnumerator Clock_ExcludesTutorialButIncludesPause_AndQuizFreeze()
        {
            CreateProfile();yield return SceneManager.LoadSceneAsync("MainMenu");yield return Frames();Click("Mission1");Click("Deploy");yield return Frames();
            yield return new WaitForSecondsRealtime(.12f);Click("BeginMission");yield return Frames();var mission=MissionController.Current;
            double before=Time.realtimeSinceStartupAsDouble;
            // The real pause screen must not change the ranking start time.
            typeof(CampaignUI).GetMethod("Show",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(CampaignUI.Current,new object[]{CampaignScreen.Pause});
            yield return new WaitForSecondsRealtime(.15f);Click("Resume");yield return Frames();
            WorldFreeze.Instance.Freeze();yield return new WaitForSecondsRealtime(.1f);WorldFreeze.Instance.Resume();
            yield return new WaitForSecondsRealtime(.65f);
            Win(mission);yield return Frames();
            Assert.GreaterOrEqual(mission.Attempt.RankingMilliseconds,(long)((Time.realtimeSinceStartupAsDouble-before-.1)*1000));
            Assert.Less(mission.Attempt.RankingMilliseconds,(long)((mission.Attempt.BattleEnd-mission.Attempt.BattleStart)*1000)-70,"Tutorial time must be excluded.");
        }
        [UnityTest] public IEnumerator RandomRewards_AreSavedWithSeedForReview()
        {
            var field=typeof(RankedResult).GetField("rewards");
            Assert.IsNotNull(field,"BR-24 requires random rewards as well as the seed in saved records.");
            CreateProfile();yield return Deploy();var mission=MissionController.Current;
            var reward=new DBP.Interaction.CrateReward {weaponId="W-MAT49",ammoType="9x19",ammo=20,grenades=1};
            var crate=DBP.Interaction.QuizCrate.Spawn(Vector3.zero,Quaternion.identity,reward,true,null);
            typeof(DBP.Interaction.QuizCrate).GetMethod("Resolve",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(crate,new object[]{new QuizResult {level=1,questionId="audit-test",outcome=QuizOutcome.Correct},Object.FindAnyObjectByType<DBP.Interaction.Interactor>()});
            Win(mission);yield return Frames();
            var saved=LeaderboardService.Open().Rows(mission.Attempt.RankingResult.group,false).Single().result;
            Assert.AreEqual(mission.Attempt.Seed,saved.seed);
            var audit=(System.Collections.IList)field.GetValue(saved);
            Assert.GreaterOrEqual(audit.Count,2);
            string json=JsonUtility.ToJson(saved);StringAssert.Contains("audit-test",json);StringAssert.Contains("W-MAT49",json);
            Object.Destroy(crate.gameObject);
        }
        [UnityTest] public IEnumerator CorruptBoards_ShowErrorAndRetry_WithoutInterruptingMissionResults()
        {
            CreateProfile();Directory.CreateDirectory(folder);
            string broken="{\"version\":99,\"combat\":[],\"knowledge\":[]}";
            File.WriteAllText(LeaderboardService.StoragePath,broken);File.WriteAllText(LeaderboardService.StoragePath+".bak",broken);
            Click("OpenLeaderboard");yield return Frames();StringAssert.Contains("Dữ liệu gốc",Find<Text>("LeaderboardError").text);
            Assert.AreEqual(broken,File.ReadAllText(LeaderboardService.StoragePath));
            File.Delete(LeaderboardService.StoragePath);File.Delete(LeaderboardService.StoragePath+".bak");
            Click("RetryLeaderboardRead");yield return Frames();Assert.IsNotNull(Find<Text>("LeaderboardEmpty"));
            Click("LeaderboardBack");yield return Frames();yield return Deploy();var mission=MissionController.Current;
            File.WriteAllText(LeaderboardService.StoragePath,broken);File.WriteAllText(LeaderboardService.StoragePath+".bak",broken);
            Win(mission);yield return Frames();Assert.AreEqual(CampaignScreen.Results,CampaignUI.Current.ScreenState);
            Assert.IsNotEmpty(mission.Attempt.RankingSaveError);Assert.IsTrue(Find<Button>("RetryRankingSave").interactable);
            File.Delete(LeaderboardService.StoragePath);File.Delete(LeaderboardService.StoragePath+".bak");
            Click("RetryRankingSave");yield return Frames();Assert.IsNull(mission.Attempt.RankingSaveError);
        }
    }
}
