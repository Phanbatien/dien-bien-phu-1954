using System;
using System.Linq;
using DBP.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DBP.EditorTools
{
    /// Hầm chữ A (T09, T25): nơi họp tác chiến và mua nâng cấp trước giờ xuất trận. Mái chữ A lợp gỗ,
    /// vách đất, bàn bản đồ, điện đài, giá súng, đèn dầu chập chờn. Hiện sau màn hình chuẩn bị (BunkerBackdrop).
    /// Menu DBP → Dựng hầm chữ A. Dòng lệnh: -executeMethod DBP.EditorTools.ProjectWiring.BuildBunkerCI
    public static partial class ProjectWiring
    {
        public const string BunkerScenePath = "Assets/_Project/Scenes/Bunker_HamChuA.unity";

        public static void BuildBunkerCI()
        {
            try { BuildBunker(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        [MenuItem("DBP/Dựng hầm chữ A")]
        public static void BuildBunker()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureSharedAssets();
            var earth = LoadOrMakeMat("HamDat", new Color(0.3f, 0.23f, 0.16f));
            var log = LoadOrMakeMat("HamGo", new Color(0.36f, 0.26f, 0.16f));
            var cloth = LoadOrMakeMat("HamVaiBat", new Color(0.28f, 0.3f, 0.2f));
            var brass = LoadOrMakeMat("HamDongThau", new Color(0.62f, 0.5f, 0.25f));
            var root = new GameObject("HamChuA").transform;

            // Sàn và hai mái nghiêng lợp gỗ tròn (lòng hầm rộng 4 m, sâu 7 m, đỉnh 2,6 m)
            Block(root, "San", new Vector3(0, -0.05f, 0), new Vector3(5f, 0.1f, 8f), earth);
            Block(root, "VachSau", new Vector3(0, 1.3f, 3.6f), new Vector3(5f, 2.8f, 0.3f), earth);
            for (int side = -1; side <= 1; side += 2)
            {
                var roof = new GameObject(side < 0 ? "MaiTrai" : "MaiPhai").transform;
                roof.SetParent(root, false);
                roof.localPosition = new Vector3(side * 1.05f, 1.3f, 0);
                roof.localRotation = Quaternion.Euler(0, 0, side * 38f);
                for (int i = 0; i < 18; i++)
                {
                    var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Object.DestroyImmediate(beam.GetComponent<Collider>());
                    beam.name = "Go" + i;
                    beam.transform.SetParent(roof, false);
                    beam.transform.localPosition = new Vector3(0, 0, -3.6f + i * 0.42f);
                    beam.transform.localRotation = Quaternion.identity;
                    beam.transform.localScale = new Vector3(0.2f, 1.75f, 0.2f);
                    beam.GetComponent<Renderer>().sharedMaterial = log;
                }
                Block(roof, "DatPhu", new Vector3(side * 0.25f, 0, 0), new Vector3(0.3f, 3.6f, 7.6f), earth);
            }
            Block(root, "DaNoc", new Vector3(0, 2.62f, 0), new Vector3(0.28f, 0.28f, 7.6f), log);

            // Bàn bản đồ tác chiến (bản đồ của Khôi) và bộ chỉ huy
            var table = Prop(root, "Assets/Defensive_props/Prefabs/tables/wooden_table.prefab", new Vector3(0, 0, 1.2f), 0);
            float tableTop = table ? WorldBounds(table).max.y : 0.8f;
            if (!table) Block(root, "Ban", new Vector3(0, 0.4f, 1.2f), new Vector3(1.6f, 0.8f, 1f), log);
            var map = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(map.GetComponent<Collider>());
            map.name = "BanDoTacChien";
            map.transform.SetParent(root, false);
            map.transform.localPosition = new Vector3(0, tableTop + 0.01f, 1.2f);
            map.transform.localRotation = Quaternion.Euler(90, 8, 0);
            map.transform.localScale = new Vector3(1.1f, 0.8f, 1f);
            var mapMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Props/M_BanDoTacChien.mat");
            map.GetComponent<Renderer>().sharedMaterial = mapMat ? mapMat : cloth;
            Block(root, "ThuocDo", new Vector3(0.3f, tableTop + 0.02f, 1.05f), new Vector3(0.35f, 0.01f, 0.04f), brass);
            Block(root, "SoTay", new Vector3(-0.45f, tableTop + 0.03f, 1.0f), new Vector3(0.2f, 0.04f, 0.28f), cloth);

            // Điện đài, hòm đạn, giá súng, bao cát cửa hầm
            Prop(root, "Assets/Defensive_props/Prefabs/electronics/electronics1.prefab", new Vector3(1.45f, 0, 2.6f), 200);
            Prop(root, "Assets/Defensive_props/Prefabs/boxes/box2.prefab", new Vector3(-1.5f, 0, 2.7f), 15);
            Prop(root, "Assets/Defensive_props/Prefabs/boxes/box4.prefab", new Vector3(-1.45f, 0, 1.9f), -10);
            Prop(root, "Assets/Defensive_props/Prefabs/defence/ammo_box.prefab", new Vector3(1.4f, 0, 0.2f), 80);
            Prop(root, "Assets/Defensive_props/Prefabs/fortifications/sandbag barricade.prefab", new Vector3(0, 0, -3.3f), 0);
            var rifle = AssetDatabase.LoadAssetAtPath<GameObject>(RiflePrefabPath);
            Block(root, "GiaSung", new Vector3(-1.55f, 0.55f, 0.2f), new Vector3(0.12f, 0.08f, 1.4f), log);
            for (int i = 0; rifle && i < 3; i++)
            {
                var gun = (GameObject)PrefabUtility.InstantiatePrefab(rifle, root);
                foreach (var c in gun.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                var size = WorldBounds(gun).size;
                var axis = size.x >= size.y && size.x >= size.z ? Vector3.right : size.z >= size.y ? Vector3.forward : Vector3.up;
                gun.transform.rotation = Quaternion.Euler(0, 0, -12f) * Quaternion.FromToRotation(axis, Vector3.up);
                var b = WorldBounds(gun);
                gun.transform.position += new Vector3(-1.6f - b.center.x, -b.min.y, -0.3f + i * 0.45f - b.center.z);
            }
            var helmet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(helmet.GetComponent<Collider>());
            helmet.name = "MuSat";
            helmet.transform.SetParent(root, false);
            helmet.transform.localPosition = new Vector3(-0.55f, tableTop + 0.07f, 1.5f);
            helmet.transform.localScale = new Vector3(0.3f, 0.16f, 0.33f);
            helmet.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/MuSat.mat") ?? cloth;

            // Đèn dầu treo trên bàn: nguồn sáng ấm duy nhất, đổ bóng
            var lampBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(lampBody.GetComponent<Collider>());
            lampBody.name = "DenDau";
            lampBody.transform.SetParent(root, false);
            lampBody.transform.localPosition = new Vector3(0.1f, 1.95f, 1.2f);
            lampBody.transform.localScale = new Vector3(0.14f, 0.1f, 0.14f);
            lampBody.GetComponent<Renderer>().sharedMaterial = brass;
            var lamp = new GameObject("LampLight").AddComponent<Light>();
            lamp.transform.SetParent(root, false);
            lamp.transform.localPosition = new Vector3(0.1f, 1.8f, 1.2f);
            lamp.type = LightType.Point; lamp.range = 7f; lamp.color = new Color(1f, 0.7f, 0.38f);
            lamp.shadows = LightShadows.Soft; lamp.intensity = 2.2f;
            lamp.gameObject.AddComponent<LampFlicker>();
            // Ánh trời xám lọt qua cửa hầm
            var door = new GameObject("CuaHamLight").AddComponent<Light>();
            door.transform.SetParent(root, false);
            door.transform.localPosition = new Vector3(0, 1.6f, -3.4f);
            door.type = LightType.Spot; door.spotAngle = 70f; door.range = 6f; door.intensity = 0.8f;
            door.color = new Color(0.7f, 0.75f, 0.8f); door.transform.localRotation = Quaternion.Euler(20, 0, 0);

            // Camera: đứng ở cửa hầm nhìn vào bàn bản đồ; vẽ sau camera menu, giao diện phủ lên trên
            var cam = new GameObject("CameraPoint").AddComponent<Camera>();
            cam.transform.SetParent(root, false);
            cam.transform.localPosition = new Vector3(0.35f, 1.55f, -2.4f);
            cam.transform.localRotation = Quaternion.Euler(14f, -4f, 0);
            cam.fieldOfView = 58f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 30f; cam.depth = 5;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            new GameObject("RadioPoint").transform.SetParent(root, false);
            root.Find("RadioPoint").localPosition = new Vector3(1.45f, 0.9f, 2.6f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.1f, 0.09f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.06f; RenderSettings.fogColor = new Color(0.08f, 0.07f, 0.06f);
            RenderSettings.skybox = null;
            UrpMaterialUtility.Apply(root.gameObject);

            EditorSceneManager.SaveScene(scene, BunkerScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == BunkerScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(BunkerScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            Debug.Log("[Wiring] Hầm chữ A: đã dựng " + BunkerScenePath);
        }

        static GameObject Prop(Transform root, string path, Vector3 pos, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var b = WorldBounds(go);
            go.transform.position += Vector3.up * (pos.y - b.min.y);
            return go;
        }
    }
}
