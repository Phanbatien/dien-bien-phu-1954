using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DBP.EditorTools
{
    /// Tay + súng góc nhìn thứ nhất: súng thật từ gói Low Poly Weapon, hai bàn tay ôm súng dựng bằng khối
    /// (lòng bàn tay, ngón cuộn quanh báng, ngón trỏ trên cò, ngón cái), ống tay áo kaki bộ đội có cổ tay áo.
    /// Lưu ở Resources/ViewModels/VM_*.prefab; FirstPersonArms nạp theo loại súng đang cầm.
    /// Gốc prefab = điểm tay phải cầm báng, nòng theo +Z.
    public static partial class ProjectWiring
    {
        const string VmDir = "Assets/_Project/Resources/ViewModels";
        const string VmMeshDir = "Assets/_Project/Art/ViewModel";
        const string WeaponPack = "Assets/Low Poly Weapon Series V 4/Prefabs/Low Poly Series V 4 New/Weapons/";

        struct GunSpec { public string name, prefab; public float grip, fore, gripDrop, foreDrop; }

        // Vị trí tay tính theo tỉ lệ chiều dài súng từ đuôi báng (0) tới đầu nòng (1); độ hạ theo tỉ lệ chiều cao
        static readonly GunSpec[] Guns =
        {
            new GunSpec { name = "VM_Rifle", prefab = WeaponPack + "WWII_Rifle_B.prefab", grip = 0.30f, fore = 0.56f, gripDrop = 0.10f, foreDrop = 0.05f },
            new GunSpec { name = "VM_Scoped", prefab = WeaponPack + "WWII_Recon_B.prefab", grip = 0.27f, fore = 0.55f, gripDrop = 0.12f, foreDrop = 0.06f },
            new GunSpec { name = "VM_Smg", prefab = WeaponPack + "WWII_SMG_B.prefab", grip = 0.42f, fore = 0.70f, gripDrop = 0.30f, foreDrop = 0.02f },
        };

        public static void BuildViewModelsCI()
        {
            try { BuildViewModels(true); AssetDatabase.SaveAssets(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        [MenuItem("DBP/Dựng tay + súng góc nhìn thứ nhất")]
        public static void BuildViewModelsMenu() => BuildViewModels(true);

        static void EnsureViewModels() => BuildViewModels(false);

        static void BuildViewModels(bool rebuild)
        {
            Directory.CreateDirectory(VmDir);
            Directory.CreateDirectory(VmMeshDir);
            var skin = LoadOrMakeMat("VmDa", new Color(0.78f, 0.58f, 0.44f));
            var sleeve = LoadOrMakeMat("VmAoKaki", new Color(0.42f, 0.44f, 0.30f));
            var cuff = LoadOrMakeMat("VmCoTay", new Color(0.30f, 0.32f, 0.21f));
            var handR = SaveMesh("TayPhai", RightHand());
            var handL = SaveMesh("TayTrai", LeftHand());
            var tube = SaveMesh("OngTay", Tube(0.044f, 0.062f, 8));
            var ring = SaveMesh("CoTay", Tube(0.05f, 0.05f, 8));

            foreach (var gun in Guns)
            {
                string path = $"{VmDir}/{gun.name}.prefab";
                if (!rebuild && AssetDatabase.LoadAssetAtPath<GameObject>(path)) continue;
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(gun.prefab);
                if (!source) { Debug.LogError("[ViewModel] thiếu " + gun.prefab); continue; }
                var root = new GameObject(gun.name);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
                model.name = "Sung";
                foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                var b = WorldBounds(model);
                float Z(float t) => Mathf.Lerp(b.min.z, b.max.z, t);
                var grip = new Vector3(b.center.x, b.center.y - b.size.y * gun.gripDrop, Z(gun.grip));
                var fore = new Vector3(b.center.x, b.center.y - b.size.y * gun.foreDrop, Z(gun.fore));
                model.transform.position -= grip; // gốc prefab = tay phải
                fore -= grip;
                Anchor(root.transform, "GripR", Vector3.zero);
                Anchor(root.transform, "GripL", fore);
                Anchor(root.transform, "Muzzle", new Vector3(0, b.center.y - grip.y + b.size.y * 0.2f, b.max.z - grip.z));

                // Tay phải: ôm báng, cẳng tay chạy về góc dưới phải màn hình
                Limb(root.transform, "TayPhai", Vector3.zero, new Vector3(0.20f, -0.30f, -0.32f), handR, tube, ring, skin, sleeve, cuff, new Vector3(0.03f, -0.045f, -0.05f));
                // Tay trái: đỡ ốp lót, cẳng tay về góc dưới trái
                Limb(root.transform, "TayTrai", fore, fore + new Vector3(-0.22f, -0.30f, -0.22f), handL, tube, ring, skin, sleeve, cuff, new Vector3(0f, -0.055f, -0.03f));

                UrpMaterialUtility.Apply(root);
                foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
            }
        }

        static void Anchor(Transform root, string name, Vector3 local)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.localPosition = local;
        }

        static void Limb(Transform root, string name, Vector3 hand, Vector3 elbow, Mesh handMesh, Mesh tube, Mesh ring,
            Material skin, Material sleeve, Material cuff, Vector3 wristOffset)
        {
            var limb = new GameObject(name).transform;
            limb.SetParent(root, false);
            Part(limb, "BanTay", handMesh, skin, hand, Quaternion.identity, Vector3.one * 1.2f); // to hơn thật một chút cho dễ nhìn
            var wrist = hand + wristOffset * 1.2f;
            var dir = elbow - wrist;
            var rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            Part(limb, "OngTay", tube, sleeve, wrist, rot, new Vector3(1, 1, dir.magnitude));
            Part(limb, "CoTayAo", ring, cuff, wrist + dir.normalized * 0.005f, rot, new Vector3(1, 1, 0.05f));
        }

        static void Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path = $"{VmMeshDir}/{name}.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old) { old.Clear(); EditorUtility.CopySerialized(mesh, old); EditorUtility.SetDirty(old); return old; }
            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ───────── Khối dựng tay (mặt phẳng, đúng phong cách low-poly) ─────────

        sealed class MeshBuilder
        {
            readonly List<Vector3> v = new List<Vector3>();
            readonly List<int> t = new List<int>();
            static Mesh cube;

            /// Hộp vát góc nhẹ: đủ mềm để đọc là ngón tay, vẫn phẳng mặt.
            public MeshBuilder Box(Vector3 center, Vector3 size, Vector3 euler)
            {
                if (!cube) { var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube); cube = tmp.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(tmp); }
                var m = Matrix4x4.TRS(center, Quaternion.Euler(euler), size);
                int o = v.Count;
                foreach (var p in cube.vertices) v.Add(m.MultiplyPoint3x4(p));
                foreach (var i in cube.triangles) t.Add(o + i);
                return this;
            }

            public Mesh Build()
            {
                var mesh = new Mesh();
                mesh.SetVertices(v); mesh.SetTriangles(t, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                return mesh;
            }
        }

        /// Tay phải cầm báng: gốc là tâm báng, +Z hướng nòng, +X sang phải.
        static Mesh RightHand()
        {
            var m = new MeshBuilder();
            m.Box(new Vector3(0.036f, -0.012f, -0.012f), new Vector3(0.026f, 0.082f, 0.078f), new Vector3(0, 0, -6)); // lòng bàn tay phía phải báng
            // Bốn ngón cuộn qua trước báng sang trái (mỗi ngón 2 đốt)
            for (int i = 0; i < 4; i++)
            {
                float y = 0.012f - i * 0.019f - (i == 3 ? 0.002f : 0f);
                float w = i == 3 ? 0.014f : 0.017f;
                if (i > 0) m.Box(new Vector3(0.012f, y, 0.032f), new Vector3(0.05f, w, 0.022f), new Vector3(0, -12, 0));
                if (i > 0) m.Box(new Vector3(-0.024f, y, 0.016f), new Vector3(0.022f, w, 0.03f), new Vector3(0, 35, 0));
            }
            m.Box(new Vector3(0.01f, 0.03f, 0.055f), new Vector3(0.017f, 0.017f, 0.055f), new Vector3(-8, -10, 0)); // ngón trỏ trên cò
            m.Box(new Vector3(-0.012f, 0.042f, -0.006f), new Vector3(0.02f, 0.02f, 0.06f), new Vector3(0, 20, 0));  // ngón cái vắt qua báng
            m.Box(new Vector3(0.032f, -0.04f, -0.05f), new Vector3(0.05f, 0.048f, 0.05f), new Vector3(0, 0, 0));   // cổ tay
            return m.Build();
        }

        /// Tay trái đỡ ốp lót: lòng bàn tay ngửa dưới ốp, ngón cuộn lên mặt trái, ngón cái mặt phải.
        static Mesh LeftHand()
        {
            var m = new MeshBuilder();
            m.Box(new Vector3(0, -0.04f, 0), new Vector3(0.078f, 0.024f, 0.085f), Vector3.zero);
            for (int i = 0; i < 4; i++)
            {
                float z = 0.03f - i * 0.02f;
                m.Box(new Vector3(-0.044f, -0.022f, z), new Vector3(0.02f, 0.04f, 0.017f), new Vector3(0, 0, -12));
                m.Box(new Vector3(-0.04f, 0.008f, z), new Vector3(0.018f, 0.028f, 0.016f), new Vector3(0, 0, 25));
            }
            m.Box(new Vector3(0.042f, -0.016f, 0.012f), new Vector3(0.02f, 0.046f, 0.05f), new Vector3(0, 0, 14)); // ngón cái
            m.Box(new Vector3(0, -0.055f, -0.035f), new Vector3(0.05f, 0.046f, 0.05f), Vector3.zero);             // cổ tay
            return m.Build();
        }

        /// Ống tròn 8 cạnh dọc +Z dài 1 (co giãn theo Z để đặt chiều dài), mặt phẳng.
        static Mesh Tube(float r0, float r1, int sides)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)), p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1));
                int o = v.Count;
                v.Add(p0 * r0); v.Add(p1 * r0); v.Add(p1 * r1 + Vector3.forward); v.Add(p0 * r1 + Vector3.forward);
                t.AddRange(new[] { o, o + 2, o + 1, o, o + 3, o + 2 });
            }
            var mesh = new Mesh(); mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
