using System.Collections;
using System.Linq;
using DBP.AI;
using DBP.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace DBP.Tests {
 public class SuppliedCharacterAnimationTests {
  GameObject isolated;
  [UnityTearDown] public IEnumerator Cleanup() { if(isolated) Object.Destroy(isolated); Time.timeScale=1f; yield return null; }
  [UnityTest] public IEnumerator ImportedDeath_PlaysWithoutLegacyRotation_AndActionsReturn() {
   yield return SceneManager.LoadSceneAsync("M1_HimLam"); yield return null; yield return null;
   Time.timeScale=1f;
   var sources=Object.FindObjectsByType<SoldierAI>(FindObjectsSortMode.None);
   foreach(bool officer in new[]{false,true}) {
    var source=sources.First(s=>(s.GetComponent<OfficerLeader>()!=null)==officer);
    isolated=new GameObject("Isolated supplied animation test");isolated.SetActive(false);
    isolated.transform.position=new Vector3(0,1000,0);
    var health=isolated.AddComponent<Health>();
    var visual=Object.Instantiate(source.transform.Find("Model").GetChild(0).gameObject,isolated.transform);
    var a=visual.GetComponentInChildren<Animator>(); a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    var driver=visual.GetComponentInChildren<EnemyAnimatorDriver>();
    Assert.That(driver.useAuthoredDeathAnimation,Is.True);
    isolated.SetActive(true); yield return null;
    foreach(string action in new[]{"Shoot","Reload"}) {
     a.Play("Trong",1,0f);a.Update(0f);a.ResetTrigger("StartWalking");
     a.SetTrigger(action);a.Update(.15f);a.Update(.15f);
     Assert.That(a.GetCurrentAnimatorStateInfo(1).IsName(action),Is.True,action);
     for(int i=0;i<60;i++)a.Update(.1f);
     Assert.That(a.GetCurrentAnimatorStateInfo(1).IsName("Trong"),Is.True,action+" did not finish");
    }
    var rotation=a.transform.localRotation;var position=a.transform.localPosition;
    health.TakeDamage(10000f,DamageType.Explosive,isolated.transform.position);
    yield return null;
    Assert.That(a.GetLayerWeight(1),Is.Zero,"Upper body must stop overriding death");
    for(int i=0;i<50;i++)a.Update(.1f);
    yield return new WaitForSeconds(.85f);
    Assert.That(a.GetCurrentAnimatorStateInfo(0).IsName("Death"),Is.True);
    float fallenHipHeight=a.GetBoneTransform(HumanBodyBones.Hips).position.y-visual.transform.position.y;
    Assert.That(fallenHipHeight,Is.InRange(-.15f,.45f),"Fallen body must settle near the ground");
    Assert.That(Quaternion.Angle(rotation,a.transform.localRotation),Is.LessThan(.01f),"Death was rotated twice");
    Assert.That(Vector3.Distance(position,a.transform.localPosition),Is.LessThan(.001f),"Unexpected root movement");
    Object.Destroy(isolated);yield return null;
   }
  }
 }
}
