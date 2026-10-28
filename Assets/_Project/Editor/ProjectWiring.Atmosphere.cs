using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBP.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DBP.EditorTools
{
    /// Không khí bi tráng cho 5 màn (T44, T60): dãy núi bao quanh lòng chảo, hố bom cháy sém, xác pháo cháy
    /// bốc khói, mộ súng cắm mũ của người đã ngã xuống (không máu me – BR-21), màu phim trầm ấm,
    /// chiều tà Màn 5 và pháo sáng đêm Màn 3. Dựng lại được nhiều lần, seed cố định theo màn.
    public static partial class ProjectWiring
    {
        const string MoodProfilePath = "Assets/_Project/Art/BiTrangVolumeProfile.asset";
        const string RidgeMeshPath = "Assets/_Project/Art/Meshes/DistantRidge.asset";
        const string RidgeMatDir = "Assets/_Project/Art/Materials/Ridge";
        const string HelmetMatPath = "Assets/_Project/Art/Materials/MuSat.mat";
        const string Props = "Assets/Defensive_props/Prefabs/";
        static readonly string[] Wrecks = { Props + "defence/howitzer.prefab", Props + "defence/mortar.prefab", Props + "artillery_position.prefab", Props + "fortifications/concrete_barricade_damaged.prefab" };

        static string WireAtmosphere(string sceneName)
        {
            if (!(sceneName.Length > 1 && sceneName[0] == 'M' && "12345".Contains(sceneName[1]))) return null;
            var old = GameObject.Find("Atmosphere");
            if (old) Object.DestroyImmediate(old);
            var root = new GameObject("Atmosphere").transform;
            var rng = new System.Random(StableHash(sceneName + "/khong-khi"));
            var keep = KeepOutPoints();
            var area = PlayArea(keep);
            var player = PlayerPosition();
            var center = new Vector3(area.center.x, player.y, area.center.y);
            float radius = Mathf.Max(area.width, area.height) * 0.5f + 15f;

            Lighting(sceneName);
            MoodVolume(root);
            Ridges(root, sceneName, center, radius);
            int craters = Craters(root, rng, keep, area);
            int boards = PlaceTrenchFloor(root, area);
            PlaceFlora(root, rng, keep, area);
            string features = WireFeatures(root, sceneName, rng, keep, area);
            var smoke = new List<Transform>();
            var fire = new List<Transform>();
            int wrecks = WreckSites(root, rng, keep, area, smoke, fire);
            int graves = RifleGraves(root, rng, keep, area);
            // Cột khói xa ngoài vùng chơi: làng bản, kho tàng đang cháy
            for (int i = 0; i < 4; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                var p = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (radius + 35f + (float)rng.NextDouble() * 25f);
                if (!GroundHit(p, out var hit)) continue;
                var t = new GameObject("KhoiXa").transform;
                t.SetParent(root, false); t.position = hit.point;
                smoke.Add(t);
            }

            var ambience = root.gameObject.AddComponent<BattlefieldAmbience>();
            ambience.center = center;
            ambience.radius = radius;
            ambience.flares = sceneName.StartsWith("M3");
            ambience.smokePoints = smoke.ToArray();
            ambience.firePoints = fire.ToArray();
            if (sceneName.StartsWith("M3")) root.gameObject.AddComponent<DBP.Missions.RadioChatter>(); // T47
            EnsurePlayerFarClip();
            return $"{features}, núi, {boards} ván lót hào, tre cháy + chuối, {craters} hố bom, {wrecks} xác pháo, {graves} mộ súng, {smoke.Count} cột khói, {fire.Count} đám cháy";
        }

        // ───────────────────────── Ánh sáng & màu ─────────────────────────

        static void Lighting(string sceneName)
        {
            // Thoáng hơn để thấy chiều sâu lòng chảo; đêm Màn 3 vẫn dày sương
            RenderSettings.fogDensity = sceneName.StartsWith("M3") ? 0.016f : 0.008f;
            if (sceneName.StartsWith("M3"))
            {
                // Đêm vẫn phải thấy địch: trăng sáng hơn, ánh sáng môi trường xanh nhạt
                var moon = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
                if (moon) { moon.intensity = Mathf.Max(moon.intensity, 0.7f); moon.color = new Color(0.65f, 0.75f, 1f); }
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.26f, 0.29f, 0.38f);
                RenderSettings.fogColor = new Color(0.1f, 0.12f, 0.17f);
            }
            if (!sceneName.StartsWith("M5")) return;
            // Chiều 7/5/1954: nắng xiên đỏ cam, khói bụi vàng đục
            var sun = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            if (sun)
            {
                sun.transform.rotation = Quaternion.Euler(13f, sun.transform.eulerAngles.y, 0f);
                sun.color = new Color(1f, 0.64f, 0.4f);
                sun.intensity = 1.2f;
            }
            RenderSettings.fogColor = new Color(0.6f, 0.48f, 0.4f);
            RenderSettings.ambientLight = new Color(0.48f, 0.4f, 0.35f);
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.45f, 0.4f);
        }

        static void MoodVolume(Transform root)
        {
            var go = new GameObject("BiTrangVolume");
            go.transform.SetParent(root, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0; // FreezeVolume (ưu tiên 10) phủ lên khi trả lời câu hỏi
            volume.sharedProfile = MoodProfile();
        }

        static VolumeProfile MoodProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MoodProfilePath);
            if (profile) return profile;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, MoodProfilePath);
            // Phim tài liệu cũ: bớt màu, tương phản, sáng ngả vàng, tối ngả lạnh, hạt phim, tối viền
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(-22f);
            color.contrast.Override(14f);
            color.colorFilter.Override(new Color(1f, 0.94f, 0.84f));
            var split = profile.Add<SplitToning>(true);
            split.shadows.Override(new Color(0.42f, 0.46f, 0.5f));
            split.highlights.Override(new Color(1f, 0.82f, 0.6f));
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.4f);
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.3f);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.6f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// Núi xa chỉ hiện khi camera nhìn đủ xa (mặc định 400 m).
        static void EnsurePlayerFarClip()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            var cam = prefab ? prefab.GetComponentInChildren<Camera>(true) : null;
            if (!cam || cam.farClipPlane >= 1000f) return;
            cam.farClipPlane = 1000f;
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
        }

        // ───────────────────────── Núi bao quanh ─────────────────────────

        static void Ridges(Transform root, string sceneName, Vector3 center, float radius)
        {
            var mesh = RidgeMesh();
            var fog = RenderSettings.fogColor;
            float ground = center.y - 12f;
            // Lớp gần đậm hơn, lớp xa nhạt dần vào sương
            AddRidge(root, mesh, "NuiGan", RidgeMat(sceneName + "_Gan", fog * 0.72f, fog * 0.86f), center, ground, radius + 170f, 55f, 0f);
            AddRidge(root, mesh, "NuiXa", RidgeMat(sceneName + "_Xa", fog * 0.86f, fog * 0.95f), center, ground, radius + 330f, 115f, 41f);
        }

        static void AddRidge(Transform root, Mesh mesh, string name, Material mat, Vector3 center, float ground, float r, float h, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(center.x, ground, center.z);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = new Vector3(r, h, r);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        static Material RidgeMat(string name, Color bottom, Color top)
        {
            Directory.CreateDirectory(RidgeMatDir);
            var path = $"{RidgeMatDir}/Ridge_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Project/Art/Shaders/DistantRidge.shader"));
                AssetDatabase.CreateAsset(mat, path);
            }
            bottom.a = top.a = 1f;
            mat.SetColor("_Bottom", bottom);
            mat.SetColor("_Top", top);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// Vòng núi bán kính 1, cao tối đa 1: chân ở y = 0 (uv.y = 0), đỉnh nhấp nhô (uv.y = 1).
        static Mesh RidgeMesh()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(RidgeMeshPath);
            if (mesh) return mesh;
            const int n = 192;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                // Đỉnh: vài lớp nhiễu cho dáng núi đá vôi Tây Bắc, khép vòng liền mạch
                float u = i / (float)n * 12f;
                float hgt = 0.25f + 0.5f * Mathf.PerlinNoise(Mathf.Sin(a) * 2f + 5f, Mathf.Cos(a) * 2f + 5f)
                            + 0.25f * Mathf.PerlinNoise(Mathf.Sin(a) * 7f + 11f, Mathf.Cos(a) * 7f + 11f)
                            + 0.05f * Mathf.Abs(Mathf.Sin(u * 3.1f));
                verts.Add(dir); uvs.Add(new Vector2(i / (float)n, 0));
                verts.Add(dir * 0.97f + Vector3.up * Mathf.Clamp01(hgt)); uvs.Add(new Vector2(i / (float)n, 1));
                if (i == n) break;
                int b = i * 2;
                tris.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 });
            }
            mesh = new Mesh { name = "DistantRidge" };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            Directory.CreateDirectory(Path.GetDirectoryName(RidgeMeshPath));
            AssetDatabase.CreateAsset(mesh, RidgeMeshPath);
            return mesh;
        }

        // ───────────────────────── Dấu tích chiến tranh ─────────────────────────

        static bool GroundHit(Vector3 p, out RaycastHit ground)
        {
            foreach (var hit in Physics.RaycastAll(p + Vector3.up * 80f, Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                ground = hit;
                return !hit.collider.GetComponentInParent<Health>() && hit.normal.y >= 0.75f;
            }
            ground = default;
            return false;
        }

        /// Điểm ngẫu nhiên quanh vùng chơi, cách xa người chơi/mục tiêu/hành lang ít nhất minDist.
        static bool FreeSpot(System.Random rng, List<Vector3> keep, Rect area, float ring, float minDist, out RaycastHit ground)
        {
            for (int attempt = 0; attempt < 16; attempt++)
            {
                var p = new Vector3(Mathf.Lerp(area.xMin - ring, area.xMax + ring, (float)rng.NextDouble()), 0,
                                    Mathf.Lerp(area.yMin - ring, area.yMax + ring, (float)rng.NextDouble()));
                if (keep.Any(k => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(k.x, k.z)) < minDist)) continue;
                if (GroundHit(p, out ground)) { keep.Add(ground.point); return true; }
            }
            ground = default;
            return false;
        }

        static int Craters(Transform root, System.Random rng, List<Vector3> keep, Rect area)
        {
            var scorch = ScorchMaterial();
            var scorchMesh = ScorchMesh();
            int placed = 0;
            for (int i = 0; i < 14; i++)
            {
                if (!FreeSpot(rng, keep, area, 20f, 5f, out var g)) continue;
                var crater = new GameObject("HoBom").transform;
                crater.SetParent(root, false);
                crater.position = g.point;
                crater.rotation = Quaternion.FromToRotation(Vector3.up, g.normal) * Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                float d = Mathf.Lerp(3f, 6.5f, (float)rng.NextDouble());
                var disc = new GameObject("ChaySem");
                disc.transform.SetParent(crater, false);
                disc.transform.localPosition = Vector3.up * 0.04f;
                disc.transform.localScale = new Vector3(d, 1f, d * Mathf.Lerp(0.75f, 1f, (float)rng.NextDouble())) * 0.5f;
                disc.AddComponent<MeshFilter>().sharedMesh = scorchMesh;
                var mr = disc.AddComponent<MeshRenderer>();
                mr.sharedMaterial = scorch;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                // Đất đá văng quanh miệng hố
                for (int k = 0; k < 5; k++)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Rocks[rng.Next(Rocks.Length)]);
                    if (!prefab) break;
                    var rock = (GameObject)PrefabUtility.InstantiatePrefab(prefab, crater);
                    float a = k / 5f * Mathf.PI * 2f + (float)rng.NextDouble();
                    rock.transform.localPosition = new Vector3(Mathf.Cos(a), -0.05f, Mathf.Sin(a)) * (d * 0.5f);
                    rock.transform.localRotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                    rock.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 0.7f, (float)rng.NextDouble());
                    foreach (var c in rock.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                }
                foreach (var t in crater.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
                placed++;
            }
            return placed;
        }

        /// Vết cháy sém: đĩa mép răng cưa, giữa đen dần ra mép trong suốt (màu đỉnh), không viền cứng.
        static Mesh ScorchMesh()
        {
            const string path = "Assets/_Project/Art/Meshes/Scorch.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh) return mesh;
            const int n = 28;
            var verts = new List<Vector3> { Vector3.zero };
            var colors = new List<Color> { new Color(0.05f, 0.04f, 0.035f, 0.92f) };
            var tris = new List<int>();
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                float r = 0.75f + 0.25f * Mathf.PerlinNoise(Mathf.Cos(a) * 1.7f + 3f, Mathf.Sin(a) * 1.7f + 3f);
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                verts.Add(dir * r * 0.55f); colors.Add(new Color(0.09f, 0.07f, 0.055f, 0.75f));
                verts.Add(dir * r); colors.Add(new Color(0.16f, 0.12f, 0.09f, 0f));
            }
            for (int i = 0; i < n; i++)
            {
                int a = 1 + i * 2, b = 1 + (i + 1) % n * 2;
                tris.AddRange(new[] { 0, b, a, a, b, b + 1, a, b + 1, a + 1 });
            }
            mesh = new Mesh { name = "Scorch" };
            mesh.SetVertices(verts); mesh.SetColors(colors); mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, verts.Select(v => new Vector2(0.5f, 0.5f)).ToList()); // giữa texture hạt mềm = đặc
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Material ScorchMaterial()
        {
            const string path = "Assets/_Project/Art/Materials/Greybox/Scorch.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat && mat.shader.name == "DBP/CombatParticles") return mat;
            if (mat) AssetDatabase.DeleteAsset(path);
            mat = new Material(Resources.Load<Shader>("CombatParticles"));
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static int WreckSites(Transform root, System.Random rng, List<Vector3> keep, Rect area, List<Transform> smoke, List<Transform> fire)
        {
            var charred = LoadOrMakeMat("Charred", new Color(0.06f, 0.055f, 0.05f));
            int placed = 0;
            for (int i = 0; i < 5; i++)
            {
                if (!FreeSpot(rng, keep, area, 25f, 9f, out var g)) continue;
                GameObject wreck;
                if (i == 0) wreck = BurntHull(root, charred);
                else
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Wrecks[(i - 1) % Wrecks.Length]);
                    if (!prefab) continue;
                    wreck = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                    foreach (var r in wreck.GetComponentsInChildren<Renderer>())
                        r.sharedMaterials = Enumerable.Repeat(charred, r.sharedMaterials.Length).ToArray();
                }
                wreck.name = "XacChay_" + i;
                wreck.transform.position = g.point;
                // Nghiêng lệch như bị hất văng
                wreck.transform.rotation = Quaternion.Euler(Mathf.Lerp(-14f, 14f, (float)rng.NextDouble()), (float)rng.NextDouble() * 360f, Mathf.Lerp(-14f, 14f, (float)rng.NextDouble()));
                var b = WorldBounds(wreck);
                wreck.transform.position += Vector3.up * (g.point.y - b.min.y - 0.15f);
                var top = new GameObject(i < 2 ? "Lua" : "Khoi").transform;
                top.SetParent(root, false);
                top.position = new Vector3(b.center.x, WorldBounds(wreck).max.y - 0.2f, b.center.z);
                (i < 2 ? fire : smoke).Add(top);
                if (i < 2) smoke.Add(top); // lửa cháy kèm khói đen
                placed++;
            }
            return placed;
        }

        /// Xác xe tăng cháy dựng bằng khối (không có model xe tăng trong repo).
        static GameObject BurntHull(Transform root, Material charred)
        {
            var go = new GameObject("XacXeTang");
            go.transform.SetParent(root, false);
            Block(go.transform, "Than", new Vector3(0, 0.75f, 0), new Vector3(2.8f, 1f, 5f), charred);
            Block(go.transform, "XichTrai", new Vector3(-1.55f, 0.45f, 0), new Vector3(0.5f, 0.9f, 5.2f), charred);
            Block(go.transform, "XichPhai", new Vector3(1.55f, 0.45f, 0.1f), new Vector3(0.5f, 0.9f, 5.2f), charred);
            var turret = Block(go.transform, "Thap", new Vector3(0.3f, 1.55f, -0.3f), new Vector3(1.8f, 0.6f, 2f), charred);
            turret.transform.localRotation = Quaternion.Euler(0, 28f, 9f); // tháp pháo bị hất lệch
            var barrel = Block(go.transform, "Nong", new Vector3(1.1f, 1.55f, 1.3f), new Vector3(0.18f, 0.18f, 2.4f), charred);
            barrel.transform.localRotation = Quaternion.Euler(-6f, 28f, 0);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.8f, 0);
            col.size = new Vector3(3.4f, 1.6f, 5.2f);
            return go;
        }

        /// Súng cắm ngược xuống đất, mũ sắt trên báng: nơi đồng đội đã ngã xuống.
        static int RifleGraves(Transform root, System.Random rng, List<Vector3> keep, Rect area)
        {
            var rifle = AssetDatabase.LoadAssetAtPath<GameObject>(RiflePrefabPath);
            if (!rifle) return 0;
            var helmetMat = AssetDatabase.LoadAssetAtPath<Material>(HelmetMatPath) ?? LoadOrMakeMat("MuSat", new Color(0.22f, 0.26f, 0.16f));
            int placed = 0;
            for (int i = 0; i < 6; i++)
            {
                if (!FreeSpot(rng, keep, area, 10f, 4f, out var g)) continue;
                var grave = new GameObject("MoSung").transform;
                grave.SetParent(root, false);
                grave.position = g.point;
                grave.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);

                var gun = (GameObject)PrefabUtility.InstantiatePrefab(rifle, grave);
                foreach (var c in gun.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                gun.transform.localRotation = Quaternion.identity;
                var size = WorldBounds(gun).size;
                var axis = size.x >= size.y && size.x >= size.z ? Vector3.right : size.z >= size.y ? Vector3.forward : Vector3.up;
                gun.transform.localRotation = Quaternion.Euler(Mathf.Lerp(-8f, 8f, (float)rng.NextDouble()), 0, Mathf.Lerp(-8f, 8f, (float)rng.NextDouble()))
                                              * Quaternion.FromToRotation(axis, Vector3.down);
                var b = WorldBounds(gun);
                gun.transform.position += new Vector3(g.point.x - b.center.x, g.point.y - b.min.y - 0.3f, g.point.z - b.center.z);
                b = WorldBounds(gun);

                var helmet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(helmet.GetComponent<Collider>());
                helmet.name = "MuSat";
                helmet.transform.SetParent(grave, false);
                helmet.transform.position = new Vector3(b.center.x, b.max.y - 0.02f, b.center.z);
                helmet.transform.localScale = new Vector3(0.3f, 0.16f, 0.33f);
                helmet.transform.localRotation = Quaternion.Euler(Mathf.Lerp(-12f, 12f, (float)rng.NextDouble()), 0, 0);
                helmet.GetComponent<Renderer>().sharedMaterial = helmetMat;
                foreach (var t in grave.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
                placed++;
            }
            return placed;
        }
    }
}
