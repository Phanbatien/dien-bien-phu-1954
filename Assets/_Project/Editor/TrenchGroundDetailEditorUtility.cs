using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DBP.EditorTools
{
    /// Bước 6 của Trench Map: cỏ khô, sỏi và đá rải thành GameObject (cùng cách rừng chết).
    /// Terrain detail của URP trong project này không có shader (m_TerrainDetailLit = null) nên không vẽ được.
    /// Chạy sau Bước 1–5. Mỗi lần chạy xoá Trench_Ground rồi dựng lại.
    public static class TrenchGroundDetailEditorUtility
    {
        const string ScenePath = "Assets/_Project/Scenes/Trench Map.unity";
        const string DetailDir = "Assets/_Project/Art/Terrain/Details";
        internal const string GroundRoot = "Trench_Ground";
        const int Seed = 195406;

        const string SnpGrass1 = "Assets/SimpleNaturePack/Prefabs/Grass_01.prefab";
        const string SnpGrass2 = "Assets/SimpleNaturePack/Prefabs/Grass_02.prefab";
        const string PtGrass = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab";
        const string PtGrassTexture = "Assets/Polytope Studio/Lowpoly_Environments/Sources/Textures/PT_Grass_01.png";
        const string PtRock = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Generic_Rock_01.prefab";
        const string PtRockPile = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_River_Rock_Pile_02.prefab";
        static readonly string[] SnpRocks =
        {
            "Assets/SimpleNaturePack/Prefabs/Rock_01.prefab", "Assets/SimpleNaturePack/Prefabs/Rock_02.prefab",
            "Assets/SimpleNaturePack/Prefabs/Rock_03.prefab", "Assets/SimpleNaturePack/Prefabs/Rock_04.prefab",
            "Assets/SimpleNaturePack/Prefabs/Rock_05.prefab",
        };

        const int ShortGrassCount = 420, TallGrassCount = 90, PebbleCount = 280, RockCount = 50;
        const float RockSpacing = 2.5f, GrassSpacing = 1.35f, TallSpacing = 2.4f, PebbleSpacing = 1.1f;
        const float RockColliderHeight = 0.5f;
        const float LaneLength = 22f, LaneNear = 8f;

        static System.Random rng;

        [MenuItem("DBP/Trench Map/Bước 6 – Đá & cỏ")]
        public static void Build()
        {
            EnsureScene();
            rng = new System.Random(Seed);
            var terrain = Terrain.activeTerrain;
            if (!terrain) throw new InvalidOperationException("Scene không có Terrain. Chạy Bước 1 trước.");
            if (!GameObject.Find(TrenchDressingEditorUtility.StructureRoot))
                Debug.LogWarning("[TrenchGround] Chưa dựng Bước 2–5: cỏ và đá sẽ không né được cầu, bao cát và khe bắn.");

            var existing = GameObject.Find(GroundRoot);
            if (existing) Object.DestroyImmediate(existing);
            var root = new GameObject(GroundRoot).transform;

            EnsureMaterials();
            var lanes = Lanes();
            var distance = TrenchTerrainEditorUtility.TrenchDistanceField();
            var occupied = OccupiedPoints();
            var relics = RelicPoints();

            int rocks = ScatterRocks(root, distance, lanes, occupied);
            int pebbles = ScatterPebbles(root, distance, occupied);
            int shortGrass = ScatterGrass(root, distance, lanes, occupied, relics, shortTufts: true);
            int tallGrass = ScatterGrass(root, distance, lanes, occupied, relics, shortTufts: false);

            terrain.drawInstanced = true;
            terrain.drawTreesAndFoliage = true;
            EditorUtility.SetDirty(terrain);

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TrenchGround] Bước 6: {shortGrass} cỏ thấp, {tallGrass} cỏ cao, {pebbles} sỏi, {rocks} đá trong {GroundRoot}.");
        }

        // ───────────────────────────── Vật liệu ─────────────────────────────

        static Material straw, ochre, tall, stone;

        static void EnsureMaterials()
        {
            if (!AssetDatabase.IsValidFolder(DetailDir))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(DetailDir).Replace('\\', '/'), Path.GetFileName(DetailDir));
            straw = FlatMaterial("M_CoKho_Rom", Hex("8A7A45"));
            ochre = FlatMaterial("M_CoKho_Nau", Hex("6B5A35"));
            tall = TallGrassMaterial();
            stone = FlatMaterial("M_Soi", Hex("6E665A"));
        }

        static Material FlatMaterial(string name, Color color) =>
            UrpMaterialUtility.EnsureUrpLitMaterial($"{DetailDir}/{name}.mat", mat =>
            {
                mat.SetTexture("_BaseMap", null);
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Smoothness", 0.05f);
                mat.SetFloat("_Cull", 0f);
                mat.enableInstancing = true;
            });

        static Material TallGrassMaterial()
        {
            var texture = RecolorKeepAlpha(PtGrassTexture, $"{DetailDir}/T_CoKho_Cao.png", Hex("54462A"), Hex("A8955A"));
            return UrpMaterialUtility.EnsureUrpLitMaterial($"{DetailDir}/M_CoKho_Cao.mat", mat =>
            {
                mat.SetTexture("_BaseMap", texture);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", 0.05f);
                mat.SetFloat("_Surface", 0f);
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
                mat.SetFloat("_Cull", 0f);
                mat.enableInstancing = true;
            });
        }

        static Texture2D RecolorKeepAlpha(string sourcePath, string outPath, Color dark, Color light)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
            if (existing) return existing;

            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            image.LoadImage(File.ReadAllBytes(sourcePath));
            var pixels = image.GetPixels();
            float min = 1f, max = 0f;
            foreach (var c in pixels)
            {
                if (c.a < 0.5f) continue;
                min = Mathf.Min(min, c.grayscale);
                max = Mathf.Max(max, c.grayscale);
            }
            for (int i = 0; i < pixels.Length; i++)
            {
                var c = Color.Lerp(dark, light, Mathf.InverseLerp(min, max, pixels[i].grayscale));
                c.a = pixels[i].a;
                pixels[i] = c;
            }
            image.SetPixels(pixels);
            File.WriteAllBytes(outPath, image.EncodeToPNG());
            Object.DestroyImmediate(image);

            AssetDatabase.ImportAsset(outPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(outPath);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        // ───────────────────────────── Rải ─────────────────────────────

        static int ScatterGrass(Transform root, Func<Vector2, float> distance, List<(Vector2 a, Vector2 b)> lanes, List<Vector2> occupied, List<Vector2> relics, bool shortTufts)
        {
            int target = shortTufts ? ShortGrassCount : TallGrassCount;
            float spacing = shortTufts ? GrassSpacing : TallSpacing;
            var placed = new List<Vector2>();
            int guard = 0;
            while (placed.Count < target && guard++ < 40000)
            {
                var p = new Vector2(Rand(-55f, 55f), Rand(-80f, 88f));
                float d = distance(p);
                float minD = shortTufts ? 1.15f : 4f;
                if (d < minD || d > 40f) continue;
                if (TrenchTerrainEditorUtility.InsideCraterFloor(p)) continue;
                if (TrenchDressingEditorUtility.NearPath(p, 1.2f) || TrenchDressingEditorUtility.NearBridge(p, 2.5f)) continue;
                if (TrenchDressingEditorUtility.InsideShelter(p, 0.2f)) continue;
                float lane = LaneDistance(p, lanes, out float along);
                if (!shortTufts && lane < 1.5f) continue;
                if (shortTufts && lane < 1f && along < LaneNear) continue;
                if (TooClose(p, relics, 0.55f)) continue;

                float dry = 1f - MudHint(p, d);
                float clump = Mathf.PerlinNoise(p.x * 0.16f + 71f, p.y * 0.16f + 5f);
                if (clump < (shortTufts ? 0.38f : 0.5f)) continue;
                if (dry < 0.25f && rng.NextDouble() > 0.15) continue;
                if (d < 3f && rng.NextDouble() > 0.35) continue;
                if (TooClose(p, placed, spacing) || TooClose(p, occupied, 1.1f)) continue;
                placed.Add(p);
            }

            foreach (var p in placed)
            {
                string path = shortTufts
                    ? (rng.NextDouble() < 0.5 ? SnpGrass1 : SnpGrass2)
                    : PtGrass;
                var mat = shortTufts ? (rng.NextDouble() < 0.5 ? straw : ochre) : tall;
                float height = shortTufts ? Rand(0.28f, 0.5f) : Rand(0.45f, 0.7f);
                var go = PlaceMesh(path, p, height, heightAxis: true, root, "CoKho");
                Recolor(go, mat);
                TrenchDressingEditorUtility.StripColliders(go);
                occupied.Add(p);
            }
            if (placed.Count < target)
                Debug.LogWarning($"[TrenchGround] Chỉ rải được {placed.Count}/{target} khóm cỏ {(shortTufts ? "thấp" : "cao")}.");
            return placed.Count;
        }

        static int ScatterPebbles(Transform root, Func<Vector2, float> distance, List<Vector2> occupied)
        {
            var placed = new List<Vector2>();
            int guard = 0;
            while (placed.Count < PebbleCount && guard++ < 40000)
            {
                var p = new Vector2(Rand(-50f, 50f), Rand(-75f, 85f));
                float d = distance(p);
                bool floor = d < 0f && !TrenchDressingEditorUtility.InsideShelter(p, 0.1f)
                    && TrenchDressingEditorUtility.RouteDistance(p) > 0.7f
                    && TrenchDressingEditorUtility.RouteDistance(p) < TrenchTerrainEditorUtility.HalfWidth;
                bool rim = OnCraterRim(p);
                bool path = TrenchDressingEditorUtility.NearPath(p, 1.4f);
                bool scatter = d > 1.2f && d < 28f && MudHint(p, d) > 0.35f;
                if (!floor && !rim && !path && !scatter) continue;
                if (TrenchDressingEditorUtility.NearBridge(p, 2f)) continue;
                if (TrenchTerrainEditorUtility.InsideCraterFloor(p) && !rim) continue;
                if (TooClose(p, placed, PebbleSpacing) || TooClose(p, occupied, 0.7f)) continue;
                placed.Add(p);
            }

            foreach (var p in placed)
            {
                var go = PlaceMesh(SnpRocks[rng.Next(SnpRocks.Length)], p, Rand(0.08f, 0.18f), heightAxis: false, root, "Soi");
                Recolor(go, stone);
                TrenchDressingEditorUtility.StripColliders(go);
                occupied.Add(p);
            }
            if (placed.Count < PebbleCount)
                Debug.LogWarning($"[TrenchGround] Chỉ rải được {placed.Count}/{PebbleCount} viên sỏi.");
            return placed.Count;
        }

        static int ScatterRocks(Transform root, Func<Vector2, float> distance, List<(Vector2 a, Vector2 b)> lanes, List<Vector2> occupied)
        {
            var placed = new List<Vector2>();
            int guard = 0;
            while (placed.Count < RockCount && guard++ < 30000)
            {
                var p = new Vector2(Rand(-55f, 55f), Rand(-80f, 88f));
                float d = distance(p);
                if (d < 2.5f || d > 40f) continue;
                if (TrenchTerrainEditorUtility.InsideCraterFloor(p)) continue;
                if (TrenchDressingEditorUtility.NearPath(p, 1.8f) || TrenchDressingEditorUtility.NearBridge(p, 3f)) continue;
                if (LaneDistance(p, lanes, out _) < 2f) continue;
                if (TooClose(p, placed, RockSpacing) || TooClose(p, occupied, 2f)) continue;
                placed.Add(p);
            }

            foreach (var p in placed)
            {
                double pick = rng.NextDouble();
                string path = pick < 0.7 ? SnpRocks[rng.Next(SnpRocks.Length)] : pick < 0.85 ? PtRock : PtRockPile;
                bool big = rng.NextDouble() < 0.12;
                float size = big ? Rand(1.6f, 2.4f) : Rand(0.45f, 1.3f);
                size = Mathf.Min(size, (distance(p) - 0.5f) * 2f);
                var go = PlaceMesh(path, p, size, heightAxis: false, root, "Da");
                go.transform.rotation = Quaternion.Euler(Rand(-10f, 10f), Rand(0f, 360f), Rand(-10f, 10f)) * go.transform.rotation;
                var b = TrenchDressingEditorUtility.BoundsOf(go);
                go.transform.position += Vector3.down * b.size.y * Rand(0.1f, 0.2f);
                b = TrenchDressingEditorUtility.BoundsOf(go);
                if (b.size.y < RockColliderHeight) TrenchDressingEditorUtility.StripColliders(go);
                occupied.Add(p);
            }
            if (placed.Count < RockCount)
                Debug.LogWarning($"[TrenchGround] Chỉ rải được {placed.Count}/{RockCount} hòn đá.");
            return placed.Count;
        }

        static GameObject PlaceMesh(string path, Vector2 p, float targetSize, bool heightAxis, Transform root, string name)
        {
            var go = TrenchDressingEditorUtility.Spawn(path, root);
            go.name = name;
            KeepLod0(go);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var size = TrenchDressingEditorUtility.BoundsOf(go).size;
            float current = heightAxis ? Mathf.Max(0.05f, size.y) : Mathf.Max(0.05f, Mathf.Max(size.x, size.z));
            go.transform.localScale *= targetSize / current;
            TrenchDressingEditorUtility.Sit(go, p, Rand(0f, 360f));
            return go;
        }

        /// Prefab gói có LODGroup: giữ LOD0 để cỏ/đá luôn hiện trong Scene view, không bị cull khi camera editor xa.
        static void KeepLod0(GameObject go)
        {
            var lod = go.GetComponent<LODGroup>();
            if (lod) Object.DestroyImmediate(lod);
            var keep = new HashSet<Transform> { go.transform };
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.name.EndsWith("_LOD0") || filter.transform == go.transform)
                {
                    keep.Add(filter.transform);
                    filter.gameObject.SetActive(true);
                }
            }
            if (keep.Count <= 1) return;
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (keep.Contains(filter.transform)) continue;
                if (filter.name.Contains("_LOD")) Object.DestroyImmediate(filter.gameObject);
            }
        }

        static void Recolor(GameObject go, Material mat)
        {
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = mat;
        }

        // ───────────────────────────── Luật chỗ trống ─────────────────────────────

        static List<Vector2> OccupiedPoints()
        {
            var points = new List<Vector2>();
            foreach (var rootName in new[] { TrenchDressingEditorUtility.StructureRoot, TrenchDressingEditorUtility.FortRoot, TrenchDressingEditorUtility.StoryRoot })
            {
                var root = GameObject.Find(rootName);
                if (!root) continue;
                foreach (Transform child in root.transform)
                    CollectOccupied(child, points);
            }
            return points;
        }

        static void CollectOccupied(Transform child, List<Vector2> points)
        {
            if (child.name == "DayThepGai" || child.name == "BreachBlocker") return;
            bool folder = child.GetComponent<Renderer>() == null && child.GetComponent<Collider>() == null && child.childCount > 0;
            if (folder)
            {
                foreach (Transform nested in child) CollectOccupied(nested, points);
                return;
            }
            var c = TrenchDressingEditorUtility.BoundsOf(child.gameObject).center;
            points.Add(new Vector2(c.x, c.z));
        }

        static bool TooClose(Vector2 p, List<Vector2> points, float radius)
        {
            float r2 = radius * radius;
            foreach (var q in points)
                if ((p - q).sqrMagnitude < r2) return true;
            return false;
        }

        static bool OnCraterRim(Vector2 p)
        {
            foreach (var (c, r, _) in TrenchTerrainEditorUtility.AllCraters())
            {
                float dist = Vector2.Distance(p, c);
                if (dist > r * 0.95f && dist < r * 1.7f) return true;
            }
            return false;
        }

        static List<Vector2> RelicPoints()
        {
            var points = new List<Vector2>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t.name != "SungHong" && t.name != "MuSat" && t.name != "LuuDan") continue;
                points.Add(new Vector2(t.position.x, t.position.z));
            }
            return points;
        }

        /// Bùn gần đúng từ khoảng cách hào / hố / đường mòn — không đọc alphamap mỗi điểm.
        static float MudHint(Vector2 p, float trenchDistance)
        {
            float mud = 0f;
            if (trenchDistance < 3.5f) mud = Mathf.Max(mud, 1f - trenchDistance / 3.5f);
            foreach (var (c, r, _) in TrenchTerrainEditorUtility.AllCraters())
            {
                float dist = Vector2.Distance(p, c);
                if (dist < r * 2.2f) mud = Mathf.Max(mud, 1f - dist / (r * 2.2f));
            }
            if (TrenchDressingEditorUtility.NearPath(p, 1.6f)) mud = Mathf.Max(mud, 0.75f);
            return Mathf.Clamp01(mud);
        }

        static List<(Vector2 a, Vector2 b)> Lanes()
        {
            var lanes = new List<(Vector2, Vector2)>();
            foreach (var ray in TrenchDressingEditorUtility.FiringRays())
            {
                var a = new Vector2(ray.origin.x, ray.origin.z);
                var dir = new Vector2(ray.dir.x, ray.dir.z);
                if (dir.sqrMagnitude < 1e-6f) continue;
                lanes.Add((a, a + dir.normalized * LaneLength));
            }
            return lanes;
        }

        static float LaneDistance(Vector2 p, List<(Vector2 a, Vector2 b)> lanes, out float along)
        {
            float best = float.MaxValue;
            along = float.MaxValue;
            foreach (var (a, b) in lanes)
            {
                float dist = TrenchDressingEditorUtility.SegmentDistance(p, a, b);
                if (dist >= best) continue;
                best = dist;
                along = Vector2.Dot(p - a, (b - a).normalized);
            }
            return best;
        }

        // ───────────────────────────── Kiểm tra ─────────────────────────────

        internal static void AppendValidation(Action<bool, string> check)
        {
            var root = GameObject.Find(GroundRoot);
            if (!root)
            {
                check(true, "Bước 6 chưa dựng — bỏ qua kiểm tra đá & cỏ");
                return;
            }
            Physics.SyncTransforms();

            int grass = 0, pebbles = 0, rocks = 0;
            foreach (Transform child in root.transform)
            {
                if (child.name == "CoKho") grass++;
                else if (child.name == "Soi") pebbles++;
                else if (child.name == "Da") rocks++;
            }
            check(grass >= 200, $"Trench_Ground cỏ khô: {grass} (>= 200)");
            check(pebbles >= 80, $"Trench_Ground sỏi: {pebbles} (>= 80)");
            check(rocks >= 30, $"Trench_Ground đá: {rocks} (>= 30)");

            var distance = TrenchTerrainEditorUtility.TrenchDistanceField();
            var bridges = new List<Bounds>(TrenchDressingEditorUtility.BridgeBounds());
            int intoTrench = 0, onBridge = 0;
            string first = null;
            foreach (Transform child in root.transform)
            {
                if (child.name != "Da") continue;
                var b = TrenchDressingEditorUtility.BoundsOf(child.gameObject);
                bool hitsTrench = false;
                foreach (var corner in new[]
                {
                    new Vector2(b.min.x, b.min.z), new Vector2(b.min.x, b.max.z),
                    new Vector2(b.max.x, b.min.z), new Vector2(b.max.x, b.max.z), new Vector2(b.center.x, b.center.z),
                })
                    hitsTrench |= distance(corner) < TrenchTerrainEditorUtility.WallRun;
                if (hitsTrench) { intoTrench++; first ??= $"({b.center.x:0.0}, {b.center.z:0.0})"; }
                foreach (var bridge in bridges)
                    if (bridge.Intersects(b)) { onBridge++; break; }
            }
            check(intoTrench == 0, $"Đá chìa vào hào: {intoTrench}{(first != null ? " " + first : "")}");
            check(onBridge == 0, $"Đá chạm cầu: {onBridge}");

            var lanes = Lanes();
            int laneHits = 0;
            foreach (Transform child in root.transform)
            {
                if (child.name != "CoKho") continue;
                var p = new Vector2(child.position.x, child.position.z);
                float lane = LaneDistance(p, lanes, out float along);
                float h = TrenchDressingEditorUtility.BoundsOf(child.gameObject).size.y;
                if (h >= 0.4f && lane < 1.2f) laneHits++;
                else if (lane < 0.8f && along < LaneNear) laneHits++;
            }
            check(laneHits == 0, $"Cỏ trong khe bắn: {laneHits}");
        }

        static float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }

        static void EnsureScene()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.path == ScenePath) return;
            if (active.isDirty)
                throw new InvalidOperationException($"Scene '{active.name}' chưa lưu. Lưu lại rồi mở {ScenePath}.");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }
}
