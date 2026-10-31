using System.Collections;
using System.Linq;
using System.Reflection;
using DBP.AI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace DBP.Tests
{
 public class SuppliedAlliedCharacterAnimationTests
 {
  GameObject isolated;
  [UnityTearDown] public IEnumerator Cleanup(){if(isolated)Object.Destroy(isolated);Time.timeScale=1f;yield return null;}
  [UnityTest] public IEnumerator Allies_ReloadAfterFiveShots_BlockFire_ResumeAndFallToGround()
  {
   yield return SceneManager.LoadSceneAsync("M1_HimLam");yield return null;yield return null;
   Time.timeScale=1f;
   var source=ComradeFollower.All.First();
   foreach(var ally in ComradeFollower.All.ToArray())ally.enabled=false;
   var enemy=SoldierAI.All.First(s=>s.isActiveAndEnabled);
   foreach(var e in SoldierAI.All.ToArray())e.enabled=false;
   isolated=new GameObject("Supplied allied playback check");isolated.SetActive(false);
   isolated.transform.position=source.transform.position;
   isolated.AddComponent<NavMeshAgent>().enabled=false;
   var follower=isolated.AddComponent<ComradeFollower>();
   follower.reloadSeconds=source.reloadSeconds;
   var model=Object.Instantiate(source.GetComponentInChildren<Animator>().gameObject,isolated.transform);
   model.transform.localPosition=Vector3.zero;
   var animator=model.GetComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   var driver=model.GetComponent<EnemyAnimatorDriver>();
   isolated.SetActive(true);yield return null;
   typeof(ComradeFollower).GetField("target",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(follower,enemy);
   var fire=typeof(ComradeFollower).GetMethod("Fire",BindingFlags.Instance|BindingFlags.NonPublic);
   for(int i=0;i<4;i++){fire.Invoke(follower,null);Assert.That(follower.IsReloading,Is.False);}
   fire.Invoke(follower,null);Assert.That(follower.IsReloading,Is.True);
   var shots=typeof(ComradeFollower).GetField("shotsFired",BindingFlags.Instance|BindingFlags.NonPublic);
   fire.Invoke(follower,null);Assert.That((int)shots.GetValue(follower),Is.Zero,"A shot escaped during reloading");
   yield return new WaitForSeconds(.6f);
   Assert.That(animator.GetCurrentAnimatorStateInfo(1).IsName("Reload"),Is.True);
   yield return new WaitForSeconds(follower.reloadSeconds);
   Assert.That(follower.IsReloading,Is.False);
   fire.Invoke(follower,null);Assert.That((int)shots.GetValue(follower),Is.EqualTo(1),"Firing did not resume");
   for(int i=0;i<4;i++)fire.Invoke(follower,null);
   Assert.That(follower.IsReloading,Is.True);
   var originalRotation=model.transform.localRotation;var originalPosition=model.transform.localPosition;
   follower.TakeHit(10000f);yield return null;
   Assert.That(follower.IsReloading,Is.False,"Dead allies cannot reload");
   Assert.That(driver.useAuthoredDeathAnimation,Is.True);
   Assert.That(animator.GetLayerWeight(1),Is.Zero);
   for(int i=0;i<50;i++)animator.Update(.1f);
   yield return new WaitForSeconds(.35f);
   Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Death"),Is.True);
   float height=animator.GetBoneTransform(HumanBodyBones.Hips).position.y-isolated.transform.position.y;
   Assert.That(height,Is.InRange(-.15f,.45f),"The fallen ally is floating");
   Assert.That(Quaternion.Angle(originalRotation,model.transform.localRotation),Is.LessThan(.01f));
   Assert.That(Vector3.Distance(originalPosition,model.transform.localPosition),Is.LessThan(.001f));
  }
 }
}
