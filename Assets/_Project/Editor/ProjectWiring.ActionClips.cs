using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Clip Mixamo trưởng nhóm cung cấp (31/10), thay clip tự dựng: bộ đội ngồi bắn (Crouch Fire Rifle),
    /// địch đâm dao (Stabbing) và ném lựu đạn (Grenade Throw). Chạy sau khi dựng lại controller nên không bị ghi đè.
    public static partial class ProjectWiring
    {
        const string CrouchFireClip = SuppliedAlliedDir + "Crouch Fire Rifle.fbx";
        const string EnemyStabClip = "Assets/_Project/Art/Animations/Enemies/Stabbing.fbx";
        const string EnemyThrowClip = "Assets/_Project/Art/Animations/Enemies/Grenade Throw.fbx";

        static void ApplyActionClips()
        {
            var crouch = HumanClip(CrouchFireClip, "Crouch Fire Rifle", true);
            var stab = HumanClip(EnemyStabClip, "Stabbing", false);
            var thrown = HumanClip(EnemyThrowClip, "Grenade Throw", false);

            var allied = AssetDatabase.LoadAssetAtPath<AnimatorController>(AlliedControllerPath);
            if (allied && crouch) SetMotion(allied, 0, "Crouch", crouch);

            foreach (var path in new[] { SoldierControllerPath, SuppliedCharacters + "Enemies/Soldier.controller", SuppliedCharacters + "EnemyLeader/Soldier.controller" })
            {
                var enemy = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (!enemy) continue;
                if (stab) SetMotion(enemy, 1, "Stab", stab);
                if (thrown) SetMotion(enemy, 1, "Throw", thrown);
            }
        }

        static void SetMotion(AnimatorController controller, int layer, string state, Motion motion)
        {
            controller.layers[layer].stateMachine.states.First(s => s.state.name == state).state.motion = motion;
            EditorUtility.SetDirty(controller);
        }

        /// Nhập FBX thành clip Humanoid, chôn chuyển động gốc vào tư thế (lính do NavMesh di chuyển). Chỉ nhập lại khi chưa đúng.
        static AnimationClip HumanClip(string path, string name, bool loop)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) return null;
            if (importer.animationType != ModelImporterAnimationType.Human || importer.clipAnimations.Length == 0 || importer.clipAnimations[0].name != name)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true; importer.importCameras = false; importer.importLights = false;
                var clips = importer.defaultClipAnimations;
                foreach (var c in clips)
                {
                    c.name = name;
                    c.loopTime = loop;
                    c.lockRootRotation = true; c.lockRootPositionXZ = true; c.lockRootHeightY = true;
                    c.keepOriginalOrientation = true; c.keepOriginalPositionXZ = true; c.keepOriginalPositionY = true;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
        }
    }
}
