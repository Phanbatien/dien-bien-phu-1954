using System.IO;
using System.Linq;
using DBP.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DBP.EditorTools
{
    /// Bazooka (31/10): dự án không có model nên dựng bằng khối theo dáng ống phóng 60 mm — ống dài, loe đuôi, hai tay cầm,
    /// tựa vai, thước ngắm, đạn rocket thò ở đầu ống. Dùng cho tay góc nhìn thứ nhất (VM_Bazooka), thân nhân vật góc thứ ba
    /// và điểm cấp ở Màn 2. Gốc model = tay cầm sau, trục +z = đầu ống. Cùng tệp: khẩu đại liên Màn 2 có phần nòng xoay được.
    public static partial class ProjectWiring
    {
        const float BazookaAxisY = .1f;

        static Mesh BazookaMesh()
        {
            var m = new PartsMesh(3); // 0 ống ôliu · 1 kim loại tối · 2 gỗ
            var axis = new Vector3(0, BazookaAxisY, 0);
            Vector3 Z(float z) => axis + Vector3.forward * z;
            m.Rod(Z(-.68f), Z(.68f), .045f, 0);
            m.Rod(Z(-.76f), Z(-.72f), .072f, 1); // đuôi loe
            m.Rod(Z(-.72f), Z(-.68f), .058f, 1);
            m.Rod(Z(.64f), Z(.69f), .053f, 1);   // vành đầu ống
            foreach (var z in new[] { -.42f, -.12f, .18f, .44f }) m.Rod(Z(z), Z(z + .03f), .05f, 1);
            m.Rod(new Vector3(0, .06f, -.02f), new Vector3(0, -.1f, -.07f), .021f, 2); // tay cầm sau + cò
            m.Box(new Vector3(0, .045f, .02f), new Vector3(.03f, .03f, .1f), Quaternion.identity, 1);
            m.Rod(new Vector3(0, .06f, .32f), new Vector3(0, -.07f, .3f), .02f, 2);    // tay cầm trước
            m.Box(new Vector3(0, .035f, -.3f), new Vector3(.055f, .07f, .24f), Quaternion.identity, 2); // tựa vai
            m.Box(new Vector3(.062f, BazookaAxisY, -.06f), new Vector3(.03f, .05f, .12f), Quaternion.identity, 1); // hộp pin
            m.Box(new Vector3(-.058f, .165f, .26f), new Vector3(.008f, .065f, .02f), Quaternion.identity, 1);      // thước ngắm trước
            m.Box(new Vector3(-.058f, .155f, -.12f), new Vector3(.035f, .03f, .012f), Quaternion.identity, 1);     // thước ngắm sau
            return SaveMesh("MoHinh_Bazooka", m.Build("MoHinh_Bazooka"));
        }

        static Mesh RocketMesh()
        {
            var m = new PartsMesh(2); // 0 thân · 1 đầu đồng
            m.Rod(new Vector3(0, 0, -.32f), Vector3.zero, .03f, 0);
            m.Ellipsoid(new Vector3(0, 0, .04f), new Vector3(.08f, .08f, .2f), Quaternion.identity, 1);
            foreach (var a in new[] { 0f, 90f }) m.Box(new Vector3(0, 0, -.32f), new Vector3(.09f, .006f, .06f), Quaternion.Euler(0, 0, a), 0); // cánh đuôi
            return SaveMesh("MoHinh_RocketBazooka", m.Build("MoHinh_RocketBazooka"));
        }

        static GameObject BuildBazooka(string name)
        {
            var root = new GameObject(name);
            root.AddComponent<MeshFilter>().sharedMesh = BazookaMesh();
            root.AddComponent<MeshRenderer>().sharedMaterials = new[]
            {
                LoadOrMakeMat("BazookaOliu", new Color(.27f, .31f, .17f)),
                LoadOrMakeMat("BazookaKimLoai", new Color(.1f, .1f, .09f)),
                LoadOrMakeMat("BazookaGo", new Color(.36f, .22f, .11f)),
            };
            var rocket = new GameObject("DanRocket");
            rocket.transform.SetParent(root.transform, false);
            rocket.transform.localPosition = new Vector3(0, BazookaAxisY, .68f);
            rocket.AddComponent<MeshFilter>().sharedMesh = RocketMesh();
            rocket.AddComponent<MeshRenderer>().sharedMaterials = new[]
            {
                LoadOrMakeMat("BazookaOliu", new Color(.27f, .31f, .17f)),
                LoadOrMakeMat("DongDan", new Color(.72f, .56f, .24f)),
            };
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(0, BazookaAxisY, .78f);
            return root;
        }

        static void BuildBazookaViewModel(bool rebuild)
        {
            string path = $"{VmDir}/VM_Bazooka.prefab";
            if (!rebuild && AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("VM_Bazooka");
                SceneManager.MoveGameObjectToScene(root, preview);
                var model = BuildBazooka("Sung");
                SceneManager.MoveGameObjectToScene(model, preview);
                model.transform.SetParent(root.transform, false);
                var fore = new Vector3(0, -.02f, .3f);
                Anchor(root.transform, "GripR", Vector3.zero);
                Anchor(root.transform, "GripL", fore);
                Anchor(root.transform, "Muzzle", new Vector3(0, BazookaAxisY, .78f));
                Anchor(root.transform, "DuoiOng", new Vector3(0, BazookaAxisY, -.78f));
                AddAlliedPlayerArms(root, fore, preview);
                root.AddComponent<BazookaView>();
                foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        /// Điểm cấp bazooka Màn 2: ẩn khối xám, đặt ống phóng nằm trên hòm đạn cùng vài quả rocket.
        static string DressBazookaSupply()
        {
            var supply = All<Transform>().FirstOrDefault(t => t.name == "DiemCap_Bazooka");
            if (!supply) return "";
            if (Ground(new Vector3(supply.position.x, 0, supply.position.z), out var ground)) supply.position = ground + Vector3.up * supply.lossyScale.y * .5f;
            supply.GetComponent<MeshRenderer>().enabled = false;
            var holder = UnscaledChild(supply, "ModelBazooka");
            var floor = Vector3.down * supply.lossyScale.y * .5f;
            PlaceFitted(holder, AmmoBoxPrefab, floor, 20f, .9f);
            var baz = BuildBazooka("Bazooka");
            baz.transform.SetParent(holder, false);
            baz.transform.localPosition = floor + new Vector3(0, .62f, 0);
            baz.transform.localRotation = Quaternion.Euler(-8f, 70f, 0f);
            for (int i = 0; i < 3; i++)
            {
                var r = new GameObject("RocketDuPhong");
                r.transform.SetParent(holder, false);
                r.transform.localPosition = floor + new Vector3(-.75f + i * .14f, .04f, .25f);
                r.transform.localRotation = Quaternion.Euler(0, 10f + i * 4f, 0);
                r.AddComponent<MeshFilter>().sharedMesh = RocketMesh();
                r.AddComponent<MeshRenderer>().sharedMaterials = new[] { LoadOrMakeMat("BazookaOliu", new Color(.27f, .31f, .17f)), LoadOrMakeMat("DongDan", new Color(.72f, .56f, .24f)) };
            }
            return "ống phóng và đạn rocket trên hòm";
        }

        /// Đại liên Màn 2 thấy rõ: giá ba chân cao tới ngang ngực người ngồi, thân súng lớn đặt ngay dưới tầm mắt, hòm đạn, dây đạn.
        /// Phần xoay (NongSung) không đánh dấu tĩnh để MountedGun quay theo tâm ngắm.
        static void DressMountedGun(GameObject gun)
        {
            var seat = GameObject.Find("Seat");
            if (seat) gun.transform.position = seat.transform.position + Vector3.up * 1f + Vector3.forward * .95f;
            var old = gun.transform.Find("ModelSungMay");
            if (old) Object.DestroyImmediate(old.gameObject);
            gun.GetComponent<MeshRenderer>().enabled = false;
            var model = UnscaledChild(gun.transform, "ModelSungMay");
            float floorY = seat ? seat.transform.position.y - gun.transform.position.y - .05f : -1.2f;

            // Giá: ba chân thép to từ trục súng xuống đất
            var steel = LoadOrMakeMat("BazookaKimLoai", new Color(.1f, .1f, .09f));
            var legs = new PartsMesh(1);
            foreach (var a in new[] { 90f, 210f, 330f })
            {
                var foot = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * .75f, floorY, Mathf.Sin(a * Mathf.Deg2Rad) * .75f);
                legs.Rod(Vector3.down * .12f, foot, .028f, 0);
                legs.Rod(foot, foot + Vector3.down * .05f, .05f, 0); // đế chân
            }
            legs.Rod(Vector3.down * .16f, Vector3.zero, .05f, 0); // trục quay
            var stand = new GameObject("GiaBaChan");
            stand.transform.SetParent(model, false);
            stand.AddComponent<MeshFilter>().sharedMesh = SaveMesh("MoHinh_GiaDaiLien", legs.Build("MoHinh_GiaDaiLien"));
            stand.AddComponent<MeshRenderer>().sharedMaterial = steel;

            // Phần xoay: thân súng thật của gói, dây đạn, hộp đạn treo bên hông
            var pivot = new GameObject("NongSung").transform;
            pivot.SetParent(model, false);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(WarMap + "Weapons/SM_Wep_German_HeavyMachineGun_01.prefab")
                        ?? AssetDatabase.LoadAssetAtPath<GameObject>(LmgPreset);
            float length = 1f;
            if (asset)
            {
                var body = (GameObject)PrefabUtility.InstantiatePrefab(asset, pivot);
                foreach (var c in body.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                FixBuiltinMaterials(body);
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                body.transform.localScale = Vector3.one * 1.25f;
                length = WorldBounds(body).max.z - pivot.position.z;
            }
            var muzzle = new GameObject("DauNong").transform;
            muzzle.SetParent(pivot, false);
            muzzle.localPosition = new Vector3(0, .05f, length);

            var brass = LoadOrMakeMat("DongDan", new Color(.72f, .56f, .24f));
            var beltMesh = new PartsMesh(1);
            for (int i = 0; i < 14; i++)
            {
                var p = new Vector3(-.09f - i * .032f, .03f - i * i * .0028f, .12f);
                beltMesh.Rod(p + Vector3.back * .035f, p + Vector3.forward * .035f, .009f, 0);
            }
            var belt = new GameObject("DayDan");
            belt.transform.SetParent(pivot, false);
            belt.AddComponent<MeshFilter>().sharedMesh = SaveMesh("MoHinh_DayDanDaiLien", beltMesh.Build("MoHinh_DayDanDaiLien"));
            belt.AddComponent<MeshRenderer>().sharedMaterial = brass;
            var box = PlaceFitted(pivot, AmmoBoxPrefab, new Vector3(-.52f, -.5f, .12f), 90f, .42f);
            if (box) foreach (var t in box.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            PlaceFitted(model, AmmoBoxPrefab, new Vector3(.75f, floorY, -.35f), 15f, .6f);
        }
    }
}
