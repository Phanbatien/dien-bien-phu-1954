using DBP.AI;
using UnityEditor;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Hình dáng các mục tiêu (dựng lại mỗi lần nối scene, không đụng AI/collider):
    /// xe tăng M24 Chaffee nhiều chi tiết, lô cốt bê tông có bao cát + gỗ nóc, ổ súng máy vòng bao cát thật,
    /// gói bộc phá vải bạt có dây buộc. Thay các khối hộp tạm.
    public static partial class ProjectWiring
    {
        const string SandbagPrefab = "Assets/Defensive_props/Prefabs/fortifications/sandbag barricade.prefab";
        const string SandbagPrefab2 = "Assets/Defensive_props/Prefabs/fortifications/sandbag barricade2.prefab";
        const string AmmoBoxPrefab = "Assets/Defensive_props/Prefabs/defence/ammo_box.prefab";

        static Material Mat(string name, Color c) => LoadOrMakeMat(name, c);

        static GameObject Cyl(Transform parent, string name, Vector3 pos, Vector3 euler, float radius, float length, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = new Vector3(radius * 2, length / 2, radius * 2);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// Đặt prefab cho vừa chiều dài mong muốn (theo trục X của prefab), đáy chạm mặt đất cục bộ y.
        static GameObject PlaceFitted(Transform parent, string path, Vector3 pos, float yaw, float length)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = Vector3.zero;
            var b = WorldBounds(go);
            float longest = Mathf.Max(b.size.x, b.size.z);
            if (longest > 0.01f) go.transform.localScale *= length / longest;
            if (b.size.z > b.size.x) yaw += 90f;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localPosition = pos;
            b = WorldBounds(go);
            go.transform.position += Vector3.up * (parent.TransformPoint(pos).y - b.min.y);
            return go;
        }

        // ───────── Xe tăng M24 Chaffee ─────────

        static void DressTank(GameObject go)
        {
            var tank = go.GetComponent<ChaffeeTank>();
            var visual = UnscaledChild(go.transform, "Visual");
            var olive = tankMat; var dark = darkMat;
            var rubber = Mat("CaoSuXich", new Color(0.12f, 0.12f, 0.11f));
            float g = -0.85f; // mặt đất cục bộ (đáy xích)
            // Thân dưới, giáp trước nghiêng, sàn trên, chắn bùn
            Block(visual, "ThanDuoi", new Vector3(0, g + 0.62f, 0), new Vector3(2.3f, 0.5f, 4.5f), olive);
            var glacis = Block(visual, "GiapTruoc", new Vector3(0, g + 0.86f, 2.05f), new Vector3(2.3f, 0.12f, 1.05f), olive);
            glacis.transform.localRotation = Quaternion.Euler(-28, 0, 0);
            Block(visual, "SanTren", new Vector3(0, g + 1.0f, -0.45f), new Vector3(2.5f, 0.28f, 3.5f), olive);
            Block(visual, "DuoiXe", new Vector3(0, g + 0.95f, -2.25f), new Vector3(2.4f, 0.35f, 0.25f), olive);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 1.38f;
                Block(visual, "ChanBun", new Vector3(x, g + 0.92f, 0), new Vector3(0.62f, 0.05f, 4.9f), olive);
                Block(visual, "Xich", new Vector3(x, g + 0.42f, 0), new Vector3(0.5f, 0.12f, 4.6f), rubber);            // mặt trên xích
                Block(visual, "XichDay", new Vector3(x, g + 0.04f, 0), new Vector3(0.5f, 0.08f, 3.9f), rubber);        // mặt đáy
                for (int i = 0; i < 5; i++)
                    Cyl(visual, "BanhChiuLuc", new Vector3(side * 1.4f, g + 0.3f, -1.55f + i * 0.78f), new Vector3(0, 0, 90), 0.3f, 0.42f, dark);
                Cyl(visual, "BanhSao", new Vector3(side * 1.4f, g + 0.5f, 2.15f), new Vector3(0, 0, 90), 0.28f, 0.4f, dark);
                Cyl(visual, "BanhDan", new Vector3(side * 1.4f, g + 0.45f, -2.15f), new Vector3(0, 0, 90), 0.26f, 0.4f, dark);
                Block(visual, "HopDo", new Vector3(side * 0.85f, g + 1.22f, -1.6f), new Vector3(0.5f, 0.2f, 0.7f), olive);
            }
            Cyl(visual, "OngXa", new Vector3(0.7f, g + 0.95f, -2.42f), new Vector3(90, 0, 0), 0.08f, 0.2f, dark);
            Cyl(visual, "DenPha", new Vector3(-0.85f, g + 1.08f, 2.3f), new Vector3(90, 0, 0), 0.09f, 0.1f, dark);

            // Tháp pháo xoay được: gốc tròn, thân vát, mặt nạ, nòng 75 mm có chụp hãm, tháp chỉ huy, súng máy nóc
            var turret = new GameObject("Turret").transform;
            turret.SetParent(visual, false);
            turret.localPosition = new Vector3(0, g + 1.32f, -0.25f);
            Cyl(turret, "VongThap", new Vector3(0, -0.05f, 0), Vector3.zero, 0.85f, 0.2f, olive);
            Block(turret, "ThanThap", new Vector3(0, 0.22f, -0.05f), new Vector3(1.55f, 0.42f, 1.7f), olive);
            Block(turret, "NocThap", new Vector3(0, 0.45f, -0.1f), new Vector3(1.3f, 0.08f, 1.4f), olive);
            Block(turret, "MatNa", new Vector3(0, 0.2f, 0.92f), new Vector3(0.8f, 0.38f, 0.22f), dark);
            Cyl(turret, "NongPhao", new Vector3(0, 0.2f, 2.15f), new Vector3(90, 0, 0), 0.07f, 2.3f, dark);
            Cyl(turret, "ChupHam", new Vector3(0, 0.2f, 3.35f), new Vector3(90, 0, 0), 0.11f, 0.22f, dark);
            Cyl(turret, "ThapChiHuy", new Vector3(0.38f, 0.58f, -0.35f), Vector3.zero, 0.24f, 0.22f, olive);
            Cyl(turret, "NapChiHuy", new Vector3(0.38f, 0.7f, -0.35f), Vector3.zero, 0.2f, 0.04f, dark);
            Cyl(turret, "SungMayNoc", new Vector3(-0.35f, 0.62f, 0.1f), new Vector3(90, 0, 0), 0.025f, 0.9f, dark);
            Block(turret, "GiaSungMay", new Vector3(-0.35f, 0.54f, -0.1f), new Vector3(0.06f, 0.14f, 0.06f), dark);
            Block(turret, "HopSauThap", new Vector3(0, 0.2f, -1.0f), new Vector3(1.2f, 0.32f, 0.35f), olive);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(turret, false);
            muzzle.localPosition = new Vector3(0, 0.2f, 3.5f);
            if (tank) { SetRef(tank, "turret", turret); SetRef(tank, "muzzle", muzzle); }
        }

        // ───────── Lô cốt bê tông ─────────

        static void DressBunker(GameObject go)
        {
            var bunker = go.GetComponent<Bunker>();
            var size = go.transform.lossyScale;
            var visual = UnscaledChild(go.transform, "Visual");
            var stain = Mat("BeTongBan", new Color(0.4f, 0.39f, 0.36f));
            var wood = Mat("GoTron", new Color(0.36f, 0.26f, 0.17f));
            float floor = -size.y / 2;
            Block(visual, "Than", Vector3.zero, size, concreteMat);
            Block(visual, "ChanTuong", new Vector3(0, floor + 0.25f, 0), new Vector3(size.x + 0.3f, 0.5f, size.z + 0.3f), stain);
            Block(visual, "Mai", new Vector3(0, size.y / 2 + 0.2f, 0), new Vector3(size.x + 0.7f, 0.4f, size.z + 0.7f), concreteMat);
            // Gỗ tròn và đất phủ nóc
            for (int i = 0; i < 6; i++)
                Cyl(visual, "GoNoc", new Vector3(0, size.y / 2 + 0.5f, -size.z / 2 + 0.35f + i * (size.z - 0.7f) / 5f), new Vector3(0, 0, 90), 0.14f, size.x + 0.4f, wood);
            Block(visual, "DatNoc", new Vector3(0, size.y / 2 + 0.72f, 0), new Vector3(size.x * 0.9f, 0.25f, size.z * 0.9f), Mat("DatPhu", new Color(0.33f, 0.25f, 0.18f)));
            // Lỗ châu mai: khe sâu + mái che
            Block(visual, "LoChauMai", new Vector3(0, size.y * 0.15f, size.z / 2 + 0.01f), new Vector3(size.x * 0.6f, 0.32f, 0.06f), darkMat);
            Block(visual, "MaiChe", new Vector3(0, size.y * 0.15f + 0.26f, size.z / 2 + 0.2f), new Vector3(size.x * 0.75f, 0.12f, 0.45f), concreteMat);
            // Bao cát thật chắn hai bên và phía sau, chừa mặt trước cho lỗ châu mai
            float r = Mathf.Max(size.x, size.z) * 0.5f + 0.9f;
            for (int i = 0; i < 6; i++)
            {
                float a = (90f + 50f + i * 52f) * Mathf.Deg2Rad; // từ trước-phải vòng ra sau tới trước-trái
                var p = new Vector3(Mathf.Cos(a) * r, floor, Mathf.Sin(a) * r);
                PlaceFitted(visual, i % 2 == 0 ? SandbagPrefab : SandbagPrefab2, p, -a * Mathf.Rad2Deg + 90f, 1.8f);
            }
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(visual, false);
            muzzle.localPosition = new Vector3(0, size.y * 0.15f, size.z / 2 + 0.3f);
            if (bunker) SetRef(bunker, "muzzle", muzzle);
        }

        // ───────── Ổ súng máy ─────────

        static void DressNest(GameObject go)
        {
            var visual = UnscaledChild(go.transform, "Visual");
            float floor = -0.6f;
            // Vòng bao cát hình móng ngựa mở phía sau
            for (int i = 0; i < 5; i++)
            {
                float a = (90f - 100f + i * 50f) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(a) * 1.5f, floor, Mathf.Sin(a) * 1.5f);
                PlaceFitted(visual, i % 2 == 0 ? SandbagPrefab : SandbagPrefab2, p, -a * Mathf.Rad2Deg + 90f, 1.6f);
            }
            var lmg = AssetDatabase.LoadAssetAtPath<GameObject>(LmgModelPath);
            if (lmg)
            {
                var gun = (GameObject)PrefabUtility.InstantiatePrefab(lmg, visual);
                gun.transform.localPosition = new Vector3(0, 0.25f, 0.9f);
                gun.transform.localRotation = Quaternion.identity;
                foreach (var c in gun.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            }
            PlaceFitted(visual, AmmoBoxPrefab, new Vector3(0.7f, floor, -0.2f), 20f, 0.6f);
            PlaceFitted(visual, AmmoBoxPrefab, new Vector3(-0.6f, floor, -0.4f), -35f, 0.6f);
        }

        // ───────── Gói bộc phá ─────────

        static void DressSatchel(GameObject go)
        {
            var old = go.transform.Find("Visual");
            if (old) Object.DestroyImmediate(old.gameObject);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            var canvas = Mat("VaiBat", new Color(0.36f, 0.38f, 0.25f));
            var strap = Mat("DayBuoc", new Color(0.2f, 0.17f, 0.12f));
            // Đơn vị theo khối hộp 0,5 × 0,35 × 0,4 của collider
            Block(visual, "Goi", Vector3.zero, new Vector3(0.96f, 0.9f, 0.9f), canvas);
            Block(visual, "Nap", new Vector3(0, 0.47f, -0.12f), new Vector3(0.9f, 0.08f, 0.7f), canvas);
            Block(visual, "Day1", new Vector3(-0.25f, 0, 0), new Vector3(0.08f, 0.95f, 0.95f), strap);
            Block(visual, "Day2", new Vector3(0.25f, 0, 0), new Vector3(0.08f, 0.95f, 0.95f), strap);
            Block(visual, "Quai", new Vector3(0, 0.62f, 0), new Vector3(0.6f, 0.06f, 0.08f), strap);
            go.GetComponent<Renderer>().enabled = false;
        }
    }
}
