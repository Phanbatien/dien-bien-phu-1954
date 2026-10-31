using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DBP.AI;
namespace DBP.EditorTools
{
    public static partial class ProjectWiring
    {
        const string SuppliedAlliedDir = "Assets/_Project/Art/Animations/Allies/Supplied/";
        static readonly string[] SuppliedAlliedNames = { "Dying", "Firing Rifle Running", "Firing Rifle Standing", "Firing Rifle", "Reload", "Run Forward" };

        [MenuItem("DBP/Allies/Apply supplied six animations")]
        public static void ApplySuppliedAlliedAnimations()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before applying animations.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scene edits before applying animations.");
            foreach (var name in SuppliedAlliedNames)
            {
                string path = SuppliedAlliedDir + name + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (!importer) throw new FileNotFoundException("Missing supplied allied animation", path);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true; importer.importCameras = false; importer.importLights = false;
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    clip.name = name;
                    clip.loopTime = name == "Run Forward" || name == "Firing Rifle Running";
                    clip.lockRootRotation = true; clip.lockRootPositionXZ = true; clip.lockRootHeightY = true;
                    clip.keepOriginalOrientation = true; clip.keepOriginalPositionXZ = true; clip.keepOriginalPositionY = true;
                }
                importer.clipAnimations = clips; importer.SaveAndReimport();
                var avatar = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Animator>()?.avatar;
                if (!avatar || !avatar.isHuman || !avatar.isValid) throw new InvalidOperationException("Invalid humanoid animation: " + name);
            }
            ApplyAlliedAnimationClips();
            var prefab = PrefabUtility.LoadPrefabContents(ComradePrefabPath);
            try
            {
                ConfigureAlliedAnimation(prefab.GetComponent<ComradeFollower>());
                PrefabUtility.SaveAsPrefabAsset(prefab, ComradePrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var original = SceneManager.GetActiveScene();
            try
            {
                foreach (var path in MissionScenes)
                {
                    var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
                    if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    try
                    {
                        var allies = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ComradeFollower>(true)).ToArray();
                        if (allies.Length == 0) continue;
                        foreach (var ally in allies) ConfigureAlliedAnimation(ally);
                        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                        Debug.Log($"[Allied animations] {scene.name}: updated {allies.Length} allies.");
                    }
                    finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
                }
            }
            finally { if (original.isLoaded) SceneManager.SetActiveScene(original); AssetDatabase.SaveAssets(); }
        }

        static AnimationClip SuppliedAlliedClip(string name) => AssetDatabase.LoadAllAssetsAtPath(SuppliedAlliedDir + name + ".fbx")
            .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));

        static bool HasSuppliedAlliedAnimations => File.Exists(SuppliedAlliedDir + "Dying.fbx");

        static void ConfigureAlliedAnimation(ComradeFollower ally)
        {
            ally.GetComponentInChildren<EnemyAnimatorDriver>(true).useAuthoredDeathAnimation = true;
            ally.reloadSeconds = SuppliedAlliedClip("Reload").length;
            var animator = ally.GetComponentInChildren<Animator>(true);
            var rifle = animator.GetComponentsInChildren<Transform>(true).First(t => t.name == "AlliedRifle");
            var grip = animator.GetComponent<AlliedRifleGrip>();
            if (!grip)
            {
                grip = animator.gameObject.AddComponent<AlliedRifleGrip>();
                grip.localGrip = rifle.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightHand).position);
            }
            grip.rifle = rifle;
            grip.rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            grip.leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            EditorUtility.SetDirty(grip);
            EditorUtility.SetDirty(ally); EditorUtility.SetDirty(ally.GetComponentInChildren<EnemyAnimatorDriver>(true));
            if (PrefabUtility.IsPartOfPrefabInstance(ally))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(ally);
                PrefabUtility.RecordPrefabInstancePropertyModifications(ally.GetComponentInChildren<EnemyAnimatorDriver>(true));
                PrefabUtility.RecordPrefabInstancePropertyModifications(grip);
            }
        }

        // Called after rebuilding the ordinary controllers, so the supplied actions survive system wiring.
        static void ApplyAlliedAnimationClips()
        {
            if (!HasSuppliedAlliedAnimations) return;
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AlliedControllerPath);
            if (!controller) return;
            var sm = controller.layers[0].stateMachine;
            sm.states.First(s => s.state.name == "Death").state.motion = SuppliedAlliedClip("Dying");
            var loco = (BlendTree)sm.states.First(s => s.state.name == "Locomotion").state.motion;
            var children = loco.children;
            children[children.Length - 1].motion = SuppliedAlliedClip("Run Forward"); loco.children = children;
            if (!controller.parameters.Any(p => p.name == "FireVariant")) controller.AddParameter("FireVariant", AnimatorControllerParameterType.Float);
            if (!controller.parameters.Any(p => p.name == "Reload")) controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
            var upper = controller.layers[1].stateMachine;
            var shoot = upper.states.First(s => s.state.name == "Shoot").state;
            var fire = shoot.motion as BlendTree;
            if (!fire || fire.name != "Allied supplied firing")
            {
                fire = new BlendTree { name = "Allied supplied firing", blendParameter = "Speed", useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(fire, controller);
            }
            var standing = AssetDatabase.LoadAllAssetsAtPath(AlliedControllerPath).OfType<BlendTree>().FirstOrDefault(t => t.name == "Allied stationary variations");
            if (!standing)
            {
                standing = new BlendTree { name = "Allied stationary variations", blendParameter = "FireVariant", useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(standing, controller);
            }
            standing.children = new[] {
                new ChildMotion { motion = SuppliedAlliedClip("Firing Rifle Standing"), threshold = 0, timeScale = 1 },
                new ChildMotion { motion = SuppliedAlliedClip("Firing Rifle"), threshold = 1, timeScale = 1 }
            };
            fire.children = new[] {
                new ChildMotion { motion = standing, threshold = 0, timeScale = 1 },
                new ChildMotion { motion = SuppliedAlliedClip("Firing Rifle Running"), threshold = 2, timeScale = 1 }
            };
            shoot.motion = fire;
            if (!upper.states.Any(s => s.state.name == "Reload"))
            {
                var reload = upper.AddState("Reload"); reload.motion = SuppliedAlliedClip("Reload");
                var enter = upper.AddAnyStateTransition(reload); enter.AddCondition(AnimatorConditionMode.If, 0, "Reload"); enter.duration = .1f; enter.canTransitionToSelf = false;
                var exit = reload.AddTransition(upper.defaultState); exit.hasExitTime = true; exit.exitTime = 1f; exit.duration = .1f;
            }
            EditorUtility.SetDirty(loco); EditorUtility.SetDirty(fire); EditorUtility.SetDirty(standing); EditorUtility.SetDirty(controller);
        }
    }
}
