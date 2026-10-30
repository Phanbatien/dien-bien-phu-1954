using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DBP.EditorTools
{
    /// Player weapon views use textured arms extracted from the allied soldier.
    public static partial class ProjectWiring
    {
        const string VmDir = "Assets/_Project/Resources/ViewModels";
        const string VmMeshDir = "Assets/_Project/Art/ViewModel";
        const string WeaponPack = "Assets/Low Poly Weapon Series V 4/Prefabs/Low Poly Series V 4 New/Weapons/";
        struct GunSpec { public string name, prefab; public float grip, fore, gripDrop, foreDrop; }
        static readonly GunSpec[] Guns =
        {
            new GunSpec { name = "VM_Rifle", prefab = WeaponPack + "WWII_Rifle_B.prefab", grip = .30f, fore = .56f, gripDrop = .10f, foreDrop = .05f },
            new GunSpec { name = "VM_Scoped", prefab = WeaponPack + "WWII_Recon_B.prefab", grip = .27f, fore = .55f, gripDrop = .12f, foreDrop = .06f },
            new GunSpec { name = "VM_Smg", prefab = WeaponPack + "WWII_SMG_B.prefab", grip = .42f, fore = .70f, gripDrop = .30f, foreDrop = .02f },
        };
        public static void BuildViewModelsCI()
        {
            try { BuildViewModels(true); AssetDatabase.SaveAssets(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        [MenuItem("DBP/Dựng tay + súng góc nhìn thứ nhất")]
        public static void BuildViewModelsMenu() { BuildViewModels(true); AssetDatabase.SaveAssets(); }
        static void EnsureViewModels() => BuildViewModels(false);
        static void BuildViewModels(bool rebuild)
        {
            Directory.CreateDirectory(VmDir);
            Directory.CreateDirectory(VmMeshDir);
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (var gun in Guns)
                {
                    string path = $"{VmDir}/{gun.name}.prefab";
                    if (!rebuild && AssetDatabase.LoadAssetAtPath<GameObject>(path)) continue;
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(gun.prefab);
                    if (!source) throw new System.InvalidOperationException("Missing weapon: " + gun.prefab);
                    var root = new GameObject(gun.name);
                    SceneManager.MoveGameObjectToScene(root, preview);
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);
                    model.transform.SetParent(root.transform, false);
                    model.name = "Sung";
                    foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                    var b = WorldBounds(model);
                    var grip = new Vector3(b.center.x, b.center.y - b.size.y * gun.gripDrop, Mathf.Lerp(b.min.z, b.max.z, gun.grip));
                    var fore = new Vector3(b.center.x, b.center.y - b.size.y * gun.foreDrop, Mathf.Lerp(b.min.z, b.max.z, gun.fore)) - grip;
                    model.transform.position -= grip;
                    Anchor(root.transform, "GripR", Vector3.zero);
                    Anchor(root.transform, "GripL", fore);
                    Anchor(root.transform, "Muzzle", new Vector3(0, b.center.y - grip.y + b.size.y * .2f, b.max.z - grip.z));
                    AddAlliedPlayerArms(root, fore, preview);
                    UrpMaterialUtility.Apply(root);
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Object.DestroyImmediate(root);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            BuildBazookaViewModel(rebuild);
        }
        static void Anchor(Transform root, string name, Vector3 local)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.localPosition = local;
        }
        static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path = $"{VmMeshDir}/{name}.asset";
            mesh.name = name;
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old) { old.Clear(); EditorUtility.CopySerialized(mesh, old); EditorUtility.SetDirty(old); Object.DestroyImmediate(mesh); return old; }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
