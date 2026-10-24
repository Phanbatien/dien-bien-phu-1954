using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DBP.Core;
using DBP.Missions;
using DBP.Quiz;
using DBP.Upgrades;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DBP.Tests
{
    public class PolishFlowTests
    {
        IDisposable scope; string folder; float volume;
        static Text Notice => Object.FindObjectsByType<Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="NoticeText");
        static AudioSource Voice(string name)=>Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Single(s=>s.name==name);
        [UnitySetUp] public IEnumerator Setup()
        {
            folder=Path.Combine(Application.temporaryCachePath,"t69-"+Guid.NewGuid().ToString("N"));
            scope=ProfileService.UseTemporaryStore(Path.Combine(folder,"profiles.json")); volume=AudioListener.volume;
            Time.timeScale=1; InputLock.ResetAll(); AudioListener.pause=false;
            yield return SceneManager.LoadSceneAsync("Sandbox"); yield return null; yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1; InputLock.ResetAll();AudioListener.pause=false;
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            scope.Dispose(); AudioListener.volume=volume;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        [UnityTest] public IEnumerator Mix_BackgroundAndFootstepsStayBehindWeapons_UIObeysMaster()
        {
            yield return SceneManager.LoadSceneAsync("M1_HimLam"); yield return null; yield return null;Time.timeScale=1;
            var gun=CampaignAudio.Effect("mat49_A",Vector3.zero,false);
            var step=CampaignAudio.Effect("step_mud02",Vector3.zero,false);
            Assert.LessOrEqual(Voice("MissionMusic").volume,.3f,"Music must leave headroom for action.");
            Assert.LessOrEqual(Voice("AirplaneAmbience").volume,.2f);
            Assert.Less(step.volume,gun.volume);
            Assert.LessOrEqual(gun.volume,.65f);
            var ui=CampaignAudio.Interface("ui_timeout");
            Assert.IsTrue(ui.ignoreListenerPause); Assert.IsFalse(ui.ignoreListenerVolume);
            AudioListener.volume=0; Assert.AreEqual(0,AudioListener.volume);
        }
        [UnityTest] public IEnumerator Combat_DucksAndRestoresMusic_WithoutChangingMaster()
        {
            yield return SceneManager.LoadSceneAsync("M1_HimLam");yield return null;yield return null;Time.timeScale=1;
            var music=Voice("MissionMusic");float before=music.volume;AudioListener.volume=.4f;
            CampaignAudio.Effect("mat49_A",Vector3.zero,false);
            yield return new WaitForSecondsRealtime(.15f);Assert.Less(music.volume,before*.9f);
            yield return new WaitForSecondsRealtime(1.2f);Assert.AreEqual(before,music.volume,.01f);
            Assert.AreEqual(.4f,AudioListener.volume,.001f);
        }
        [UnityTest] public IEnumerator CorrectQuizAndTimeout_ShowTruthfulVisibleFeedback()
        {
            var session=FieldQuizSession.Instance;Assert.IsTrue(session.TryOpen(1,12,_=>{}));
            var question=(Question)typeof(FieldQuizSession).GetField("current",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(session);
            typeof(FieldQuizSession).GetMethod("OnAnswer",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(session,new object[]{question.correct});
            yield return null;
            Assert.NotNull(Notice,"Correct answers need visible feedback, not only sound.");StringAssert.Contains("kiến thức",Notice.text);
            yield return new WaitForSecondsRealtime(3.3f);
            Assert.IsTrue(session.TryOpen(1,5,.03f,_=>{}),"Use a remaining question; the level-1 fixture is intentionally not repeated.");
            yield return new WaitForSecondsRealtime(.1f);StringAssert.Contains("Hết giờ",Notice.text);
        }
        [UnityTest] public IEnumerator SuccessfulSave_ShowsNotice_FailedWriteDoesNot()
        {
            ProfileService.Repository.Create("Save test","SE",false);
            var mission=MissionController.Current;
            using(var locked=new FileStream(Path.Combine(folder,"profiles.json.tmp"),FileMode.Create,FileAccess.Write,FileShare.None))
            {
                mission.Setup("T69","Test","M2","M2_GiuDocLap",mission.Player,new System.Collections.Generic.List<Objective>(),true);
                yield return null;yield return null;
                Assert.IsTrue(mission.Attempt.SaveFailed);
                Assert.IsTrue(Notice==null||!Notice.text.Contains("Đã lưu"),"Failed persistence must not announce success.");
            }
            Assert.IsTrue(mission.TrySaveProgress());yield return null;
            Assert.NotNull(Notice);StringAssert.Contains("Đã lưu",Notice.text);
        }
        [UnityTest] public IEnumerator NoticeQueue_IsBounded_NonBlocking_ExpiresWhilePaused_AndCleansUp()
        {
            var type=typeof(DBP.UI.CampaignUI).Assembly.GetType("DBP.UI.CampaignNotices");
            Assert.NotNull(type,"T69 needs an unscaled bounded notice overlay.");
            var show=type.GetMethod("Show");
            for(int i=0;i<20;i++)show.Invoke(null,new object[]{"Nhận thưởng "+i});
            yield return null;Assert.NotNull(Notice);
            Assert.IsTrue(Notice.GetComponentsInParent<CanvasGroup>().All(g=>!g.blocksRaycasts));
            Assert.IsFalse(Notice.raycastTarget);
            Time.timeScale=0;yield return new WaitForSecondsRealtime(9f);
            Assert.IsTrue(Notice==null||!Notice.gameObject.activeInHierarchy,"A bounded queue must finish even while paused.");
            show.Invoke(null,new object[]{"Scene cleanup"});yield return null;var old=Notice;
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;Assert.IsFalse(old);
        }
        [UnityTest] public IEnumerator FinalMissionSave_WritesBeforeAnnouncingSuccess()
        {
            ProfileService.Repository.Create("Final mission","SE",false);var mission=MissionController.Current;
            using(var locked=new FileStream(Path.Combine(folder,"profiles.json.tmp"),FileMode.Create,FileAccess.Write,FileShare.None))
            {
                mission.Setup("M5","Final",null,null,mission.Player,new System.Collections.Generic.List<Objective>(),true);
                yield return null;yield return null;
                Assert.IsTrue(mission.Attempt.SaveFailed,"Final-mission success must still verify persistence.");
                Assert.IsTrue(Notice==null||!Notice.text.Contains("Đã lưu"));
            }
            Assert.IsTrue(mission.TrySaveProgress());yield return null;StringAssert.Contains("Đã lưu",Notice.text);
        }
        [UnityTest] public IEnumerator EmptyOrDuplicateZeroReward_DoesNotAnnounceReceivedItems()
        {
            var who=Object.FindAnyObjectByType<DBP.Interaction.Interactor>();
            foreach(var id in new[]{"", "unknown",who.Weapons.Inventory.Current.Def.id})
            {
                var crate=DBP.Interaction.QuizCrate.Spawn(Vector3.one*100,Quaternion.identity,
                    new DBP.Interaction.CrateReward{weaponId=id,ammo=0,grenades=0},true,null);
                if(id=="unknown")LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex("\\[WeaponCatalog\\] Không có vũ khí 'unknown'"));
                typeof(DBP.Interaction.QuizCrate).GetMethod("Resolve",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(crate,new object[]{
                    new QuizResult{questionId="t69-empty",level=1,timeLimit=12,remaining=6,outcome=QuizOutcome.Correct},who});
                yield return null;
                Assert.IsTrue(Notice==null||!Notice.text.Contains("Đã nhận"),"An empty item reward must not claim an inventory grant.");
                Object.Destroy(crate.gameObject);
            }
        }
        [UnityTest] public IEnumerator NewWeaponReward_ShowsReceivedItems()
        {
            var who=Object.FindAnyObjectByType<DBP.Interaction.Interactor>();
            var crate=DBP.Interaction.QuizCrate.Spawn(Vector3.one*100,Quaternion.identity,
                new DBP.Interaction.CrateReward{weaponId="W-MAT49",ammo=0,grenades=0},true,null);
            typeof(DBP.Interaction.QuizCrate).GetMethod("Resolve",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(crate,new object[]{
                new QuizResult{questionId="t69-reward",level=1,timeLimit=12,remaining=6,outcome=QuizOutcome.Correct},who});
            yield return null;
            Assert.IsTrue(who.Weapons.Inventory.Weapons.Any(w=>w.Def.id=="W-MAT49"));
            Assert.NotNull(Notice);StringAssert.Contains("Đã nhận",Notice.text);
            Object.Destroy(crate.gameObject);
        }
        [UnityTest] public IEnumerator ResultNotice_DoesNotCoverLeaderboardButton()
        {
            yield return SceneManager.LoadSceneAsync("M1_HimLam");yield return null;yield return null;
            MissionController.Current.Player.TakeDamage(100000,DamageType.Bullet,Vector3.zero);yield return null;
            var type=typeof(DBP.UI.CampaignUI).Assembly.GetType("DBP.UI.CampaignNotices");type.GetMethod("Show").Invoke(null,new object[]{"Đã lưu tiến độ nhiệm vụ trên máy."});
            yield return null;Canvas.ForceUpdateCanvases();
            var card=Notice.transform.parent.GetComponent<RectTransform>();
            var button=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="ResultLeaderboard").GetComponent<RectTransform>();
            var a=new Vector3[4];var b=new Vector3[4];card.GetWorldCorners(a);button.GetWorldCorners(b);
            bool overlap=a[0].x<b[2].x&&a[2].x>b[0].x&&a[0].y<b[2].y&&a[2].y>b[0].y;
            Assert.IsFalse(overlap,"Feedback must not obscure the result navigation button.");
        }
    }
}
