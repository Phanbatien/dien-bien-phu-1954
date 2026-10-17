using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DBP.EditorTools
{
    /// Bước 1 của Trench Map: hào chữ chi, bệ bắn, hố bom, lối vỡ mép hào và sơn bùn/cỏ úa lên Terrain.
    /// Không đụng tới New Terrain.asset: Terrain trong scene được trỏ sang TrenchMap_TerrainData.asset.
    /// Menu DBP → Trench Map. Sau khi chạy có thể chải tay (Smooth Height / Paint Texture) cho tự nhiên hơn.
    public static class TrenchTerrainEditorUtility
    {
        const string ScenePath = "Assets/_Project/Scenes/Trench Map.unity";
        const string ArtDir = "Assets/_Project/Art/Terrain";
        const string DataPath = ArtDir + "/TrenchMap_TerrainData.asset";
        const string MudLayerPath = ArtDir + "/TL_BunDat.terrainlayer";
        const string GrassLayerPath = ArtDir + "/TL_CoUa.terrainlayer";
        const string SourceMudLayer = "Assets/Polytope Studio/Lowpoly_Demos/Environment_Free/Helpers/Ground_Layer_01.terrainlayer";
        const string SourceGrassLayer = "Assets/Polytope Studio/Lowpoly_Demos/Environment_Free/Helpers/Ground_Layer_02.terrainlayer";
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";

        internal const float TerrainSize = 200f, TerrainHeight = 30f;
        internal const int HeightRes = 1025, AlphaRes = 1024, DetailRes = 1024;
        internal static readonly Vector3 Origin = new Vector3(-100f, 0f, -100f);

        // Terrain không đào xuống dưới 0 được nên mặt đất tự nhiên đặt ở 10 m.
        internal const float Ground = 10f;
        internal const float Floor = 8.1f;
        internal const float HalfWidth = 1.1f;
        internal const float WallRun = 0.33f;
        internal const float StepHeight = 0.5f, StepDepth = 0.6f;
        internal const float RampRun = 1.8f, RampHalfWidth = 1.0f;

        // Khớp Player.prefab: CharacterController r = 0,35, skin 0,08, slope 45°, mắt ở 1,65 m.
        internal const float PlayerRadius = 0.35f, PlayerSkin = 0.08f, PlayerHeight = 1.8f, EyeHeight = 1.65f, SlopeLimit = 45f;

        /// Tim tuyến hào (x, z). Mỗi nhịp 9–15 m, bẻ góc 90°, hướng địch là +Z.
        internal static readonly Vector2[] Route =
        {
            new Vector2(0, -40), new Vector2(0, -28), new Vector2(10, -28), new Vector2(10, -14),
            new Vector2(-1, -14), new Vector2(-1, 1), new Vector2(9, 1), new Vector2(9, 13),
            new Vector2(-3, 13), new Vector2(-3, 26), new Vector2(6, 26), new Vector2(6, 40),
        };

        internal static readonly (Vector2 center, Vector2 half)[] Bays =
        {
            (new Vector2(0, -41.5f), new Vector2(2.5f, 2.5f)),   // hầm xuất phát
            (new Vector2(6, 43f), new Vector2(4f, 4f)),           // trận địa trung tâm
        };

        /// Hố bom ngoài vùng giữa hai tuyến: tâm (x, z), bán kính, độ sâu.
        static readonly (Vector2 c, float r, float depth)[] Craters =
        {
            (new Vector2(-15, -30), 2.5f, 0.8f), (new Vector2(22, -35), 3.0f, 1.0f),
            (new Vector2(-20, -5), 1.8f, 0.6f),  (new Vector2(25, -8), 2.8f, 1.1f),
            (new Vector2(-14, 24), 3.0f, 1.2f),  (new Vector2(20, 18), 2.2f, 0.7f),
            (new Vector2(-25, 35), 2.6f, 0.9f),  (new Vector2(18, 48), 3.0f, 1.0f),
            (new Vector2(0, 58), 2.4f, 0.8f),    (new Vector2(-10, 52), 1.8f, 0.6f),
            (new Vector2(30, 30), 2.5f, 0.9f),   (new Vector2(-30, 10), 2.9f, 1.0f),
        };

        /// Hố pháo phá vỡ mép hào: điểm trên tim hào, hướng ra ngoài, đường bùn dẫn đi tiếp.
        internal static readonly (Vector2 at, Vector2 dir, Vector2 pathEnd)[] Breaches =
        {
            (new Vector2(10, -21), Vector2.right, new Vector2(35, -2)),
            (new Vector2(4, 1), Vector2.down, new Vector2(-12, -24)),
            (new Vector2(-3, 19.5f), Vector2.left, new Vector2(-28, 40)),
        };
        internal const float BreachCraterRadius = 2.4f, BreachCraterDepth = 0.9f, BreachCraterOffset = 1.2f;
        const float RimFactor = 1.6f;

        internal static readonly (Vector2 a, Vector2 b)[] ExtraMudPaths =
        {
            (new Vector2(6, 47), new Vector2(12, 90)),
        };

        /// Hầm chữ A và ụ súng máy. Floor của ụ cao hơn đáy hào đúng bằng bệ bắn.
        internal readonly struct Shelter
        {
            public readonly string Name;
            public readonly Vector2 Center;
            public readonly Vector2 Half;
            public readonly float Floor;

            public Shelter(string name, Vector2 center, Vector2 half, float floor)
            {
                Name = name;
                Center = center;
                Half = half;
                Floor = floor;
            }
        }

        internal static readonly Shelter[] Shelters =
        {
            new Shelter("Niche_A", new Vector2(-1.6f, -34f), new Vector2(0.8f, 0.7f), Floor),
            new Shelter("Niche_B", new Vector2(-2.6f, -7f), new Vector2(0.8f, 0.7f), Floor),
            // Vách sau (-Z) của nhịp (-3, 26)–(6, 26). Mép bắc trùng mép hào nên cửa thông thẳng.
            new Shelter("Command", new Vector2(1.5f, 23.4f), new Vector2(1.75f, 1.5f), Floor),
            // Phía đông góc (9, 13), ngoài lòng hào, để mặt ụ ở Floor + 0,5 m.
            new Shelter("MgNest", new Vector2(11.15f, 13f), new Vector2(1.2f, 1.2f), Floor + StepHeight),
        };

        enum CutKind { Trench, Step, Ramp }

        struct Cut
        {
            public CutKind kind;
            public Vector2 center, half;
            public float floor;
            public Vector2 rampDir;
            public float rampTop;
        }

        // ───────────────────────────── Menu ─────────────────────────────

        [MenuItem("DBP/Trench Map/Dựng địa hình (Bước 1)")]
        public static void Build()
        {
            var scene = EnsureScene();
            var terrain = EnsureTerrain();
            var data = terrain.terrainData;
            bool hadDetails = data.detailPrototypes.Length > 0;

            ConfigureData(data);
            var cuts = BuildCuts();
            data.SetHeights(0, 0, ComputeHeights(cuts));
            data.SetAlphamaps(0, 0, ComputeAlphamaps(cuts));
            terrain.Flush();

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[TrenchTerrain] Đã dựng {Route.Length - 1} nhịp hào, {Craters.Length} hố bom, {Breaches.Length} lối vỡ mép hào, {Shelters.Length} hầm/ụ.");
            Debug.Log(Validate());
            if (hadDetails)
                Debug.LogWarning("[TrenchTerrain] Địa hình đã đổi: chạy lại \"Bước 6 – Đá & cỏ\" để rải lại cỏ, sỏi và đá cho khớp.");
        }

        [MenuItem("DBP/Trench Map/Đặt Player vào đầu hào")]
        public static void PlacePlayer()
        {
            var scene = EnsureScene();
            var existing = GameObject.FindGameObjectWithTag("Player");
            var player = existing ? existing : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath), scene);
            UrpMaterialUtility.Apply(player);
            var start = Route[0];
            float floor = Terrain.activeTerrain ? Terrain.activeTerrain.SampleHeight(new Vector3(start.x, 0, start.y)) : Floor;
            player.transform.SetPositionAndRotation(new Vector3(start.x, floor + 0.05f, start.y + 1f), Quaternion.identity);

            // Player mang camera riêng: tắt camera mặc định của scene để không có hai AudioListener.
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (!cam.transform.IsChildOf(player.transform) && cam.CompareTag("MainCamera"))
                    cam.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[TrenchTerrain] Player đặt tại {player.transform.position}");
        }

        [MenuItem("DBP/Trench Map/Kiểm tra kích thước hào")]
        public static void ValidateMenu() => Debug.Log(Validate());

        // ───────────────────────────── Thiết lập ─────────────────────────────

        static UnityEngine.SceneManagement.Scene EnsureScene()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;
            if (active.isDirty)
                throw new InvalidOperationException($"Scene '{active.name}' chưa lưu. Lưu lại rồi mở {ScenePath}.");
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static Terrain EnsureTerrain()
        {
            System.IO.Directory.CreateDirectory(ArtDir);
            var terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var terrain = terrains.Length > 0 ? terrains[0] : null;

            // Lưu nét điêu khắc cũ (nếu có) vào asset gốc trước khi Terrain chuyển sang dữ liệu mới.
            AssetDatabase.SaveAssets();
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath);
            if (!data)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, DataPath);
            }

            if (!terrain) terrain = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
            terrain.terrainData = data;
            if (terrains.Length > 1) Debug.LogWarning($"[TrenchTerrain] Có {terrains.Length} Terrain trong scene, chỉ dùng '{terrain.name}'.");
            var collider = terrain.GetComponent<TerrainCollider>();
            if (!collider) collider = terrain.gameObject.AddComponent<TerrainCollider>();
            collider.terrainData = data;
            terrain.gameObject.name = "Terrain_TrenchMap";
            terrain.transform.SetPositionAndRotation(Origin, Quaternion.identity);
            terrain.transform.localScale = Vector3.one;
            GameObjectUtility.SetStaticEditorFlags(terrain.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
            return terrain;
        }

        static void ConfigureData(TerrainData data)
        {
            data.heightmapResolution = HeightRes;
            data.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
            data.alphamapResolution = AlphaRes;
            data.baseMapResolution = 1024;
            data.SetDetailResolution(DetailRes, 32);
            data.terrainLayers = new[]
            {
                Layer(MudLayerPath, SourceMudLayer, ArtDir + "/T_BunDat.png", new Color(0.24f, 0.13f, 0.08f), new Color(0.58f, 0.34f, 0.21f)),
                Layer(GrassLayerPath, SourceGrassLayer, ArtDir + "/T_CoUa.png", new Color(0.27f, 0.25f, 0.12f), new Color(0.66f, 0.58f, 0.32f)),
            };
        }

        /// Bản sao của layer Polytope dùng texture đã nhuộm lại, không sửa asset gốc của gói.
        static TerrainLayer Layer(string path, string source, string texturePath, Color dark, Color light)
        {
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (!layer)
            {
                if (!AssetDatabase.CopyAsset(source, path)) throw new InvalidOperationException($"Không chép được {source}");
                layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            }
            var sourceTexture = AssetDatabase.LoadAssetAtPath<TerrainLayer>(source).diffuseTexture;
            layer.diffuseTexture = Recolor(sourceTexture, texturePath, dark, light);
            // URP Terrain Lit bỏ qua diffuse remap nên màu nằm hẳn trong texture.
            layer.diffuseRemapMin = Vector4.zero;
            layer.diffuseRemapMax = Vector4.one;
            layer.tileSize = new Vector2(4f, 4f);
            EditorUtility.SetDirty(layer);
            return layer;
        }

        /// Giữ độ sáng tối (chi tiết) của texture gốc, ánh xạ sang dải màu dark → light.
        static Texture2D Recolor(Texture2D source, string outPath, Color dark, Color light)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
            if (existing) return existing;

            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            image.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(source)));
            var pixels = image.GetPixels();
            float min = 1f, max = 0f;
            foreach (var c in pixels) { float l = c.grayscale; min = Mathf.Min(min, l); max = Mathf.Max(max, l); }
            for (int i = 0; i < pixels.Length; i++)
            {
                float t = Mathf.InverseLerp(min, max, pixels[i].grayscale);
                pixels[i] = Color.Lerp(dark, light, t);
            }
            image.SetPixels(pixels);
            System.IO.File.WriteAllBytes(outPath, image.EncodeToPNG());
            Object.DestroyImmediate(image);

            AssetDatabase.ImportAsset(outPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(outPath);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        // ───────────────────────────── Hình học ─────────────────────────────

        static List<Cut> BuildCuts()
        {
            var cuts = new List<Cut>();
            for (int i = 0; i < Route.Length - 1; i++)
            {
                Vector2 a = Route[i], b = Route[i + 1];
                Vector2 mid = (a + b) / 2f;
                float hx = Mathf.Abs(b.x - a.x) / 2f, hz = Mathf.Abs(b.y - a.y) / 2f;
                cuts.Add(new Cut { kind = CutKind.Trench, center = mid, half = new Vector2(hx + HalfWidth, hz + HalfWidth), floor = Floor });

                // Bệ bắn dọc vách phía địch (+Z) trên các nhịp chạy ngang.
                if (hx > 0f && hx > HalfWidth)
                    cuts.Add(new Cut
                    {
                        kind = CutKind.Step,
                        center = new Vector2(mid.x, mid.y + HalfWidth + StepDepth / 2f),
                        half = new Vector2(hx - HalfWidth, StepDepth / 2f),
                        floor = Floor + StepHeight,
                    });
            }

            foreach (var (center, half) in Bays)
                cuts.Add(new Cut { kind = CutKind.Trench, center = center, half = half, floor = Floor });

            foreach (var shelter in Shelters)
                cuts.Add(new Cut { kind = CutKind.Trench, center = shelter.Center, half = shelter.Half, floor = shelter.Floor });

            foreach (var (at, dir, _) in Breaches)
            {
                Vector2 inner = at + dir * HalfWidth;
                Vector2 outer = at + dir * (HalfWidth + RampRun);
                Vector2 craterCenter = outer + dir * BreachCraterOffset;
                float top = Ground + CraterOffset(Vector2.Distance(outer, craterCenter), BreachCraterRadius, BreachCraterDepth);
                Vector2 half = Mathf.Abs(dir.x) > 0f
                    ? new Vector2(RampRun / 2f, RampHalfWidth)
                    : new Vector2(RampHalfWidth, RampRun / 2f);
                cuts.Add(new Cut { kind = CutKind.Ramp, center = (inner + outer) / 2f, half = half, floor = Floor, rampDir = dir, rampTop = top });
            }
            return cuts;
        }

        internal static IEnumerable<(Vector2 c, float r, float depth)> AllCraters()
        {
            foreach (var c in Craters) yield return c;
            foreach (var (at, dir, _) in Breaches)
                yield return (at + dir * (HalfWidth + RampRun + BreachCraterOffset), BreachCraterRadius, BreachCraterDepth);
        }

        /// Lòng chảo parabol + gờ đất văng (0,2–0,4 m) tới 1,6 bán kính; dốc tối đa ~37° để Player leo được.
        static float CraterOffset(float dist, float radius, float depth)
        {
            if (dist < radius) return -depth * (1f - (dist / radius) * (dist / radius));
            float rimEnd = radius * RimFactor;
            if (dist < rimEnd) return (0.1f + 0.25f * depth) * Mathf.Sin(Mathf.PI * (dist - radius) / (rimEnd - radius));
            return 0f;
        }

        internal static float DistanceFromTrench(Vector2 p) => TrenchDistance(p, BuildCuts());

        /// Dùng khi cần đo hàng triệu điểm: dựng danh sách nhát cắt một lần.
        internal static Func<Vector2, float> TrenchDistanceField()
        {
            var cuts = BuildCuts();
            return p => TrenchDistance(p, cuts);
        }

        internal static bool InsideCraterFloor(Vector2 p)
        {
            foreach (var (c, r, _) in AllCraters())
                if (Vector2.Distance(p, c) < r) return true;
            return false;
        }

        internal static float RectDistance(Vector2 p, Vector2 center, Vector2 half)
        {
            float dx = Mathf.Abs(p.x - center.x) - half.x, dz = Mathf.Abs(p.y - center.y) - half.y;
            float outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dz, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(dx, dz), 0f);
        }

        static float TrenchDistance(Vector2 p, List<Cut> cuts)
        {
            float d = float.MaxValue;
            foreach (var cut in cuts) d = Mathf.Min(d, RectDistance(p, cut.center, cut.half));
            return d;
        }

        static float NaturalGround(Vector2 p, List<Cut> cuts)
        {
            float g = Ground;
            foreach (var (c, r, depth) in AllCraters()) g += CraterOffset(Vector2.Distance(p, c), r, depth);
            float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6f, 15f, TrenchDistance(p, cuts)));
            if (fade > 0f)
            {
                float n = (Mathf.PerlinNoise(p.x * 0.035f + 13.1f, p.y * 0.035f + 7.7f) - 0.5f) * 0.5f
                        + (Mathf.PerlinNoise(p.x * 0.11f + 3.3f, p.y * 0.11f + 91.2f) - 0.5f) * 0.15f;
                g += n * fade;
            }
            return g;
        }

        static float CutHeight(in Cut cut, Vector2 p, float ground)
        {
            float d = RectDistance(p, cut.center, cut.half);
            if (d >= WallRun) return ground;
            float floor = cut.floor;
            if (cut.kind == CutKind.Ramp)
            {
                float along = Vector2.Dot(p - cut.center, cut.rampDir);
                float extent = Mathf.Abs(cut.rampDir.x) > 0f ? cut.half.x : cut.half.y;
                floor = Mathf.Lerp(Floor, cut.rampTop, Mathf.InverseLerp(-extent, extent, along));
            }
            return d <= 0f ? floor : Mathf.Lerp(floor, ground, d / WallRun);
        }

        static float HeightAt(Vector2 p, List<Cut> cuts)
        {
            float g = NaturalGround(p, cuts);
            float h = g;
            foreach (var cut in cuts) h = Mathf.Min(h, CutHeight(cut, p, g));
            return h;
        }

        static float[,] ComputeHeights(List<Cut> cuts)
        {
            var heights = new float[HeightRes, HeightRes];
            float step = TerrainSize / (HeightRes - 1);
            for (int z = 0; z < HeightRes; z++)
            {
                if (z % 64 == 0) EditorUtility.DisplayProgressBar("Trench Map", "Điêu khắc hào và hố bom", z / (float)HeightRes);
                for (int x = 0; x < HeightRes; x++)
                {
                    var p = new Vector2(Origin.x + x * step, Origin.z + z * step);
                    heights[z, x] = Mathf.Clamp01(HeightAt(p, cuts) / TerrainHeight);
                }
            }
            EditorUtility.ClearProgressBar();
            return heights;
        }

        // ───────────────────────────── Sơn texture ─────────────────────────────

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        static float MudWeight(Vector2 p, List<Cut> cuts)
        {
            float splat = Mathf.Clamp01((Mathf.PerlinNoise(p.x * 0.35f + 51f, p.y * 0.35f + 17f) - 0.35f) * 2.5f);

            float dTrench = TrenchDistance(p, cuts);
            if (dTrench < WallRun + 0.15f) return 1f;
            float mud = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 3.5f, dTrench - WallRun))) * (0.3f + 0.6f * splat);

            foreach (var (c, r, _) in AllCraters())
            {
                float dist = Vector2.Distance(p, c);
                if (dist < r) return 1f;
                mud = Mathf.Max(mud, (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(r, r * 2.2f, dist))) * (0.35f + 0.6f * splat));
            }

            float jitter = (Mathf.PerlinNoise(p.x * 0.2f + 7f, p.y * 0.2f + 3f) - 0.5f) * 1.2f;
            float pathNoise = 0.6f + 0.4f * Mathf.PerlinNoise(p.x * 0.5f + 9f, p.y * 0.5f + 2f);
            void Path(Vector2 a, Vector2 b) =>
                mud = Mathf.Max(mud, 0.75f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1.6f, SegmentDistance(p, a, b) + jitter))) * pathNoise);
            foreach (var (at, dir, end) in Breaches) Path(at + dir * (HalfWidth + RampRun + BreachCraterOffset), end);
            foreach (var (a, b) in ExtraMudPaths) Path(a, b);

            // Đất bị cày xới rải rác khắp vùng giữa hai tuyến.
            float churn = Mathf.PerlinNoise(p.x * 0.03f + 101f, p.y * 0.03f + 57f);
            mud = Mathf.Max(mud, Mathf.Clamp01((churn - 0.6f) * 4f) * (0.25f + 0.3f * splat));
            return Mathf.Clamp01(mud);
        }

        static float[,,] ComputeAlphamaps(List<Cut> cuts)
        {
            var maps = new float[AlphaRes, AlphaRes, 2];
            float step = TerrainSize / AlphaRes;
            for (int z = 0; z < AlphaRes; z++)
            {
                if (z % 64 == 0) EditorUtility.DisplayProgressBar("Trench Map", "Sơn bùn đất và cỏ úa", z / (float)AlphaRes);
                for (int x = 0; x < AlphaRes; x++)
                {
                    var p = new Vector2(Origin.x + (x + 0.5f) * step, Origin.z + (z + 0.5f) * step);
                    float mud = MudWeight(p, cuts);
                    maps[z, x, 0] = mud;
                    maps[z, x, 1] = 1f - mud;
                }
            }
            EditorUtility.ClearProgressBar();
            return maps;
        }

        // ───────────────────────────── Kiểm tra ─────────────────────────────

        /// Đo lại trên Terrain thật (không dùng công thức) để bắt lỗi do độ phân giải heightmap.
        public static string Validate()
        {
            var terrain = Terrain.activeTerrain;
            if (!terrain) return "[TrenchTerrain] Không có Terrain trong scene.";
            Physics.SyncTransforms();
            float H(Vector2 p) => terrain.SampleHeight(new Vector3(p.x, 0, p.y)) + terrain.transform.position.y;

            var report = new StringBuilder("[TrenchTerrain] Kiểm tra kích thước hào\n");
            int failures = 0;
            void Check(bool ok, string line)
            {
                if (!ok) failures++;
                report.AppendLine((ok ? "  OK   " : "  FAIL ") + line);
            }

            for (int i = 0; i < Route.Length - 1; i++)
            {
                // Đo ở 30% nhịp: tránh góc cua và lối vỡ mép hào (đặt ở giữa nhịp).
                Vector2 a = Route[i], b = Route[i + 1], probe = Vector2.Lerp(a, b, 0.3f);
                Vector2 dir = (b - a).normalized, side = new Vector2(-dir.y, dir.x);
                float len = Vector2.Distance(a, b);
                float floor = H(probe);
                float rimOffset = HalfWidth + StepDepth + WallRun + 0.4f;
                // Bỏ điểm đo rơi vào gờ hố bom (cao hơn mặt đất 0,2–0,4 m) để không tính nhầm độ sâu.
                float rim = float.MinValue;
                foreach (var candidate in new[] { probe + side * rimOffset, probe - side * rimOffset })
                {
                    bool nearCrater = false;
                    foreach (var (c, r, _) in AllCraters()) nearCrater |= Vector2.Distance(candidate, c) < r * RimFactor;
                    if (!nearCrater) rim = Mathf.Max(rim, H(candidate));
                }
                float width = FloorExtent(H, probe, side, floor) + FloorExtent(H, probe, -side, floor);
                Check(len >= 8f && len <= 15f, $"Nhịp {i + 1}: dài {len:0.0} m (8–15)");
                Check(rim - floor >= 1.8f && rim - floor <= 2.0f, $"Nhịp {i + 1}: sâu {rim - floor:0.00} m (1,8–2,0)");
                Check(width >= 2.0f && width <= 2.4f, $"Nhịp {i + 1}: rộng đáy {width:0.00} m (2,0–2,4)");
                Check(floor + EyeHeight < rim, $"Nhịp {i + 1}: mắt đứng {floor + EyeHeight:0.00} < mép {rim:0.00}");
            }

            Vector2 stepProbe = (Route[1] + Route[2]) / 2f + Vector2.up * (HalfWidth + StepDepth / 2f);
            float stepTop = H(stepProbe), stepRim = H(stepProbe + Vector2.up * (StepDepth / 2f + WallRun + 0.4f));
            Check(stepTop + EyeHeight > stepRim, $"Bệ bắn: mắt {stepTop + EyeHeight:0.00} > mép {stepRim:0.00} (ngó qua được)");

            for (int i = 0; i < Breaches.Length; i++)
            {
                var (at, dir, _) = Breaches[i];
                float maxSlope = 0f;
                for (float s = 0f; s < HalfWidth + RampRun + BreachCraterOffset + BreachCraterRadius * RimFactor; s += 0.2f)
                {
                    Vector2 p0 = at + dir * s, p1 = at + dir * (s + 0.2f);
                    maxSlope = Mathf.Max(maxSlope, Mathf.Atan2(Mathf.Abs(H(p1) - H(p0)), 0.2f) * Mathf.Rad2Deg);
                }
                Check(maxSlope < SlopeLimit, $"Lối vỡ mép hào {i + 1}: dốc lớn nhất {maxSlope:0.0}° (< {SlopeLimit}°)");
            }

            var cuts = BuildCuts();
            foreach (var (c, r, _) in Craters)
            {
                // Mặt đất xung quanh có gợn nhẹ, nên đo so với vòng ngoài gờ đất chứ không so với 10 m.
                float outside = 0f;
                for (int k = 0; k < 8; k++)
                {
                    float angle = k * Mathf.PI / 4f;
                    outside += H(c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r * (RimFactor + 0.1f)) / 8f;
                }
                float measured = outside - H(c);
                float clearance = TrenchDistance(c, cuts) - r * RimFactor;
                Check(r * 2f >= 3f && r * 2f <= 6f && measured >= 0.5f && measured <= 1.25f && clearance > 1f,
                    $"Hố bom ({c.x}, {c.y}): Ø {r * 2:0.0} m, sâu {measured:0.00} m, cách hào {clearance:0.0} m");
            }

            int blocked = 0, samples = 0;
            string blocker = null;
            var hits = new Collider[8];
            float bottom = PlayerSkin + PlayerRadius + 0.05f, top = PlayerHeight - PlayerRadius;
            for (int i = 0; i < Route.Length - 1; i++)
            {
                Vector2 a = Route[i], b = Route[i + 1];
                float len = Vector2.Distance(a, b);
                for (float s = 0f; s <= len; s += 0.25f)
                {
                    Vector2 p = Vector2.Lerp(a, b, s / len);
                    float floor = H(p);
                    var basePos = new Vector3(p.x, floor, p.y);
                    samples++;
                    int count = Physics.OverlapCapsuleNonAlloc(basePos + Vector3.up * bottom, basePos + Vector3.up * top, PlayerRadius, hits, ~0, QueryTriggerInteraction.Ignore);
                    for (int k = 0; k < count; k++)
                        if (!hits[k].CompareTag("Player")) { blocked++; blocker ??= hits[k].name; break; }
                }
            }
            Check(blocked == 0, $"Va chạm Player dọc tim hào: {blocked}/{samples} điểm bị cấn{(blocker != null ? $" ({blocker})" : "")}");

            TrenchDressingEditorUtility.AppendValidation(Check, H);
            TrenchGroundDetailEditorUtility.AppendValidation(Check);

            report.Insert(0, failures == 0 ? "" : $"[{failures} lỗi] ");
            return report.ToString();
        }

        static float FloorExtent(Func<Vector2, float> h, Vector2 origin, Vector2 dir, float floor)
        {
            for (float s = 0.05f; s < 5f; s += 0.05f)
                if (h(origin + dir * s) > floor + 0.1f) return s;
            return 5f;
        }
    }
}
