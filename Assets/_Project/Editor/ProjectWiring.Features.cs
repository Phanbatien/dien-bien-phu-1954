using System.Collections.Generic;
using System.Linq;
using DBP.Core;
using DBP.Player;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Model thay khối hộp tạm, dùng lại đồ của Màn 1:
    ///   • Màn 3: bục đồi E, D ốp bao cát chồng lớp, lan can bao cát, ụ chữ U chỗ địch đứng, 2 thang gỗ mỗi đồi; khúc gỗ đổ thật.
    ///   • Màn 2: ụ súng máy = đại liên có chân chống + giá ba chân + dây đạn + hòm đạn; khối vuông thành hàng rào chống tăng.
    ///   • Mọi màn: thêm cây cháy, cây gãy đổ, gốc cụt.
    /// Gọi trong bước "Không khí bi tráng" (dựng lại mỗi lần, đồ nằm dưới gốc Atmosphere).
    public static partial class ProjectWiring
    {
        const string LmgPreset = "Assets/Low Poly Weapon Series V 4/Prefabs/Low Poly Series V 4 New/Weapons/PreSet/WWII_LMG_B_PreSet.prefab";
        const string RsgHedgehog = "Assets/RSG/URP/Prefabs/P_rsg_military_antitank_hedgehog_01.prefab";
        const string RsgWire = "Assets/RSG/URP/Prefabs/P_rsg_military_barbed_wire_01.prefab";
        const string PineLogs = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_logs.prefab";

        static string WireFeatures(Transform root, string sceneName, System.Random rng, List<Vector3> keep, Rect area)
        {
            var notes = new List<string>();
            if (sceneName.StartsWith("M3")) notes.Add(DressHills(root));
            if (sceneName.StartsWith("M2")) notes.Add(DressM2(root));
            if (sceneName.StartsWith("M5") && !HasBattlefieldTerrain) notes.Add(RiverBanks(root)); // có địa hình thì lòng sông đã thoải
            notes.Add($"{FallenTrees(root, rng, keep, area)} cây gãy đổ / cháy");
            notes.Add(WireWarMap(root, sceneName, rng, keep, area));
            return string.Join(", ", notes.Where(n => !string.IsNullOrEmpty(n)));
        }

        // ───────── Màn 5: bờ sông dốc thoải ─────────

        /// Lòng sông Nậm Rốm thấp hơn hai bờ 1,5 m, thành đứng nên rơi xuống là kẹt. Đắp dốc đất ~27° dọc hai bờ để leo lên được.
        static string RiverBanks(Transform root)
        {
            var river = GameObject.Find("SongNamRom");
            var ground = GameObject.Find("Ground");
            if (!river || !river.TryGetComponent<BoxCollider>(out var box)) return "";
            var b = box.bounds;
            float drop = 0f - b.max.y, run = drop * 2f, len = Mathf.Sqrt(run * run + drop * drop), angle = Mathf.Atan2(drop, run) * Mathf.Rad2Deg;
            var mat = ground && ground.TryGetComponent<Renderer>(out var gr) ? gr.sharedMaterial : null;
            // Hai đoạn mỗi bờ, chừa khoảng dưới đầu cầu để dốc (vùng cấm AI) không đè lên mặt cầu
            var bridge = GameObject.Find("CauMuongThanh");
            var bb = bridge && bridge.TryGetComponent<Collider>(out var bc) ? bc.bounds : new Bounds(b.center, Vector3.zero);
            var spans = new[] { (b.min.x, bb.min.x - 1f), (bb.max.x + 1f, b.max.x) };
            foreach (int side in new[] { -1, 1 })
            foreach (var (x0, x1) in spans)
            {
                float edge = side < 0 ? b.min.z : b.max.z;
                var bank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bank.name = "BoSongDoc";
                bank.transform.SetParent(root, true);
                bank.transform.rotation = Quaternion.Euler(-side * angle, 0, 0);
                bank.transform.localScale = new Vector3(x1 - x0, 0.4f, len);
                var mid = new Vector3((x0 + x1) * 0.5f, -drop * 0.5f, edge - side * run * 0.5f);
                bank.transform.position = mid - bank.transform.up * 0.2f;
                if (mat) bank.GetComponent<Renderer>().sharedMaterial = mat;
                GameObjectUtility.SetStaticEditorFlags(bank, StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic);
                NotWalkable(bank);
            }
            NotWalkable(river); // AI phải đi qua cầu, không lội sông
            return "bờ sông dốc thoải (chừa đầu cầu), AI qua cầu";
        }

        static void NotWalkable(GameObject go)
        {
            if (!go.TryGetComponent<NavMeshModifier>(out var mod)) mod = go.AddComponent<NavMeshModifier>();
            mod.overrideArea = true;
            mod.area = 1; // Not Walkable
        }

        // ───────── Màn 3: bục bao cát + thang ─────────

        static string DressHills(Transform root)
        {
            int ladders = 0, bags = 0;
            var player = PlayerPosition();
            foreach (var name in new[] { "DoiE", "DoiD" })
            {
                var hill = GameObject.Find(name);
                if (!hill || !hill.TryGetComponent<BoxCollider>(out var box)) continue;
                var b = box.bounds;
                var dress = new GameObject("BaoCat_" + name).transform;
                dress.SetParent(root, false);
                // Mặt hướng về người chơi (để đặt thang)
                var toPlayer = player - b.center; toPlayer.y = 0;
                Vector3 faceDir = Mathf.Abs(toPlayer.x) > Mathf.Abs(toPlayer.z) ? new Vector3(Mathf.Sign(toPlayer.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(toPlayer.z));
                var ladderSpots = new List<Vector3>();
                var faceCenter = new Vector3(b.center.x + faceDir.x * b.extents.x, b.min.y, b.center.z + faceDir.z * b.extents.z);
                var along = Vector3.Cross(Vector3.up, faceDir);
                foreach (float off in new[] { -b.size.magnitude * 0.12f, b.size.magnitude * 0.12f }) ladderSpots.Add(faceCenter + along * off);

                // Bốn mặt bục: bao cát chồng lớp (gạch so le), lan can bao cát mép trên chừa khe chỗ thang
                foreach (var dir in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                {
                    float half = Mathf.Abs(dir.x) > 0 ? b.extents.z : b.extents.x;
                    var side = Vector3.Cross(Vector3.up, dir);
                    var face = new Vector3(b.center.x + dir.x * b.extents.x, b.min.y, b.center.z + dir.z * b.extents.z);
                    float bagLen = 2.2f, bagH = SandbagRowHeight();
                    int rows = Mathf.CeilToInt(b.size.y / bagH), cols = Mathf.CeilToInt(half * 2f / bagLen);
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                        {
                            float x = -half + bagLen * (c + 0.5f + (r % 2) * 0.5f);
                            if (x > half) continue;
                            var p = face + side * x + dir * 0.25f + Vector3.up * (r * bagH);
                            PlaceFitted(dress, (r + c) % 2 == 0 ? SandbagPrefab : SandbagPrefab2, dress.InverseTransformPoint(p), Quaternion.LookRotation(side).eulerAngles.y + (r * 7 + c * 13) % 6 - 3, bagLen + 0.1f);
                            bags++;
                        }
                    // Lan can trên đỉnh
                    for (int c = 0; c < cols; c++)
                    {
                        var p = face + side * (-half + bagLen * (c + 0.5f)) - dir * 0.6f + Vector3.up * b.size.y;
                        if (ladderSpots.Any(l => Vector2.Distance(new Vector2(l.x, l.z), new Vector2(p.x, p.z)) < 2.2f)) continue; // khe lên thang
                        PlaceFitted(dress, SandbagPrefab, dress.InverseTransformPoint(p), Quaternion.LookRotation(side).eulerAngles.y, bagLen);
                        bags++;
                    }
                }
                // Ụ chữ U chỗ địch đứng trên bục
                foreach (var h in All<Health>().Where(h => h.Kind != TargetKind.Player && b.Contains(new Vector3(h.transform.position.x, b.max.y - 0.1f, h.transform.position.z))))
                {
                    var center = new Vector3(h.transform.position.x, b.max.y, h.transform.position.z);
                    var fwd = player - center; fwd.y = 0; fwd = fwd.normalized;
                    for (int i = -1; i <= 1; i++)
                    {
                        var d = Quaternion.Euler(0, i * 70f, 0) * fwd;
                        PlaceFitted(dress, SandbagPrefab2, dress.InverseTransformPoint(center + d * 1.4f), Quaternion.LookRotation(Vector3.Cross(Vector3.up, d)).eulerAngles.y, 1.8f);
                        bags++;
                    }
                }
                // Thang gỗ dựa vào mặt bục
                foreach (var spot in ladderSpots) { BuildLadder(dress, spot, faceDir, b.size.y + 1.1f); ladders++; }
            }
            // Khúc gỗ đổ dạng khối → khúc gỗ thật
            int logs = 0;
            foreach (var t in All<Transform>().Where(t => t.name.StartsWith("CayDo_") && t.GetComponent<MeshRenderer>()).ToArray())
            {
                t.GetComponent<MeshRenderer>().enabled = false;
                var log = PlaceFitted(root, PineLogs, root.InverseTransformPoint(new Vector3(t.position.x, t.position.y - t.lossyScale.y / 2f, t.position.z)), t.eulerAngles.y + 90f, 2.2f);
                if (log) logs++;
            }
            return $"{bags} bao cát ốp bục, {ladders} thang, {logs} khúc gỗ";
        }

        static float sandbagRowHeight;
        static float SandbagRowHeight()
        {
            if (sandbagRowHeight > 0f) return sandbagRowHeight;
            var tmp = new GameObject("DoChieuCao").transform;
            var go = PlaceFitted(tmp, SandbagPrefab, Vector3.zero, 0, 2.3f);
            sandbagRowHeight = go ? Mathf.Max(0.3f, WorldBounds(go).size.y * 0.92f) : 0.6f;
            Object.DestroyImmediate(tmp.gameObject);
            return sandbagRowHeight;
        }

        /// Thang gỗ: hai thanh dọc, bậc mỗi 0,35 m, nghiêng 12° tựa vào bục; vùng trèo là trigger bao quanh.
        static void BuildLadder(Transform parent, Vector3 baseCenter, Vector3 faceDir, float height)
        {
            var wood = LoadOrMakeMat("ThangGo", new Color(0.45f, 0.32f, 0.19f));
            var go = new GameObject("Thang");
            go.transform.SetParent(parent, false);
            float lean = 12f, reach = height * Mathf.Sin(lean * Mathf.Deg2Rad);
            go.transform.position = baseCenter + faceDir * (reach + 0.9f);
            go.transform.rotation = Quaternion.LookRotation(-faceDir); // forward = hướng vào bục
            var m = new PartsMesh(1);
            var top = new Vector3(0, height, reach);
            foreach (float x in new[] { -0.28f, 0.28f }) m.Rod(new Vector3(x, 0, 0), new Vector3(x, 0, 0) + top, 0.04f, 0);
            for (float y = 0.3f; y < height - 0.1f; y += 0.35f)
            {
                float z = reach * y / height;
                m.Rod(new Vector3(-0.3f, y, z), new Vector3(0.3f, y, z), 0.025f, 0);
            }
            go.AddComponent<MeshFilter>().sharedMesh = SaveFlora("ThangGo_" + Mathf.RoundToInt(height * 10), m.Build("ThangGo"));
            go.AddComponent<MeshRenderer>().sharedMaterial = wood;
            var area = go.AddComponent<BoxCollider>();
            area.center = new Vector3(0, height * 0.5f + 0.3f, reach * 0.5f - 0.2f);
            area.size = new Vector3(1.3f, height + 0.8f, reach + 1.6f);
            go.AddComponent<Ladder>();
            // Liên kết NavMesh chân thang → mặt bục để lính AI cũng trèo được (LadderClimber)
            var link = go.AddComponent<NavMeshLink>();
            link.startPoint = new Vector3(0, 0, -1.8f); // ra khỏi vùng bao cát chân bục, chạm NavMesh
            link.endPoint = new Vector3(0, height - 1.1f, reach + 1.9f);
            link.width = 0.8f;
            link.bidirectional = true;
        }

        // ───────── Màn 2: súng máy + hàng rào chống tăng ─────────

        static string DressM2(Transform root)
        {
            var notes = new List<string>();
            var gun = GameObject.Find("USungMay");
            if (gun)
            {
                var old = gun.transform.Find("ModelSungMay");
                if (old) Object.DestroyImmediate(old.gameObject);
                gun.GetComponent<MeshRenderer>().enabled = false;
                var model = UnscaledChild(gun.transform, "ModelSungMay");
                model.name = "ModelSungMay";
                // Súng máy hạng nặng trên giá ba chân (War Map); không có gói thì dùng LMG + giá tự dựng
                var hmg = PlaceFitted(model, WarMap + "Weapons/SM_Wep_German_HeavyMachineGun_Tripod_01.prefab", new Vector3(0, -0.77f, 0), 90f, 1.5f);
                var lmg = hmg ? null : PlaceFitted(model, LmgPreset, Vector3.zero, 90f, 1.35f) ?? PlaceFitted(model, LmgModelPath, Vector3.zero, 90f, 1.35f);
                if (lmg) { lmg.transform.localPosition = new Vector3(0, -0.05f, 0); var lb = WorldBounds(lmg); lmg.transform.position += Vector3.up * (gun.transform.position.y - lb.center.y); }
                var metal = darkMat;
                // Giá ba chân, dây đạn vắt ra, hòm đạn dưới chân
                for (int i = 0; i < (hmg ? 0 : 3); i++)
                {
                    float a = (90f + i * 120f) * Mathf.Deg2Rad;
                    var foot = new Vector3(Mathf.Cos(a) * 0.7f, -0.65f, Mathf.Sin(a) * 0.7f - 0.1f);
                    var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Object.DestroyImmediate(leg.GetComponent<Collider>());
                    leg.transform.SetParent(model, false);
                    leg.transform.localPosition = foot * 0.5f + new Vector3(0, -0.12f, 0);
                    leg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (new Vector3(0, -0.12f, 0) - foot).normalized);
                    leg.transform.localScale = new Vector3(0.04f, foot.magnitude * 0.5f, 0.04f);
                    leg.GetComponent<Renderer>().sharedMaterial = metal;
                }
                var brass = LoadOrMakeMat("DongDan", new Color(0.72f, 0.56f, 0.24f));
                for (int i = 0; i < 9; i++)
                {
                    var round = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Object.DestroyImmediate(round.GetComponent<Collider>());
                    round.transform.SetParent(model, false);
                    round.transform.localPosition = new Vector3(0.12f + i * 0.035f, -0.05f - i * i * 0.004f, 0.05f);
                    round.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    round.transform.localScale = new Vector3(0.018f, 0.04f, 0.018f);
                    round.GetComponent<Renderer>().sharedMaterial = brass;
                }
                PlaceFitted(model, AmmoBoxPrefab, new Vector3(0.45f, -0.75f, -0.2f), 15f, 0.55f);
                notes.Add("đại liên trên giá ba chân");
            }
            int fence = 0;
            foreach (var t in All<Transform>().Where(t => t.name.StartsWith("BaoCat_") && t.GetComponent<MeshRenderer>() && t.GetComponent<BoxCollider>()).ToArray())
            {
                t.GetComponent<MeshRenderer>().enabled = false; // giữ va chạm làm chỗ nấp
                var bottom = new Vector3(t.position.x, t.position.y - t.lossyScale.y / 2f, t.position.z);
                PlaceFitted(root, RsgHedgehog, root.InverseTransformPoint(bottom + Vector3.right * 0.45f), 30f + fence * 23f, 1.5f);
                PlaceFitted(root, RsgHedgehog, root.InverseTransformPoint(bottom - Vector3.right * 0.55f), 75f + fence * 17f, 1.3f);
                PlaceFitted(root, RsgWire, root.InverseTransformPoint(bottom + Vector3.forward * 0.9f), 0f, 2.3f);
                fence++;
            }
            if (fence > 0) notes.Add($"{fence} đoạn hàng rào chống tăng");
            return string.Join(", ", notes);
        }

        // ───────── Mọi màn: cây cháy, cây gãy đổ, gốc cụt ─────────

        static int FallenTrees(Transform root, System.Random rng, List<Vector3> keep, Rect area)
        {
            var group = new GameObject("CayGayDo").transform;
            group.SetParent(root, false);
            var charred = LoadOrMakeMat("Charred", new Color(0.06f, 0.055f, 0.05f));
            int placed = 0;
            for (int i = 0; i < 30; i++)
            {
                if (!FreeSpot(rng, keep, area, 26f, 3.5f, out var g)) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(i % 3 == 2 ? Debris[rng.Next(Debris.Length)] : DeadTrees[rng.Next(DeadTrees.Length)]);
                if (!prefab) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                go.transform.position = g.point;
                float yaw = (float)rng.NextDouble() * 360f;
                if (i % 3 == 0)
                {
                    // Cây gãy đổ nằm ngang, hơi lún đất
                    go.transform.rotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(84f + (float)rng.NextDouble() * 8f, 0, 0);
                    go.transform.position += Vector3.down * 0.15f;
                }
                else go.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 10f - 5f, yaw, (float)rng.NextDouble() * 10f - 5f);
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.3f, (float)rng.NextDouble());
                if (i % 2 == 1) // cháy đen
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(charred, r.sharedMaterials.Length).ToArray();
                foreach (var t in go.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
                placed++;
            }
            return placed;
        }
    }
}
