using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Chiến trường Màn 2–5 bằng gói POLYGON War Map (Trí, nhánh model-v2, có giấy phép), dùng gần hết gói:
    ///   • Mọi màn: dây thép gai, chướng ngại, tôn, ụ đất, cây chết, xác pháo, xác xe tăng, xác máy bay, hố nước,
    ///     pháo cao xạ, đại liên trên giá, súng trường dựng; nền trời có pháo cao xạ bắn, đạn vạch và máy bay bay qua.
    ///   • Màn 4: vách hào hộp được thay bằng vách hào gỗ của gói (giữ va chạm cũ), đắp ụ đất phía sau, bậc bắn,
    ///     thang, cầu ván bắc ngang hào, hầm cốt ở cuối hào.
    ///   • Màn 5: xác khinh khí cầu lớn làm hậu cảnh, pháo nguyên vẹn bên bờ địch.
    /// Vật lớn có hộp va chạm để làm chỗ nấp; mọi thứ tránh lối đi và mục tiêu (FreeSpot).
    public static partial class ProjectWiring
    {
        const string WarMap = "Assets/PolygonWarMap/Prefabs/";

        struct Scatter
        {
            public string[] prefabs; public int count; public float length, sink; public bool cover;
            public Scatter(int count, float length, bool cover, float sink, params string[] prefabs)
            { this.prefabs = prefabs; this.count = count; this.length = length; this.cover = cover; this.sink = sink; }
        }

        static readonly Scatter[] WarMapCommon =
        {
            new Scatter(14, 5f, false, 0f, "Environment/SM_Prop_Wire_Fence_01", "Environment/SM_Prop_Wire_Fence_02", "Environment/SM_Prop_Wire_Fence_03",
                "Environment/SM_Prop_Wire_Fence_04", "Environment/SM_Prop_Wire_Fence_Tangled_01", "Environment/SM_Prop_Wire_Fence_Tangled_02", "Environment/SM_Prop_Wire_Fence_Tangled_03"),
            new Scatter(8, 3.4f, true, 0f, "Environment/SM_Prop_Barricade_01", "Environment/SM_Prop_Barricade_02", "Environment/SM_Prop_Barricade_03"),
            new Scatter(8, 1.8f, false, 0f, "Environment/SM_Prop_Iron_Sheet_01", "Environment/SM_Prop_Iron_Sheet_02", "Environment/SM_Prop_Iron_Sheet_03"),
            new Scatter(6, 6f, false, .15f, "Environment/SM_Env_Trench_Mound_01", "Environment/SM_Env_Trench_Mound_02", "Environment/SM_Env_Trench_Mound_03", "Environment/SM_Env_Trench_Mound_04"),
            new Scatter(5, 3.4f, false, .05f, "Environment/SM_Env_Trench_Dirt_01"),
            new Scatter(8, 4.6f, false, 0f, "Environment/SM_Env_Tree_Dead_04"),
            new Scatter(5, 7f, false, -.02f, "Environment/SM_Env_Crater_Water_01", "Environment/SM_Env_Crater_Water_02"),
            new Scatter(3, 5.4f, true, 0f, "Vehicles/SM_Veh_Artillery_01_Destroyed"),
            new Scatter(3, 8f, true, 0f, "Vehicles/SM_Veh_German_Tank_01_Destroyed", "Vehicles/SM_Veh_British_Tank_01_Destroyed", "Vehicles/SM_Veh_Small_Tank_01_Destroyed"),
            new Scatter(2, 8.5f, true, 0f, "Vehicles/SM_Veh_British_Plane_01_Destroyed", "Vehicles/SM_Veh_German_Plane_01_Destroyed"),
            new Scatter(2, 2f, true, 0f, "Weapons/SM_Wep_AAGun_01"),
            new Scatter(3, 1.4f, false, 0f, "Weapons/SM_Wep_German_HeavyMachineGun_Tripod_01", "Weapons/SM_Wep_British_HeavyMachineGun_Tripod_01"),
            new Scatter(3, 1.6f, false, 0f, "Environment/SM_Prop_Trench_Planks_01", "Environment/SM_Prop_Trench_Planks_02"),
        };

        static string WireWarMap(Transform root, string sceneName, System.Random rng, List<Vector3> keep, Rect area)
        {
            if (!(sceneName.StartsWith("M2") || sceneName.StartsWith("M3") || sceneName.StartsWith("M4") || sceneName.StartsWith("M5") || sceneName.StartsWith("M6"))) return "";
            if (!AssetDatabase.IsValidFolder("Assets/PolygonWarMap")) return "";
            FixWarMapParticles();
            int grounds = GroundTexture();
            var group = new GameObject("ChienTruongWarMap").transform;
            group.SetParent(root, false);
            var notes = new List<string>();
            if (sceneName.StartsWith("M4")) notes.Add(BuildTrenches(group, rng, keep));
            notes.Add(ComposeBattlefield(group, sceneName, keep)); // bố cục có chủ đích quanh tuyến đánh (M2, M3, M5)

            var extras = new List<Scatter>(WarMapCommon);
            if (sceneName.StartsWith("M5")) extras.Add(new Scatter(2, 5.4f, true, 0f, "Vehicles/SM_Veh_Artillery_01"));
            if (sceneName.StartsWith("M6"))
            {
                // Thử thách vô hạn: nhét gần hết gói — gấp đôi đồ rải, thêm xe / máy bay / pháo nguyên vẹn làm chỗ nấp
                extras.AddRange(WarMapCommon);
                extras.Add(new Scatter(3, 7.8f, true, 0f, "Vehicles/SM_Veh_German_Tank_01", "Vehicles/SM_Veh_British_Tank_01", "Vehicles/SM_Veh_Small_Tank_01"));
                extras.Add(new Scatter(2, 8.6f, true, 0f, "Vehicles/SM_Veh_Plane_British_WW1_01", "Vehicles/SM_Veh_Plane_German_WW1_01"));
                extras.Add(new Scatter(3, 5.4f, true, 0f, "Vehicles/SM_Veh_Artillery_01"));
                extras.Add(new Scatter(3, 1.5f, false, 0f, "Weapons/SM_Wep_German_HeavyMachineGun_01", "Weapons/SM_Wep_British_HeavyMachineGun_01"));
                extras.Add(new Scatter(4, 2.9f, false, 0f, "Environment/SM_Prop_Ladder_01", "Environment/SM_Prop_Ladder_02"));
                extras.Add(new Scatter(3, 3.4f, false, 0f, "Environment/SM_Prop_Trench_Bridge_01", "Environment/SM_Prop_Trench_Bridge_02"));
                extras.Add(new Scatter(4, 9f, false, 0f, "Environment/SM_Prop_Blimp_Piece_01", "Environment/SM_Prop_Blimp_Piece_02", "Environment/SM_Prop_Blimp_Piece_03",
                    "Environment/SM_Prop_Blimp_Piece_04", "Environment/SM_Prop_Blimp_Piece_05"));
                // Khinh khí cầu nguyên vẹn lơ lửng trên trời
                Raw(group, "Vehicles/SM_Veh_Blimp_01", new Vector3(area.center.x + 30f, 75f, area.center.y + 90f), 120f, Vector3.one);
            }
            int placed = 0;
            foreach (var s in extras)
                for (int i = 0; i < s.count; i++)
                {
                    if (!FreeSpot(rng, keep, area, 24f, s.cover ? 6f : 4f, out var g)) continue;
                    if (PlaceWar(group, s.prefabs[rng.Next(s.prefabs.Length)], g.point, (float)rng.NextDouble() * 360f, s.length, s.sink, s.cover)) placed++;
                }
            notes.Add($"{placed} vật chiến trường War Map");
            if (sceneName.StartsWith("M5")) notes.Add(Backdrop(group, area, "Vehicles/SM_Veh_Blimp_Destroyed_02", 1f, 60f) ? "xác khinh khí cầu" : "");
            else notes.Add(Backdrop(group, area, "Vehicles/SM_Veh_Blimp_Destroyed_01", 1f, 55f) ? "xác khinh khí cầu" : "");
            notes.Add($"{SkyFx(group, area, rng)} hiệu ứng nền trời");
            notes.Add($"{grounds} mặt đất cỏ úa");
            return string.Join(", ", notes.Where(n => n != ""));
        }

        static bool PlaceWar(Transform parent, string prefab, Vector3 at, float yaw, float length, float sink, bool cover)
        {
            var go = PlaceFitted(parent, WarMap + prefab + ".prefab", parent.InverseTransformPoint(at), yaw, length);
            if (!go) return false;
            go.transform.position += Vector3.down * sink;
            // Nước hố bom của gói sáng như tuyết trên nền đất → nước bùn tối, bóng
            if (prefab.Contains("Crater_Water"))
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterial = MuddyWater();
            // Ụ đất / đất xới của gói màu xám sáng → màu đất khớp địa hình
            if (prefab.Contains("Mound") || prefab.Contains("Trench_Dirt"))
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterial = Earth();
            Settle(go, cover);
            return true;
        }

        static void Settle(GameObject go, bool cover)
        {
            FixBuiltinMaterials(go);
            if (cover)
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (!mf.GetComponent<Collider>()) mf.gameObject.AddComponent<BoxCollider>();
            foreach (var t in go.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, cover ? StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic : StaticEditorFlags.BatchingStatic);
        }

        /// Hạt hiệu ứng của gói dùng vật liệu dựng sẵn của Unity (hoặc để trống), dây thép rối không có vật liệu:
        /// gán vật liệu thật (hạt URP cộng sáng / atlas của gói) để bộ đổi URP chung không tạo vật liệu đục mới mỗi lần nối.
        static void FixBuiltinMaterials(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m && AssetDatabase.GetAssetPath(m).StartsWith("Assets/")) continue;
                    mats[i] = r is ParticleSystemRenderer ? WarFxMaterial(m) : AssetDatabase.LoadAssetAtPath<Material>("Assets/PolygonWarMap/Materials/PolygonWarMap_Mat_01_A.mat");
                    changed = true;
                }
                if (mats.Length == 0 && !(r is ParticleSystemRenderer)) { mats = new[] { AssetDatabase.LoadAssetAtPath<Material>("Assets/PolygonWarMap/Materials/PolygonWarMap_Mat_01_A.mat") }; changed = true; }
                if (changed) r.sharedMaterials = mats;
            }
        }

        static Material WarFxMaterial(Material source)
        {
            var tex = source && source.HasProperty("_MainTex") ? source.mainTexture : null;
            string name = tex ? tex.name : "Trang";
            string path = $"Assets/_Project/Art/Materials/WarMapFx/Hat_{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m) return m;
            System.IO.Directory.CreateDirectory("Assets/_Project/Art/Materials/WarMapFx");
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            if (tex) m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f); // trong suốt, cộng sáng (lửa đầu nòng, đạn vạch)
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// Đặt prefab theo hướng và tỉ lệ cho sẵn (không tự xoay như PlaceFitted), bỏ va chạm.
        static GameObject Raw(Transform parent, string prefab, Vector3 pos, float yaw, Vector3 scale)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(WarMap + prefab + ".prefab");
            if (!asset) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.Scale(go.transform.localScale, scale);
            Settle(go, false);
            return go;
        }

        // ───────── Màn 4: chiến hào gỗ ─────────

        /// Mỗi vách hộp (VachHao_*, HaoNgang_*) được ốp bằng tấm vách hào của gói (Insert_02: một vách gỗ, dài 3,53 m,
        /// mặt trong ở x = 0,85 so với gốc). Vách hộp chỉ ẩn hình, vẫn giữ va chạm và NavMesh như cũ.
        static string BuildTrenches(Transform group, System.Random rng, List<Vector3> keep)
        {
            var walls = All<Renderer>().Where(r => r.name.StartsWith("VachHao_") || r.name.StartsWith("HaoNgang_")).ToArray();
            int pieces = 0;
            const float PieceLen = 3.53f, InnerX = .85f, Depth = 1.61f;
            foreach (var wall in walls)
            {
                var b = wall.bounds;
                bool alongZ = b.size.z >= b.size.x;
                // Hướng từ lòng hào ra vách
                Vector3 outward;
                if (wall.name.StartsWith("VachHao_")) outward = new Vector3(Mathf.Sign(b.center.x), 0, 0);
                else outward = new Vector3(0, 0, wall.name.EndsWith("_Sau") ? 1 : -1);
                float yaw = alongZ ? (outward.x > 0 ? 0f : 180f) : (outward.z > 0 ? -90f : 90f);
                float len = alongZ ? b.size.z : b.size.x;
                int n = Mathf.Max(1, Mathf.RoundToInt(len / PieceLen));
                float step = len / n, sy = b.size.y / Depth;
                var inner = b.center - outward * b.extents.x * (alongZ ? 1 : 0) - outward * b.extents.z * (alongZ ? 0 : 1);
                for (int i = 0; i < n; i++)
                {
                    float d = -len * .5f + step * (i + .5f);
                    var along = alongZ ? new Vector3(0, 0, d) : new Vector3(d, 0, 0);
                    var pivot = new Vector3(inner.x, b.min.y + Depth * sy, inner.z) + along - outward * InnerX;
                    string piece = rng.Next(3) == 0 ? "Environment/SM_Env_Trench_Deep_Straight_Insert_02_Dirt" : "Environment/SM_Env_Trench_Deep_Straight_Insert_02";
                    if (Raw(group, piece, pivot, yaw, new Vector3(1f, sy, step / PieceLen))) pieces++;
                }
                // Ụ đất đắp sau vách che mép tấm vách, dốc xuống mặt đất (có địa hình thì hào đã chìm trong đất)
                if (!HasBattlefieldTerrain) for (float d = -len * .5f + 5f; d < len * .5f; d += 10f)
                {
                    var along = alongZ ? new Vector3(0, 0, d) : new Vector3(d, 0, 0);
                    var p = new Vector3(inner.x, b.min.y, inner.z) + along + outward * 3.6f;
                    var bank = Raw(group, "Environment/SM_Env_Trench_Mound_02", p, yaw + 180f, new Vector3(1.2f, b.size.y / .77f, 1f));
                    if (bank) foreach (var r in bank.GetComponentsInChildren<Renderer>()) r.sharedMaterial = Earth();
                }
                wall.enabled = false;
                // Hai đầu vách: ụ đất che mặt cắt của tấm vách
                if (!HasBattlefieldTerrain) foreach (int end in new[] { -1, 1 })
                {
                    var along = (alongZ ? Vector3.forward : Vector3.right) * end * (len * .5f + .4f);
                    var cap = Raw(group, "Environment/SM_Env_Trench_Mound_01", new Vector3(inner.x, b.min.y, inner.z) + along + outward * 1.6f, yaw, new Vector3(1f, b.size.y / .84f, 1f));
                    if (cap) foreach (var r in cap.GetComponentsInChildren<Renderer>()) r.sharedMaterial = Earth();
                }
                // Thang dựa vách (bậc bắn của gói sâu 1 m, chiếm nửa lòng hào 2 m nên không dùng)
                if (len > 9f)
                {
                    Raw(group, rng.Next(2) == 0 ? "Environment/SM_Prop_Ladder_01" : "Environment/SM_Prop_Ladder_02",
                        new Vector3(inner.x, b.min.y, inner.z) - outward * .15f + (alongZ ? new Vector3(0, 0, -len * .25f) : new Vector3(-len * .25f, 0, 0)),
                        yaw + 90f, Vector3.one * .9f);
                }
            }
            // Cầu ván bắc ngang hào chính, súng trường dựng vách
            var main = walls.Where(w => w.name.StartsWith("VachHao_")).ToArray();
            int bridges = 0;
            if (main.Length > 0)
            {
                float zMin = main.Min(w => w.bounds.min.z), zMax = main.Max(w => w.bounds.max.z), top = main.Max(w => w.bounds.max.y);
                foreach (float t in new[] { .3f, .7f })
                {
                    var p = new Vector3(0, top + .05f, Mathf.Lerp(zMin, zMax, t));
                    if (Raw(group, "Environment/SM_Prop_Trench_Bridge_02", p, 90f, Vector3.one)) bridges++;
                }
                // Hầm cốt chìm dưới mặt đất (phần đào bị nền che), chỉ lộ nóc và lan can bao cát
                Raw(group, "Environment/SM_Env_Trench_Deep_Bunker_01", new Vector3(-18f, 0f, zMax + 4f), 180f, Vector3.one);
                for (int i = 0; i < 6; i++)
                {
                    var w = main[rng.Next(main.Length)];
                    float z = Mathf.Lerp(w.bounds.min.z + 1f, w.bounds.max.z - 1f, (float)rng.NextDouble());
                    var rifle = Raw(group, rng.Next(2) == 0 ? "Weapons/SM_Wep_German_Rifle_01" : "Weapons/SM_Wep_British_Rifle_01",
                        new Vector3(w.bounds.center.x - Mathf.Sign(w.bounds.center.x) * .35f, .02f, z), 0f, Vector3.one);
                    if (rifle) rifle.transform.rotation = Quaternion.Euler(-72f, Mathf.Sign(w.bounds.center.x) * 90f, 0f);
                }
            }
            return $"{pieces} tấm vách hào gỗ, {bridges} cầu ván, hầm cốt";
        }

        // ───────── Mặt đất, nước ─────────

        /// Nền khối phẳng một màu → cỏ úa (cùng ảnh với địa hình Màn 1), lặp 5 m một ô theo kích thước từng khối.
        static int GroundTexture()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Terrain/T_CoUa.png");
            if (!tex) return 0;
            int n = 0;
            foreach (var r in All<MeshRenderer>().Where(r => r.name == "Ground" || r.name == "BoSongBen_Kia" || r.name == "DoiDocLap").ToArray())
            {
                var size = r.bounds.size;
                var tiles = new Vector2(Mathf.Max(1, Mathf.Round(size.x / 5f)), Mathf.Max(1, Mathf.Round(size.z / 5f)));
                string path = $"Assets/_Project/Art/Materials/Greybox/DatCoUa_{tiles.x}x{tiles.y}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m)
                {
                    m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(m, path);
                }
                m.SetTexture("_BaseMap", tex);
                m.SetTextureScale("_BaseMap", tiles);
                m.SetColor("_BaseColor", new Color(.92f, .86f, .72f));
                m.SetFloat("_Smoothness", .08f);
                EditorUtility.SetDirty(m);
                r.sharedMaterial = m;
                n++;
            }
            return n;
        }

        /// Đất đắp ụ (màu bùn khô khớp nền cỏ úa) thay màu xám sáng của ụ trong gói.
        static Material Earth()
        {
            var m = LoadOrMakeMat("DatDapU", new Color(.42f, .32f, .2f));
            m.SetFloat("_Smoothness", .05f);
            return m;
        }

        static Material MuddyWater()
        {
            var m = LoadOrMakeMat("NuocHoBom", new Color(.13f, .12f, .08f));
            m.SetColor("_BaseColor", new Color(.13f, .12f, .08f));
            m.SetFloat("_Smoothness", .25f); // bóng nhẹ: không phản chiếu trời sáng trắng
            return m;
        }

        // ───────── Hậu cảnh & nền trời ─────────

        static bool Backdrop(Transform group, Rect area, string prefab, float scale, float beyond)
        {
            var at = new Vector3(area.center.x + area.width * .5f + beyond, 0f, area.yMax + beyond * .6f);
            return Raw(group, prefab, at, -35f, Vector3.one * scale) != null;
        }

        /// Pháo cao xạ bắn lên trời, đạn vạch xa và máy bay bay qua — đặt ở rìa xa để thấy mà không vướng trận.
        static int SkyFx(Transform group, Rect area, System.Random rng)
        {
            var fx = new (string prefab, int count)[]
            {
                ("FX/FX_War_Gunshot_Background_AA_01", 3), ("FX/FX_War_Gunshot_Heavy_Repeating_Tracers_01", 2),
                ("FX/FX_War_Gunshot_Heavy_Single_Tracers_01", 2), ("FX/FX_Plane_A_Spawn_01", 1), ("FX/FX_Plane_B_Spawn_Shooting_01", 1),
            };
            int n = 0;
            foreach (var (prefab, count) in fx)
                for (int i = 0; i < count; i++)
                {
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = Mathf.Max(area.width, area.height) * .5f + 70f + (float)rng.NextDouble() * 40f;
                    var p = new Vector3(area.center.x + Mathf.Cos(a) * r, 0f, area.center.y + Mathf.Sin(a) * r);
                    if (Raw(group, prefab, p, (float)rng.NextDouble() * 360f, Vector3.one)) n++;
                }
            return n;
        }

        /// Vật liệu hạt của gói dùng shader hạt đời cũ (hồng trong URP) → đổi sang URP Particles/Unlit, giữ màu và ảnh.
        static void FixWarMapParticles()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (!shader) return;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/PolygonWarMap/Materials" }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (!m || m.shader == shader || !m.shader.name.StartsWith("Legacy Shaders/Particles") && m.shader.isSupported) continue;
                var tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                var color = m.HasProperty("_TintColor") ? m.GetColor("_TintColor") * 2f : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                bool additive = m.shader.name.Contains("Additive");
                m.shader = shader;
                m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", additive ? 2f : 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = 3000;
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                EditorUtility.SetDirty(m);
            }
        }
    }
}
