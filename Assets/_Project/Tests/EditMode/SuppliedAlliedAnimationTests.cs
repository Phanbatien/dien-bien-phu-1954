using System.Linq;
using DBP.AI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace DBP.Tests {
 public class SuppliedAlliedAnimationTests {
  public const string Folder="Assets/_Project/Art/Animations/Allies/Supplied/";
  [Test] public void AlliedController_UsesEverySuppliedClip_AndBakesDeathToGround() {
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Project/Art/Animations/Allies/AlliedSoldier.controller");
   foreach(string name in new[]{"Dying","Firing Rifle Running","Firing Rifle Standing","Firing Rifle","Reload","Run Forward"}) {
    string path=Folder+name+".fbx";
    Assert.That(controller.animationClips.Any(c=>AssetDatabase.GetAssetPath(c)==path),Is.True,name+" is not connected");
    var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
    Assert.That(model.GetComponent<Animator>().avatar.isHuman,Is.True,name);
   }
   var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"Dying.fbx");
   Assert.That(importer.clipAnimations.All(c=>c.lockRootHeightY && c.lockRootPositionXZ),Is.True);
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/BoDoi.prefab");
   Assert.That(prefab.GetComponentInChildren<EnemyAnimatorDriver>().useAuthoredDeathAnimation,Is.True);
   Assert.That(controller.parameters.Any(p=>p.name=="Reload"),Is.True);
   Assert.That(controller.parameters.Any(p=>p.name=="FireVariant"),Is.True);
  }
 }
}
