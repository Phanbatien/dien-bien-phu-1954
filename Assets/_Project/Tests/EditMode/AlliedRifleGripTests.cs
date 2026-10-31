using System.Linq;
using DBP.AI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DBP.Tests {
 public class AlliedRifleGripTests {
  [Test] public void Rifle_StaysAtRightHand_InEverySuppliedPose(){
   var preview=EditorSceneManager.NewPreviewScene();
   try{
    var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/BoDoi.prefab"),preview);
    var a=go.GetComponentInChildren<Animator>();a.enabled=false;
    var grip=a.GetComponent<AlliedRifleGrip>();Assert.That(grip,Is.Not.Null);
    foreach(string name in new[]{"Dying","Firing Rifle Running","Firing Rifle Standing","Firing Rifle","Reload","Run Forward"}){
     var c=AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Animations/Allies/Supplied/"+name+".fbx").OfType<AnimationClip>().First(x=>!x.name.StartsWith("__preview"));
     foreach(float t in new[]{.1f,.4f,.8f}){
      c.SampleAnimation(a.gameObject,c.length*t);grip.Align();
      Assert.That(Vector3.Distance(grip.rightHand.position,grip.rifle.TransformPoint(grip.localGrip)),Is.LessThan(.003f),name);
      Assert.That(Vector3.Dot(grip.rifle.forward,(grip.leftHand.position-grip.rightHand.position).normalized),Is.GreaterThan(.999f),name);
     }
    }
   }finally{EditorSceneManager.ClosePreviewScene(preview);}
  }
 }
}
