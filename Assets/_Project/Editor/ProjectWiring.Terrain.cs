using System.Collections.Generic;
using System.Linq;
using DBP.Core;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Địa hình thật cho Màn 2–5 (31/10), cùng tinh thần chiến hào Màn 1: thay nền khối phẳng bằng Terrain điêu khắc —
    /// gợn đất, hố pháo có gờ đất văng, bùn / cỏ úa, cỏ khô; hào đào chữ chi; đồi khối thành gò đất (M2), đắp đất chân đồi
    /// chừa mặt có thang (M3), đắp ụ hai bên chiến hào để hào chìm trong đất (M4), lòng sông tự nhiên (M5).
    /// Chỗ có lính, mục tiêu, hòm, điểm xuất phát được giữ phẳng để lối chơi không đổi. Chạy trước bước trang trí.
    public static partial class ProjectWiring
    {
        const float TerrainSize = 260f, TerrainHeight = 26f, TerrainBase = 8f; // mặt đất cũ (y = 0) nằm ở độ cao 8 m của Terrain
        const int TerrainRes = 1025;

        struct Protect { public Vector2 c; public float r; }
        struct Rect2 { public Vector2 min, max; public float top, frac, run; public bool skipSouth; }

        static bool HasBattlefieldTerrain => GameObject.Find("DiaHinh");

        static string WireTerrain(string sceneName)
        {
            if (!(sceneName.StartsWith("M2") || sceneName.StartsWith("M3") || sceneName.StartsWith("M4") || sceneName.StartsWith("M5") || sceneName.StartsWith("M6"))) return "";
            // Màn 6: mẫu lính ẩn chỉ để nhân bản, dồn về giữa để không kéo lệch vùng rải đồ
            var templatesRoot = All<Transform>().FirstOrDefault(t => t.name == "MauDich");
            if (templatesRoot) { int k = 0; foreach (Transform t in templatesRoot) t.position = new Vector3(-4f + 1.6f * k++, 0f, 0f); }
            var old = GameObject.Find("DiaHinh");
            if (old) Object.DestroyImmediate(old);
            foreach (var r in All<Collider>().Where(c => c.name == "Ground" || c.name == "BoSongBen_Kia")) r.enabled = true; // đo lại từ đầu
            var center = sceneName.StartsWith("M2") ? new Vector2(0, 30) : sceneName.StartsWith("M6") ? Vector2.zero : new Vector2(0, 45);
            var origin = new Vector3(center.x - TerrainSize / 2f, -TerrainBase, center.y - TerrainSize / 2f);
            var rng = new System.Random(sceneName.GetHashCode());

            // ── Vùng giữ phẳng ──
            var protect = new List<Protect>();
            foreach (var h in All<Health>()) protect.Add(new Protect { c = XZ(h.transform.position), r = h.Kind == TargetKind.Infantry ? 1.6f : 4f });
            foreach (var t in All<Transform>().Where(t => t.parent && t.parent.name == "CrateSpawner")) protect.Add(new Protect { c = XZ(t.position), r = 1.5f });
            foreach (var z in All<BoxCollider>().Where(b => b.isTrigger && b.name.StartsWith("Zone_")))
                protect.Add(new Protect { c = XZ(z.bounds.center), r = Mathf.Max(z.bounds.extents.x, z.bounds.extents.z) * .7f + 1f });
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) protect.Add(new Protect { c = XZ(player.transform.position), r = 4f });
            foreach (var t in All<Transform>().Where(t => t.name.StartsWith("Ham_"))) protect.Add(new Protect { c = XZ(t.position), r = 6f });

            // ── Hình khối chính ──
            var hills = new List<Rect2>();
            foreach (var name in new[] { "DoiDocLap", "DoiE", "DoiD" })
            {
                var go = GameObject.Find(name);
                if (!go || !go.TryGetComponent<Collider>(out var c)) continue;
                var b = c.bounds;
                bool m2 = name == "DoiDocLap";
                hills.Add(new Rect2 { min = new Vector2(b.min.x, b.min.z), max = new Vector2(b.max.x, b.max.z), top = b.max.y, frac = m2 ? 1f : .3f, run = m2 ? 7f : 5f, skipSouth = !m2 });
                if (m2) go.GetComponent<Renderer>().enabled = false; // gò đất thay khối
            }
            var corridors = new List<Rect2>(); // lòng hào Màn 4
            var walls = All<Renderer>().Where(r => r.name.StartsWith("VachHao_")).Select(r => r.bounds).ToArray();
            if (walls.Length > 0)
                corridors.Add(new Rect2 { min = new Vector2(-1f, walls.Min(w => w.min.z)), max = new Vector2(1f, walls.Max(w => w.max.z)) });
            foreach (var g in All<Renderer>().Where(r => r.name.StartsWith("HaoNgang_") && r.name.EndsWith("_Truoc")))
            {
                var b = g.bounds;
                // Nhánh hào nối liền hào chính (kéo tới trục x = 0)
                corridors.Add(new Rect2 { min = new Vector2(b.center.x < 0 ? b.min.x : 0f, b.max.z), max = new Vector2(b.center.x < 0 ? 0f : b.max.x, b.max.z + 2f) });
            }
            float bermTop = walls.Length > 0 ? walls.Max(w => w.max.y) : 0f;
            var river = GameObject.Find("SongNamRom");
            var riverBand = river ? river.GetComponent<Collider>().bounds : default;

            // Hào chữ chi trang trí (đào nông, vách dốc vừa sức người leo)
            var trenches = new List<Vector2[]>();
            if (sceneName.StartsWith("M2")) trenches.Add(Zigzag(-34, 34, 17f, 2.2f));
            if (sceneName.StartsWith("M3")) { trenches.Add(Zigzag(-34, -8, 11f, 2f)); trenches.Add(Zigzag(8, 34, 11f, 2f)); }
            if (sceneName.StartsWith("M6")) { trenches.Add(Zigzag(-44, 44, 20f, 2.4f)); trenches.Add(Zigzag(-44, 44, -20f, 2.4f)); }
            // Màn 5: chiến hào bờ nam liền một dải, sâu hơn (đánh giữ hào mở màn); hào địch bờ bắc hai bên
            if (sceneName.StartsWith("M5")) { trenches.Add(Zigzag(-42, 42, 30f, 2.4f)); trenches.Add(Zigzag(-40, -16, 66f, 2f)); trenches.Add(Zigzag(16, 40, 66f, 2f)); }

            // Hố pháo
            var craters = new List<(Vector2 c, float r, float d)>();
            for (int i = 0; i < 170; i++)
            {
                var c = new Vector2(center.x + (float)(rng.NextDouble() - .5) * 230f, center.y + (float)(rng.NextDouble() - .5) * 230f);
                float r = 1.4f + (float)rng.NextDouble() * (i % 9 == 0 ? 5f : 2.6f), d = r * (.22f + (float)rng.NextDouble() * .18f);
                if (protect.Any(p => Vector2.Distance(p.c, c) < p.r + r * 1.7f)) continue;
                if (corridors.Any(k => RectDist(c, k.min, k.max) < r * 1.7f + 1.5f)) continue;
                if (river && Mathf.Abs(c.y - riverBand.center.z) < riverBand.extents.z + r * 1.7f + 2f) continue;
                if (trenches.Any(line => LineDist(c, line) < r * 1.7f + 1.5f)) continue;
                craters.Add((c, r, d));
            }

            // ── Tính độ cao ──
            float step = TerrainSize / (TerrainRes - 1);
            var heights = new float[TerrainRes, TerrainRes];
            var mudAt = new float[TerrainRes, TerrainRes];
            float seed = (float)rng.NextDouble() * 100f;
            for (int zi = 0; zi < TerrainRes; zi++)
                for (int xi = 0; xi < TerrainRes; xi++)
                {
                    var p = new Vector2(origin.x + xi * step, origin.z + zi * step);
                    float s = 0f, mud = 0f;
                    // Đồi: mặt đỉnh trùng khối, sườn đắp đất
                    foreach (var h in hills)
                    {
                        float d = RectDist(p, h.min, h.max);
                        if (d <= 0f) { s = Mathf.Max(s, h.top - .03f); continue; }
                        if (h.skipSouth && p.y < h.min.y + .5f) continue; // mặt có thang (Màn 3)
                        s = Mathf.Max(s, h.top * h.frac * (1f - Smooth(0f, h.run, d)));
                        mud = Mathf.Max(mud, 1f - Smooth(0f, h.run, d));
                    }
                    // Chiến hào Màn 4: lòng hào ở 0, hai bên đắp đất cao bằng vách, thoải dần ra đồng
                    bool inCorridor = false;
                    if (corridors.Count > 0)
                    {
                        float d = corridors.Min(k => RectDist(p, k.min, k.max));
                        inCorridor = d <= 0f;
                        if (!inCorridor) s = Mathf.Max(s, bermTop * (1f - Smooth(1.6f, 10f, d)));
                        mud = Mathf.Max(mud, 1f - Smooth(0f, 4f, d));
                    }
                    // Lòng sông
                    if (river)
                    {
                        float dz = Mathf.Abs(p.y - riverBand.center.z) - riverBand.extents.z;
                        // Bờ thoải ~34° (người leo được), mép bờ ngang đầu cầu nên lên cầu không bị hụt
                        if (dz < 1f) { s = Mathf.Min(s, -2.4f * (1f - Smooth(-3.5f, 1f, dz))); }
                        mud = Mathf.Max(mud, 1f - Smooth(-1f, 5f, dz));
                    }
                    // Hào chữ chi
                    foreach (var line in trenches)
                    {
                        float d = LineDist(p, line);
                        float depth = line == trenches[0] && river ? 1.6f : 1.25f; // hào giữ của Màn 5 sâu ngang ngực
                        if (d < 3.2f) { s = Mathf.Min(s, -depth * (1f - Smooth(.9f, 2.9f, d))); mud = Mathf.Max(mud, 1f - Smooth(.9f, 3.5f, d)); }
                    }
                    // Chi tiết: gợn đất + hố pháo, giảm dần về 0 ở vùng giữ phẳng / trong lòng hào
                    float keep = inCorridor ? 0f : 1f;
                    foreach (var q in protect) keep = Mathf.Min(keep, Smooth(q.r, q.r + 3f, Vector2.Distance(p, q.c)));
                    float detail = (Mathf.PerlinNoise(p.x * .03f + seed, p.y * .03f) - .5f) * .9f + (Mathf.PerlinNoise(p.x * .11f, p.y * .11f + seed) - .5f) * .3f;
                    foreach (var (c, r, d) in craters)
                    {
                        float dist = Vector2.Distance(p, c);
                        if (dist > r * 1.7f) continue;
                        detail += Crater(dist, r, d);
                        mud = Mathf.Max(mud, 1f - Smooth(r * .6f, r * 1.7f, dist));
                    }
                    // Vùng giữ phẳng có lính nằm trên ụ đất Màn 4: hạ ụ xuống quanh chỗ đứng
                    if (corridors.Count > 0 && s > 0f) foreach (var q in protect) s *= Smooth(q.r * .8f, q.r + 2.5f, Vector2.Distance(p, q.c));
                    heights[zi, xi] = Mathf.Clamp01((s + detail * keep + TerrainBase) / TerrainHeight);
                    mud = Mathf.Max(mud, Smooth(.55f, .8f, Mathf.PerlinNoise(p.x * .05f + 7f, p.y * .05f + seed)));
                    mudAt[zi, xi] = Mathf.Clamp01(mud);
                }

            // ── Terrain ──
            string dataPath = $"Assets/_Project/Art/Terrain/{sceneName}_DiaHinh.asset";
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
            if (!data) { data = new TerrainData(); AssetDatabase.CreateAsset(data, dataPath); }
            data.heightmapResolution = TerrainRes;
            data.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
            data.alphamapResolution = 512;
            data.baseMapResolution = 1024;
            data.terrainLayers = new[]
            {
                AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/_Project/Art/Terrain/TL_BunDat.terrainlayer"),
                AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/_Project/Art/Terrain/TL_CoUa.terrainlayer"),
            };
            data.SetHeights(0, 0, heights);
            var alpha = new float[512, 512, 2];
            for (int z = 0; z < 512; z++)
                for (int x = 0; x < 512; x++)
                {
                    float m = mudAt[z * (TerrainRes - 1) / 511, x * (TerrainRes - 1) / 511];
                    alpha[z, x, 0] = m; alpha[z, x, 1] = 1f - m;
                }
            data.SetAlphamaps(0, 0, alpha);
            // Cỏ khô (chi tiết Terrain) trên nền cỏ úa
            var grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Terrain/Details/T_CoKho_Cao.png");
            if (grassTex)
            {
                data.SetDetailResolution(512, 16);
                data.detailPrototypes = new[] { new DetailPrototype { prototypeTexture = grassTex, renderMode = DetailRenderMode.GrassBillboard, minWidth = .5f, maxWidth = .9f, minHeight = .35f, maxHeight = .7f,
                    healthyColor = new Color(.85f, .78f, .5f), dryColor = new Color(.6f, .5f, .3f), noiseSpread = .3f } };
                var grass = new int[512, 512];
                for (int z = 0; z < 512; z++)
                    for (int x = 0; x < 512; x++)
                    {
                        float m = mudAt[z * (TerrainRes - 1) / 511, x * (TerrainRes - 1) / 511];
                        grass[z, x] = m < .35f && rng.NextDouble() < .55 ? rng.Next(1, 5) : 0;
                    }
                data.SetDetailLayer(0, 0, 0, grass);
            }
            EditorUtility.SetDirty(data);

            var go2 = Terrain.CreateTerrainGameObject(data);
            go2.name = "DiaHinh";
            go2.transform.position = origin;
            Physics.SyncTransforms(); // tia dò mặt đất ở các bước sau phải thấy Terrain ở vị trí mới
            var terrain = go2.GetComponent<Terrain>();
            terrain.materialTemplate = TerrainMaterial();
            terrain.detailObjectDistance = 70f;
            terrain.drawInstanced = true;
            GameObjectUtility.SetStaticEditorFlags(go2, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);

            // Nền khối cũ: ẩn hình, bỏ va chạm (Terrain thay)
            foreach (var name in new[] { "Ground", "BoSongBen_Kia" })
            {
                var g = GameObject.Find(name);
                if (!g) continue;
                g.GetComponent<Renderer>().enabled = false;
                g.GetComponent<Collider>().enabled = false;
            }
            // Sông: AI không lội (trừ dải cầu)
            if (river)
            {
                foreach (var v in All<NavMeshModifierVolume>().Where(v => v.name == "CamLoiSong").ToArray()) Object.DestroyImmediate(v.gameObject);
                var bridge = GameObject.Find("CauMuongThanh");
                var bb = bridge ? bridge.GetComponent<Collider>().bounds : new Bounds(Vector3.zero, Vector3.zero);
                foreach (var (x0, x1) in new[] { (origin.x, bb.min.x - .6f), (bb.max.x + .6f, origin.x + TerrainSize) })
                {
                    var v = new GameObject("CamLoiSong").AddComponent<NavMeshModifierVolume>();
                    v.transform.position = new Vector3((x0 + x1) / 2f, -1f, riverBand.center.z);
                    v.size = new Vector3(x1 - x0, 4f, riverBand.size.z + 3f);
                    v.center = Vector3.zero;
                    v.area = 1;
                }
            }
            return $"địa hình thật {craters.Count} hố pháo, {trenches.Count} đường hào, {hills.Count} gò đồi{(corridors.Count > 0 ? ", hào chìm trong đất" : "")}{(river ? ", lòng sông" : "")}";
        }

        static Material TerrainMaterial()
        {
            const string path = "Assets/_Project/Art/Terrain/DiaHinh_URP.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Vector2 XZ(Vector3 p) => new Vector2(p.x, p.z);
        static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        static float RectDist(Vector2 p, Vector2 min, Vector2 max)
        {
            float dx = Mathf.Max(min.x - p.x, 0f, p.x - max.x), dz = Mathf.Max(min.y - p.y, 0f, p.y - max.y);
            float outside = Mathf.Sqrt(dx * dx + dz * dz);
            if (outside > 0f) return outside;
            return -Mathf.Min(Mathf.Min(p.x - min.x, max.x - p.x), Mathf.Min(p.y - min.y, max.y - p.y));
        }

        static float LineDist(Vector2 p, Vector2[] line)
        {
            float best = float.MaxValue;
            for (int i = 1; i < line.Length; i++)
            {
                var a = line[i - 1]; var ab = line[i] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        /// Hào chữ chi dọc trục x tại z, lệch ±amp mỗi 6 m.
        static Vector2[] Zigzag(float x0, float x1, float z, float amp)
        {
            var pts = new List<Vector2>();
            int i = 0;
            for (float x = x0; x <= x1 + .01f; x += 6f, i++) pts.Add(new Vector2(x, z + (i % 2 == 0 ? -amp : amp) * .5f));
            return pts.ToArray();
        }

        /// Lòng chảo + gờ đất văng tới 1,7 bán kính (như hố bom Màn 1).
        static float Crater(float dist, float r, float depth)
        {
            if (dist < r) return -depth * (1f - (dist / r) * (dist / r));
            float rimEnd = r * 1.7f;
            return (.1f + .25f * depth) * Mathf.Sin(Mathf.PI * (dist - r) / (rimEnd - r));
        }
    }
}
