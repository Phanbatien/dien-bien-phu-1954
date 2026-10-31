using System.Collections.Generic;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Bố cục chiến trường có chủ đích (31/10) cho Màn 2, 3, 5 bằng gói War Map — đặt quanh tuyến đánh, mục tiêu và lối đi
    /// thay vì rải ngẫu nhiên ngoài rìa: ụ đất, hàng rào thép gai chừa lối, xác xe / pháo / máy bay làm chỗ nấp,
    /// trận địa Pháp có đại liên, cao xạ, pháo. Vật có hộp va chạm (cover) được đặt tránh vùng mục tiêu, cửa hầm, đầu cầu.
    public static partial class ProjectWiring
    {
        static List<Vector3> composeKeep;

        static string ComposeBattlefield(Transform group, string sceneName, List<Vector3> keep)
        {
            composeKeep = keep;
            int before = group.childCount;
            if (sceneName.StartsWith("M2")) ComposeM2(group);
            else if (sceneName.StartsWith("M3")) ComposeM3(group);
            else if (sceneName.StartsWith("M5")) ComposeM5(group);
            else return "";
            return $"{group.childCount - before} vật bố cục chiến trường";
        }

        /// Đặt một vật tại (x, z) trên mặt đất (kể cả mặt đồi).
        static GameObject Put(Transform g, string prefab, float x, float z, float yaw, float length, bool cover = false, float sink = 0f)
        {
            var p = new Vector3(x, 0, z);
            if (Ground(p, out var point)) p = point;
            if (!PlaceWar(g, prefab, p, yaw, length, sink, cover)) return null;
            composeKeep?.Add(p);
            return g.GetChild(g.childCount - 1).gameObject;
        }

        /// Một hàng vật nối tiếp từ (x0, z0) tới (x1, z1), bỏ các đoạn nằm trong khoảng hở 'gaps' (theo x).
        static void Line(Transform g, string[] prefabs, float x0, float z0, float x1, float z1, float step, float length, params (float from, float to)[] gaps)
        {
            var a = new Vector3(x0, 0, z0); var b = new Vector3(x1, 0, z1);
            float yaw = Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg + 90f;
            int n = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(a, b) / step));
            for (int i = 0; i <= n; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)n);
                bool inGap = false;
                foreach (var (from, to) in gaps) if (p.x > from && p.x < to) inGap = true;
                if (!inGap) Put(g, prefabs[i % prefabs.Length], p.x, p.z, yaw + (i % 2) * 6f - 3f, length);
            }
        }

        static readonly string[] WireFences = { "Environment/SM_Prop_Wire_Fence_01", "Environment/SM_Prop_Wire_Fence_02", "Environment/SM_Prop_Wire_Fence_03" };
        static readonly string[] WireTangled = { "Environment/SM_Prop_Wire_Fence_Tangled_01", "Environment/SM_Prop_Wire_Fence_Tangled_02", "Environment/SM_Prop_Wire_Fence_Tangled_03" };
        static readonly string[] Barricades = { "Environment/SM_Prop_Barricade_01", "Environment/SM_Prop_Barricade_02", "Environment/SM_Prop_Barricade_03" };

        /// Ụ đất màu đất (mẫu Mound của gói) ốp mép khối đồi / công sự.
        static void Bank(Transform g, Vector3 at, float yaw, float height, float lengthScale = 1f)
        {
            var go = Raw(g, "Environment/SM_Env_Trench_Mound_02", at, yaw, new Vector3(1.1f, height / .77f, lengthScale));
            if (go) foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterial = Earth();
        }

        // ───────── Màn 2: giữ đồi Độc Lập ─────────
        static void ComposeM2(Transform g)
        {
            // Đồi (khối 24 × 14, cao 2): ụ đất phủ ba mặt, bậc bắn sau bao cát hai bên ụ súng máy
            var hill = GameObject.Find("DoiDocLap");
            if (hill)
            {
                var b = hill.GetComponent<Collider>().bounds;
                if (!HasBattlefieldTerrain) // có địa hình thì đồi đã là gò đất
                for (float z = b.min.z + 4f; z < b.max.z; z += 9f) { Bank(g, new Vector3(b.min.x - 1.2f, 0, z), 180f, b.size.y); Bank(g, new Vector3(b.max.x + 1.2f, 0, z), 0f, b.size.y); }
                if (!HasBattlefieldTerrain) for (float x = b.min.x + 5f; x < b.max.x; x += 10f) Bank(g, new Vector3(x, 0, b.max.z + 1.2f), -90f, b.size.y);
                Put(g, "Environment/SM_Env_Trench_Firestep_05", -7f, 10.3f, 0f, 7.6f);
                Put(g, "Environment/SM_Env_Trench_Firestep_05", 7f, 10.3f, 0f, 7.6f);
                Put(g, "Environment/SM_Prop_Ladder_01", 0f, b.min.z - .2f, 0f, .8f);
                Put(g, "Environment/SM_Prop_Trench_Planks_01", -4f, 6f, 90f, 2.7f);
                Put(g, "Environment/SM_Prop_Trench_Planks_02", 4f, 6f, 90f, 2.8f);
            }
            // Vùng giữa hai trận tuyến: hai hàng thép gai chừa lối, xác xe pháo máy bay, hố nước
            Line(g, WireFences, -30f, 22f, 30f, 22f, 10f, 10f, (-8f, -3f), (3f, 8f));
            Line(g, WireFences, -30f, 34f, 30f, 34f, 10f, 10f, (-2f, 4f));
            foreach (var (x, z) in new[] { (-16f, 27f), (17f, 29f), (-2f, 40f) }) Put(g, WireTangled[Mathf.Abs((int)x) % 3], x, z, x * 7f, 5f);
            Put(g, "Vehicles/SM_Veh_German_Tank_01_Destroyed", -13f, 30f, 30f, 7.5f, true);
            Put(g, "Vehicles/SM_Veh_Small_Tank_01_Destroyed", 14f, 41f, -60f, 5.2f, true);
            Put(g, "Vehicles/SM_Veh_Artillery_01_Destroyed", 6f, 27f, 150f, 5.4f, true);
            Put(g, "Vehicles/SM_Veh_British_Plane_01_Destroyed", -22f, 45f, 20f, 8.7f, true);
            foreach (var (x, z) in new[] { (-4f, 30f), (10f, 35f), (-9f, 44f), (3f, 48f) }) Put(g, Barricades[(int)(z) % 3], x, z, x * 11f, 3.4f, true);
            foreach (var (x, z) in new[] { (-19f, 24f), (21f, 31f), (24f, 52f), (-26f, 56f), (-10f, 15f) }) Put(g, "Environment/SM_Env_Tree_Dead_04", x, z, z * 13f, 4.6f);
            // Trận địa Pháp phía bắc: chướng ngại, ụ đất, đại liên, cao xạ, pháo
            Line(g, Barricades, -22f, 66f, 22f, 66f, 7f, 3.4f, (-4f, 4f));
            Put(g, "Weapons/SM_Wep_German_HeavyMachineGun_Tripod_01", -8f, 64f, 180f, 1.4f);
            Put(g, "Weapons/SM_Wep_British_HeavyMachineGun_Tripod_01", 9f, 64f, 180f, 1.4f);
            Put(g, "Weapons/SM_Wep_AAGun_01", 19f, 72f, 200f, 2f, true);
            Put(g, "Vehicles/SM_Veh_Artillery_01", -17f, 74f, 180f, 5.4f, true);
            Put(g, "Environment/SM_Env_Trench_Mound_01", 0f, 69f, 0f, 4.5f);
        }

        // ───────── Màn 3: đêm đồi E, D ─────────
        static void ComposeM3(Transform g)
        {
            foreach (var name in new[] { "DoiE", "DoiD" })
            {
                var hill = GameObject.Find(name);
                if (!hill) continue;
                var b = hill.GetComponent<Collider>().bounds;
                float m = 3.5f;
                // Thép gai vòng quanh chân đồi, mặt nam (phía thang) chừa lối ở giữa
                Line(g, WireFences, b.min.x - m, b.max.z + m, b.max.x + m, b.max.z + m, 9f, 9f);
                Line(g, WireFences, b.min.x - m, b.min.z - m, b.min.x - m, b.max.z + m, 9f, 9f);
                Line(g, WireFences, b.max.x + m, b.min.z - m, b.max.x + m, b.max.z + m, 9f, 9f);
                Line(g, WireFences, b.min.x - m, b.min.z - m, b.max.x + m, b.min.z - m, 6f, 6f, (b.center.x - 6f, b.center.x + 6f));
                // Trên đỉnh: đại liên và tôn che
                Put(g, "Weapons/SM_Wep_German_HeavyMachineGun_Tripod_01", b.center.x + 4f, b.max.z - 3f, 180f, 1.4f);
                Put(g, "Environment/SM_Prop_Iron_Sheet_02", b.center.x - 5f, b.center.z + 3f, 30f, 1.8f);
            }
            var d = GameObject.Find("DoiD");
            if (d) { var b = d.GetComponent<Collider>().bounds; Put(g, "Weapons/SM_Wep_AAGun_01", b.center.x - 3f, b.center.z + 4f, 200f, 2f); }
            // Thung lũng: tuyến chướng ngại ngoài của Pháp, xác xe pháo, máy bay rơi, hố nước, ụ đất
            Line(g, Barricades, -30f, 44f, 30f, 44f, 8f, 3.4f, (-6f, 2f));
            Put(g, "Vehicles/SM_Veh_German_Tank_01_Destroyed", -4f, 36f, 40f, 7.5f, true);
            Put(g, "Vehicles/SM_Veh_British_Tank_01_Destroyed", 13f, 50f, -25f, 8f, true);
            Put(g, "Vehicles/SM_Veh_Artillery_01_Destroyed", -14f, 28f, 120f, 5.4f, true);
            Put(g, "Vehicles/SM_Veh_German_Plane_01_Destroyed", 20f, 30f, -70f, 6.5f, true);
            foreach (var (x, z) in new[] { (-3f, 16f), (9f, 21f), (-17f, 40f) }) Put(g, "Environment/SM_Env_Trench_Mound_0" + (1 + (int)Mathf.Abs(x) % 4), x, z, x * 9f, 4.5f);
            foreach (var (x, z) in new[] { (-28f, 30f), (27f, 44f), (-30f, 80f), (32f, 62f) }) Put(g, "Environment/SM_Env_Tree_Dead_04", x, z, z * 7f, 4.6f);
            Put(g, "Vehicles/SM_Veh_Artillery_01", 0f, 96f, 180f, 5.4f, true);
        }

        // ───────── Màn 5: vượt sông, ổ đề kháng, hầm chỉ huy ─────────
        static void ComposeM5(Transform g)
        {
            // Bờ nam (ta): tập kết sau chướng ngại và ụ đất; xác xe, hố nước
            foreach (var (x, z) in new[] { (-9f, 24f), (9f, 27f), (-14f, 33f), (14f, 34f) }) Put(g, Barricades[(int)z % 3], x, z, x > 0 ? -10f : 10f, 3.4f, true);
            Put(g, "Environment/SM_Env_Trench_Mound_02", -6f, 17f, 90f, 6f);
            Put(g, "Environment/SM_Env_Trench_Mound_01", 7f, 15f, 0f, 4.5f);
            Put(g, "Vehicles/SM_Veh_British_Tank_01_Destroyed", -22f, 20f, 60f, 8f, true);
            // Bờ bắc (địch): thép gai dọc bờ chừa đầu cầu; ổ đề kháng có ụ đất và đại liên
            Line(g, WireFences, -34f, 57f, 34f, 57f, 10f, 10f, (-6f, 6f));
            foreach (var x in new[] { -9f, 9f }) Put(g, Barricades[(int)Mathf.Abs(x) % 3], x, 60f, 0f, 3.4f, true);
            foreach (var name in new[] { "ODeKhang_1", "ODeKhang_2", "ODeKhang_3" })
            {
                var nest = GameObject.Find(name);
                if (!nest) continue;
                var p = nest.transform.position;
                Put(g, "Environment/SM_Env_Trench_Mound_01", p.x, p.z - 3.2f, 0f, 4.2f);
                Put(g, "Weapons/SM_Wep_German_HeavyMachineGun_Tripod_01", p.x + 2.2f, p.z - 1.5f, 180f, 1.4f);
                Put(g, "Environment/SM_Prop_Iron_Sheet_01", p.x - 2.4f, p.z + 1f, 70f, 1.8f);
            }
            // Quanh hầm chỉ huy (z 96–104, cửa phía nam để trống): cao xạ, bãi pháo, xe tăng đỗ, khinh khí cầu vỡ
            Put(g, "Weapons/SM_Wep_AAGun_01", -12f, 99f, 160f, 2f, true);
            Put(g, "Weapons/SM_Wep_AAGun_01", 12f, 99f, 200f, 2f, true);
            for (int i = 0; i < 3; i++) { Put(g, "Vehicles/SM_Veh_Artillery_01", -24f, 86f + i * 7f, 160f, 5.4f, true); Put(g, "Vehicles/SM_Veh_Artillery_01", 24f, 86f + i * 7f, 200f, 5.4f, true); }
            Put(g, "Vehicles/SM_Veh_German_Tank_01", 30f, 108f, 200f, 7.7f, true);
            Put(g, "Vehicles/SM_Veh_Small_Tank_01", -30f, 108f, 160f, 5f, true);
            Put(g, "Environment/SM_Prop_Blimp_Piece_02", -40f, 70f, 40f, 15f);
            Put(g, "Environment/SM_Prop_Blimp_Piece_05", 42f, 76f, -50f, 14f);
            Line(g, Barricades, -16f, 92f, -6f, 92f, 4f, 3.4f);
            Line(g, Barricades, 6f, 92f, 16f, 92f, 4f, 3.4f);
            foreach (var (x, z) in new[] { (-26f, 64f), (28f, 70f), (-20f, 118f), (22f, 120f), (-30f, 30f), (30f, 12f) }) Put(g, "Environment/SM_Env_Tree_Dead_04", x, z, z * 9f, 4.6f);
        }
    }
}
