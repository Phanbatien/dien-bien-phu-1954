using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DBP.EditorTools
{
    /// Cây cối và hào dựng bằng khối, đúng phong cách low-poly (hướng 1):
    /// bụi tre cháy sém, cây chuối lá rách, ván lót sàn hào và cọc gỗ chống vách.
    /// Gọi từ bước "Không khí bi tráng"; seed cố định theo màn.
    public static partial class ProjectWiring
    {
        const string FloraDir = "Assets/_Project/Art/Meshes/Flora";

        /// Gộp nhiều khối (hộp, trụ) thành một mesh nhiều submesh (mỗi submesh một vật liệu).
        sealed class PartsMesh
        {
            readonly List<Vector3> v = new List<Vector3>();
            readonly List<int>[] subs;
            static Mesh cube, cylinder;

            public PartsMesh(int submeshes) { subs = new List<int>[submeshes]; for (int i = 0; i < submeshes; i++) subs[i] = new List<int>(); }

            static Mesh Prim(PrimitiveType type)
            {
                var tmp = GameObject.CreatePrimitive(type);
                var m = tmp.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(tmp);
                return m;
            }

            void Add(Mesh src, Matrix4x4 m, int sub)
            {
                int o = v.Count;
                foreach (var p in src.vertices) v.Add(m.MultiplyPoint3x4(p));
                foreach (var i in src.triangles) subs[sub].Add(o + i);
            }

            static Mesh sphere;

            /// Viên nang từ a tới b (ngón tay, cổ tay): đầu tròn, không góc cạnh.
            public void Capsule(Vector3 a, Vector3 b, float r, int sub)
            {
                // Stretch only the shaft: stretching a whole capsule also elongates
                // its end caps, leaving swollen beads at every finger joint.
                Rod(a, b, r, sub);
                Ellipsoid(a, Vector3.one * (r * 2f), Quaternion.identity, sub);
                Ellipsoid(b, Vector3.one * (r * 2f), Quaternion.identity, sub);
            }

            /// Chuỗi viên nang nối các khớp (ngón tay nhiều đốt).
            public void Chain(float r, int sub, params Vector3[] joints)
            {
                for (int i = 0; i + 1 < joints.Length; i++) Capsule(joints[i], joints[i + 1], r * (1f - i * 0.08f), sub);
            }

            public void Ellipsoid(Vector3 c, Vector3 size, Quaternion rot, int sub) { if (!sphere) sphere = Prim(PrimitiveType.Sphere); Add(sphere, Matrix4x4.TRS(c, rot, size), sub); }

            public void Box(Vector3 c, Vector3 size, Quaternion rot, int sub) { if (!cube) cube = Prim(PrimitiveType.Cube); Add(cube, Matrix4x4.TRS(c, rot, size), sub); }

            /// Trụ từ a tới b, bán kính r.
            public void Rod(Vector3 a, Vector3 b, float r, int sub)
            {
                if (!cylinder) cylinder = Prim(PrimitiveType.Cylinder);
                var d = b - a;
                Add(cylinder, Matrix4x4.TRS((a + b) * 0.5f, Quaternion.FromToRotation(Vector3.up, d.normalized), new Vector3(r * 2, d.magnitude * 0.5f, r * 2)), sub);
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(v);
                mesh.subMeshCount = subs.Length;
                for (int i = 0; i < subs.Length; i++) mesh.SetTriangles(subs[i], i);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                return mesh;
            }
        }

        static Mesh SaveFlora(string name, Mesh mesh)
        {
            Directory.CreateDirectory(FloraDir);
            string path = $"{FloraDir}/{name}.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ───────── Tre cháy ─────────

        static Mesh BambooClump(int seed)
        {
            var rng = new System.Random(seed);
            float R(float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());
            var m = new PartsMesh(2); // 0 thân tre cháy, 1 lá khô
            int stalks = rng.Next(7, 12);
            for (int s = 0; s < stalks; s++)
            {
                var basePos = new Vector3(R(-0.6f, 0.6f), 0, R(-0.6f, 0.6f));
                var lean = new Vector3(basePos.x, 0, basePos.z).normalized * R(0.05f, 0.18f) + new Vector3(R(-0.05f, 0.05f), 1, R(-0.05f, 0.05f));
                lean.Normalize();
                bool broken = rng.NextDouble() < 0.4;
                int segs = broken ? rng.Next(1, 3) : rng.Next(4, 7);
                float r = R(0.035f, 0.055f);
                var p = basePos;
                for (int i = 0; i < segs; i++)
                {
                    var q = p + lean * R(0.9f, 1.25f);
                    m.Rod(p, q, r, 0);
                    m.Rod(q - lean * 0.03f, q + lean * 0.03f, r * 1.25f, 0); // đốt tre
                    if (!broken && i >= segs - 2 && rng.NextDouble() < 0.6)
                        m.Box(q + new Vector3(R(-0.2f, 0.2f), -0.1f, R(-0.2f, 0.2f)), new Vector3(0.05f, 0.01f, 0.35f), Quaternion.Euler(R(20, 60), R(0, 360), 0), 1);
                    p = q;
                }
                if (broken) // ngọn gãy gập
                    m.Rod(p, p + new Vector3(R(-0.8f, 0.8f), -0.3f, R(-0.8f, 0.8f)), r * 0.9f, 0);
            }
            return m.Build("TreChay_" + seed);
        }

        // ───────── Cây chuối ─────────

        static Mesh Banana(int seed)
        {
            var rng = new System.Random(seed);
            float R(float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());
            var m = new PartsMesh(3); // 0 thân, 1 lá xanh, 2 lá khô cháy
            float h = R(1.6f, 2.4f);
            m.Rod(Vector3.zero, new Vector3(0, h * 0.5f, 0), 0.13f, 0);
            m.Rod(new Vector3(0, h * 0.5f, 0), new Vector3(0.03f, h, 0), 0.1f, 0);
            int leaves = rng.Next(6, 9);
            for (int i = 0; i < leaves; i++)
            {
                float yaw = i * 360f / leaves + R(-15, 15);
                bool dry = rng.NextDouble() < 0.45;
                float len = R(1.2f, 1.8f), droop = dry ? R(55, 85) : R(15, 40);
                var rot = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(-90 + droop, 0, 0);
                var top = new Vector3(0, h, 0);
                m.Rod(top, top + rot * Vector3.up * len, 0.015f, 0);                                          // gân lá
                m.Box(top + rot * Vector3.up * (len * 0.55f), new Vector3(dry ? 0.22f : 0.4f, len * 0.9f, 0.015f), rot, dry ? 2 : 1); // phiến lá
            }
            return m.Build("Chuoi_" + seed);
        }

        static void PlaceFlora(Transform root, System.Random rng, List<Vector3> keep, Rect area)
        {
            var stalk = LoadOrMakeMat("TreChay", new Color(0.28f, 0.24f, 0.16f));
            var dryLeaf = LoadOrMakeMat("LaKho", new Color(0.55f, 0.47f, 0.27f));
            var trunk = LoadOrMakeMat("ThanChuoi", new Color(0.36f, 0.4f, 0.24f));
            var leaf = LoadOrMakeMat("LaChuoi", new Color(0.32f, 0.45f, 0.2f));
            var bamboo = Enumerable.Range(0, 3).Select(i => SaveFlora("TreChay_" + i, BambooClump(100 + i))).ToArray();
            var banana = Enumerable.Range(0, 3).Select(i => SaveFlora("Chuoi_" + i, Banana(200 + i))).ToArray();
            var flora = new GameObject("CayCoi").transform;
            flora.SetParent(root, false);
            void Put(string name, Mesh mesh, Material[] mats, float minDist)
            {
                if (!FreeSpot(rng, keep, area, 22f, minDist, out var g)) return;
                var go = new GameObject(name);
                go.transform.SetParent(flora, false);
                go.transform.position = g.point - Vector3.up * 0.05f;
                go.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.25f, (float)rng.NextDouble());
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterials = mats;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            }
            for (int i = 0; i < 12; i++) Put("BuiTreChay", bamboo[rng.Next(3)], new[] { stalk, dryLeaf }, 4f);
            for (int i = 0; i < 8; i++) Put("CayChuoi", banana[rng.Next(3)], new[] { trunk, leaf, dryLeaf }, 4f);
        }

        // ───────── Ván lót sàn hào, cọc gỗ chống vách ─────────

        /// Dò lòng hào: điểm mặt đất có vách ở hai phía đối diện trong vòng 1,8 m (vách đất địa hình hoặc khối).
        /// Rải ván lót dọc theo hào, cọc gỗ sát vách. Dùng được cho hào khoét địa hình (Màn 1) lẫn hào khối (Màn 4).
        static int PlaceTrenchFloor(Transform root, Rect area)
        {
            var wood = LoadOrMakeMat("VanHao", new Color(0.4f, 0.3f, 0.19f));
            var darkWood = LoadOrMakeMat("CocGo", new Color(0.27f, 0.2f, 0.13f));
            var board = SaveFlora("VanLotHao", DuckBoard());
            var stake = SaveFlora("CocGo", Stake());
            var parent = new GameObject("VanHao").transform;
            parent.SetParent(root, false);
            var placed = new List<Vector3>();
            var stakes = new List<Vector3>();
            int count = 0;
            for (float x = area.xMin - 10; x <= area.xMax + 10 && count < 500; x += 1.1f)
            for (float z = area.yMin - 10; z <= area.yMax + 10 && count < 500; z += 1.1f)
            {
                if (!GroundHit(new Vector3(x, 0, z), out var g)) continue;
                var p = g.point + Vector3.up * 0.8f;
                bool ew = Wall(p, Vector3.right, out var e) & Wall(p, Vector3.left, out var w);
                bool ns = Wall(p, Vector3.forward, out var n) & Wall(p, Vector3.back, out var s);
                if (ew == ns) continue; // không phải lòng hào (hoặc hố kín bốn phía)
                // Lòng hào chạy theo trục không có vách; đặt ván ở giữa hai vách
                var along = ew ? Vector3.forward : Vector3.right;
                var mid = ew ? (e.point + w.point) * 0.5f : (n.point + s.point) * 0.5f;
                float width = ew ? Vector3.Distance(e.point, w.point) : Vector3.Distance(n.point, s.point);
                if (width < 0.9f || width > 3.4f) continue;
                if (!GroundHit(new Vector3(mid.x, 0, mid.z), out var floor) || Mathf.Abs(floor.point.y - g.point.y) > 0.5f) continue;
                if (placed.Any(q => (q - floor.point).sqrMagnitude < 1.0f)) continue;
                placed.Add(floor.point);
                var go = new GameObject("VanLot");
                go.transform.SetParent(parent, false);
                go.transform.position = floor.point + Vector3.up * 0.02f;
                go.transform.rotation = Quaternion.LookRotation(along) * Quaternion.Euler(0, (Mathf.PerlinNoise(x * 0.7f, z * 0.7f) - 0.5f) * 8f, 0);
                go.AddComponent<MeshFilter>().sharedMesh = board;
                go.AddComponent<MeshRenderer>().sharedMaterial = wood;
                count++;
                // Cọc gỗ sát hai vách, cách nhau khoảng 2,2 m
                foreach (var hit in ew ? new[] { e, w } : new[] { n, s })
                {
                    if (stakes.Any(q => (q - hit.point).sqrMagnitude < 4.8f)) continue;
                    stakes.Add(hit.point);
                    var post = new GameObject("CocGo");
                    post.transform.SetParent(parent, false);
                    var inward = (mid - hit.point); inward.y = 0;
                    post.transform.position = new Vector3(hit.point.x, floor.point.y, hit.point.z) + inward.normalized * 0.08f;
                    post.transform.rotation = Quaternion.LookRotation(inward.normalized) * Quaternion.Euler(-6, 0, 0);
                    post.AddComponent<MeshFilter>().sharedMesh = stake;
                    post.AddComponent<MeshRenderer>().sharedMaterial = darkWood;
                }
            }
            foreach (var t in parent.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            return count;
        }

        static bool Wall(Vector3 p, Vector3 dir, out RaycastHit hit) =>
            Physics.Raycast(p, dir, out hit, 1.8f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(hit.normal.y) < 0.6f
            && !hit.collider.GetComponentInParent<DBP.Core.Health>();

        /// Tấm ván lót 1 m: hai thanh dầm dọc, 6 thanh ngang hơi lệch như đóng tay.
        static Mesh DuckBoard()
        {
            var m = new PartsMesh(1);
            m.Box(new Vector3(-0.26f, 0.03f, 0), new Vector3(0.06f, 0.06f, 1.0f), Quaternion.identity, 0);
            m.Box(new Vector3(0.26f, 0.03f, 0), new Vector3(0.06f, 0.06f, 1.0f), Quaternion.identity, 0);
            for (int i = 0; i < 6; i++)
                m.Box(new Vector3((i % 2) * 0.02f, 0.075f, -0.42f + i * 0.168f), new Vector3(0.68f, 0.03f, 0.11f), Quaternion.Euler(0, (i % 3 - 1) * 2.5f, 0), 0);
            return m.Build("VanLotHao");
        }

        /// Cọc gỗ tròn cao 1,7 m, đầu vạt, có dây néo ngang.
        static Mesh Stake()
        {
            var m = new PartsMesh(1);
            m.Rod(Vector3.zero, new Vector3(0, 1.6f, 0), 0.055f, 0);
            m.Box(new Vector3(0, 1.66f, 0), new Vector3(0.09f, 0.1f, 0.09f), Quaternion.Euler(0, 45, 20), 0);
            m.Box(new Vector3(0, 1.1f, -0.02f), new Vector3(0.12f, 0.04f, 0.04f), Quaternion.identity, 0);
            return m.Build("CocGo");
        }
    }
}
