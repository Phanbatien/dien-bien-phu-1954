using System.IO;
using System.Linq;
using DBP.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Gói model nhân vật v3 (nhánh feature/model-v3) cho người làm hoạt ảnh: mỗi loại nhân vật một prefab sạch
    /// (model + rig Humanoid + Animator + controller + súng / lưỡi lê, không có script AI) và một scene trưng bày.
    /// Chạy lại được: menu DBP → Model v3 → Xuất model nhân vật.
    public static class ModelV3Export
    {
        const string Dir = "Assets/_Project/Art/Characters/ModelV3";
        const string ScenePath = Dir + "/TrungBay_NhanVat.unity";

        [MenuItem("DBP/Model v3/Xuất model nhân vật")]
        public static void Export()
        {
            Directory.CreateDirectory(Dir);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/M1_HimLam.unity");
            var soldiers = Object.FindObjectsByType<SoldierAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var infantry = soldiers.First(s => !s.GetComponent<OfficerLeader>()).transform.Find("Model");
            var officer = soldiers.First(s => s.GetComponent<OfficerLeader>()).transform.Find("Model");
            var ally = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/BoDoi.prefab").transform.Find("Model");
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab").transform.Find("ThanNhanVat");

            Save(ally, "BoDoi", 1f);
            Save(infantry, "LinhPhap", 1f);
            Save(officer, "SiQuanPhap", 1.15f);
            Save(officer, "TrumPhap", 1.15f * 1.25f);
            Save(player, "NhanVatNguoiChoi", 1f);
            BuildShowcase();
            AssetDatabase.SaveAssets();
        }

        public static void ExportCI()
        {
            try { Export(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Save(Transform source, string name, float scale)
        {
            var copy = Object.Instantiate(source.gameObject);
            copy.name = name;
            copy.transform.position = Vector3.zero;
            copy.transform.rotation = Quaternion.identity;
            copy.transform.localScale = source.lossyScale * scale;
            // Chỉ giữ phần hình + Animator: bỏ script điều khiển (AI, driver), va chạm
            foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
            foreach (var c in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in copy.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            // Chân chạm mặt đất (y = 0) tại gốc prefab
            var rs = copy.GetComponentsInChildren<Renderer>(true);
            if (rs.Length > 0)
            {
                float minY = rs.Min(r => r.bounds.min.y);
                foreach (Transform child in copy.transform) child.position += Vector3.up * -minY;
            }
            var animator = copy.GetComponent<Animator>();
            if (animator) { animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
            PrefabUtility.SaveAsPrefabAsset(copy, $"{Dir}/{name}.prefab");
            Object.DestroyImmediate(copy);
        }

        static void BuildShowcase()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "San";
            ground.transform.localScale = new Vector3(3, 1, 2);
            ground.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Greybox/DatCoUa_60x60.mat");
            string[] names = { "BoDoi", "LinhPhap", "SiQuanPhap", "TrumPhap", "NhanVatNguoiChoi" };
            for (int i = 0; i < names.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{names[i]}.prefab");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.position = new Vector3((i - 2) * 2.2f, 0, 0);
                go.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            var cam = Camera.main;
            cam.transform.position = new Vector3(0, 1.3f, -6.2f);
            cam.transform.rotation = Quaternion.Euler(4, 0, 0);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
