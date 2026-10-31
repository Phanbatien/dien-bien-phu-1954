using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using DBP.AI;
namespace DBP.Tests {
 public class SuppliedCharacterTests {
  [TestCase("Enemies")][TestCase("EnemyLeader")]
  public void SuppliedVisual_HasRigTexturesAndAllFiveAnimations(string kind) {
   string root="Assets/_Project/Art/Characters/Supplied/"+kind;
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Soldier.prefab");
   Assert.That(prefab,Is.Not.Null,"New character visual has not been built");
   var a=prefab.GetComponentInChildren<Animator>();
   Assert.That(a.avatar.isValid && a.avatar.isHuman,Is.True);
   Assert.That(a.applyRootMotion,Is.False);
   var deathImporter=(ModelImporter)AssetImporter.GetAtPath(root+"/animation/Dying.fbx");
   Assert.That(deathImporter.clipAnimations.All(c=>c.lockRootHeightY && c.lockRootPositionXZ),Is.True,
    "Keep the full fall in the pose while navigation owns root movement");
   foreach(var r in prefab.GetComponentsInChildren<Renderer>())
    foreach(var m in r.sharedMaterials) {Assert.That(m,Is.Not.Null); Assert.That(m.shader.name,Does.StartWith("Universal Render Pipeline/"));}
   foreach(string name in new[]{"Dying","Firing Rifle","Firing Rifle Running","Reload","Start Walking"})
    Assert.That(a.runtimeAnimatorController.animationClips.Any(c=>AssetDatabase.GetAssetPath(c)==root+"/animation/"+name+".fbx"),Is.True,name+" is not connected");
   var driver=new SerializedObject(prefab.GetComponentInChildren<EnemyAnimatorDriver>());
   Assert.That(driver.FindProperty("useAuthoredDeathAnimation")?.boolValue,Is.True,"Do not rotate an already animated falling body");
  }
 }
}
