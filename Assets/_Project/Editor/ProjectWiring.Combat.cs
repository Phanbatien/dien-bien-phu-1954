using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBP.AI;
using DBP.Core;
using DBP.DebugTools;
using DBP.Interaction;
using DBP.Missions;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.EditorTools
{
    /// Gắn hệ thống AI & chiến đấu của Đạt (T07–T59) và model lính/lô cốt/xe tăng (T32, T33) vào scene có sẵn.
    public static partial class ProjectWiring
    {
        const string GrenadePrefabPath = "Assets/_Project/Prefabs/Weapons/Grenade.prefab";
        const string SoldierControllerPath = "Assets/_Project/Art/Characters/EnemySoldier.controller";
        const string InfantryPrefabPath = "Assets/tt-3d/LowPolyWarStarterPack/Character/Prefabs/Infantry.prefab";
        const string RiflePrefabPath = "Assets/tt-3d/LowPolyWarStarterPack/Weapon/Prefabs/Rifle1.prefab";
        const string LmgModelPath = "Assets/Low Poly Weapon Series V 4/Models/Low Poly Series V 4 New/Weapons/WWII_LMG_B.fbx";
        const string StarterAnims = "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/";
        const string NavMeshDir = "Assets/_Project/Scenes/NavMesh";

        static Material concreteMat, sandbagMat, metalMat, tankMat, darkMat, satchelMat;

        /// AI & chiến đấu cho một scene. Chạy lại được: phần đã gắn thì giữ nguyên.
        static string WireCombat(string sceneName)
        {
            var notes = new List<string>();
            EnsureSharedAssets();
            var enemies = All<Health>().Where(h => h.Kind != TargetKind.Player).ToList();

            // 1. Đặt lại lên mặt đất (Màn 1 có Terrain cao 8–10 m, hình nộm cũ còn ở độ cao 0)
            int snapped = enemies.Count(h => SnapToGround(h.transform));
            if (snapped > 0) notes.Add($"đặt lại {snapped} mục tiêu lên mặt đất");

            // 2. Lính bộ binh: AI theo màn + model + hoạt ảnh + chết thì ngã mờ (T07, T08, T32, T39, T52)
            int soldiers = 0;
            foreach (var h in enemies.Where(h => h.Kind == TargetKind.Infantry))
            {
                if (WireSoldier(h, sceneName)) soldiers++;
            }
            if (soldiers > 0) notes.Add($"{soldiers} lính có AI + model");
            int reinforced = WireReinforcements(sceneName, enemies);
            if (reinforced > 0) { notes.Add($"thêm {reinforced} lính địch (đủ 12)"); enemies = All<Health>().Where(h => h.Kind != TargetKind.Player).ToList(); }
            int officers = WireOfficers(enemies.Where(h => h.Kind == TargetKind.Infantry).ToList());
            if (officers > 0) notes.Add($"{officers} sĩ quan Pháp");

            // 3. Lô cốt + bộc phá (T15, T33)
            int bunkers = 0;
            foreach (var h in enemies.Where(h => h.Kind == TargetKind.Bunker))
                if (WireBunker(h)) bunkers++;
            if (bunkers > 0) notes.Add($"{bunkers} lô cốt + bộc phá");

            // 4. Ổ đề kháng / tổ súng máy (T59)
            int nests = 0;
            foreach (var h in enemies.Where(h => h.Kind == TargetKind.MachineGunNest))
                if (WireNest(h)) nests++;
            if (nests > 0) notes.Add($"{nests} ổ súng máy");

            // 5. Xe tăng Chaffee (T45)
            foreach (var h in enemies.Where(h => h.Kind == TargetKind.Tank))
                if (WireTank(h)) notes.Add("xe tăng Chaffee + điểm cấp bazooka");

            // 6. Nhóm phản kích theo đợt (T39)
            int groups = WireCounterAttacks();
            if (groups > 0) notes.Add($"{groups} nhóm phản kích");

            foreach (var satchel in All<SatchelCharge>()) DressSatchel(satchel.gameObject);

            // 7. Vật tư nhiệm vụ dự phòng (T30, BR-17)
            if (WireEmergencySupply()) notes.Add("điểm cấp bộc phá dự phòng");

            // 8. Nổ A1 làm hiệu lệnh ở Màn 5 (T59, BR-03)
            if (sceneName.StartsWith("M5") && WireA1Explosion()) notes.Add("sự kiện nổ đồi A1");

            // 9. NavMesh cho AI đi lại
            BakeNavMesh(sceneName);
            notes.Add("bake NavMesh");
            if (sceneName.Length > 1 && sceneName[0] == 'M' && "12345".Contains(sceneName[1]))
            {
                int comrades = WireComrades();
                if (comrades > 0) notes.Add($"{comrades} đồng đội bộ đội ta");
            }
            return string.Join(", ", notes);
        }

        // ───────────────────────── Lính ─────────────────────────

        static bool WireSoldier(Health h, string sceneName)
        {
            var go = h.gameObject;
            bool changed = false;
            var agent = go.GetComponent<NavMeshAgent>();
            if (!agent)
            {
                agent = go.AddComponent<NavMeshAgent>();
                agent.radius = 0.5f;      // × scale 0,7 = 0,35 m
                agent.height = 2f;
                agent.baseOffset = 1f;    // tâm capsule cao 1 m so với mặt đất
                agent.speed = 3.2f;
                agent.angularSpeed = 300f;
                agent.stoppingDistance = 1.5f;
                changed = true;
            }

            bool hasAi = go.GetComponent<SoldierAI>() || go.GetComponent<SentryAI>() || go.GetComponent<TrenchSoldierAI>();
            if (!hasAi)
            {
                if (sceneName.StartsWith("M3")) go.AddComponent<SentryAI>();          // lính gác ban đêm (T39)
                else if (sceneName.StartsWith("M4"))
                {
                    var trench = go.AddComponent<TrenchSoldierAI>();                    // cận chiến trong hào (T52)
                    SetRef(trench, "grenadePrefab", AssetDatabase.LoadAssetAtPath<Grenade>(GrenadePrefabPath));
                }
                else go.AddComponent<SoldierAI>();                                    // lính bắn trả, nấp (T07)
                changed = true;
            }

            foreach (var d in go.GetComponents<DisableOnDeath>()) { Object.DestroyImmediate(d); changed = true; }
            if (!go.GetComponent<EnemyDeathHandler>()) { go.AddComponent<EnemyDeathHandler>(); changed = true; }

            var eye = go.transform.Find("Eye");
            if (!eye)
            {
                eye = new GameObject("Eye").transform;
                eye.SetParent(go.transform, false);
                eye.localPosition = new Vector3(0, 0.7f, 0);
                foreach (var ai in go.GetComponents<MonoBehaviour>())
                    if (ai is SoldierAI || ai is SentryAI || ai is TrenchSoldierAI) { SetRefIfExists(ai, "eyePoint", eye); SetRefIfExists(ai, "muzzle", eye); }
                changed = true;
            }

            if (!go.transform.Find("Model")) { AddSoldierModel(go.transform); changed = true; }
            return changed;
        }

        /// Mỗi phe 10–20 người: màn ít lính địch thì nhân bản lính có sẵn (cùng AI, model) rải quanh chỗ cũ cho đủ 12.
        static int WireReinforcements(string sceneName, List<Health> enemies)
        {
            if (!(sceneName.Length > 1 && sceneName[0] == 'M' && "12345".Contains(sceneName[1]))) return 0;
            var infantry = enemies.Where(h => h.Kind == TargetKind.Infantry && !h.GetComponent<DBP.AI.OfficerLeader>()).ToList();
            var sources = infantry.Where(h => h.gameObject.activeInHierarchy).ToList();
            if (sources.Count == 0) sources = infantry;
            if (sources.Count == 0) return 0;
            int have = infantry.Count + enemies.Count(h => h.GetComponent<DBP.AI.OfficerLeader>());
            var rng = new System.Random(StableHash(sceneName + "/tang-vien"));
            var player = PlayerPosition();
            int added = 0;
            for (int i = 0; have + added < 12 && i < 60; i++)
            {
                var src = sources[rng.Next(sources.Count)];
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = Mathf.Lerp(4f, 10f, (float)rng.NextDouble());
                var p = src.transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                if ((p - player).sqrMagnitude < 20f * 20f || !GroundHit(p, out var g)) continue;
                var clone = Object.Instantiate(src.gameObject, src.transform.parent);
                clone.name = "TangVien_" + (have + added);
                float halfHeight = src.transform.position.y - BottomY(src.transform);
                clone.transform.position = g.point + Vector3.up * halfHeight;
                clone.transform.rotation = Quaternion.LookRotation(Flat(player - g.point).normalized);
                added++;
            }
            return added;
        }

        const string OfficerPrefabPath = "Assets/tt-3d/LowPolyWarStarterPack/Character/Prefabs/Officer.prefab";

        /// Sĩ quan Pháp (model Officer): khoảng 1 người mỗi 7 lính, đứng phía sau đội hình, cách nhau ≥ 15 m.
        static int WireOfficers(List<Health> infantry)
        {
            if (infantry.Count == 0) return 0;
            var player = PlayerPosition();
            var chosen = infantry.Where(h => h.GetComponent<DBP.AI.OfficerLeader>()).ToList();
            int want = Mathf.Max(1, infantry.Count / 7);
            foreach (var h in infantry.OrderByDescending(h => (h.transform.position - player).sqrMagnitude))
            {
                if (chosen.Count >= want) break;
                if (chosen.Contains(h) || chosen.Any(c => Vector3.Distance(c.transform.position, h.transform.position) < 15f)) continue;
                chosen.Add(h);
            }
            int added = 0;
            foreach (var h in chosen)
            {
                if (h.GetComponent<DBP.AI.OfficerLeader>()) continue;
                h.gameObject.AddComponent<DBP.AI.OfficerLeader>();
                h.gameObject.name = "SiQuanPhap_" + added;
                AddSoldierModel(h.transform, OfficerPrefabPath);
                added++;
            }
            return added;
        }

        static void AddSoldierModel(Transform soldier, string prefabPath = InfantryPrefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!prefab) return;
            var root = UnscaledChild(soldier, "Model");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            // Cao 1,8 m, chân chạm đáy capsule (tâm capsule ở 1 m trên mặt đất)
            var bounds = WorldBounds(model);
            float scale = bounds.size.y > 0.01f ? 1.8f / bounds.size.y : 1f;
            model.transform.localScale = Vector3.one * scale;
            bounds = WorldBounds(model);
            model.transform.position += Vector3.up * (soldier.position.y - 1f - bounds.min.y);

            if (!model.TryGetComponent<Animator>(out var animator)) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SoldierControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            if (!model.GetComponent<EnemyAnimatorDriver>()) model.AddComponent<EnemyAnimatorDriver>();

            // Súng trên tay phải
            var hand = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            var rifle = AssetDatabase.LoadAssetAtPath<GameObject>(RiflePrefabPath);
            if (hand && rifle)
            {
                var gun = (GameObject)PrefabUtility.InstantiatePrefab(rifle, hand);
                gun.transform.localPosition = Vector3.zero;
                gun.transform.localRotation = Quaternion.Euler(0, 90, 0);
                foreach (var c in gun.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            }
            foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c); // capsule giữ va chạm
            soldier.GetComponent<MeshRenderer>().enabled = false;
        }

        // ───────────────────────── Lô cốt ─────────────────────────

        static bool WireBunker(Health h)
        {
            var go = h.gameObject;
            if (go.GetComponent<Bunker>()) { DressBunker(go); return false; }
            var bunker = go.AddComponent<Bunker>();
            var size = go.transform.lossyScale; // khối 4 × 2,5 × 4
            var forward = DirectionToPlayer(go.transform.position);
            go.transform.rotation = Quaternion.LookRotation(forward);

            DressBunker(go);
            go.GetComponent<MeshRenderer>().enabled = false;

            // Bộc phá đặt sẵn trước lô cốt, hướng về phía người chơi (BR-17)
            var satchelGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            satchelGo.name = "BocPha_" + go.name;
            satchelGo.transform.position = go.transform.position + forward * (size.z / 2 + 2.2f);
            satchelGo.transform.localScale = new Vector3(0.5f, 0.35f, 0.4f);
            satchelGo.GetComponent<Renderer>().sharedMaterial = satchelMat;
            var satchel = satchelGo.AddComponent<SatchelCharge>();
            satchel.Count = 2;
            SnapToGround(satchelGo.transform);
            return true;
        }

        // ───────────────────────── Ổ súng máy ─────────────────────────

        static bool WireNest(Health h)
        {
            var go = h.gameObject;
            if (go.GetComponent<ResistanceNest>()) { DressNest(go); return false; }
            go.AddComponent<ResistanceNest>();
            var forward = DirectionToPlayer(go.transform.position);
            go.transform.rotation = Quaternion.LookRotation(forward);
            DressNest(go);
            go.GetComponent<MeshRenderer>().enabled = false;
            return true;
        }

        // ───────────────────────── Xe tăng ─────────────────────────

        static bool WireTank(Health h)
        {
            var go = h.gameObject;
            if (go.GetComponent<ChaffeeTank>()) { DressTank(go); return false; }
            var tank = go.AddComponent<ChaffeeTank>();
            var toPlayer = DirectionToPlayer(go.transform.position);
            go.transform.rotation = Quaternion.LookRotation(toPlayer);

            DressTank(go);
            go.GetComponent<MeshRenderer>().enabled = false;

            // Lộ trình: tiến về phía người chơi rồi dừng cách khoảng 18 m
            var path = new GameObject("LoTrinh_" + go.name).transform;
            var wps = new List<Transform>();
            var player = PlayerPosition();
            float total = Vector3.Distance(Flat(go.transform.position), Flat(player));
            for (int i = 1; i <= 3; i++)
            {
                var wp = new GameObject("Diem_" + i).transform;
                wp.SetParent(path, false);
                float t = Mathf.Clamp01((total - 18f) / Mathf.Max(1f, total)) * i / 3f;
                wp.position = Vector3.Lerp(go.transform.position, player, t);
                SnapToGround(wp);
                wps.Add(wp);
            }
            var so = new SerializedObject(tank);
            var list = so.FindProperty("waypoints");
            list.arraySize = wps.Count;
            for (int i = 0; i < wps.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = wps[i];

            // Phương án chống tăng bảo đảm (BR-17): điểm cấp bazooka cạnh người chơi
            var supply = GameObject.CreatePrimitive(PrimitiveType.Cube);
            supply.name = "DiemCap_Bazooka";
            supply.transform.position = player + Quaternion.Euler(0, 90, 0) * toPlayer * -2.5f;
            supply.transform.localScale = new Vector3(1.2f, 0.4f, 0.4f);
            supply.GetComponent<Renderer>().sharedMaterial = metalMat;
            var sp = supply.AddComponent<MissionSupplyPoint>();
            SetString(sp, "supplyType", "Bazooka");
            SetInt(sp, "quantity", 4);
            SnapToGround(supply.transform);
            var points = so.FindProperty("antiTankSupplyPoints");
            points.arraySize = 1;
            points.GetArrayElementAtIndex(0).objectReferenceValue = supply.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // ───────────────────────── Nhóm phản kích, vật tư, A1 ─────────────────────────

        static int WireCounterAttacks()
        {
            int n = 0;
            foreach (var repel in All<RepelWavesObjective>())
            foreach (var wave in repel.waves.Where(w => w))
            {
                if (wave.GetComponent<CounterAttackGroup>()) continue;
                var members = wave.GetComponentsInChildren<SoldierAI>(true).ToList();
                if (members.Count == 0) continue; // lính hào (Màn 4) tự xử lý, không dùng nhóm phản kích
                var group = wave.AddComponent<CounterAttackGroup>();
                var so = new SerializedObject(group);
                var list = so.FindProperty("members");
                list.arraySize = members.Count;
                for (int i = 0; i < members.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = members[i];
                var retreat = new GameObject("DiemRut_" + wave.name).transform;
                retreat.SetParent(wave.transform.parent, false);
                var center = members.Count > 0 ? members.Aggregate(Vector3.zero, (s, m) => s + m.transform.position) / members.Count : wave.transform.position;
                retreat.position = center - DirectionToPlayer(center) * 25f;
                SnapToGround(retreat);
                so.FindProperty("retreatPoint").objectReferenceValue = retreat;
                so.ApplyModifiedPropertiesWithoutUndo();
                n++;
            }
            return n;
        }

        static bool WireEmergencySupply()
        {
            if (!All<Bunker>().Any() || All<EmergencyResupply>().Any()) return false;
            var player = PlayerPosition();
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "DiemCap_BocPha_DuPhong";
            go.transform.position = player + Vector3.right * 2.5f;
            go.transform.localScale = new Vector3(0.8f, 0.5f, 0.5f);
            go.GetComponent<Renderer>().sharedMaterial = satchelMat;
            var sp = go.AddComponent<MissionSupplyPoint>();
            SetString(sp, "supplyType", "Satchel");
            SetInt(sp, "quantity", 1);
            SnapToGround(go.transform);
            var manager = new GameObject("CapBoSungKhanCap").AddComponent<EmergencyResupply>();
            manager.emergencyPoints.Add(sp);
            return true;
        }

        static bool WireA1Explosion()
        {
            if (All<ExplosionA1Event>().Any()) return false;
            var mission = All<MissionController>().FirstOrDefault();
            if (!mission) return false;
            var player = PlayerPosition();
            var go = new GameObject("NoDoiA1_HieuLenh");
            go.transform.SetParent(mission.transform, false);
            go.transform.position = player + DirectionForward() * 6f + Vector3.up;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(12, 4, 4);
            var a1 = go.AddComponent<ExplosionA1Event>();
            a1.Title = "Hiệu lệnh: bộc phá đồi A1 nổ (20h30 ngày 6/5/1954)";
            var point = new GameObject("DiemNo").transform;
            point.SetParent(go.transform, false);
            point.position = player + DirectionForward() * 120f + Vector3.up * 5f;
            SetRefIfExists(a1, "explosionPoint", point);
            var so = new SerializedObject(mission);
            var list = so.FindProperty("objectives");
            list.InsertArrayElementAtIndex(0);
            list.GetArrayElementAtIndex(0).objectReferenceValue = a1;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // ───────────────────────── NavMesh ─────────────────────────

        static void BakeNavMesh(string sceneName)
        {
            var surface = All<NavMeshSurface>().FirstOrDefault();
            if (!surface)
            {
                surface = new GameObject("NavMesh").AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            }
            // Mục tiêu và vật tương tác không được khoét lỗ trên NavMesh
            var temp = All<Health>().Select(h => h.GetComponent<Collider>())
                .Concat(All<SatchelCharge>().Select(s => s.GetComponent<Collider>()))
                .Concat(All<MissionSupplyPoint>().Select(s => s.GetComponent<Collider>()))
                .Where(c => c && c.enabled).ToList();
            foreach (var c in temp) c.enabled = false;
            surface.BuildNavMesh();
            foreach (var c in temp) c.enabled = true;

            Directory.CreateDirectory(NavMeshDir);
            var path = $"{NavMeshDir}/{sceneName}_NavMesh.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            EditorUtility.SetDirty(surface);
        }

        // ───────────────────────── Tài nguyên dùng chung ─────────────────────────

        static void EnsureSharedAssets()
        {
            concreteMat ??= LoadOrMakeMat("Concrete", new Color(0.5f, 0.5f, 0.48f));
            sandbagMat ??= LoadOrMakeMat("Sandbag", new Color(0.6f, 0.55f, 0.4f));
            metalMat ??= LoadOrMakeMat("Metal", new Color(0.2f, 0.22f, 0.2f));
            tankMat ??= LoadOrMakeMat("TankOlive", new Color(0.33f, 0.36f, 0.24f));
            darkMat ??= LoadOrMakeMat("DarkSteel", new Color(0.08f, 0.08f, 0.08f));
            satchelMat ??= LoadOrMakeMat("Satchel", new Color(0.25f, 0.32f, 0.18f));
            EnsureGrenadePrefab();
            EnsureSoldierController();
            EnsurePlayerGrenades();
            EnsureSmgViewModel();
            EnsureCrateModel();
        }

        /// Model hòm tiếp tế cho QuizCrate: hòm đạn quân dụng (khác hòm gỗ trang trí), chuẩn hóa về khối 1×1×1 tâm ở gốc.
        static void EnsureCrateModel()
        {
            const string path = "Assets/_Project/Resources/HomTiepTe.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Defensive_props/Prefabs/defence/ammo_box.prefab");
            if (!source) return;
            var root = new GameObject("HomTiepTe");
            var fit = new GameObject("Fit").transform; // khung không xoay, co giãn theo trục thế giới
            fit.SetParent(root.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, fit);
            foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var b = WorldBounds(model);
            // Cạnh dài nằm theo trục X như khối hộp 0,9 × 0,6 × 0,6
            if (b.size.z > b.size.x) { model.transform.rotation = Quaternion.Euler(0, 90, 0); b = WorldBounds(model); }
            fit.localScale = new Vector3(1f / b.size.x, 1f / b.size.y, 1f / b.size.z);
            b = WorldBounds(model);
            fit.position -= b.center;
            UrpMaterialUtility.Apply(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        public static void EnsureCrateModelCI()
        {
            try { EnsureCrateModel(); AssetDatabase.SaveAssets(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static Material LoadOrMakeMat(string name, Color color)
        {
            var path = $"Assets/_Project/Art/Materials/Greybox/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void EnsureGrenadePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GrenadePrefabPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(GrenadePrefabPath));
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Grenade";
            go.transform.localScale = Vector3.one * 0.12f;
            go.GetComponent<Renderer>().sharedMaterial = LoadOrMakeMat("Satchel", new Color(0.25f, 0.32f, 0.18f));
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.6f;
            go.AddComponent<Grenade>();
            PrefabUtility.SaveAsPrefabAsset(go, GrenadePrefabPath);
            Object.DestroyImmediate(go);
        }

        static void EnsureSoldierController()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(SoldierControllerPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(SoldierControllerPath));
            AnimationClip Clip(string file) => AssetDatabase.LoadAllAssetsAtPath(StarterAnims + file)
                .OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            var controller = AnimatorController.CreateAnimatorControllerAtPath(SoldierControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var state = controller.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            var idle = Clip("Stand--Idle.anim.fbx");
            var walk = Clip("Locomotion--Walk_N.anim.fbx");
            var run = Clip("Locomotion--Run_N.anim.fbx");
            if (idle) tree.AddChild(idle, 0f);
            if (walk) tree.AddChild(walk, 1.6f);
            if (run) tree.AddChild(run, 4.5f);
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
        }

        /// Ném lựu đạn bằng phím G (T14) cho prefab người chơi.
        static void EnsurePlayerGrenades()
        {
            const string playerPath = "Assets/_Project/Prefabs/Player.prefab";
            var root = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                if (root.GetComponentInChildren<GrenadeThrowController>(true)) return;
                var cam = root.GetComponentInChildren<Camera>(true);
                var thrower = cam.gameObject.AddComponent<GrenadeThrowController>();
                var point = new GameObject("ThrowPoint").transform;
                point.SetParent(cam.transform, false);
                point.localPosition = new Vector3(0.2f, -0.1f, 0.6f);
                SetRef(thrower, "grenadePrefab", AssetDatabase.LoadAssetAtPath<Grenade>(GrenadePrefabPath));
                SetRef(thrower, "throwPoint", point);
                SetRef(thrower, "cam", cam);
                PrefabUtility.SaveAsPrefabAsset(root, playerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // ───────────────────────── Tiện ích ─────────────────────────

        /// Đặt đáy vật lên mặt đất thật bên dưới (dò từ trên cao, bỏ qua chính nó và các mục tiêu khác).
        static bool SnapToGround(Transform t)
        {
            Physics.SyncTransforms();
            var own = t.GetComponentsInChildren<Collider>(true);
            float bottomOffset = t.position.y - BottomY(t);
            foreach (var hit in Physics.RaycastAll(new Vector3(t.position.x, t.position.y + 60f, t.position.z), Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore)
                         .OrderBy(h => h.distance))
            {
                if (own.Contains(hit.collider) || hit.collider.GetComponentInParent<Health>() || hit.collider.GetComponentInParent<QuizCrate>()) continue;
                float y = hit.point.y + bottomOffset;
                if (Mathf.Abs(y - t.position.y) < 0.02f) return false;
                t.position = new Vector3(t.position.x, y, t.position.z);
                return true;
            }
            return false;
        }

        /// Đáy thế giới tính từ hình dạng collider/mesh, đúng cả với object đang tắt (bounds của chúng rỗng).
        static float BottomY(Transform t)
        {
            switch (t.GetComponent<Collider>())
            {
                case BoxCollider box: return t.TransformPoint(box.center - Vector3.up * box.size.y / 2f).y;
                case CapsuleCollider cap when cap.direction == 1: return t.TransformPoint(cap.center - Vector3.up * cap.height / 2f).y;
                case SphereCollider sph: return t.TransformPoint(sph.center).y - sph.radius * t.lossyScale.y;
            }
            var mf = t.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh) return t.TransformPoint(new Vector3(0, mf.sharedMesh.bounds.min.y, 0)).y;
            return t.position.y;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// Con có tỉ lệ thế giới = 1 dưới một vật bị co giãn (để dựng model theo đơn vị mét).
        static Transform UnscaledChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing) Object.DestroyImmediate(existing.gameObject);
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            var s = parent.lossyScale;
            child.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
            return child;
        }

        static GameObject Block(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // va chạm dùng collider gốc của mục tiêu
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(go, 0);
            return go;
        }

        static Vector3 PlayerPosition()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            return p ? p.transform.position : Vector3.zero;
        }

        static Vector3 DirectionForward()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            return p ? Flat(p.transform.forward).normalized : Vector3.forward;
        }

        static Vector3 DirectionToPlayer(Vector3 from)
        {
            var d = Flat(PlayerPosition() - from);
            return d.sqrMagnitude > 0.01f ? d.normalized : Vector3.back;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name} không có trường '{field}'");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRefIfExists(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
