using System;
using System.Linq;
using DBP.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DBP.EditorTools
{
    public static partial class ProjectWiring
    {
        const string AlliedAnimatedModelPath = "Assets/_Project/Art/Animations/Allies/Firing_Rifle.fbx";
        const string AlliedControllerPath = "Assets/_Project/Art/Animations/Allies/AlliedSoldier.controller";
        const string AlliedTexturePrefix = "Assets/_Project/Art/Characters/JungleSentinel/Meshy_AI_Jungle_Sentinel_20261014_texture";

        [MenuItem("DBP/Allies/Build supplied soldier and firing animation")]
        public static void BuildAlliedSoldier()
        {
            BuildJungleComrade();
            AssetDatabase.SaveAssets();
        }

        static GameObject BuildJungleComrade()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(AlliedAnimatedModelPath);
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(AlliedAnimatedModelPath);
            var avatar = source.GetComponent<Animator>()?.avatar;
            if (!avatar || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("The supplied allied soldier needs a valid humanoid rig.");

            // Build away from the user's open scene, including while the Editor is playing.
            var preview = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("BoDoi");
            SceneManager.MoveGameObjectToScene(root, preview);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);
                model.transform.SetParent(root.transform, false);
                model.name = "Model";
                var bounds = WorldBounds(model);
                model.transform.localScale *= 1.72f / bounds.size.y;
                bounds = WorldBounds(model);
                model.transform.position -= Vector3.up * bounds.min.y;
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = AlliedMaterial();
                foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(collider);

                var animator = model.GetComponent<Animator>();
                animator.runtimeAnimatorController = AlliedController();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                model.AddComponent<EnemyAnimatorDriver>();

                // Fit the rifle in the supplied aiming pose, then retain that
                // hand-relative grip while locomotion and recoil animate the rig.
                var firingClip = animator.runtimeAnimatorController.animationClips
                    .First(c => AssetDatabase.GetAssetPath(c) == AlliedAnimatedModelPath);
                firingClip.SampleAnimation(model, 0f);
                var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                const string weaponPath = WeaponPack + "WWII_Rifle_B.prefab";
                var gun = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath), preview);
                gun.name = "AlliedRifle";
                var gunBounds = WorldBounds(gun);
                var grip = new Vector3(gunBounds.center.x, gunBounds.center.y - gunBounds.size.y * .1f,
                    Mathf.Lerp(gunBounds.min.z, gunBounds.max.z, .3f));
                var muzzlePoint = new Vector3(gunBounds.center.x, gunBounds.center.y, gunBounds.max.z);
                gun.transform.localScale *= .95f / gunBounds.size.z;
                gun.transform.rotation = Quaternion.LookRotation(leftHand.position - rightHand.position, root.transform.up);
                gun.transform.position += rightHand.position - gun.transform.TransformPoint(grip);
                gun.transform.SetParent(rightHand, true);
                foreach (var collider in gun.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                UrpMaterialUtility.Apply(gun);
                var muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(gun.transform, false);
                muzzle.localPosition = muzzlePoint;
                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius = .35f;
                agent.height = 1.75f;
                agent.speed = 2.2f;
                agent.angularSpeed = 360f;
                agent.stoppingDistance = .8f;
                agent.avoidancePriority = 70;
                root.AddComponent<ComradeFollower>().muzzle = muzzle;
                return PrefabUtility.SaveAsPrefabAsset(root, ComradePrefabPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        static Material AlliedMaterial()
        {
            string path = AlliedTexturePrefix + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            var normalImporter = (TextureImporter)AssetImporter.GetAtPath(AlliedTexturePrefix + "_normal.png");
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
            var metallicImporter = (TextureImporter)AssetImporter.GetAtPath(AlliedTexturePrefix + "_metallic.png");
            metallicImporter.sRGBTexture = false;
            metallicImporter.SaveAndReimport();
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "JungleSentinel" };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(AlliedTexturePrefix + ".png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(AlliedTexturePrefix + "_normal.png"));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(AlliedTexturePrefix + "_metallic.png"));
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Smoothness", .25f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static AnimatorController AlliedController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AlliedControllerPath);
            if (controller) return controller;
            controller = AnimatorController.CreateAnimatorControllerAtPath(AlliedControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            var locomotion = controller.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            AnimationClip Clip(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .First(c => !c.name.StartsWith("__preview__"));
            tree.AddChild(Clip(StarterAnims + "Stand--Idle.anim.fbx"), 0f);
            tree.AddChild(Clip(StarterAnims + "Locomotion--Walk_N.anim.fbx"), 1.6f);
            tree.AddChild(Clip(StarterAnims + "Locomotion--Run_N.anim.fbx"), 4.5f);
            var machine = controller.layers[0].stateMachine;
            machine.defaultState = locomotion;
            var fire = machine.AddState("FireRifle");
            fire.motion = Clip(AlliedAnimatedModelPath);
            var start = machine.AddAnyStateTransition(fire);
            start.AddCondition(AnimatorConditionMode.If, 0f, "Shoot");
            start.duration = .08f;
            start.hasExitTime = false;
            start.canTransitionToSelf = true;
            var end = fire.AddTransition(locomotion);
            end.hasExitTime = true;
            end.exitTime = 1f;
            end.duration = .12f;
            EditorUtility.SetDirty(controller);
            return controller;
        }
    }
}
