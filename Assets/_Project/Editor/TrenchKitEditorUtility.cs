using System;
using System.Collections.Generic;
using System.IO;
using DBP.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DBP.EditorTools
{
    /// T04/T09: một ô hào 4 m và bộ prefab ghép khít (thẳng, góc, chữ T, bao cát, hầm chữ A, nối).
    /// Lòng hào rộng 2,2 m, vách 1,9 m — nhân vật (r = 0,35, cao 1,8) đi lọt.
    /// Dòng lệnh: -executeMethod DBP.EditorTools.TrenchKitEditorUtility.BuildCI
    public static class TrenchKitEditorUtility
    {
        public const float Module = 4f;
        public const float InnerHalf = 1.1f;
        public const float WallThick = 0.45f;
        public const float WallHeight = 1.9f;
        public const float Connector = 2f;

        const string Dir = "Assets/_Project/Prefabs/Trench";
        const string MatDir = "Assets/_Project/Art/Materials/Greybox";
        const string DevScene = "Assets/_Project/Scenes/Dev/T09_HaoThu.unity";
        const string ViewPath = "Assets/_Project/Prefabs/ViewModel_Arms.prefab";
        const string PlayerPath = "Assets/_Project/Prefabs/Player.prefab";
        const string MosinMeshPath = "Assets/_Project/Art/ViewModel/MosinLow.asset";
        const int RifleTriLimit = 3000;

        [MenuItem("DBP/Hào/Dựng bộ module")]
        public static void BuildMenu() => Build();

        public static void BuildCI() => Build();

        public static void Build()
        {
            Directory.CreateDirectory(Dir);
            var mud = Mat("Mud.mat");
            var earth = Mat("Trench.mat");
            var wood = Mat("Wood.mat");
            var sand = Mat("Sandbag.mat");

            Save("Hao_Thang", Straight(Module, earth, mud));
            Save("Hao_Noi", Straight(Connector, earth, mud));
            Save("Hao_Goc", Corner(earth, mud));
            Save("Hao_ChuT", Tee(earth, mud));
            Save("Hao_BaoCat", Sandbags(earth, mud, sand));
            Save("Hao_HamChuA", Shelter(earth, mud, wood));
            AssetDatabase.SaveAssets();
            Save("Hao_DoanThu_30m", Assemble30());

            ProveWalkable();
            WriteDevScene();
            AttachMotions();
            Debug.Log("[TrenchKit] 6 module + đoạn 30 m. Lòng 2,2 m, vách 1,9 m.");
        }

        static GameObject Straight(float length, Material earth, Material mud)
        {
            var root = new GameObject(length >= Module ? "Hao_Thang" : "Hao_Noi");
            Floor(root.transform, new Vector3(0f, -0.1f, length * 0.5f), new Vector3(InnerHalf * 2f, 0.2f, length + 0.04f), mud);
            Wall(root.transform, earth, -1f, length * 0.5f, length);
            Wall(root.transform, earth, 1f, length * 0.5f, length);
            return root;
        }

        static GameObject Corner(Material earth, Material mud)
        {
            var root = new GameObject("Hao_Goc");
            float outer = InnerHalf + WallThick;
            Floor(root.transform, new Vector3(0f, -0.1f, (Module + InnerHalf) * 0.5f), new Vector3(InnerHalf * 2f, 0.2f, Module + InnerHalf), mud);
            Floor(root.transform, new Vector3((InnerHalf + Module) * 0.5f, -0.1f, Module), new Vector3(Module - InnerHalf, 0.2f, InnerHalf * 2f), mud);
            Wall(root.transform, earth, -1f, (Module + outer) * 0.5f, Module + outer);
            Box(root.transform, "Vach_DongNam", new Vector3(InnerHalf + WallThick * 0.5f, WallHeight * 0.5f, (Module - InnerHalf) * 0.5f),
                new Vector3(WallThick, WallHeight, Module - InnerHalf), earth);
            Box(root.transform, "Vach_NamNhanh", new Vector3((InnerHalf + Module + WallThick * 0.5f) * 0.5f, WallHeight * 0.5f, Module - InnerHalf - WallThick * 0.5f),
                new Vector3(Module - InnerHalf + WallThick * 0.5f, WallHeight, WallThick), earth);
            Box(root.transform, "Vach_Bac", new Vector3((Module - outer) * 0.5f, WallHeight * 0.5f, Module + InnerHalf + WallThick * 0.5f),
                new Vector3(Module + outer, WallHeight, WallThick), earth);
            return root;
        }

        static GameObject Tee(Material earth, Material mud)
        {
            var root = new GameObject("Hao_ChuT");
            float branch = Module - InnerHalf;
            Floor(root.transform, new Vector3(0f, -0.1f, Module * 0.5f), new Vector3(InnerHalf * 2f, 0.2f, Module + 0.04f), mud);
            Floor(root.transform, new Vector3((InnerHalf + Module) * 0.5f, -0.1f, Module * 0.5f), new Vector3(branch, 0.2f, InnerHalf * 2f), mud);
            Wall(root.transform, earth, -1f, Module * 0.5f, Module);
            float stub = (Module * 0.5f - InnerHalf) * 0.5f;
            Box(root.transform, "Vach_DongTruoc", new Vector3(InnerHalf + WallThick * 0.5f, WallHeight * 0.5f, stub),
                new Vector3(WallThick, WallHeight, Module * 0.5f - InnerHalf), earth);
            Box(root.transform, "Vach_DongSau", new Vector3(InnerHalf + WallThick * 0.5f, WallHeight * 0.5f, Module - stub),
                new Vector3(WallThick, WallHeight, Module * 0.5f - InnerHalf), earth);
            Box(root.transform, "Vach_NamNhanh", new Vector3(InnerHalf + branch * 0.5f, WallHeight * 0.5f, Module * 0.5f - InnerHalf - WallThick * 0.5f),
                new Vector3(branch, WallHeight, WallThick), earth);
            Box(root.transform, "Vach_BacNhanh", new Vector3(InnerHalf + branch * 0.5f, WallHeight * 0.5f, Module * 0.5f + InnerHalf + WallThick * 0.5f),
                new Vector3(branch, WallHeight, WallThick), earth);
            return root;
        }

        static GameObject Sandbags(Material earth, Material mud, Material sand)
        {
            var root = Straight(Module, earth, mud);
            root.name = "Hao_BaoCat";
            for (int i = 0; i < 3; i++)
            {
                Box(root.transform, $"BaoCat_{i}", new Vector3(InnerHalf + WallThick * 0.5f, WallHeight + 0.14f, 0.7f + i * 1.2f),
                    new Vector3(0.5f, 0.28f, 0.9f), sand);
            }
            return root;
        }

        static GameObject Shelter(Material earth, Material mud, Material wood)
        {
            var root = Straight(Module, earth, mud);
            root.name = "Hao_HamChuA";
            Floor(root.transform, new Vector3(-InnerHalf - 0.8f, -0.1f, Module * 0.5f), new Vector3(1.6f, 0.2f, 2.2f), mud);
            var left = Box(root.transform, "Mai_Trai", new Vector3(-1.85f, 1.15f, Module * 0.5f), new Vector3(1.3f, 0.08f, 2f), wood);
            left.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
            var right = Box(root.transform, "Mai_Phai", new Vector3(-2.35f, 1.15f, Module * 0.5f), new Vector3(1.3f, 0.08f, 2f), wood);
            right.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
            return root;
        }

        public static void BuildAllCI()
        {
            Build();
            Mission1DressingEditorUtility.Apply();
        }

        static GameObject Assemble30()
        {
            var root = new GameObject("Hao_DoanThu_30m");
            float z = 0f;
            for (int i = 0; i < 7; i++)
            {
                var piece = PrefabUtility.InstantiatePrefab(Load("Hao_Thang")) as GameObject;
                piece.transform.SetParent(root.transform, false);
                piece.transform.localPosition = new Vector3(0f, 0f, z);
                z += Module;
            }
            var join = PrefabUtility.InstantiatePrefab(Load("Hao_Noi")) as GameObject;
            join.transform.SetParent(root.transform, false);
            join.transform.localPosition = new Vector3(0f, 0f, z);
            return root;
        }

        static void ProveWalkable()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                Sweep(Load("Hao_Thang"), new Vector3(0f, 0f, 0.4f), new Vector3(0f, 0f, Module - 0.4f), "thẳng");
                Sweep(Load("Hao_Noi"), new Vector3(0f, 0f, 0.3f), new Vector3(0f, 0f, Connector - 0.3f), "nối");
                Sweep(Load("Hao_Goc"), new Vector3(0f, 0f, 0.4f), new Vector3(0f, 0f, Module - 0.3f), "góc dọc");
                Sweep(Load("Hao_Goc"), new Vector3(0.4f, 0f, Module), new Vector3(Module - 0.4f, 0f, Module), "góc ngang");
                Sweep(Load("Hao_ChuT"), new Vector3(0f, 0f, 0.4f), new Vector3(0f, 0f, Module - 0.4f), "chữ T dọc");
                Sweep(Load("Hao_ChuT"), new Vector3(0.4f, 0f, Module * 0.5f), new Vector3(Module - 0.4f, 0f, Module * 0.5f), "chữ T ngang");
                Sweep(Load("Hao_BaoCat"), new Vector3(0f, 0f, 0.4f), new Vector3(0f, 0f, Module - 0.4f), "bao cát");
                Sweep(Load("Hao_HamChuA"), new Vector3(0f, 0f, 0.4f), new Vector3(0f, 0f, Module - 0.4f), "hầm chữ A");
                Sweep(Load("Hao_DoanThu_30m"), new Vector3(0f, 0f, 0.4f), new Vector3(0f, 0f, 29.4f), "đoạn 30 m");
            }
            finally
            {
                foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (go && go.transform.parent == null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void Sweep(GameObject prefab, Vector3 from, Vector3 to, string label)
        {
            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (!go) throw new InvalidOperationException($"Không tạo được prefab cho '{label}'.");
            var lift = new Vector3(0f, 200f, 0f);
            go.transform.position = lift;
            Physics.SyncTransforms();
            int steps = Mathf.Max(1, Mathf.CeilToInt((to - from).magnitude / 0.4f));
            for (int i = 0; i <= steps; i++)
            {
                var p = Vector3.Lerp(from, to, i / (float)steps) + lift;
                var hits = Physics.OverlapCapsule(p + new Vector3(0f, 0.55f, 0f), p + new Vector3(0f, 1.4f, 0f), 0.35f, ~0, QueryTriggerInteraction.Ignore);
                if (hits.Length > 0)
                {
                    string hitName = hits[0].name;
                    UnityEngine.Object.DestroyImmediate(go);
                    throw new InvalidOperationException($"Hào {label} kẹt tại {hitName} ({p}).");
                }
            }
            UnityEngine.Object.DestroyImmediate(go);
        }

        static void WriteDevScene()
        {
            Directory.CreateDirectory("Assets/_Project/Scenes/Dev");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var run = PrefabUtility.InstantiatePrefab(Load("Hao_DoanThu_30m")) as GameObject;
            run.transform.position = Vector3.zero;
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            if (player)
            {
                var body = PrefabUtility.InstantiatePrefab(player) as GameObject;
                body.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, 1f), Quaternion.identity);
            }
            EditorSceneManager.SaveScene(scene, DevScene);
        }

        static void AttachMotions()
        {
            var root = PrefabUtility.LoadPrefabContents(ViewPath);
            try
            {
                if (!root.GetComponent<ViewModelMotions>())
                    root.AddComponent<ViewModelMotions>();
                var arms = root.GetComponent<FirstPersonArms>();
                if (arms && arms.rifle) ReplaceRifleMesh(arms.rifle);
                int tris = 0;
                if (arms && arms.rifle)
                {
                    foreach (var filter in arms.rifle.GetComponentsInChildren<MeshFilter>(true))
                        if (filter.sharedMesh) tris += filter.sharedMesh.triangles.Length / 3;
                    foreach (var skin in arms.rifle.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        if (skin.sharedMesh) tris += skin.sharedMesh.triangles.Length / 3;
                }
                if (tris == 0 || tris > RifleTriLimit)
                    throw new InvalidOperationException($"Mosin view model {tris} tam giác (cần 1..{RifleTriLimit}).");
                PrefabUtility.SaveAsPrefabAsset(root, ViewPath);
                Debug.Log($"[T16] View model {tris} tam giác, đã gắn hoạt ảnh bắn/nạp/đi.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var player = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var arms = player.GetComponentInChildren<FirstPersonArms>(true);
                if (arms && !arms.GetComponent<ViewModelMotions>())
                    arms.gameObject.AddComponent<ViewModelMotions>();
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        static void ReplaceRifleMesh(Transform rifle)
        {
            for (int i = rifle.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(rifle.GetChild(i).gameObject);

            var mesh = BuildMosinMesh();
            var previous = AssetDatabase.LoadAssetAtPath<Mesh>(MosinMeshPath);
            if (previous) AssetDatabase.DeleteAsset(MosinMeshPath);
            AssetDatabase.CreateAsset(mesh, MosinMeshPath);

            var go = new GameObject("Mosin_Low");
            go.transform.SetParent(rifle, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat("Metal.mat");
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Mesh BuildMosinMesh()
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var src = temp.GetComponent<MeshFilter>().sharedMesh;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uv = new List<Vector2>();
            void Add(Vector3 center, Vector3 size)
            {
                int offset = verts.Count;
                var matrix = Matrix4x4.TRS(center, Quaternion.identity, size);
                var sourceVerts = src.vertices;
                var sourceUv = src.uv;
                for (int i = 0; i < sourceVerts.Length; i++)
                {
                    verts.Add(matrix.MultiplyPoint3x4(sourceVerts[i]));
                    uv.Add(sourceUv != null && sourceUv.Length > i ? sourceUv[i] : Vector2.zero);
                }
                foreach (var index in src.triangles)
                    tris.Add(index + offset);
            }

            Add(new Vector3(0f, -0.01f, -0.08f), new Vector3(0.045f, 0.07f, 0.26f));
            Add(new Vector3(0f, 0.015f, 0.12f), new Vector3(0.04f, 0.05f, 0.14f));
            Add(new Vector3(0f, 0.03f, 0.42f), new Vector3(0.014f, 0.014f, 0.5f));
            Add(new Vector3(0f, -0.025f, 0.1f), new Vector3(0.025f, 0.05f, 0.07f));
            Add(new Vector3(0.03f, 0.04f, 0.12f), new Vector3(0.04f, 0.012f, 0.012f));

            var mesh = new Mesh { name = "MosinLow" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            UnityEngine.Object.DestroyImmediate(temp);
            return mesh;
        }

        static void Wall(Transform parent, Material mat, float sign, float z, float length)
        {
            float x = sign * (InnerHalf + WallThick * 0.5f);
            Box(parent, sign < 0f ? "Vach_Trai" : "Vach_Phai", new Vector3(x, WallHeight * 0.5f, z),
                new Vector3(WallThick, WallHeight, length), mat);
        }

        static void Floor(Transform parent, Vector3 center, Vector3 size, Material mat) =>
            Box(parent, "San", center, size, mat);

        static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        static Material Mat(string file)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{file}");
            if (!mat) throw new InvalidOperationException($"Thiếu material {file}.");
            return mat;
        }

        static void Save(string name, GameObject root)
        {
            root.name = name;
            PrefabUtility.SaveAsPrefabAsset(root, $"{Dir}/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        static GameObject Load(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{name}.prefab");
    }
}
