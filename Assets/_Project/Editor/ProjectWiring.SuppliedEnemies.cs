using System;
using System.IO;
using System.Linq;
using DBP.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DBP.EditorTools
{
    public static partial class ProjectWiring
    {
        const string SuppliedCharacters = "Assets/_Project/Art/Characters/Supplied/";

        [MenuItem("DBP/Enemies/Apply supplied models and animations")]
        public static void ApplySuppliedEnemies()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before replacing mission assets.");
            // Additive editing preserves the user's open scene and any unsaved changes.
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scene changes before applying enemy models.");
            var regular = BuildSuppliedVisual("Enemies");
            var leader = BuildSuppliedVisual("EnemyLeader");
            var original = SceneManager.GetActiveScene();
            try
            {
                foreach (var path in MissionScenes)
                {
                    var scene = SceneManager.GetSceneByPath(path);
                    bool opened = !scene.isLoaded;
                    if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    SceneManager.SetActiveScene(scene);
                    try
                    {
                        int count = 0;
                        foreach (var h in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DBP.Core.Health>(true)))
                        {
                            if (!h.GetComponent<SoldierAI>() && !h.GetComponent<SentryAI>() && !h.GetComponent<TrenchSoldierAI>()) continue;
                            ReplaceSuppliedVisual(h.transform, h.GetComponent<OfficerLeader>() ? leader : regular);
                            count++;
                        }
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                        Debug.Log($"[Supplied characters] {scene.name}: replaced {count} enemies.");
                    }
                    finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
                }
            }
            finally { if (original.isLoaded) SceneManager.SetActiveScene(original); AssetDatabase.SaveAssets(); }
        }

        static AnimationClip SuppliedClip(string kind, string name) => AssetDatabase.LoadAllAssetsAtPath(
            SuppliedCharacters + kind + "/animation/" + name + ".fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));

        static AnimatorController SuppliedController(string kind)
        {
            string path = SuppliedCharacters + kind + "/Soldier.controller";
            if (!File.Exists(path)) AssetDatabase.CopyAsset(SoldierControllerPath, path);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            var sm = controller.layers[0].stateMachine;
            sm.states.First(s => s.state.name == "Death").state.motion = SuppliedClip(kind, "Dying");
            var upper = controller.layers[1].stateMachine;
            var shoot = upper.states.First(s => s.state.name == "Shoot").state;
            var fireTree = shoot.motion as BlendTree;
            if (!fireTree || !AssetDatabase.IsSubAsset(fireTree))
            {
                fireTree = new BlendTree { name = "Supplied rifle fire", blendParameter = "Speed", useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(fireTree, controller);
            }
            fireTree.children = new[] {
                new ChildMotion { motion = SuppliedClip(kind, "Firing Rifle"), threshold = 0f, timeScale = 1f },
                new ChildMotion { motion = SuppliedClip(kind, "Firing Rifle Running"), threshold = 2f, timeScale = 1f }
            };
            shoot.motion = fireTree;
            if (!controller.parameters.Any(p => p.name == "Reload")) controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
            if (!upper.states.Any(s => s.state.name == "Reload"))
            {
                var reload = upper.AddState("Reload"); reload.motion = SuppliedClip(kind, "Reload");
                var enter = upper.AddAnyStateTransition(reload); enter.AddCondition(AnimatorConditionMode.If, 0f, "Reload");
                enter.canTransitionToSelf = false; enter.duration = .1f;
                var exit = reload.AddTransition(upper.defaultState); exit.hasExitTime = true; exit.exitTime = 1f; exit.duration = .15f;
            }
            if (!controller.parameters.Any(p => p.name == "StartWalking")) controller.AddParameter("StartWalking", AnimatorControllerParameterType.Trigger);
            if (!sm.states.Any(s => s.state.name == "StartWalking"))
            {
                var walk = sm.AddState("StartWalking"); walk.motion = SuppliedClip(kind, "Start Walking");
                var enter = sm.AddAnyStateTransition(walk); enter.AddCondition(AnimatorConditionMode.If, 0f, "StartWalking");
                enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crouch"); enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "Climb");
                enter.canTransitionToSelf = false; enter.duration = .12f;
                var exit = walk.AddTransition(sm.defaultState); exit.hasExitTime = true; exit.exitTime = .9f; exit.duration = .15f;
                var stop = walk.AddTransition(sm.defaultState); stop.AddCondition(AnimatorConditionMode.Less, .1f, "Speed"); stop.duration = .1f;
                Link(walk, sm.states.First(s => s.state.name == "Crouch").state, "Crouch", true);
                Link(walk, sm.states.First(s => s.state.name == "Climb").state, "Climb", true);
            }
            EditorUtility.SetDirty(fireTree); EditorUtility.SetDirty(controller);
            return controller;
        }

        static Material SuppliedMaterial(string kind, string name, string texture = null)
        {
            string root = SuppliedCharacters + kind + "/";
            string path = root + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", texture == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(root + "textures/" + texture));
            mat.SetColor("_BaseColor", texture == null ? new Color(.18f, .19f, .16f) : Color.white);
            mat.SetFloat("_Smoothness", .2f);
            EditorUtility.SetDirty(mat); return mat;
        }

        static GameObject BuildSuppliedVisual(string kind)
        {
            string path = SuppliedCharacters + kind + "/Soldier.prefab";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SuppliedCharacters + kind + "/animation/Firing Rifle.fbx");
            if (!source || !source.GetComponent<Animator>().avatar.isHuman) throw new InvalidOperationException("Import a humanoid firing model first: " + kind);
            var preview = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Supplied" + kind);
            SceneManager.MoveGameObjectToScene(root, preview);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);
                model.transform.SetParent(root.transform, false);
                model.name = "Rig";
                var bounds = WorldBounds(model); model.transform.localScale *= 1.8f / bounds.size.y;
                bounds = WorldBounds(model); model.transform.position -= Vector3.up * bounds.min.y;
                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = kind == "EnemyLeader" ? SuppliedMaterial(kind, "Officer", "gltf_embedded_0.jpeg")
                        : r.name.Contains("body") ? SuppliedMaterial(kind, "Body", "SK_alliedsoldier_body_D.jpg")
                        : r.name.Contains("head") ? SuppliedMaterial(kind, "Head", "SK_alliedsoldier_head_D.jpg") : SuppliedMaterial(kind, "Equipment");
                }
                var a = model.GetComponent<Animator>(); a.runtimeAnimatorController = SuppliedController(kind);
                a.applyRootMotion = false; a.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                model.AddComponent<EnemyAnimatorDriver>().useAuthoredDeathAnimation = true;
                SuppliedClip(kind, "Firing Rifle").SampleAnimation(model, 0f);
                var right = a.GetBoneTransform(HumanBodyBones.RightHand); var left = a.GetBoneTransform(HumanBodyBones.LeftHand);
                var gun = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPack + "WWII_Rifle_B.prefab"), preview);
                gun.name = "Rifle1_Supplied";
                var gb = WorldBounds(gun);
                var grip = new Vector3(gb.center.x, gb.center.y - gb.size.y * .1f, Mathf.Lerp(gb.min.z, gb.max.z, .3f));
                gun.transform.localScale *= .95f / gb.size.z;
                gun.transform.rotation = Quaternion.LookRotation(left.position - right.position, root.transform.up);
                gun.transform.position += right.position - gun.transform.TransformPoint(grip);
                gun.transform.SetParent(right, true); UrpMaterialUtility.Apply(gun); FitBayonet(gun.transform);
                foreach (var col in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        static void ReplaceSuppliedVisual(Transform soldier, GameObject prefab)
        {
            var holder = UnscaledChild(soldier, "Model");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
            model.transform.localPosition = Vector3.down;
            var a = model.GetComponentInChildren<Animator>();
            var mr = soldier.GetComponent<MeshRenderer>(); if (mr) mr.enabled = false;
            // Keep the original eye/muzzle and all combat settings; reconnect only the visual animator.
            foreach (var component in soldier.GetComponents<MonoBehaviour>())
            {
                var so = new SerializedObject(component);
                var p = so.FindProperty("animator"); if (p != null) p.objectReferenceValue = a;
                if (component is EnemyDeathHandler) so.FindProperty("waitBeforeFade").floatValue = SuppliedClip(prefab.name.Contains("EnemyLeader") ? "EnemyLeader" : "Enemies", "Dying").length + .15f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
