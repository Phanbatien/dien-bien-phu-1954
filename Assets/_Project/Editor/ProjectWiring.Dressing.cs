using System.Collections.Generic;
using System.Linq;
using DBP.Core;
using DBP.Interaction;
using DBP.Missions;
using DBP.Weapons;
using UnityEditor;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Môi trường Màn 2–5 (T41, T46, T60, bổ sung T54), sửa dây thép gai Màn 1, súng tiểu liên trên tay (T55).
    /// Rải đồ có sẵn trong repo theo seed cố định của từng màn, tránh khu vực chơi; dựng lại được nhiều lần.
    public static partial class ProjectWiring
    {
        const string Polytope = "Assets/Polytope Studio/Lowpoly_Environments/Sources/Meshes/";
        const string Nature = "Assets/SimpleNaturePack/Models/";
        const string Rsg = "Assets/RSG/Base/Models/";

        static readonly string[] DeadTrees = { Polytope + "Trees/PT_Pine_Tree_03_dead.fbx", Polytope + "Trees/PT_Pine_Tree_03_dead_cut.fbx", Polytope + "Trees/PT_Fruit_Tree_01_dead.fbx" };
        static readonly string[] Debris = { Polytope + "Trees/PT_Pine_Tree_03_logs.fbx", Polytope + "Trees/PT_Pine_Tree_03_stump.fbx", Polytope + "Trees/PT_Fruit_Tree_01_stump.fbx", Nature + "Stump_01.fbx", Nature + "Branch_01.fbx" };
        static readonly string[] Rocks = { Polytope + "Rocks/PT_Generic_Rock_01.fbx", Polytope + "Rocks/PT_River_Rock_Pile_02.fbx", Nature + "Rock_01.fbx", Nature + "Rock_02.fbx", Nature + "Rock_03.fbx", Nature + "Rock_04.fbx", Nature + "Rock_05.fbx" };
        static readonly string[] Shrubs = { Polytope + "Shrubs/PT_Generic_Shrub_01_dead.fbx", Nature + "Bush_02.fbx", Nature + "Bush_03.fbx" };
        static readonly string[] Grass = { Nature + "Grass_01.fbx", Nature + "Grass_02.fbx", Polytope + "Plants/PT_Grass_02.fbx" };
        const string Wire = Rsg + "SM_rsg_military_barbed_wire_01.FBX";
        const string Hedgehog = Rsg + "SM_rsg_military_antitank_hedgehog_01.FBX";
        const string Concrete = Rsg + "SM_rsg_military_concret_block_01.FBX";
        const string SmgModel = "Assets/Low Poly Weapon Series V 4/Models/Low Poly Series V 4 New/Weapons/WWII_SMG_B.fbx";

        struct Spec { public string[] models; public int count; public float minDist, ring; public float scaleMin, scaleMax; }

        static string WireDressing(string sceneName)
        {
            if (sceneName.StartsWith("M1")) return FixFloatingWire();
            if (!(sceneName.StartsWith("M2") || sceneName.StartsWith("M3") || sceneName.StartsWith("M4") || sceneName.StartsWith("M5"))) return null;

            var old = GameObject.Find("Dressing");
            if (old) Object.DestroyImmediate(old);
            var root = new GameObject("Dressing").transform;
            var rng = new System.Random(StableHash(sceneName)); // cùng màn → cùng cách rải, mọi máy như nhau
            var keep = KeepOutPoints();
            var area = PlayArea(keep);

            var specs = new List<Spec>
            {
                new Spec { models = DeadTrees, count = sceneName.StartsWith("M4") ? 12 : 26, minDist = 9f, ring = 30f, scaleMin = 0.8f, scaleMax = 1.3f },
                new Spec { models = Debris, count = 18, minDist = 4f, ring = 12f, scaleMin = 0.8f, scaleMax = 1.2f },
                new Spec { models = Rocks, count = 26, minDist = 3f, ring = 15f, scaleMin = 0.7f, scaleMax = 1.6f },
                new Spec { models = Shrubs, count = sceneName.StartsWith("M3") ? 26 : 12, minDist = 3f, ring = 15f, scaleMin = 0.8f, scaleMax = 1.3f },
                new Spec { models = Grass, count = 70, minDist = 1.5f, ring = 6f, scaleMin = 0.9f, scaleMax = 1.4f },
                new Spec { models = new[] { Wire }, count = 14, minDist = 3f, ring = 6f, scaleMin = 1f, scaleMax = 1f },
                new Spec { models = new[] { Hedgehog }, count = sceneName.StartsWith("M3") ? 0 : 8, minDist = 4f, ring = 8f, scaleMin = 1f, scaleMax = 1f },
            };
            if (sceneName.StartsWith("M5")) specs.Add(new Spec { models = new[] { Concrete }, count = 8, minDist = 4f, ring = 6f, scaleMin = 1f, scaleMax = 1f });

            int placed = 0;
            foreach (var spec in specs)
                for (int i = 0; i < spec.count; i++)
                {
                    // Thử vài vị trí; bỏ nếu quá gần khu vực chơi
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        var p = new Vector3(Mathf.Lerp(area.xMin - spec.ring, area.xMax + spec.ring, (float)rng.NextDouble()), 0,
                                            Mathf.Lerp(area.yMin - spec.ring, area.yMax + spec.ring, (float)rng.NextDouble()));
                        if (keep.Any(k => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(k.x, k.z)) < spec.minDist)) continue;
                        if (!Ground(p, out var ground)) continue;
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.models[rng.Next(spec.models.Length)]);
                        if (!prefab) break;
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                        go.transform.position = ground;
                        go.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                        go.transform.localScale = Vector3.one * Mathf.Lerp(spec.scaleMin, spec.scaleMax, (float)rng.NextDouble());
                        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                        foreach (var t in go.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
                        placed++;
                        break;
                    }
                }

            string extra = sceneName.StartsWith("M5") ? DressBridge(root) : "";
            return $"rải {placed} vật trang trí{extra}";
        }

        static int StableHash(string s)
        {
            unchecked { int h = 17; foreach (char ch in s) h = h * 31 + ch; return h; }
        }

        /// Điểm không được đặt đồ trang trí: người chơi, mục tiêu, vùng nhiệm vụ, điểm hòm, vật tư.
        static List<Vector3> KeepOutPoints()
        {
            var pts = new List<Vector3> { PlayerPosition() };
            pts.AddRange(All<Health>().Select(h => h.transform.position));
            pts.AddRange(All<CrateSpawner>().SelectMany(s => s.points.Where(p => p).Select(p => p.position)));
            pts.AddRange(All<MissionSupplyPoint>().Select(s => s.transform.position));
            pts.AddRange(All<DBP.AI.SatchelCharge>().Select(s => s.transform.position));
            foreach (var o in All<Objective>())
            {
                Collider col = o is ReachZoneObjective r ? r.zone : o is CaptureZoneObjective c ? c.zone : null;
                if (!col && o.TryGetComponent<Collider>(out var own)) col = own;
                if (col) { var b = col.bounds; pts.Add(b.center); pts.Add(new Vector3(b.min.x, 0, b.min.z)); pts.Add(new Vector3(b.max.x, 0, b.max.z)); }
            }
            // Đường đi từ người chơi tới từng mục tiêu: chừa hành lang trống
            var player = PlayerPosition();
            foreach (var h in All<Health>().Where(h => h.Kind != TargetKind.Player))
                for (float t = 0.2f; t < 1f; t += 0.2f) pts.Add(Vector3.Lerp(player, h.transform.position, t));
            return pts;
        }

        static Rect PlayArea(List<Vector3> pts)
        {
            float x0 = pts.Min(p => p.x), x1 = pts.Max(p => p.x), z0 = pts.Min(p => p.z), z1 = pts.Max(p => p.z);
            return Rect.MinMaxRect(x0, z0, x1, z1);
        }

        static bool Ground(Vector3 p, out Vector3 point)
        {
            foreach (var hit in Physics.RaycastAll(p + Vector3.up * 80f, Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                if (hit.collider.GetComponentInParent<Health>() || hit.normal.y < 0.75f) { point = p; return false; }
                point = hit.point;
                return true;
            }
            point = p;
            return false;
        }

        /// Màn 5: cầu ván phẳng (War Map, hai nhịp) đúng bằng tấm ván va chạm: mặt cầu ngang mặt bờ nên đi trên cầu,
        /// không xuyên qua; cọc gỗ hai bên làm lan can. (Cầu vòm Polytope cũ cao 1,8–3,1 m so với mặt đi.)
        static string DressBridge(Transform root)
        {
            var plank = GameObject.Find("CauMuongThanh");
            if (!plank) return "";
            var old = GameObject.Find("CauMuongThanh_Model");
            if (old) Object.DestroyImmediate(old);
            var deck = plank.GetComponent<Renderer>().bounds;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(WarMap + "Environment/SM_Prop_Trench_Bridge_02.prefab");
            if (!asset) return "";
            var go = new GameObject("CauMuongThanh_Model").transform;
            go.SetParent(root, false);
            const float PieceLen = 5.82f, PieceWidth = 3.22f, PieceHeight = .47f;
            int n = Mathf.CeilToInt(deck.size.z / PieceLen);
            float len = deck.size.z / n;
            for (int i = 0; i < n; i++)
            {
                var piece = (GameObject)PrefabUtility.InstantiatePrefab(asset, go);
                foreach (var c in piece.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                piece.transform.localScale = new Vector3(deck.size.x / PieceWidth, 1f, len / PieceLen);
                // Mặt ván (đỉnh mẫu) trùng mặt tấm va chạm
                piece.transform.position = new Vector3(deck.center.x, deck.max.y - PieceHeight, deck.min.z + len * (i + .5f));
            }
            for (float z = deck.min.z + .3f; z <= deck.max.z - .2f; z += 2f)
                foreach (int side in new[] { -1, 1 })
                {
                    var post = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WarMap + "Environment/SM_Env_Trench_Pole_01.prefab"), go);
                    foreach (var c in post.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                    post.transform.localScale = new Vector3(1f, .6f, 1f);
                    post.transform.position = new Vector3(deck.center.x + side * (deck.extents.x - .15f), deck.max.y - .15f, z);
                }
            plank.GetComponent<Renderer>().enabled = false;
            return ", cầu ván Mường Thanh";
        }

        /// Màn 1: dây thép gai trôi lơ lửng (đáy cao hơn mặt đất > 0,4 m) được hạ xuống.
        static string FixFloatingWire()
        {
            int fixedCount = 0;
            foreach (var mf in All<MeshFilter>().Where(m => m.sharedMesh && m.sharedMesh.name.Contains("barbed_wire")))
            {
                var t = mf.transform;
                var r = mf.GetComponent<Renderer>();
                if (!r) continue;
                float bottom = r.bounds.min.y;
                if (!Ground(new Vector3(r.bounds.center.x, 0, r.bounds.center.z), out var g)) continue;
                if (bottom - g.y > 0.4f)
                {
                    t.position += Vector3.down * (bottom - g.y - 0.05f);
                    fixedCount++;
                }
            }
            return fixedCount > 0 ? $"hạ {fixedCount} dây thép gai lơ lửng xuống mặt đất" : null;
        }

        /// T55: thêm tiểu liên vào mô hình tay góc nhìn thứ nhất và bộ đổi súng theo vũ khí đang cầm.
        static void EnsureSmgViewModel()
        {
            const string path = "Assets/_Project/Prefabs/ViewModel_Arms.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<WeaponViewModelSwitcher>()) return;
                var arms = root.GetComponentInChildren<DBP.Player.FirstPersonArms>(true);
                var rifle = arms ? arms.rifle : null;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmgModel);
                if (!rifle || !prefab) return;
                var smg = (GameObject)PrefabUtility.InstantiatePrefab(prefab, rifle.parent);
                smg.name = "SMG_MAT49_Kieu50";
                smg.transform.localPosition = rifle.localPosition;
                smg.transform.localRotation = rifle.localRotation;
                var rb = WorldBounds(rifle.gameObject);
                var sb = WorldBounds(smg);
                float ratio = Mathf.Max(sb.size.x, sb.size.y, sb.size.z);
                if (ratio > 0.0001f) smg.transform.localScale = smg.transform.localScale * (Mathf.Max(rb.size.x, rb.size.y, rb.size.z) * 0.72f / ratio);
                foreach (var c in smg.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                smg.SetActive(false);
                UrpMaterialUtility.Apply(smg);
                var sw = root.AddComponent<WeaponViewModelSwitcher>();
                sw.rifleModel = rifle.gameObject;
                sw.smgModel = smg;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
