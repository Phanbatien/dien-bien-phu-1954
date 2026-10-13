using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBP.Core;
using DBP.DebugTools;
using DBP.Interaction;
using DBP.Missions;
using DBP.Player;
using DBP.Quiz;
using DBP.Upgrades;
using DBP.Weapons;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace DBP.EditorTools
{
    /// Dựng scene greybox cho 5 màn + Sandbox bằng khối hộp, đã nối sẵn mục tiêu (T29, T43, T50, T51, T58).
    /// Khôi (3D) thay khối hộp bằng model; Đạt thay hình nộm bằng AI. Mặc định KHÔNG ghi đè scene đã có.
    /// Dòng lệnh: -executeMethod DBP.EditorTools.SceneBuilder.BuildMissingCI
    public static class SceneBuilder
    {
        const string Root = "Assets/_Project";
        const string SceneDir = Root + "/Scenes";
        const string MatDir = Root + "/Art/Materials/Greybox";
        const string PrefabDir = Root + "/Prefabs";
        const string PlayerPrefabPath = PrefabDir + "/Player.prefab";
        const string FreezeProfilePath = Root + "/Art/FreezeVolumeProfile.asset";

        static readonly (string name, Action build)[] Scenes =
        {
            ("M1_HimLam", BuildM1),
            ("M2_GiuDocLap", BuildM2),
            ("M3_DoiED", BuildM3),
            ("M4_LongHao", BuildM4),
            ("M5_ChieuMungBay", BuildM5),
            ("Sandbox", BuildSandbox),
        };

        [MenuItem("DBP/Greybox/Tạo scene còn thiếu")]
        public static void BuildMissing() => Build(overwrite: false);

        [MenuItem("DBP/Greybox/Dựng lại tất cả (ghi đè scene!)")]
        public static void RebuildAll()
        {
            if (EditorUtility.DisplayDialog("Ghi đè scene?", "Mọi chỉnh sửa trong 6 scene greybox sẽ mất.", "Ghi đè", "Hủy"))
                Build(overwrite: true);
        }

        public static void RebuildAllCI()
        {
            try { Build(overwrite: true); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void BuildMissingCI()
        {
            try { Build(overwrite: false); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Build(bool overwrite)
        {
            foreach (var dir in new[] { SceneDir, MatDir, PrefabDir }) Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            freezeProfile = FreezeProfile();
            playerPrefab = PlayerPrefab();

            var buildScenes = new List<EditorBuildSettingsScene>();
            foreach (var (name, build) in Scenes)
            {
                string path = $"{SceneDir}/{name}.unity";
                if (overwrite || !File.Exists(path))
                {
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    build();
                    EditorSceneManager.SaveScene(scene, path);
                    Debug.Log($"[SceneBuilder] Đã dựng {path}");
                }
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        // ───────────────────────────── Năm màn ─────────────────────────────

        /// Màn 1: hướng dẫn → vượt tuyến rào → phá toàn bộ lô cốt → tới điểm tập kết (BR-32).
        static void BuildM1()
        {
            SetupScene(night: false);
            var player = Spawn(new Vector3(0, 0, 2), 0);
            var env = Group("Environment");
            Trench(env, 0, -2, 18);
            for (int i = -6; i <= 6; i++) Box($"RaoKemGai_{i}", new Vector3(i * 2f, 0.3f, 25f), new Vector3(1.9f, 0.6f, 0.2f), Mat("Wire", new Color(0.3f, 0.3f, 0.3f)), env);
            for (int i = 0; i < 8; i++) Box($"HoBom_{i}", new Vector3(-12 + i * 3.5f, 0.3f, 38 + (i % 3) * 5), new Vector3(2f, 0.6f, 2f), Mat("Mud", MudColor), env);

            var enemies = Group("Enemies");
            var bunkers = new List<Health>
            {
                Dummy("LoCot_1", new Vector3(-9, 0, 52), TargetKind.Bunker, 300, enemies),
                Dummy("LoCot_2", new Vector3(9, 0, 58), TargetKind.Bunker, 300, enemies),
            };
            Dummy("LinhPhap_1", new Vector3(-5, 0, 50), TargetKind.Infantry, 100, enemies);
            Dummy("LinhPhap_2", new Vector3(5, 0, 54), TargetKind.Infantry, 100, enemies);
            Dummy("LinhPhap_3", new Vector3(0, 0, 62), TargetKind.Infantry, 100, enemies);

            Spawner(Points(-10, 10, 6, 48, 12, 0f), StandardRewards("W-MAT49"));
            Phone("DienThoai_GoiPhao", new Vector3(2.2f, 0, 16), bunkers[0]);

            Mission("M1", "Mở màn Him Lam", "M2", "M2_GiuDocLap", player, true,
                Make<TutorialObjective>("Làm quen: đi, bắn, thay đạn"),
                Reach("Vượt tuyến rào kẽm gai", new Vector3(0, 1, 31), new Vector3(30, 3, 4)),
                Destroy("Phá toàn bộ lô cốt", bunkers),
                Reach("Tới điểm tập kết", new Vector3(0, 1, 75), new Vector3(10, 3, 6)));
        }

        /// Màn 2: giữ ụ súng máy, đẩy lùi các đợt phản kích; đợt cuối có xe tăng Chaffee (BR-32).
        static void BuildM2()
        {
            SetupScene(night: false);
            var env = Group("Environment");
            Box("DoiDocLap", new Vector3(0, 1, 5), new Vector3(24, 2, 14), Mat("Mud", MudColor), env);
            for (int i = -4; i <= 4; i++)
                if (i != 0) Box($"BaoCat_{i}", new Vector3(i * 2.2f, 2.4f, 11.5f), new Vector3(2f, 0.8f, 0.8f), Mat("Sandbag", SandbagColor), env);
            var player = Spawn(new Vector3(0, 2.1f, 4), 0);

            var gun = Box("USungMay", new Vector3(0, 2.6f, 10.6f), new Vector3(0.5f, 0.4f, 1.4f), Mat("Metal", MetalColor), env);
            var seat = new GameObject("Seat").transform;
            seat.SetParent(gun.transform.parent, false);
            seat.localPosition = new Vector3(0, 2.05f, 9.6f);
            var mg = gun.AddComponent<MountedGun>();
            SetRef(mg, "seat", seat);

            Spawner(Points(-10, 10, 0, 9, 12, 2f), StandardRewards("W-MAT49"));

            var waves = Group("Waves");
            var list = new List<GameObject>();
            for (int w = 0; w < 3; w++)
            {
                var root = new GameObject($"Dot_{w + 1}");
                root.transform.SetParent(waves, false);
                int count = 4 + w * 2;
                for (int i = 0; i < count; i++)
                    Dummy($"LinhPhap_{w + 1}_{i}", new Vector3(-14 + i * 28f / count, 0, 45 + w * 6 + (i % 2) * 3), TargetKind.Infantry, 100, root.transform);
                if (w == 2) Dummy("XeTang_Chaffee", new Vector3(0, 0, 70), TargetKind.Tank, 600, root.transform);
                root.SetActive(false);
                list.Add(root);
            }

            var repel = Make<RepelWavesObjective>("Đẩy lùi toàn bộ đợt phản kích và phá xe tăng");
            repel.waves = list;
            Mission("M2", "Giữ Độc Lập", "M3", "M3_DoiED", player, true, repel);
        }

        /// Màn 3 (đêm): hạ lính gác và tổ súng máy được đánh dấu, tới điểm rút. Tổ súng máy KHÔNG miễn đạn.
        static void BuildM3()
        {
            SetupScene(night: true);
            var player = Spawn(new Vector3(0, 0, 0), 0);
            var env = Group("Environment");
            Box("DoiE", new Vector3(-15, 3, 60), new Vector3(20, 6, 20), Mat("Mud", MudColor), env);
            Box("DoiD", new Vector3(15, 2.5f, 75), new Vector3(18, 5, 18), Mat("Mud", MudColor), env);
            for (int i = 0; i < 6; i++) Box($"CayDo_{i}", new Vector3(-10 + i * 4, 0.6f, 20 + i * 3), new Vector3(1.5f, 1.2f, 0.6f), Mat("Wood", WoodColor), env);

            var enemies = Group("Enemies");
            var targets = new List<Health>
            {
                Dummy("LinhGac_1", new Vector3(-18, 6, 55), TargetKind.Infantry, 100, enemies),
                Dummy("LinhGac_2", new Vector3(-10, 6, 64), TargetKind.Infantry, 100, enemies),
                Dummy("LinhGac_3", new Vector3(12, 5, 70), TargetKind.Infantry, 100, enemies),
                Dummy("ToSungMay_1", new Vector3(-14, 6, 66), TargetKind.MachineGunNest, 150, enemies),
                Dummy("ToSungMay_2", new Vector3(17, 5, 80), TargetKind.MachineGunNest, 150, enemies),
            };

            Spawner(Points(-12, 12, 4, 40, 12, 0f), StandardRewards("W-MAT49"));

            Mission("M3", "Đêm đồi E, D", "M4", "M4_LongHao", player, true,
                Destroy("Hạ lính gác và tổ súng máy", targets),
                Reach("Tới điểm rút", new Vector3(-20, 1, 5), new Vector3(6, 3, 6)));
        }

        /// Màn 4 "Trong lòng hào" (T51, T54): mê cung giao thông hào, chiếm 4 đoạn theo thứ tự rồi đẩy lùi phản kích (BR-32).
        static void BuildM4()
        {
            SetupScene(night: false);
            var player = Spawn(new Vector3(0, 0, 0), 0);
            var env = Group("Environment");
            var sand = Mat("Sandbag", SandbagColor);

            // Trục hào chính chạy dọc màn; vách chừa lối vào 4 nhánh ngang cụt (trái 18, phải 30, phải 52, trái 64).
            var trench = Mat("Trench", TrenchColor);
            float[][] left = { new float[] { -3, 16.5f }, new float[] { 19.5f, 62.5f }, new float[] { 65.5f, 88 } };
            float[][] right = { new float[] { -3, 28.5f }, new float[] { 31.5f, 50.5f }, new float[] { 53.5f, 88 } };
            foreach (var s in left) WallZ(env, -1.25f, s[0], s[1], trench);
            foreach (var s in right) WallZ(env, 1.25f, s[0], s[1], trench);
            TrenchX(env, 18, -1.5f, -12);
            TrenchX(env, 30, 1.5f, 12);
            TrenchX(env, 52, 1.5f, 12);
            TrenchX(env, 64, -1.5f, -12);
            for (int i = 0; i < 8; i++)
                Box($"BaoCat_{i}", new Vector3(i % 2 == 0 ? -0.7f : 0.7f, 0.35f, 10 + i * 10), new Vector3(0.8f, 0.7f, 1.4f), sand, env);

            var enemies = Group("Enemies");
            var objectives = new List<Objective>();
            float[] sections = { 14, 34, 56, 76 };
            for (int s = 0; s < sections.Length; s++)
            {
                float z = sections[s];
                var holders = new List<Health>
                {
                    Dummy($"GiuHao_{s + 1}_a", new Vector3(-0.5f, 0, z + 2), TargetKind.Infantry, 100, enemies),
                    Dummy($"GiuHao_{s + 1}_b", new Vector3(0.5f, 0, z + 5), TargetKind.Infantry, 100, enemies),
                };
                if (s % 2 == 1) holders.Add(Dummy($"GiuHao_{s + 1}_c", new Vector3(0, 0, z + 7), TargetKind.Infantry, 100, enemies));
                var capture = Make<CaptureZoneObjective>($"Chiếm đoạn hào {s + 1}");
                capture.zone = Zone($"DoanHao_{s + 1}", new Vector3(0, 1, z + 4), new Vector3(3, 3, 9));
                capture.holders = holders;
                objectives.Add(capture);
            }

            var waves = Group("Waves");
            var counter = new GameObject("PhanKich_Cuoi");
            counter.transform.SetParent(waves, false);
            for (int i = 0; i < 6; i++) Dummy($"PhanKich_{i}", new Vector3(-0.5f + (i % 2), 0, 84 + i * 0.6f), TargetKind.Infantry, 100, counter.transform);
            counter.SetActive(false);
            var repel = Make<RepelWavesObjective>("Đẩy lùi phản kích cuối");
            repel.waves = new List<GameObject> { counter };
            objectives.Add(repel);

            var points = new List<Vector3>();
            for (int i = 0; i < 6; i++) points.Add(new Vector3(i % 2 == 0 ? 0.7f : -0.7f, 0, 5 + i * 12.5f)); // phía đối diện bao cát
            points.AddRange(new[]
            {
                new Vector3(-10, 0, 18), new Vector3(-6, 0, 18), new Vector3(10, 0, 30),
                new Vector3(10, 0, 52), new Vector3(6, 0, 52), new Vector3(-10, 0, 64),
            });
            Spawner(points.ToArray(), StandardRewards("W-TYPE50"));

            Mission("M4", "Trong lòng hào", "M5", "M5_ChieuMungBay", player, true, objectives.ToArray());
        }

        /// Màn 5: vượt cầu Mường Thanh → loại bỏ ổ đề kháng → vào hầm chỉ huy → cắm cờ (BR-03, BR-32).
        static void BuildM5()
        {
            SetupScene(night: false, groundZ: -65);
            var env = Group("Environment");
            Box("BoSongBen_Kia", new Vector3(0, -0.5f, 110), new Vector3(300, 1, 120), Mat("Mud", MudColor), env);
            Box("SongNamRom", new Vector3(0, -2f, 45), new Vector3(300, 1, 10), Mat("Water", new Color(0.2f, 0.3f, 0.35f)), env);
            Box("CauMuongThanh", new Vector3(0, -0.15f, 45), new Vector3(4, 0.3f, 12), Mat("Wood", WoodColor), env);
            var player = Spawn(new Vector3(0, 0, 10), 0);

            var enemies = Group("Enemies");
            var nests = new List<Health>
            {
                Dummy("ODeKhang_1", new Vector3(-10, 0, 70), TargetKind.MachineGunNest, 150, enemies),
                Dummy("ODeKhang_2", new Vector3(10, 0, 75), TargetKind.MachineGunNest, 150, enemies),
                Dummy("ODeKhang_3", new Vector3(0, 0, 85), TargetKind.MachineGunNest, 150, enemies),
                Dummy("LinhPhap_1", new Vector3(-5, 0, 72), TargetKind.Infantry, 100, enemies),
                Dummy("LinhPhap_2", new Vector3(5, 0, 80), TargetKind.Infantry, 100, enemies),
            };

            var hq = Group("HamChiHuy");
            var bunkerMat = Mat("Concrete", ConcreteColor);
            Box("Ham_Tuong_Trai", new Vector3(-3, 1.5f, 100), new Vector3(0.6f, 3, 8), bunkerMat, hq);
            Box("Ham_Tuong_Phai", new Vector3(3, 1.5f, 100), new Vector3(0.6f, 3, 8), bunkerMat, hq);
            Box("Ham_Tuong_Sau", new Vector3(0, 1.5f, 104), new Vector3(6.6f, 3, 0.6f), bunkerMat, hq);
            Box("Ham_Mai", new Vector3(0, 3.2f, 100), new Vector3(7, 0.4f, 9), bunkerMat, hq);
            var flag = Box("Co_QuyetChienQuyetThang", new Vector3(0, 3.4f, 100), new Vector3(1.2f, 0.8f, 0.05f), Mat("Flag", new Color(0.8f, 0.1f, 0.1f)), hq);
            Object.DestroyImmediate(flag.GetComponent<Collider>());
            flag.SetActive(false);
            var raiser = hq.gameObject.AddComponent<FlagRaise>();
            SetRef(raiser, "flag", flag.transform);

            Spawner(Points(-12, 12, 12, 36, 6, 0f).Concat(Points(-12, 12, 58, 92, 6, 0f)).ToArray(), StandardRewards("W-TYPE50"));

            var mc = Mission("M5", "Chiều mùng Bảy", "", "", player, true,
                Reach("Vượt cầu Mường Thanh", new Vector3(0, 1, 54), new Vector3(8, 3, 4)),
                Destroy("Loại bỏ ổ đề kháng", nests),
                Reach("Vào hầm chỉ huy", new Vector3(0, 1, 101), new Vector3(5, 3, 5)));
            UnityEventTools.AddPersistentListener(mc.onWon, raiser.Raise);
        }

        /// Sandbox: thử mọi thứ ở một chỗ (không thuộc chiến dịch).
        static void BuildSandbox()
        {
            SetupScene(night: false);
            var player = Spawn(Vector3.zero, 0);
            var env = Group("Environment");
            Trench(env, 0, 4, 20);
            Box("TuongBan", new Vector3(0, 2, 40), new Vector3(20, 4, 1), Mat("Concrete", ConcreteColor), env);

            var crates = Group("Crates");
            Crate(crates, new Vector3(-6, 0, 3), Rarity.Common, null, 15, 0);
            Crate(crates, new Vector3(-3, 0, 3), Rarity.Uncommon, "W-MAUSER", 15, 0);
            Crate(crates, new Vector3(3, 0, 3), Rarity.Rare, "W-MAT49", 64, 1);
            Crate(crates, new Vector3(6, 0, 3), Rarity.Legendary, "W-MOSIN-SCOPE", 10, 0);
            Crate(crates, new Vector3(9, 0, 3), Rarity.Rare, "W-TYPE50", 70, 0);

            var gun = Box("USungMay", new Vector3(-10, 0.9f, 8), new Vector3(0.5f, 0.4f, 1.4f), Mat("Metal", MetalColor), env);
            var seat = new GameObject("Seat").transform;
            seat.SetParent(env, false);
            seat.localPosition = new Vector3(-10, 0, 7);
            SetRef(gun.AddComponent<MountedGun>(), "seat", seat);

            var enemies = Group("Targets");
            var targets = new List<Health>();
            for (int i = 0; i < 5; i++) targets.Add(Dummy($"Linh_{i}", new Vector3(-8 + i * 4, 0, 30), TargetKind.Infantry, 100, enemies));
            var sandboxBunker = Dummy("LoCot", new Vector3(-12, 0, 34), TargetKind.Bunker, 300, enemies);
            Phone("DienThoai_GoiPhao", new Vector3(-6, 0, 8), sandboxBunker);
            Dummy("XeTang", new Vector3(12, 0, 34), TargetKind.Tank, 600, enemies);

            Mission("SANDBOX", "Sandbox", "", "", player, true, Destroy("Hạ 5 hình nộm", targets));
        }

        // ───────────────────────────── Khối dựng ─────────────────────────────

        static readonly Color MudColor = new Color(0.36f, 0.27f, 0.19f);
        static readonly Color TrenchColor = new Color(0.45f, 0.3f, 0.2f);
        static readonly Color SandbagColor = new Color(0.6f, 0.55f, 0.4f);
        static readonly Color WoodColor = new Color(0.45f, 0.33f, 0.2f);
        static readonly Color MetalColor = new Color(0.2f, 0.22f, 0.2f);
        static readonly Color ConcreteColor = new Color(0.5f, 0.5f, 0.48f);

        static VolumeProfile freezeProfile;
        static GameObject playerPrefab;
        static Transform missionRoot;

        static void SetupScene(bool night, float groundZ = 0f)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.intensity = night ? 0.25f : 1.1f;
            sun.color = night ? new Color(0.6f, 0.7f, 1f) : new Color(1f, 0.93f, 0.82f);
            sun.transform.rotation = Quaternion.Euler(night ? 25 : 50, -30, 0);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = night ? 0.035f : 0.012f;
            RenderSettings.fogColor = night ? new Color(0.06f, 0.07f, 0.1f) : new Color(0.55f, 0.52f, 0.47f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = night ? new Color(0.12f, 0.14f, 0.2f) : new Color(0.45f, 0.43f, 0.4f);

            var ground = Box("Ground", new Vector3(0, -0.5f, groundZ), new Vector3(300, 1, groundZ < 0 ? 210 : 300), Mat("Mud", MudColor), null);
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.BatchingStatic);

            var systems = new GameObject("GameSystems");
            var freeze = systems.AddComponent<WorldFreeze>();
            var volumeGo = new GameObject("FreezeVolume");
            volumeGo.transform.SetParent(systems.transform, false);
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
            volume.weight = 0f;
            volume.sharedProfile = freezeProfile;
            freeze.SetVolume(volume);
            var session = systems.AddComponent<FieldQuizSession>();
            SetRef(session, "freeze", freeze);
            systems.AddComponent<DebugQuizView>();
            systems.AddComponent<DBP.DebugTools.DebugOverlay>();
            systems.AddComponent<FogBackground>();

            missionRoot = null;
        }

        static Transform Group(string name) => new GameObject(name).transform;

        static GameObject Box(string name, Vector3 center, Vector3 size, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (parent && parent.name == "Environment") GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        /// Một đoạn giao thông hào thẳng theo trục Z (hai vách đất cao 1,8 m, lòng hào rộng 2 m).
        static void Trench(Transform parent, float x, float z0, float z1)
        {
            float len = z1 - z0, mid = (z0 + z1) / 2f;
            var m = Mat("Trench", TrenchColor);
            Box($"Hao_{x}_{z0}_Trai", new Vector3(x - 1.25f, 0.9f, mid), new Vector3(0.5f, 1.8f, len), m, parent);
            Box($"Hao_{x}_{z0}_Phai", new Vector3(x + 1.25f, 0.9f, mid), new Vector3(0.5f, 1.8f, len), m, parent);
        }

        /// Một vách hào dọc trục Z tại x, từ z0 tới z1.
        static void WallZ(Transform parent, float x, float z0, float z1, Material m) =>
            Box($"VachHao_{x}_{z0}", new Vector3(x, 0.9f, (z0 + z1) / 2f), new Vector3(0.5f, 1.8f, z1 - z0), m, parent);

        /// Đoạn hào ngang theo trục X tại z, từ x0 tới x1.
        static void TrenchX(Transform parent, float z, float x0, float x1)
        {
            float len = Mathf.Abs(x1 - x0), mid = (x0 + x1) / 2f;
            var m = Mat("Trench", TrenchColor);
            Box($"HaoNgang_{z}_{x0}_Truoc", new Vector3(mid, 0.9f, z - 1.25f), new Vector3(len, 1.8f, 0.5f), m, parent);
            Box($"HaoNgang_{z}_{x0}_Sau", new Vector3(mid, 0.9f, z + 1.25f), new Vector3(len, 1.8f, 0.5f), m, parent);
        }

        /// Lưới điểm đặt hòm trong vùng [x0,x1] × [z0,z1] (BR-13: 10–15 điểm mỗi màn).
        static Vector3[] Points(float x0, float x1, float z0, float z1, int count, float y)
        {
            var list = new Vector3[count];
            int cols = 3, rows = Mathf.CeilToInt(count / 3f);
            for (int i = 0; i < count; i++)
            {
                float fx = (i % cols) / (float)(cols - 1);
                float fz = rows == 1 ? 0.5f : (i / cols) / (float)(rows - 1);
                list[i] = new Vector3(Mathf.Lerp(x0, x1, fx) + ((i / cols) % 2 == 0 ? 0.8f : -0.8f), y, Mathf.Lerp(z0, z1, fz));
            }
            return list;
        }

        /// Danh mục phần thưởng mặc định, đủ 4 độ hiếm (BR-15, BR-29). Cân bằng lại ở T65.
        static CrateReward[] StandardRewards(string smgId) => new[]
        {
            new CrateReward { rarity = Rarity.Common, ammoType = "7.62x54R", ammo = 15 },
            new CrateReward { rarity = Rarity.Common, ammo = 0, grenades = 2 },
            new CrateReward { rarity = Rarity.Uncommon, weaponId = "W-MAUSER", ammoType = "7.92x57", ammo = 15 },
            new CrateReward { rarity = Rarity.Rare, weaponId = smgId, ammoType = smgId == "W-TYPE50" ? "7.62x25" : "9x19", ammo = 64 },
            new CrateReward { rarity = Rarity.Rare, ammo = 0, grenades = 3 },
            new CrateReward { rarity = Rarity.Legendary, weaponId = "W-MOSIN-SCOPE", ammoType = "7.62x54R", ammo = 10 },
        };

        static CrateSpawner Spawner(Vector3[] points, CrateReward[] rewards)
        {
            var go = new GameObject("CrateSpawner");
            var spawner = go.AddComponent<CrateSpawner>();
            for (int i = 0; i < points.Length; i++)
            {
                var p = new GameObject($"DiemHom_{i + 1:00}").transform;
                p.SetParent(go.transform, false);
                p.localPosition = points[i];
                p.localRotation = Quaternion.Euler(0, (i * 47) % 360, 0);
                spawner.points.Add(p);
            }
            spawner.rewards = new List<CrateReward>(rewards);
            return spawner;
        }

        static FieldPhone Phone(string name, Vector3 basePos, Health target)
        {
            var go = Box(name, basePos + Vector3.up * 0.35f, new Vector3(0.35f, 0.7f, 0.35f), Mat("Metal", MetalColor), null);
            var phone = go.AddComponent<FieldPhone>();
            phone.target = target;
            return phone;
        }

        static Health Dummy(string name, Vector3 basePos, TargetKind kind, float hp, Transform parent)
        {
            var (type, size, mat) = kind switch
            {
                TargetKind.Bunker => (PrimitiveType.Cube, new Vector3(4, 2.5f, 4), Mat("Concrete", ConcreteColor)),
                TargetKind.MachineGunNest => (PrimitiveType.Cube, new Vector3(2, 1.2f, 2), Mat("Sandbag", SandbagColor)),
                TargetKind.Tank => (PrimitiveType.Cube, new Vector3(3, 2, 5), Mat("Tank", new Color(0.3f, 0.35f, 0.25f))),
                _ => (PrimitiveType.Capsule, new Vector3(0.7f, 1f, 0.7f), Mat("Enemy", new Color(0.55f, 0.5f, 0.3f))),
            };
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localScale = size;
            go.transform.localPosition = basePos + Vector3.up * (type == PrimitiveType.Capsule ? 1f : size.y / 2f);
            go.transform.localRotation = Quaternion.Euler(0, 180, 0);
            go.GetComponent<Renderer>().sharedMaterial = mat;

            var health = go.AddComponent<Health>();
            bool armored = kind == TargetKind.Bunker || kind == TargetKind.Tank;
            health.Configure(kind, hp, armored ? new[] { DamageType.Bullet } : Array.Empty<DamageType>());
            go.AddComponent<DisableOnDeath>();
            if (kind == TargetKind.Infantry) go.AddComponent<WeaponDrop>(); // BR-14: tỉ lệ và giới hạn ở MissionController
            return health;
        }

        static void Crate(Transform parent, Vector3 basePos, Rarity rarity, string weaponId, int ammo, int grenades)
        {
            var go = Box($"Hom_{rarity}_{parent.childCount}", basePos + Vector3.up * 0.3f, new Vector3(0.9f, 0.6f, 0.6f),
                Mat($"Crate_{rarity}", RarityRules.Color(rarity)), parent);
            var crate = go.AddComponent<QuizCrate>();
            crate.rarity = rarity;
            crate.rewardWeaponId = weaponId;
            crate.rewardAmmo = ammo;
            crate.rewardGrenades = grenades;
        }

        static GameObject Spawn(Vector3 position, float yaw)
        {
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            return player;
        }

        static Transform MissionRoot() => missionRoot ? missionRoot : missionRoot = new GameObject("Mission").transform;

        static T Make<T>(string title) where T : Objective
        {
            var go = new GameObject(title);
            go.transform.SetParent(MissionRoot(), false);
            var objective = go.AddComponent<T>();
            objective.Title = title;
            return objective;
        }

        static ReachZoneObjective Reach(string title, Vector3 center, Vector3 size)
        {
            var o = Make<ReachZoneObjective>(title);
            o.zone = Zone(title, center, size);
            return o;
        }

        static DestroyTargetsObjective Destroy(string title, List<Health> targets)
        {
            var o = Make<DestroyTargetsObjective>(title);
            o.targets = targets;
            return o;
        }

        static BoxCollider Zone(string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject($"Zone_{name}");
            go.transform.SetParent(MissionRoot(), false);
            go.transform.localPosition = center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Marker";
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            pole.transform.SetParent(go.transform, false);
            pole.transform.localPosition = new Vector3(0, 0.5f, 0);
            pole.transform.localScale = new Vector3(0.15f, 1.5f, 0.15f);
            pole.GetComponent<Renderer>().sharedMaterial = Mat("Marker", new Color(0.2f, 0.8f, 0.3f));
            return box;
        }

        static MissionController Mission(string id, string title, string nextId, string nextScene, GameObject player, bool sequential, params Objective[] objectives)
        {
            var mc = MissionRoot().gameObject.AddComponent<MissionController>();
            mc.Setup(id, title, nextId, nextScene, player.GetComponent<Health>(), new List<Objective>(objectives), sequential);
            return mc;
        }

        // ───────────────────────────── Tài nguyên dùng chung ─────────────────────────────

        static Material Mat(string name, Color color)
        {
            string path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Smoothness", 0.1f);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        /// Màn hình ngả nâu xám + tối viền khi thế giới đứng yên (BR-09).
        static VolumeProfile FreezeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(FreezeProfilePath);
            if (profile) return profile;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, FreezeProfilePath);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(-75f);
            color.colorFilter.Override(new Color(0.9f, 0.8f, 0.65f));
            color.postExposure.Override(-0.4f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.5f);
            vignette.smoothness.Override(0.45f);
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// Prefab người chơi dùng chung cho mọi màn. Đã có thì giữ nguyên (người khác có thể đã chỉnh).
        static GameObject PlayerPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existing) return existing;

            var root = new GameObject("Player") { tag = "Player" };
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0, 0.9f, 0);
            var health = root.AddComponent<Health>();
            health.Configure(TargetKind.Player, 100f);
            var body = root.AddComponent<FirstPersonController>();

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0, 1.65f, 0);
            body.cameraPivot = pivot;

            var camGo = new GameObject("MainCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var audio = camGo.AddComponent<AudioSource>();
            audio.playOnAwake = false;

            var viewModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            viewModel.name = "ViewModel_Placeholder";
            Object.DestroyImmediate(viewModel.GetComponent<Collider>());
            viewModel.transform.SetParent(camGo.transform, false);
            viewModel.transform.localPosition = new Vector3(0.25f, -0.22f, 0.55f);
            viewModel.transform.localScale = new Vector3(0.06f, 0.08f, 0.8f);
            viewModel.GetComponent<Renderer>().sharedMaterial = Mat("Wood", WoodColor);

            var weapons = camGo.AddComponent<WeaponController>();
            SetRef(weapons, "cam", cam);
            SetRef(weapons, "body", body);
            SetRef(weapons, "viewModel", viewModel);
            SetRef(weapons, "audioSource", audio);

            root.AddComponent<Interactor>();
            root.AddComponent<UpgradeApplier>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) throw new ArgumentException($"{target.GetType().Name} không có trường '{field}'");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
