using System;
using System.Collections.Generic;
using System.IO;
using DBP.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DBP.EditorTools
{
    /// Bước 2–5 của Trench Map: sàn gỗ, vách cọc, hầm chữ A, công sự, kể chuyện và sương mù.
    /// Chạy sau "Dựng địa hình (Bước 1)". Mỗi menu xoá đúng nhóm của nó rồi dựng lại.
    /// Bước 6 (đá & cỏ) nằm ở TrenchGroundDetailEditorUtility và chạy kèm "Dựng tất cả".
    /// Dòng lệnh: -executeMethod DBP.EditorTools.TrenchDressingEditorUtility.BuildAllCI
    public static class TrenchDressingEditorUtility
    {
        const string ScenePath = "Assets/_Project/Scenes/Trench Map.unity";
        const string FreezeProfilePath = "Assets/_Project/Art/FreezeVolumeProfile.asset";

        const string Pallet = "Assets/Defensive_props/Prefabs/boxes/pallet.prefab";
        const string Fence1 = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
        const string Fence2 = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_02.prefab";
        const string Bridge = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Bridge/PT_Wooden_Bridge_02.prefab";
        const string Table = "Assets/Defensive_props/Prefabs/tables/wooden_table.prefab";
        const string Workbench = "Assets/Defensive_props/Prefabs/tables/workbench.prefab";
        const string Radio = "Assets/Low Poly Weapon Series V 4/Prefabs/Low Poly Series V 2/Items/Tatical Radio.prefab";
        const string Barricade = "Assets/Defensive_props/Prefabs/fortifications/sandbag barricade.prefab";
        const string Barricade2 = "Assets/Defensive_props/Prefabs/fortifications/sandbag barricade2.prefab";
        const string Sandbag = "Assets/Defensive_props/Prefabs/fortifications/sandbag.prefab";
        const string Sandbox = "Assets/RSG/URP/Prefabs/P_rsg_military_sandbox_01.prefab";
        const string Wire = "Assets/RSG/URP/Prefabs/P_rsg_military_barbed_wire_01.prefab";
        const string Hedgehog = "Assets/RSG/URP/Prefabs/P_rsg_military_antitank_hedgehog_01.prefab";
        const string Concrete = "Assets/Defensive_props/Prefabs/fortifications/concrete_barricade_damaged.prefab";
        const string Mortar = "Assets/Defensive_props/Prefabs/defence/mortar.prefab";
        const string Casing = "Assets/Defensive_props/Prefabs/defence/Shell_casing.prefab";
        const string AmmoBox = "Assets/Defensive_props/Prefabs/defence/ammo_box.prefab";
        const string Lmg = "Assets/Low Poly Weapon Series V 4/Prefabs/Low Poly Series V 4 New/Weapons/PreSet/WWII_LMG_B_PreSet.prefab";
        const string RifleA = "Assets/Low Poly Weapon Series V 4/Prefabs/Low Poly Series V 4 New/Weapons/WWII_Rifle_A.prefab";
        const string RifleB = "Assets/Low Poly Weapon Series V 4/Prefabs/Low Poly Series V 4 New/Weapons/WWII_Rifle_B.prefab";
        const string RifleC = "Assets/tt-3d/LowPolyWarStarterPack/Weapon/Prefabs/Rifle1.prefab";
        const string GrenadeE = "Assets/Low Poly Weapon Series V 4/Prefabs/Low Poly Series V 1/Bullets/Grenade_E.prefab";
        const string GrenadeB = "Assets/Low Poly Weapon Series V 4/Prefabs/Low Poly Series V 3/Grenades/Frag Grenade_B.prefab";
        const string TreeDead = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_dead.prefab";
        const string TreeCut = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_dead_cut.prefab";
        const string Stump = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_stump.prefab";
        const string Logs = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_logs.prefab";

        internal const string StructureRoot = "Trench_Structure";
        internal const string FortRoot = "Trench_Fortifications";
        internal const string StoryRoot = "Trench_Storytelling";
        const string BridgeNorth = "TrenchBridge_North";
        const string BridgeSouth = "TrenchBridge_South";

        static readonly string[] BridgeNames = { BridgeNorth, BridgeSouth };
        static readonly Dictionary<string, MeshAxes> AxisCache = new Dictionary<string, MeshAxes>();

        struct MeshAxes
        {
            public Vector3 size;
            public int longAxis, upAxis, thickAxis;
            public float Size(int axis) => axis == 0 ? size.x : axis == 1 ? size.y : size.z;
        }

        struct Span
        {
            public float x0, x1, zStand, zPara;
        }

        static System.Random rng;

        static readonly Dictionary<string, string> FortCategory = new Dictionary<string, string>
        {
            { "BocBaoCat", "Sandbags" },
            { "GiaSung", "GunRacks" },
            { "BeBan", "Platforms" },
            { "SanBeBan", "StepFloors" },
            { "DayThepGai", "BarbedWire" },
            { "BreachBlocker", "BreachBlockers" },
            { "ChongSat", "Hedgehogs" },
            { "SungMay", "MachineGuns" },
            { "BocSungMay", "MgSandbags" },
            { "Coi", "Mortars" },
            { "VongCoi", "MortarSandbags" },
            { "VoDan", "ShellCasings" },
            { "ThungDan", "AmmoBoxes" },
        };

        // ───────────────────────────── Menu ─────────────────────────────

        [MenuItem("DBP/Trench Map/Bước 2 – Vách hào & hầm")]
        public static void Step2() => BuildStep(195401, StructureRoot, BuildStructure, "Bước 2");

        [MenuItem("DBP/Trench Map/Bước 3 – Công sự")]
        public static void Step3() => BuildStep(195402, FortRoot, BuildFortifications, "Bước 3");

        [MenuItem("DBP/Trench Map/Bước 4 – Kể chuyện")]
        public static void Step4() => BuildStep(195403, StoryRoot, BuildStory, "Bước 4");

        [MenuItem("DBP/Trench Map/Bước 5 – Ánh sáng")]
        public static void Step5()
        {
            EnsureScene();
            BuildAtmosphere();
            Save();
            Debug.Log("[TrenchDressing] Bước 5: sương mù, nắng và FreezeVolume.");
        }

        [MenuItem("DBP/Trench Map/Dựng tất cả (2–6)")]
        public static void BuildAll()
        {
            Step2();
            Step3();
            Step4();
            Step5();
            TrenchGroundDetailEditorUtility.Build();
            UrpMaterialUtility.FixOpenScene();
            Save();
        }

        public static void BuildAllCI()
        {
            try
            {
                EnsureScene();
                foreach (var name in new[] { StructureRoot, FortRoot, StoryRoot, TrenchGroundDetailEditorUtility.GroundRoot })
                {
                    var existing = GameObject.Find(name);
                    if (existing) Object.DestroyImmediate(existing);
                }
                TrenchTerrainEditorUtility.Build();
                BuildAll();
                var report = TrenchTerrainEditorUtility.Validate();
                Debug.Log(report);
                if (Application.isBatchMode) EditorApplication.Exit(report.Contains("FAIL") ? 1 : 0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static void BuildStep(int seed, string rootName, Action<Transform> build, string label)
        {
            EnsureScene();
            rng = new System.Random(seed);
            AxisCache.Clear();
            var root = RebuildRoot(rootName);
            build(root);
            Save();
            Debug.Log($"[TrenchDressing] {label}: {CountPlaced(root)} vật trong {rootName}.");
        }

        // ───────────────────────────── Bước 2 ─────────────────────────────

        static void BuildStructure(Transform root)
        {
            Duckboards(root);
            Revetment(root);
            foreach (var shelter in TrenchTerrainEditorUtility.Shelters)
            {
                if (shelter.Name == "MgNest") continue;
                bool command = shelter.Name == "Command";
                AFrame(shelter, ridgeAlongX: command, root);
                MouthConcrete(shelter, command ? Vector2.up : Vector2.right, root);
                if (command) CommandFurniture(shelter, root);
            }
            PlaceBridge(BridgeNorth, new Vector2(6f, 33f), root);
            PlaceBridge(BridgeSouth, new Vector2(10f, -17f), root);
            BayBoards(TrenchTerrainEditorUtility.Bays[0].center, TrenchTerrainEditorUtility.Bays[0].half, root);
            BayBoards(TrenchTerrainEditorUtility.Bays[1].center, TrenchTerrainEditorUtility.Bays[1].half, root);
        }

        static void Duckboards(Transform root)
        {
            var route = TrenchTerrainEditorUtility.Route;
            for (int i = 0; i < route.Length - 1; i++)
            {
                Vector2 a = route[i], b = route[i + 1], delta = b - a;
                float len = delta.magnitude;
                Vector2 dir = delta / len;
                float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                for (float s = 0.85f; s < len - 0.4f; s += 1.7f)
                {
                    Vector2 p = a + dir * s;
                    if (NearBreach(p, 2f)) continue;
                    var go = Spawn(Pallet, root);
                    go.name = "SanGo";
                    Sit(go, p, yaw + Rand(-3f, 3f));
                    SinkTop(go, Sample(p) + 0.04f);
                }
            }
        }

        static void Revetment(Transform root)
        {
            var route = TrenchTerrainEditorUtility.Route;
            float module = Mathf.Max(0.8f, LongSize(Fence1, root));
            int count = Mathf.Max(1, Mathf.FloorToInt(3f / module));
            for (int i = 0; i < route.Length - 1; i++)
            {
                Vector2 a = route[i], b = route[i + 1], delta = b - a;
                float len = delta.magnitude;
                Vector2 dir = delta / len;
                Vector2 side = new Vector2(-dir.y, dir.x);
                int n = count;
                if (n * module > len * 0.46f) n = Mathf.Max(1, Mathf.FloorToInt(len * 0.46f / module));
                // Lùi 0,85 m khỏi góc: thanh dài 2,2 m nếu bắt đầu sát góc sẽ cắm vào tim nhịp vuông góc.
                const float cornerGap = 0.85f;
                for (int end = 0; end < 2; end++)
                {
                    Vector2 origin = end == 0 ? a : b;
                    float sign = end == 0 ? 1f : -1f;
                    for (int s = -1; s <= 1; s += 2)
                    {
                        for (int m = 0; m < n; m++)
                        {
                            Vector2 p = origin + dir * (sign * (cornerGap + (m + 0.5f) * module)) + side * (s * 1.15f);
                            if (InsideShelter(p, 0.05f)) continue;
                            PlaceFence(p, dir * sign, side * s, (m + i) % 2 == 0 ? Fence1 : Fence2, root, "CocGo");
                        }
                    }
                }
            }

            BayRevetment(TrenchTerrainEditorUtility.Bays[0].center, TrenchTerrainEditorUtility.Bays[0].half, Vector2.up, root);
            BayRevetment(TrenchTerrainEditorUtility.Bays[1].center, TrenchTerrainEditorUtility.Bays[1].half, Vector2.down, root);
        }

        static void BayRevetment(Vector2 center, Vector2 half, Vector2 openDir, Transform root)
        {
            float module = Mathf.Max(0.8f, LongSize(Fence1, root));
            void Edge(Vector2 outward)
            {
                if (Vector2.Dot(outward, openDir) > 0.5f) return;
                Vector2 along = new Vector2(-outward.y, outward.x);
                float length = Mathf.Abs(along.x) > 0.5f ? half.x * 2f : half.y * 2f;
                int n = Mathf.Max(1, Mathf.RoundToInt(length / module));
                for (int i = 0; i < n; i++)
                {
                    float t = (i + 0.5f) / n - 0.5f;
                    Vector2 p = center + outward * (Mathf.Abs(outward.x) > 0.5f ? half.x : half.y) * 0.92f + along * (t * length);
                    if (InsideShelter(p, 0.05f)) continue;
                    PlaceFence(p, along, outward, i % 2 == 0 ? Fence1 : Fence2, root, "CocGo");
                }
            }
            Edge(Vector2.up);
            Edge(Vector2.down);
            Edge(Vector2.left);
            Edge(Vector2.right);
        }

        static void PlaceFence(Vector2 p, Vector2 along, Vector2 outward, string path, Transform parent, string name)
        {
            var axes = PrefabAxes(path, parent);
            var go = Spawn(path, parent);
            go.name = name;
            var up = (Vector3.up + new Vector3(outward.x, 0f, outward.y) * Mathf.Tan(10f * Mathf.Deg2Rad)).normalized;
            var rot = AxesRotation(axes, new Vector3(along.x, 0f, along.y), up);
            float y = Sample(p);
            go.transform.SetPositionAndRotation(new Vector3(p.x, y, p.y), rot);
            var b = BoundsOf(go);
            go.transform.position += new Vector3(p.x - b.center.x, y - b.min.y, p.y - b.center.z);
            ShoveFromRoute(go, 0.6f);
            Stamp(go);
        }

        static void AFrame(TrenchTerrainEditorUtility.Shelter shelter, bool ridgeAlongX, Transform parent)
        {
            var c = shelter.Center;
            var h = shelter.Half;
            float span = (ridgeAlongX ? h.y : h.x) * 2f;
            float length = (ridgeAlongX ? h.x : h.y) * 2f;
            float halfSpan = span * 0.5f;
            float lean = 35f * Mathf.Deg2Rad;
            float rise = halfSpan / Mathf.Tan(lean);
            float foot = Mathf.Max(0.25f, 2.5f - rise * (1f - Mathf.Clamp01(0.5f / halfSpan)));
            float floorY = Sample(c);
            int pairs = Mathf.Max(2, Mathf.RoundToInt(length / 1.2f));
            const float inset = 0.2f;
            for (int i = 0; i < pairs; i++)
            {
                float t = pairs == 1 ? 0.5f : i / (float)(pairs - 1);
                float along = Mathf.Lerp(-length * 0.5f + inset, length * 0.5f - inset, t);
                Vector3 ridge, footA, footB;
                if (ridgeAlongX)
                {
                    ridge = new Vector3(c.x + along, floorY + foot + rise, c.y);
                    footA = new Vector3(c.x + along, floorY + foot, c.y - halfSpan);
                    footB = new Vector3(c.x + along, floorY + foot, c.y + halfSpan);
                }
                else
                {
                    ridge = new Vector3(c.x, floorY + foot + rise, c.y + along);
                    footA = new Vector3(c.x - halfSpan, floorY + foot, c.y + along);
                    footB = new Vector3(c.x + halfSpan, floorY + foot, c.y + along);
                }
                PlaceBeam(footA, ridge, parent);
                PlaceBeam(footB, ridge, parent);
            }

            var bag = Spawn(Barricade, parent);
            bag.name = "MaiBaoCat";
            var axes = PrefabAxes(Barricade, parent);
            ScaleAxis(bag, axes.longAxis, axes.Size(axes.longAxis), Mathf.Max(0.8f, length - 0.3f));
            var rot = AxesRotation(axes, ridgeAlongX ? Vector3.right : Vector3.forward, Vector3.up);
            float ridgeY = floorY + foot + rise;
            bag.transform.SetPositionAndRotation(new Vector3(c.x, ridgeY, c.y), rot);
            var b = BoundsOf(bag);
            bag.transform.position += new Vector3(c.x - b.center.x, ridgeY - b.min.y, c.y - b.center.z);
            Stamp(bag);
        }

        static void PlaceBeam(Vector3 from, Vector3 to, Transform parent)
        {
            var axes = PrefabAxes(Fence1, parent);
            var go = Spawn(Fence1, parent);
            go.name = "DamGo";
            float len = Vector3.Distance(from, to);
            ScaleAxis(go, axes.longAxis, axes.Size(axes.longAxis), len);
            ScaleAxis(go, axes.upAxis, axes.Size(axes.upAxis), 0.28f);
            ScaleAxis(go, axes.thickAxis, axes.Size(axes.thickAxis), 0.12f);
            var mid = (from + to) * 0.5f;
            go.transform.SetPositionAndRotation(mid, AxesRotation(axes, to - from, Vector3.up));
            var b = BoundsOf(go);
            go.transform.position += mid - b.center;
            Stamp(go);
        }

        static void MouthConcrete(TrenchTerrainEditorUtility.Shelter shelter, Vector2 outward, Transform parent)
        {
            Vector2 side = new Vector2(-outward.y, outward.x);
            float outHalf = Mathf.Abs(outward.x) > 0.5f ? shelter.Half.x : shelter.Half.y;
            float sideHalf = Mathf.Abs(side.x) > 0.5f ? shelter.Half.x : shelter.Half.y;
            Vector2 mouth = shelter.Center + outward * (outHalf - 0.15f);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector2 p = mouth + side * s * (sideHalf + 0.15f);
                var go = Spawn(Concrete, parent);
                go.name = "CuaHam";
                Sit(go, p, 0f);
                ShoveFromRoute(go, 0.85f);
                KeepOffPoint(go, shelter.Center, 0.7f);
            }
        }

        static void CommandFurniture(TrenchTerrainEditorUtility.Shelter shelter, Transform parent)
        {
            var table = Spawn(Table, parent);
            table.name = "BanChiHuy";
            AlignLongAxis(table, PrefabAxes(Table, parent), Vector3.right, Vector3.up);
            Sit(table, new Vector2(2.15f, 22.35f), table.transform.eulerAngles.y);
            KeepOffPoint(table, shelter.Center, 0.6f);

            var bench = Spawn(Workbench, parent);
            bench.name = "BanTho";
            AlignLongAxis(bench, PrefabAxes(Workbench, parent), Vector3.forward, Vector3.up);
            Sit(bench, new Vector2(0.15f, 23.1f), bench.transform.eulerAngles.y);
            KeepOffPoint(bench, shelter.Center, 0.6f);
            ShoveFromRoute(bench, 0.85f);

            var top = BoundsOf(table);
            PutOnSurface(Radio, new Vector3(top.center.x - 0.15f, top.max.y, top.center.z), 15f, 0.32f, parent, "Radio");
            var map = GameObject.CreatePrimitive(PrimitiveType.Quad);
            map.name = "BanDoTacChien";
            map.transform.SetParent(parent, false);
            Object.DestroyImmediate(map.GetComponent<Collider>());
            map.transform.localScale = new Vector3(0.72f, 0.5f, 1f);
            map.GetComponent<MeshRenderer>().sharedMaterial = MapMaterial();
            map.transform.SetPositionAndRotation(new Vector3(top.center.x + 0.22f, top.max.y + 0.02f, top.center.z), Quaternion.Euler(-90f, 18f, 0f));
            Stamp(map);
        }

        static void PutOnSurface(string path, Vector3 topPoint, float yaw, float maxExtent, Transform parent, string name)
        {
            var go = Spawn(path, parent);
            go.name = name;
            float extent = Mathf.Max(0.01f, BoundsOf(go).size.magnitude);
            if (extent > maxExtent) go.transform.localScale *= maxExtent / extent;
            go.transform.SetPositionAndRotation(topPoint, Quaternion.Euler(0f, yaw, 0f));
            var b = BoundsOf(go);
            go.transform.position += new Vector3(topPoint.x - b.center.x, topPoint.y - b.min.y, topPoint.z - b.center.z);
            Stamp(go);
        }

        static void PlaceBridge(string name, Vector2 at, Transform parent)
        {
            var axes = PrefabAxes(Bridge, parent);
            var go = Spawn(Bridge, parent);
            go.name = name;
            var longDir = axes.longAxis == 2 ? Vector3.forward : Vector3.right;
            // Nhịp tại đây chạy dọc Z, nên cầu phải bắc theo X.
            if (Mathf.Abs(longDir.x) < 0.5f)
                go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            go.transform.position = new Vector3(at.x, 10.05f, at.y);
            var b = BoundsOf(go);
            go.transform.position += new Vector3(at.x - b.center.x, 10.05f - b.min.y, at.y - b.center.z);
            Stamp(go);
        }

        static void BayBoards(Vector2 center, Vector2 half, Transform parent)
        {
            var axes = PrefabAxes(Bridge, parent);
            float width = Mathf.Min(axes.size.x, axes.size.z);
            float fit = Mathf.Min(width, half.x - 0.9f);
            for (int s = -1; s <= 1; s += 2)
            {
                var go = Spawn(Bridge, parent);
                go.name = "SanHam";
                if (axes.longAxis == 0) go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            if (fit < width - 0.05f)
            {
                int thick = axes.longAxis == 0 ? 2 : 0;
                ScaleAxis(go, thick, width, Mathf.Max(0.8f, fit));
            }
            float x = center.x + s * (0.75f + fit * 0.5f);
            Sit(go, new Vector2(x, center.y), go.transform.eulerAngles.y);
            ShoveFromRoute(go, 0.8f);
        }
    }

        // ───────────────────────────── Bước 3 ─────────────────────────────

        static void BuildFortifications(Transform root)
        {
            Parapets(root);
            FiringStepWood(root);
            WireLine(root);
            Breaches(root);
            MgNest(root);
            MortarPit(root);
            ScatterHedgehogs(root);
        }

        static List<Span> FiringSpans()
        {
            var list = new List<Span>();
            var route = TrenchTerrainEditorUtility.Route;
            float hw = TrenchTerrainEditorUtility.HalfWidth;
            float sd = TrenchTerrainEditorUtility.StepDepth;
            for (int i = 0; i < route.Length - 1; i++)
            {
                Vector2 a = route[i], b = route[i + 1];
                float hx = Mathf.Abs(b.x - a.x) * 0.5f;
                if (hx <= hw) continue;
                float midX = (a.x + b.x) * 0.5f;
                float midZ = (a.y + b.y) * 0.5f;
                list.Add(new Span
                {
                    x0 = midX - (hx - hw),
                    x1 = midX + (hx - hw),
                    zStand = midZ + hw + sd * 0.35f,
                    zPara = midZ + hw + sd + 0.25f,
                });
            }
            return list;
        }

        internal static List<(Vector3 origin, Vector3 dir)> FiringRays()
        {
            var rays = new List<(Vector3, Vector3)>();
            var dir = new Vector3(0f, 0.08f, 1f).normalized;
            const float period = 3.5f, gap = 1f;
            foreach (var span in FiringSpans())
            {
                bool any = false;
                for (float s = span.x0; s + (period - gap) <= span.x1 - 0.15f; s += period)
                {
                    float desired = s + (period - gap) + gap * 0.5f;
                    if (desired > span.x1 - 0.45f) continue;
                    float x = ClearX(desired, span.x0 + 0.55f, span.x1 - 0.55f, span.zStand);
                    if (float.IsNaN(x)) continue;
                    if (rays.Exists(r => Mathf.Abs(r.Item1.x - x) < 0.8f && Mathf.Abs(r.Item1.z - span.zStand) < 0.4f)) continue;
                    rays.Add((new Vector3(x, Sample(new Vector2(x, span.zStand)) + TrenchTerrainEditorUtility.EyeHeight, span.zStand), dir));
                    any = true;
                }
                if (any) continue;
                for (float x = span.x0 + 0.6f; x <= span.x1 - 0.6f; x += 0.4f)
                {
                    if (LaneBlocked(x, span.zStand)) continue;
                    rays.Add((new Vector3(x, Sample(new Vector2(x, span.zStand)) + TrenchTerrainEditorUtility.EyeHeight, span.zStand), dir));
                    break;
                }
            }
            return rays;
        }

        static float ClearX(float desired, float x0, float x1, float z)
        {
            if (desired >= x0 && desired <= x1 && !LaneBlocked(desired, z)) return desired;
            for (float d = 0.25f; d < x1 - x0; d += 0.25f)
            {
                if (desired - d >= x0 && !LaneBlocked(desired - d, z)) return desired - d;
                if (desired + d <= x1 && !LaneBlocked(desired + d, z)) return desired + d;
            }
            return float.NaN;
        }

        static bool LaneBlocked(float x, float z0)
        {
            foreach (var b in BridgeBounds())
            {
                if (x < b.min.x - 0.4f || x > b.max.x + 0.4f) continue;
                if (z0 > b.max.z || z0 + 20f < b.min.z) continue;
                return true;
            }
            return false;
        }

        static void Parapets(Transform root)
        {
            var rays = FiringRays();
            int piece = 0;
            foreach (var span in FiringSpans())
            {
                var gaps = new List<float>();
                foreach (var ray in rays)
                    if (Mathf.Abs(ray.origin.z - span.zStand) < 0.3f) gaps.Add(ray.origin.x);
                gaps.Sort();
                float cursor = span.x0;
                foreach (var gapX in gaps)
                {
                    float g0 = gapX - 0.5f, g1 = gapX + 0.5f;
                    if (g0 - cursor > 0.7f) piece = FillRun(cursor, g0, span.zPara, root, piece);
                    var rest = SpawnFort(Sandbag, root, "GiaSung");
                    Sit(rest, new Vector2(gapX, span.zStand + 0.15f), 0f);
                    cursor = Mathf.Max(cursor, g1);
                }
                if (span.x1 - cursor > 0.7f) piece = FillRun(cursor, span.x1, span.zPara, root, piece);
            }
        }

        static int FillRun(float x0, float x1, float z, Transform parent, int piece)
        {
            float len = x1 - x0;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 2.6f));
            float pieceLen = len / n;
            for (int i = 0; i < n; i++)
            {
                string path = piece % 3 == 0 ? Barricade : piece % 3 == 1 ? Barricade2 : Sandbox;
                piece++;
                float a = x0 + i * pieceLen, b = a + pieceLen;
                var go = SpawnFort(path, parent, "BocBaoCat");
                var axes = PrefabAxes(path, parent);
                FitSpanX(go, axes, a + 0.04f, b - 0.04f, z, Sample(new Vector2((a + b) * 0.5f, z)));
            }
            return piece;
        }

        static void FitSpanX(GameObject go, MeshAxes axes, float x0, float x1, float z, float groundY)
        {
            ScaleAxis(go, axes.longAxis, axes.Size(axes.longAxis), Mathf.Max(0.4f, x1 - x0));
            if (axes.Size(axes.upAxis) > 0.85f)
                ScaleAxis(go, axes.upAxis, axes.Size(axes.upAxis), 0.8f);
            if (axes.Size(axes.thickAxis) > 1.4f)
                ScaleAxis(go, axes.thickAxis, axes.Size(axes.thickAxis), 1.2f);
            float cx = (x0 + x1) * 0.5f;
            go.transform.SetPositionAndRotation(new Vector3(cx, groundY, z), AxesRotation(axes, Vector3.right, Vector3.up));
            var b = BoundsOf(go);
            go.transform.position += new Vector3(cx - b.center.x, groundY - b.min.y, z - b.center.z);
            b = BoundsOf(go);
            if (b.min.z < z - 0.15f)
                go.transform.position += new Vector3(0f, 0f, (z - 0.15f) - b.min.z);
            Stamp(go);
        }

        static void FiringStepWood(Transform root)
        {
            var fenceAxes = PrefabAxes(Fence1, root);
            var palletAxes = PrefabAxes(Pallet, root);
            float module = Mathf.Max(0.8f, fenceAxes.Size(fenceAxes.longAxis));
            foreach (var span in FiringSpans())
            {
                float faceZ = span.zStand + 0.05f;
                for (float x = span.x0 + module * 0.5f; x < span.x1; x += module)
                {
                    var plank = SpawnFort(Fence1, root, "BeBan");
                    ScaleAxis(plank, fenceAxes.upAxis, fenceAxes.Size(fenceAxes.upAxis), 0.48f);
                    plank.transform.rotation = AxesRotation(fenceAxes, Vector3.right, Vector3.up);
                    Sit(plank, new Vector2(x, faceZ), plank.transform.eulerAngles.y);
                    ShoveFromRoute(plank, 0.6f);
                }
                float depth = palletAxes.size.z;
                float tile = Mathf.Max(0.8f, palletAxes.size.x * 0.95f);
                for (float x = span.x0 + tile * 0.5f; x < span.x1; x += tile)
                {
                    var go = SpawnFort(Pallet, root, "SanBeBan");
                    ScaleAxis(go, 2, depth, 0.55f);
                    Sit(go, new Vector2(x, span.zStand), 0f);
                    ShoveFromRoute(go, 0.6f);
                }
            }
        }

        static void WireLine(Transform root)
        {
            var rays = FiringRays();
            float wireLen = Mathf.Max(0.8f, LongSize(Wire, root));
            float step = wireLen * 0.9f;
            float gapRadius = wireLen * 0.5f + 0.5f;
            var placed = new List<Vector2>();
            var route = TrenchTerrainEditorUtility.Route;

            void TryPlace(Vector2 p, Vector2 along, bool enemySide)
            {
                if (TrenchTerrainEditorUtility.InsideCraterFloor(p)) return;
                if (NearBridge(p, Mathf.Max(1.6f, gapRadius))) return;
                if (enemySide)
                {
                    foreach (var ray in rays)
                        if (Mathf.Abs(ray.origin.x - p.x) < gapRadius && Mathf.Abs(ray.origin.z - p.y) < 6f) return;
                }
                foreach (var q in placed)
                    if (Vector2.Distance(q, p) < step * 0.65f) return;
                var go = SpawnFort(Wire, root, "DayThepGai");
                float yaw = Mathf.Atan2(along.x, along.y) * Mathf.Rad2Deg;
                Sit(go, p, yaw);
                StripColliders(go);
                placed.Add(p);
            }

            for (int i = 0; i < route.Length - 1; i++)
            {
                Vector2 a = route[i], b = route[i + 1], delta = b - a;
                float len = delta.magnitude;
                Vector2 dir = delta / len;
                Vector2 side = new Vector2(-dir.y, dir.x);
                for (float s = step * 0.5f; s < len; s += step)
                {
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        Vector2 outward = side * sign;
                        TryPlace(a + dir * s + outward * 2.3f, dir, outward.y > 0.5f);
                    }
                }
            }

            void Bay(Vector2 center, Vector2 half, Vector2 openDir)
            {
                float inset = 1.2f;
                void Edge(Vector2 outward)
                {
                    if (Vector2.Dot(outward, openDir) > 0.5f) return;
                    Vector2 along = new Vector2(-outward.y, outward.x);
                    float length = (Mathf.Abs(along.x) > 0.5f ? half.x : half.y) * 2f;
                    float edge = (Mathf.Abs(outward.x) > 0.5f ? half.x : half.y) + inset;
                    for (float s = -length * 0.5f; s <= length * 0.5f; s += step)
                        TryPlace(center + outward * edge + along * s, along, outward.y > 0.5f);
                }
                Edge(Vector2.up);
                Edge(Vector2.down);
                Edge(Vector2.left);
                Edge(Vector2.right);
            }
            Bay(TrenchTerrainEditorUtility.Bays[0].center, TrenchTerrainEditorUtility.Bays[0].half, Vector2.up);
            Bay(TrenchTerrainEditorUtility.Bays[1].center, TrenchTerrainEditorUtility.Bays[1].half, Vector2.down);
        }

        static void Breaches(Transform root)
        {
            float hw = TrenchTerrainEditorUtility.HalfWidth;
            float run = TrenchTerrainEditorUtility.RampRun;
            foreach (var (at, dir, _) in TrenchTerrainEditorUtility.Breaches)
            {
                Vector2 p = at + dir * (hw + run * 0.55f);
                float y = Sample(p);
                var blocker = new GameObject("BreachBlocker");
                blocker.transform.SetParent(Category(root, "BreachBlocker"), false);
                blocker.transform.SetPositionAndRotation(
                    new Vector3(p.x, y + 1.1f, p.y),
                    Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y), Vector3.up));
                blocker.AddComponent<BoxCollider>().size = new Vector3(2.4f, 2.5f, 0.5f);

                Vector2 side = new Vector2(-dir.y, dir.x);
                var wire = SpawnFort(Wire, root, "DayThepGai");
                Sit(wire, at + dir * (hw + run + 0.35f), Mathf.Atan2(side.x, side.y) * Mathf.Rad2Deg);
                StripColliders(wire);

                for (int k = 0; k < 3; k++)
                {
                    Vector2 hp = at + dir * (hw + run + 1.1f + 0.35f * k) + side * ((k - 1) * 0.75f);
                    var hog = SpawnFort(Hedgehog, root, "ChongSat");
                    Sit(hog, hp, Rand(0f, 180f));
                }
            }
        }

        static void MgNest(Transform root)
        {
            var nest = Nest();
            var gun = SpawnFort(Lmg, root, "SungMay");
            float extent = BoundsOf(gun).size.magnitude;
            if (extent > 2.2f || extent < 0.4f) gun.transform.localScale *= 1.4f / Mathf.Max(0.2f, extent);
            Sit(gun, nest.Center + Vector2.left * 0.45f, -90f);
            KeepOffPoint(gun, nest.Center, 0.55f);

            Sit(SpawnFort(Barricade, root, "BocSungMay"), new Vector2(nest.Center.x, nest.Center.y + nest.Half.y + 0.45f), 0f);
            Sit(SpawnFort(Barricade, root, "BocSungMay"), new Vector2(nest.Center.x, nest.Center.y - nest.Half.y - 0.45f), 0f);
            var sideBag = SpawnFort(Barricade2, root, "BocSungMay");
            Sit(sideBag, new Vector2(nest.Center.x + nest.Half.x + 0.55f, nest.Center.y), 90f);
            foreach (var bag in root.GetComponentsInChildren<Transform>())
                if (bag.name == "BocSungMay") KeepOffPoint(bag.gameObject, nest.Center, 0.7f);
        }

        static void MortarPit(Transform root)
        {
            var at = new Vector2(7.4f, 41.5f);
            var mortar = SpawnFort(Mortar, root, "Coi");
            Sit(mortar, at, 15f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                var bag = SpawnFort(Sandbag, root, "VongCoi");
                Sit(bag, at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1.25f, -a * Mathf.Rad2Deg);
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f + 0.4f;
                var shell = SpawnFort(Casing, root, "VoDan");
                Sit(shell, at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1.7f, Rand(0f, 360f));
                StripColliders(shell);
            }
            for (int i = 0; i < 2; i++)
            {
                var box = SpawnFort(AmmoBox, root, "ThungDan");
                OpenLid(box);
                Sit(box, at + new Vector2(-1.6f + i * 0.8f, -1.5f), 20f * i);
                ShoveFromRoute(box, 0.8f);
            }
        }

        static void ScatterHedgehogs(Transform root)
        {
            int placed = 0, guard = 0;
            while (placed < 6 && guard++ < 4000)
            {
                var p = new Vector2(Rand(-40f, 40f), Rand(-55f, 70f));
                float d = TrenchTerrainEditorUtility.DistanceFromTrench(p);
                if (d < 6f || d > 28f) continue;
                if (TrenchTerrainEditorUtility.InsideCraterFloor(p)) continue;
                if (NearPath(p, 2.5f) || InLane(p, 2f) || NearBridge(p, 4f)) continue;
                var go = SpawnFort(Hedgehog, root, "ChongSat");
                Sit(go, p, Rand(0f, 180f));
                placed++;
            }
        }

        // ───────────────────────────── Bước 4 ─────────────────────────────

        static void BuildStory(Transform root)
        {
            DeadForest(root);
            Debris(root);
            int green = CountGreen();
            if (green > 0)
                Debug.LogWarning($"[TrenchDressing] Có {green} cây xanh PT_Pine_Tree_03_green trong scene. Vùng giao tranh chỉ dùng cây chết.");
        }

        static void DeadForest(Transform root)
        {
            var points = new List<Vector2>();
            int guard = 0;
            while (points.Count < 57 && guard++ < 30000)
            {
                var p = new Vector2(Rand(-55f, 50f), Rand(-80f, 85f));
                float d = TrenchTerrainEditorUtility.DistanceFromTrench(p);
                if (d < 3f || d > 40f) continue;
                if (TrenchTerrainEditorUtility.InsideCraterFloor(p) || NearPath(p, 2.2f) || NearBridge(p, 3.5f)) continue;
                if (InLane(p, 2.2f)) continue;
                bool close = false;
                foreach (var q in points)
                    if (Vector2.Distance(p, q) < 3f) { close = true; break; }
                if (close) continue;
                points.Add(p);
            }
            if (points.Count < 57)
                Debug.LogWarning($"[TrenchDressing] Chỉ rải được {points.Count}/57 cây chết.");

            for (int i = 0; i < points.Count; i++)
            {
                string path = i < 25 ? (i % 2 == 0 ? TreeDead : TreeCut) : i < 45 ? Stump : Logs;
                var go = Spawn(path, root);
                float yaw = Rand(0f, 360f);
                var tilt = Quaternion.Euler(Rand(-8f, 8f), yaw, Rand(-8f, 8f));
                float y = Sample(points[i]);
                go.transform.SetPositionAndRotation(new Vector3(points[i].x, y, points[i].y), tilt);
                var b = BoundsOf(go);
                go.transform.position += new Vector3(points[i].x - b.center.x, y - b.min.y, points[i].y - b.center.z);
                Stamp(go);
            }
        }

        static void Debris(Transform root)
        {
            var nest = Nest();
            var mortar = new Vector2(7.4f, 41.5f);
            ScatterSmall(Casing, nest.Center, 6, 1.3f, 2.2f, root, "VoDan");
            ScatterSmall(Casing, mortar, 4, 2.1f, 2.8f, root, "VoDan");

            var rifles = new[] { RifleA, RifleB, RifleC };
            var route = TrenchTerrainEditorUtility.Route;
            int[] rifleCorners = { 2, 4, 7, 10 };
            int[] grenadeCorners = { 3, 6, 9 };
            int[] helmetCorners = { 1, 5, 8, 11 };
            for (int i = 0; i < rifleCorners.Length; i++)
                LayNearCorner(rifles[i % rifles.Length], route[rifleCorners[i]], rifleCorners[i], 1.15f, root, "SungHong");
            var grenades = new[] { GrenadeE, GrenadeB };
            for (int i = 0; i < grenadeCorners.Length; i++)
                LayNearCorner(grenades[i % grenades.Length], route[grenadeCorners[i]], grenadeCorners[i], 0.28f, root, "LuuDan");
            foreach (int index in helmetCorners)
                HelmetNear(route[index], index, root);

            var ammoSpots = new[] { new Vector2(-1.7f, -38.5f), new Vector2(8.2f, 44.2f), new Vector2(3.0f, 22.3f) };
            foreach (var spot in ammoSpots)
            {
                var box = Spawn(AmmoBox, root);
                box.name = "ThungDan";
                OpenLid(box);
                Sit(box, spot, Rand(0f, 360f));
                ShoveFromRoute(box, 0.8f);
                KeepOffShelters(box);
            }

            StackBoxes(new Vector2(2.1f, -43.4f), 10f, 1, root);
            StackBoxes(new Vector2(-1.9f, -43.6f), 40f, 3, root);
            StackBoxes(new Vector2(9.0f, 45.6f), 70f, 5, root);
            StackBoxes(new Vector2(2.7f, 22.15f), 15f, 2, root);
        }

        static void ScatterSmall(string path, Vector2 center, int count, float minR, float maxR, Transform parent, string name)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Rand(0f, Mathf.PI * 2f);
                float r = Rand(minR, maxR);
                var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (RouteDistance(p) < 0.5f || InLane(p, 1.2f)) continue;
                var go = Spawn(path, parent);
                go.name = name;
                Sit(go, p, Rand(0f, 360f));
                StripColliders(go);
            }
        }

        static void LayNearCorner(string path, Vector2 corner, int index, float length, Transform parent, string name)
        {
            Vector2 p = OffsetFromRoute(corner, index, 0.8f);
            if (InLane(p, 1.2f)) p = OffsetFromRoute(corner, index + 3, 0.9f);
            var axes = PrefabAxes(path, parent);
            var go = Spawn(path, parent);
            go.name = name;
            float current = axes.Size(axes.longAxis);
            if (current > 0.01f) go.transform.localScale *= length / current;
            var rot = Quaternion.Euler(0f, Rand(0f, 360f), 0f) * AxesRotation(axes, Vector3.right, Vector3.up);
            float y = Sample(p);
            go.transform.SetPositionAndRotation(new Vector3(p.x, y, p.y), rot);
            var b = BoundsOf(go);
            go.transform.position += new Vector3(p.x - b.center.x, y - b.min.y - b.size.y * 0.5f, p.y - b.center.z);
            StripColliders(go);
            Stamp(go);
        }

        static void HelmetNear(Vector2 corner, int index, Transform parent)
        {
            Vector2 p = OffsetFromRoute(corner, index, 0.75f);
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "MuSat";
            go.transform.SetParent(parent, false);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.28f, 0.14f, 0.32f);
            go.GetComponent<MeshRenderer>().sharedMaterial = HelmetMaterial();
            float y = Sample(p);
            go.transform.SetPositionAndRotation(new Vector3(p.x, y - 0.07f, p.y), Quaternion.Euler(0f, Rand(0f, 360f), Rand(-8f, 8f)));
            Stamp(go);
        }

        static void StackBoxes(Vector2 p, float yaw, int first, Transform parent)
        {
            var boxes = new List<GameObject>();
            float cursor = Sample(p);
            for (int i = 0; i < 2; i++)
            {
                var go = Spawn($"Assets/Defensive_props/Prefabs/boxes/box{(first + i - 1) % 6 + 1}.prefab", parent);
                go.name = "HomGo";
                go.transform.rotation = Quaternion.Euler(0f, yaw + i * 8f, 0f);
                go.transform.position = new Vector3(p.x, cursor, p.y);
                var b = BoundsOf(go);
                go.transform.position += new Vector3(p.x - b.center.x, cursor - b.min.y, p.y - b.center.z);
                Stamp(go);
                boxes.Add(go);
                cursor = BoundsOf(go).max.y;
            }
            var before = boxes[0].transform.position;
            ShoveFromRoute(boxes[0], 0.85f);
            KeepOffShelters(boxes[0]);
            var delta = boxes[0].transform.position - before;
            for (int i = 1; i < boxes.Count; i++) boxes[i].transform.position += delta;
        }

        static void OpenLid(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>())
            {
                if (t == go.transform) continue;
                string n = t.name.ToLowerInvariant();
                if (n.Contains("lid") || n.Contains("cover") || n.Contains("nap"))
                    t.localRotation = Quaternion.Euler(-110f, 0f, 0f);
            }
        }

        // ───────────────────────────── Bước 5 ─────────────────────────────

        static void BuildAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.016f;
            RenderSettings.fogColor = Hex("8C8478");
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Hex("6E6A64");

            Light sun = null;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (light.type == LightType.Directional) { sun = light; break; }
            if (!sun)
            {
                sun = new GameObject("Directional Light").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.shadows = LightShadows.Soft;
            sun.intensity = 1f;
            sun.color = Hex("FAF0E0");
            sun.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(FreezeProfilePath);
            if (!profile) throw new InvalidOperationException("Thiếu " + FreezeProfilePath);
            SceneBuilder.CreateGameSystems(profile);
        }

        // ───────────────────────────── Bản đồ ─────────────────────────────

        static Material MapMaterial()
        {
            const string dir = "Assets/_Project/Art/Props";
            const string texPath = dir + "/T_BanDoTacChien.png";
            const string matPath = dir + "/M_BanDoTacChien.mat";
            Directory.CreateDirectory(dir);
            var image = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            PaintMap(image);
            File.WriteAllBytes(texPath, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(texPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            return UrpMaterialUtility.EnsureUrpLitMaterial(matPath, mat =>
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", 0.2f);
            });
        }

        static Material HelmetMaterial()
        {
            const string path = "Assets/_Project/Art/Materials/MuSat.mat";
            return UrpMaterialUtility.EnsureUrpLitMaterial(path, mat =>
            {
                mat.SetColor("_BaseColor", new Color(0.22f, 0.26f, 0.16f));
                mat.SetFloat("_Smoothness", 0.35f);
            });
        }

        static void PaintMap(Texture2D tex)
        {
            var paper = Hex("C4B48A");
            var ink = Hex("4A3B28");
            var red = Hex("8C2A22");
            var blue = Hex("2A3F6E");
            var grid = Hex("A89468");
            for (int y = 0; y < 512; y++)
            for (int x = 0; x < 512; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.02f, y * 0.02f);
                var c = Color.Lerp(paper, Hex("B09A72"), n * 0.4f);
                int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(511 - x, 511 - y));
                if (edge < 18) c = Color.Lerp(ink, c, edge / 18f);
                tex.SetPixel(x, y, c);
            }
            for (int i = 48; i < 480; i += 32)
            {
                Stroke(tex, i, 36, i, 476, grid);
                Stroke(tex, 36, i, 476, i, grid);
            }
            for (int k = 0; k < 4; k++)
            {
                int prev = -1;
                for (int x = 70; x < 450; x++)
                {
                    int y = 120 + k * 70 + (int)(18f * Mathf.Sin(x * 0.03f + k));
                    if (prev >= 0) Stroke(tex, x - 1, prev, x, y, ink);
                    prev = y;
                }
            }
            for (int x = 50; x < 470; x++)
            {
                int y = 300 + (int)(26f * Mathf.Sin(x * 0.018f));
                Disk(tex, x, y, 3, blue);
            }
            Arrow(tex, 100, 90, 210, 230, red);
            Arrow(tex, 400, 430, 270, 300, red);
            Arrow(tex, 150, 400, 230, 320, blue);
            Disk(tex, 230, 250, 7, red);
            Disk(tex, 170, 360, 7, blue);
            Label(tex, "MUONG THANH", 196, 228, ink);
            Label(tex, "HIM LAM", 148, 372, ink);
            Label(tex, "N", 450, 450, ink);
            tex.Apply();
        }

        static void Stroke(Texture2D tex, int x0, int y0, int x1, int y1, Color color)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                if (x >= 0 && y >= 0 && x < tex.width && y < tex.height) tex.SetPixel(x, y, color);
            }
        }

        static void Disk(Texture2D tex, int cx, int cy, int r, Color color)
        {
            for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
                if (x * x + y * y <= r * r) Stroke(tex, cx + x, cy + y, cx + x, cy + y, color);
        }

        static void Arrow(Texture2D tex, int x0, int y0, int x1, int y1, Color color)
        {
            Stroke(tex, x0, y0, x1, y1, color);
            var dir = new Vector2(x1 - x0, y1 - y0).normalized;
            var side = new Vector2(-dir.y, dir.x);
            var tip = new Vector2(x1, y1);
            var left = tip - dir * 16f + side * 7f;
            var right = tip - dir * 16f - side * 7f;
            Stroke(tex, x1, y1, Mathf.RoundToInt(left.x), Mathf.RoundToInt(left.y), color);
            Stroke(tex, x1, y1, Mathf.RoundToInt(right.x), Mathf.RoundToInt(right.y), color);
        }

        static void Label(Texture2D tex, string text, int x, int y, Color color)
        {
            for (int i = 0; i < text.Length; i++)
            {
                var rows = Glyph(text[i]);
                for (int row = 0; row < rows.Length; row++)
                for (int col = 0; col < 5; col++)
                    if ((rows[row] & (1 << (4 - col))) != 0)
                        Disk(tex, x + i * 7 + col, y + (6 - row), 0, color);
            }
        }

        static byte[] Glyph(char c)
        {
            switch (c)
            {
                case 'M': return new byte[] { 0x11, 0x1B, 0x15, 0x11, 0x11, 0x11, 0x11 };
                case 'U': return new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E };
                case 'O': return new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E };
                case 'N': return new byte[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 };
                case 'G': return new byte[] { 0x0E, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0E };
                case 'T': return new byte[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 };
                case 'H': return new byte[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 };
                case 'A': return new byte[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 };
                case 'I': return new byte[] { 0x0E, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E };
                case 'L': return new byte[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F };
                case ' ': return new byte[] { 0, 0, 0, 0, 0, 0, 0 };
                default: return new byte[] { 0x1F, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1F };
            }
        }

        // ───────────────────────────── Kiểm tra ─────────────────────────────

        internal static void AppendValidation(Action<bool, string> check, Func<Vector2, float> height)
        {
            if (!GameObject.Find(StructureRoot))
            {
                check(true, "Bước 2–5 chưa dựng — bỏ qua kiểm tra công sự");
                return;
            }
            Physics.SyncTransforms();

            check(GameObject.Find(StructureRoot).transform.childCount > 10, "Trench_Structure có đồ đạc");
            var fort = GameObject.Find(FortRoot);
            check(fort && fort.transform.childCount > 10, "Trench_Fortifications có đồ đạc");
            var story = GameObject.Find(StoryRoot);
            check(story && story.transform.childCount > 20, "Trench_Storytelling có rừng chết và hiện vật");

            foreach (var shelter in TrenchTerrainEditorUtility.Shelters)
            {
                float h = height(shelter.Center);
                check(Mathf.Abs(h - shelter.Floor) < 0.3f, $"{shelter.Name}: cao độ {h:0.00} m (kỳ vọng {shelter.Floor:0.00})");
                bool blocked = CapsuleBlocked(new Vector3(shelter.Center.x, h, shelter.Center.y), out var who);
                check(!blocked, $"{shelter.Name}: chỗ đứng {(blocked ? "kẹt " + who : "trống")}");
            }

            int blockers = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (t.name == "BreachBlocker") blockers++;
            check(blockers == TrenchTerrainEditorUtility.Breaches.Length, $"BreachBlocker x{blockers}");

            float hw = TrenchTerrainEditorUtility.HalfWidth;
            float run = TrenchTerrainEditorUtility.RampRun;
            for (int i = 0; i < TrenchTerrainEditorUtility.Breaches.Length; i++)
            {
                var (at, dir, _) = TrenchTerrainEditorUtility.Breaches[i];
                Vector2 p0 = at + dir * (hw * 0.25f);
                Vector2 p1 = at + dir * (hw + run + 1.4f);
                var a = new Vector3(p0.x, height(p0) + 0.45f, p0.y);
                var b = new Vector3(p1.x, height(p1) + 0.45f, p1.y);
                var delta = b - a;
                bool hitBlocker = Physics.SphereCast(a, 0.28f, delta.normalized, out var hit, delta.magnitude, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider.gameObject.name == "BreachBlocker";
                string struck = hit.collider != null ? " (trúng " + hit.collider.name + ")" : "";
                check(hitBlocker, $"Lối vỡ {i + 1}: {(hitBlocker ? "chặn bởi BreachBlocker" : "không chạm BreachBlocker" + struck)}");
            }

            var rays = FiringRays();
            check(rays.Count >= FiringSpans().Count, $"Khe bắn: {rays.Count} (mỗi bệ một khe)");
            foreach (var ray in rays)
            {
                bool blocked = Physics.Raycast(ray.origin, ray.dir, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore);
                check(!blocked, blocked
                    ? $"Khe bắn ({ray.origin.x:0.0}, {ray.origin.z:0.0}) bị {hit.collider.name} chặn ở {hit.distance:0.0} m"
                    : $"Khe bắn ({ray.origin.x:0.0}, {ray.origin.z:0.0}) thông 20 m");
            }

            foreach (var name in BridgeNames)
            {
                var go = GameObject.Find(name);
                if (!go) { check(false, $"{name} chưa đặt"); continue; }
                float minY = BoundsOf(go).min.y;
                check(minY >= 10.04f, $"{name}: đáy {minY:0.00} m (>= 10,05)");
            }

            check(CountGreen() == 0, "Không có cây xanh quanh hào");

            var fog = Hex("8C8478");
            check(RenderSettings.fog && RenderSettings.fogMode == FogMode.ExponentialSquared
                && Mathf.Abs(RenderSettings.fogDensity - 0.016f) < 0.0008f
                && ColorsClose(RenderSettings.fogColor, fog),
                $"Sương mù ExpSquared {RenderSettings.fogDensity:0.000} {RenderSettings.fogColor}");

            Light sun = null;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (light.type == LightType.Directional) { sun = light; break; }
            var expected = Quaternion.Euler(40f, -30f, 0f);
            check(sun && sun.shadows == LightShadows.Soft && Quaternion.Angle(sun.transform.rotation, expected) < 1.5f
                && ColorsClose(sun.color, Hex("FAF0E0")),
                "Nắng mềm 40° #FAF0E0");

            var volumeGo = GameObject.Find("FreezeVolume");
            var volume = volumeGo ? volumeGo.GetComponent<Volume>() : null;
            var freeze = Object.FindAnyObjectByType<WorldFreeze>();
            bool wired = false;
            if (freeze)
            {
                var prop = new SerializedObject(freeze).FindProperty("freezeVolume");
                wired = prop != null && prop.objectReferenceValue == volume;
            }
            check(volume && volume.isGlobal && volume.sharedProfile && wired, "FreezeVolume gắn FreezeVolumeProfile và WorldFreeze");
        }

        static bool CapsuleBlocked(Vector3 basePos, out string blocker)
        {
            float bottom = TrenchTerrainEditorUtility.PlayerSkin + TrenchTerrainEditorUtility.PlayerRadius + 0.05f;
            float top = TrenchTerrainEditorUtility.PlayerHeight - TrenchTerrainEditorUtility.PlayerRadius;
            var hits = new Collider[8];
            int count = Physics.OverlapCapsuleNonAlloc(
                basePos + Vector3.up * bottom, basePos + Vector3.up * top,
                TrenchTerrainEditorUtility.PlayerRadius, hits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!hits[i].CompareTag("Player")) { blocker = hits[i].name; return true; }
            blocker = null;
            return false;
        }

        static int CountGreen()
        {
            int green = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name.Contains("PT_Pine_Tree_03_green")) green++;
            return green;
        }

        static bool ColorsClose(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;

        // ───────────────────────────── Hình học dùng chung ─────────────────────────────

        static TrenchTerrainEditorUtility.Shelter Nest()
        {
            foreach (var shelter in TrenchTerrainEditorUtility.Shelters)
                if (shelter.Name == "MgNest") return shelter;
            throw new InvalidOperationException("Thiếu MgNest");
        }

        internal static bool InsideShelter(Vector2 p, float pad)
        {
            foreach (var shelter in TrenchTerrainEditorUtility.Shelters)
                if (TrenchTerrainEditorUtility.RectDistance(p, shelter.Center, shelter.Half) < pad) return true;
            return false;
        }

        static bool NearBreach(Vector2 p, float radius)
        {
            foreach (var (at, _, _) in TrenchTerrainEditorUtility.Breaches)
                if (Vector2.Distance(p, at) < radius) return true;
            return false;
        }

        internal static bool NearPath(Vector2 p, float radius)
        {
            foreach (var (at, dir, end) in TrenchTerrainEditorUtility.Breaches)
            {
                float along = TrenchTerrainEditorUtility.HalfWidth + TrenchTerrainEditorUtility.RampRun + TrenchTerrainEditorUtility.BreachCraterOffset;
                if (SegmentDistance(p, at + dir * along, end) < radius) return true;
            }
            foreach (var (a, b) in TrenchTerrainEditorUtility.ExtraMudPaths)
                if (SegmentDistance(p, a, b) < radius) return true;
            return false;
        }

        internal static bool NearBridge(Vector2 p, float radius)
        {
            foreach (var b in BridgeBounds())
            {
                var c = new Vector2(b.center.x, b.center.z);
                var extent = b.size.x >= b.size.z ? new Vector2(b.extents.x, 0f) : new Vector2(0f, b.extents.z);
                if (Vector2.Distance(p, c + extent) < radius || Vector2.Distance(p, c - extent) < radius) return true;
                if (p.x > b.min.x - radius && p.x < b.max.x + radius && p.y > b.min.z - radius && p.y < b.max.z + radius) return true;
            }
            return false;
        }

        internal static bool InLane(Vector2 p, float radius)
        {
            foreach (var ray in FiringRays())
            {
                var a = new Vector2(ray.origin.x, ray.origin.z);
                var step = new Vector2(ray.dir.x, ray.dir.z);
                if (step.sqrMagnitude < 1e-6f) continue;
                var b = a + step.normalized * 22f;
                if (SegmentDistance(p, a, b) < radius) return true;
            }
            return false;
        }

        internal static float RouteDistance(Vector2 p)
        {
            var route = TrenchTerrainEditorUtility.Route;
            float d = float.MaxValue;
            for (int i = 0; i < route.Length - 1; i++)
                d = Mathf.Min(d, SegmentDistance(p, route[i], route[i + 1]));
            return d;
        }

        static Vector2 OffsetFromRoute(Vector2 corner, int salt, float distance)
        {
            var route = TrenchTerrainEditorUtility.Route;
            int index = Mathf.Clamp(salt, 1, route.Length - 2);
            Vector2 prev = route[index] - route[index - 1];
            Vector2 side = new Vector2(-prev.y, prev.x).normalized;
            if ((salt & 1) == 0) side = -side;
            Vector2 p = corner + side * distance;
            if (RouteDistance(p) < 0.55f) p = corner - side * distance;
            return p;
        }

        internal static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        static void ShoveFromRoute(GameObject go, float minDist)
        {
            var route = TrenchTerrainEditorUtility.Route;
            for (int n = 0; n < 8; n++)
            {
                var b = BoundsOf(go);
                float best = float.MaxValue;
                Vector2 away = Vector2.right;
                for (int i = 0; i < route.Length - 1; i++)
                {
                    Vector2 q = Closest(new Vector2(b.center.x, b.center.z), route[i], route[i + 1]);
                    float dx = Mathf.Max(Mathf.Max(b.min.x - q.x, q.x - b.max.x), 0f);
                    float dz = Mathf.Max(Mathf.Max(b.min.z - q.y, q.y - b.max.z), 0f);
                    float dist = new Vector2(dx, dz).magnitude;
                    if (dist >= best) continue;
                    best = dist;
                    away = new Vector2(b.center.x, b.center.z) - q;
                    if (away.sqrMagnitude < 0.01f) away = Vector2.right;
                }
                if (best >= minDist) return;
                away.Normalize();
                go.transform.position += new Vector3(away.x, 0f, away.y) * (minDist - best + 0.06f);
            }
        }

        static void KeepOffPoint(GameObject go, Vector2 point, float radius)
        {
            for (int n = 0; n < 6; n++)
            {
                var b = BoundsOf(go);
                float cx = Mathf.Clamp(point.x, b.min.x, b.max.x);
                float cz = Mathf.Clamp(point.y, b.min.z, b.max.z);
                float dist = Vector2.Distance(point, new Vector2(cx, cz));
                if (dist >= radius) return;
                var away = new Vector2(b.center.x - point.x, b.center.z - point.y);
                if (away.sqrMagnitude < 0.01f) away = Vector2.right;
                away.Normalize();
                go.transform.position += new Vector3(away.x, 0f, away.y) * (radius - dist + 0.05f);
            }
        }

        static void KeepOffShelters(GameObject go)
        {
            foreach (var shelter in TrenchTerrainEditorUtility.Shelters)
                KeepOffPoint(go, shelter.Center, 0.65f);
        }

        static Vector2 Closest(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return a + ab * t;
        }

        internal static IEnumerable<Bounds> BridgeBounds()
        {
            foreach (var name in BridgeNames)
            {
                var go = GameObject.Find(name);
                if (go) yield return BoundsOf(go);
            }
        }

        internal static float Sample(Vector2 p)
        {
            var terrain = Terrain.activeTerrain;
            if (!terrain) throw new InvalidOperationException("Scene không có Terrain. Chạy Bước 1 trước.");
            return terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
        }

        static float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }

        // ───────────────────────────── Prefab ─────────────────────────────

        /// Nhóm các bản trùng tên dưới Trench_Fortifications. Prefab đo kích thước vẫn gắn tạm vào root.
        static Transform Category(Transform root, string objectName)
        {
            if (!FortCategory.TryGetValue(objectName, out var folder)) folder = objectName;
            var found = root.Find(folder);
            if (found) return found;
            var go = new GameObject(folder);
            go.transform.SetParent(root, false);
            return go.transform;
        }

        static GameObject SpawnFort(string path, Transform root, string objectName)
        {
            var go = Spawn(path, Category(root, objectName));
            go.name = objectName;
            return go;
        }

        static int CountPlaced(Transform root)
        {
            int count = 0;
            foreach (Transform child in root)
            {
                bool folder = child.GetComponent<Renderer>() == null && child.GetComponent<Collider>() == null && child.childCount > 0;
                count += folder ? child.childCount : 1;
            }
            return count;
        }

        static Transform RebuildRoot(string name)
        {
            var existing = GameObject.Find(name);
            if (existing) Object.DestroyImmediate(existing);
            return new GameObject(name).transform;
        }

        internal static GameObject Spawn(string path, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) throw new InvalidOperationException("Thiếu prefab " + path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            UrpMaterialUtility.Apply(go);
            return go;
        }

        static GameObject SpawnNamed(string path, Transform parent, string name)
        {
            var go = Spawn(path, parent);
            go.name = name;
            return go;
        }

        internal static GameObject Sit(GameObject go, Vector2 p, float yaw)
        {
            float y = Sample(p);
            go.transform.SetPositionAndRotation(new Vector3(p.x, y, p.y), Quaternion.Euler(0f, yaw, 0f));
            var b = BoundsOf(go);
            go.transform.position += new Vector3(p.x - b.center.x, y - b.min.y, p.y - b.center.z);
            Stamp(go);
            return go;
        }

        static void SinkTop(GameObject go, float topY)
        {
            var b = BoundsOf(go);
            go.transform.position += Vector3.up * (topY - b.max.y);
        }

        static void AlignLongAxis(GameObject go, MeshAxes axes, Vector3 direction, Vector3 up)
        {
            go.transform.rotation = AxesRotation(axes, direction, up);
        }

        static void ScaleAxis(GameObject go, int axis, float currentSize, float target)
        {
            var scale = go.transform.localScale;
            scale[axis] *= target / Mathf.Max(0.01f, currentSize);
            go.transform.localScale = scale;
        }

        static float LongSize(string path, Transform parent)
        {
            var axes = PrefabAxes(path, parent);
            return axes.Size(axes.longAxis);
        }

        static MeshAxes PrefabAxes(string path, Transform parent)
        {
            if (AxisCache.TryGetValue(path, out var cached)) return cached;
            var go = Spawn(path, parent);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var axes = AxesOf(go);
            Object.DestroyImmediate(go);
            AxisCache[path] = axes;
            Debug.Log($"[TrenchDressing] {Path.GetFileName(path)} size {axes.size}");
            return axes;
        }

        static MeshAxes AxesOf(GameObject go)
        {
            var size = BoundsOf(go).size;
            int longAxis = 0;
            if (size.y > size[longAxis]) longAxis = 1;
            if (size.z > size[longAxis]) longAxis = 2;
            int up = longAxis == 1 ? 0 : 1;
            float best = -1f;
            for (int i = 0; i < 3; i++)
            {
                if (i == longAxis) continue;
                float score = size[i] + (i == 1 ? 0.02f : 0f);
                if (score > best) { best = score; up = i; }
            }
            return new MeshAxes { size = size, longAxis = longAxis, upAxis = up, thickAxis = 3 - longAxis - up };
        }

        static Quaternion AxesRotation(MeshAxes axes, Vector3 lengthDir, Vector3 upDir)
        {
            if (lengthDir.sqrMagnitude < 1e-6f) lengthDir = Vector3.forward;
            lengthDir.Normalize();
            upDir = Vector3.ProjectOnPlane(upDir, lengthDir);
            if (upDir.sqrMagnitude < 1e-6f) upDir = Vector3.up;
            upDir.Normalize();

            var img = new Vector3[3];
            img[axes.longAxis] = lengthDir;
            img[axes.upAxis] = upDir;
            int third = axes.thickAxis;
            if (third == 2) img[2] = Vector3.Cross(img[0], img[1]);
            else if (third == 0) img[0] = Vector3.Cross(img[1], img[2]);
            else img[1] = Vector3.Cross(img[2], img[0]);
            if (img[third].sqrMagnitude < 1e-6f) return Quaternion.LookRotation(lengthDir, upDir);
            img[third].Normalize();

            var m = Matrix4x4.identity;
            m.SetColumn(0, new Vector4(img[0].x, img[0].y, img[0].z, 0f));
            m.SetColumn(1, new Vector4(img[1].x, img[1].y, img[1].z, 0f));
            m.SetColumn(2, new Vector4(img[2].x, img[2].y, img[2].z, 0f));
            return m.rotation;
        }

        internal static Bounds BoundsOf(GameObject go)
        {
            bool any = false;
            var bounds = new Bounds(go.transform.position, Vector3.zero);
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (!mesh) continue;
                var local = mesh.bounds;
                var matrix = filter.transform.localToWorldMatrix;
                foreach (var corner in Corners(local))
                {
                    var world = matrix.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(world, Vector3.zero); any = true; }
                    else bounds.Encapsulate(world);
                }
            }
            return bounds;
        }

        static IEnumerable<Vector3> Corners(Bounds bounds)
        {
            var c = bounds.center;
            var e = bounds.extents;
            for (int i = 0; i < 8; i++)
                yield return c + Vector3.Scale(e, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
        }

        internal static void StripColliders(GameObject go)
        {
            foreach (var collider in go.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(collider);
        }

        internal static void Stamp(GameObject go)
        {
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        static void EnsureScene()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.path == ScenePath) return;
            if (active.isDirty)
                throw new InvalidOperationException($"Scene '{active.name}' chưa lưu. Lưu lại rồi mở {ScenePath}.");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static void Save()
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }
}
