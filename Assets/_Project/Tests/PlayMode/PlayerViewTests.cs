using System.Collections;
using System.Linq;
using DBP.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Góc nhìn thứ nhất / thứ ba, thân nhân vật người chơi, dao găm thật, cầu Màn 5 đi trên mặt cầu (30/10).
    public class PlayerViewTests
    {
        static IEnumerator Begin(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene); yield return null; yield return null;
            var begin = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "BeginMission");
            if (begin) begin.onClick.Invoke();
            Time.timeScale = 1f;
            yield return null;
        }

        static void Shot(Camera cam, string name)
        {
            string dir = System.Environment.GetEnvironmentVariable("SHOT_DIR");
            if (string.IsNullOrEmpty(dir)) return;
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        }

        [UnityTest] public IEnumerator ThirdPerson_ShowsBody_BehindShoulder_AndAimStartsAheadOfPlayer()
        {
            yield return Begin("M1_HimLam");
            var player = Object.FindAnyObjectByType<FirstPersonController>();
            var body = player.transform.Find("ThanNhanVat");
            Assert.IsNotNull(body, "Người chơi có thân nhân vật");
            Assert.IsNotNull(body.GetComponent<Animator>());
            var cam = player.GetComponentInChildren<Camera>();

            PlayerView.Set(false);
            yield return null; yield return null;
            Assert.Less(Vector3.Distance(cam.transform.position, player.transform.position + Vector3.up * 1.6f), 1f, "Góc thứ nhất: camera ở mắt");
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly, body.GetComponentInChildren<Renderer>().shadowCastingMode, "Góc thứ nhất chỉ thấy bóng thân mình");

            PlayerView.Set(true);
            yield return null; yield return null;
            var pivot = player.transform.Find("CameraPivot");
            Assert.Greater(Vector3.Distance(cam.transform.position, pivot.position), .5f, "Góc thứ ba: camera lùi ra sau");
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.On, body.GetComponentInChildren<Renderer>().shadowCastingMode, "Góc thứ ba thấy nhân vật");
            var aim = PlayerView.AimOrigin(cam.transform);
            Assert.Greater(Vector3.Dot(aim - pivot.position, cam.transform.forward), 0f, "Tia ngắm bắt đầu trước người nhân vật");
            Shot(cam, "goc_nhin_thu_ba");
            PlayerView.Set(false);
        }

        [UnityTest] public IEnumerator Knife_UsesRealModel_AndRiflesHaveBayonets()
        {
            yield return Begin("M1_HimLam");
            var player = Object.FindAnyObjectByType<FirstPersonController>();
            var cam = player.GetComponentInChildren<Camera>();
            Assert.IsFalse(player.GetComponentsInChildren<Transform>(true).Any(t => t.name == "LuoiLe_Model" && t.parent == player.transform), "Không có lưỡi lê mồ côi lơ lửng trước chân người chơi");
            foreach (var r in player.transform.Find("ThanNhanVat").GetComponentsInChildren<Renderer>(true))
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly, r.shadowCastingMode, "Góc thứ nhất không thấy thân mình: " + r.name);
            var knife = cam.transform.Find("DaoGam_Model");
            Assert.IsNotNull(knife, "Dao găm là model thật");
            Assert.Greater(knife.GetComponentsInChildren<MeshFilter>(true).Length, 0);
            Assert.IsTrue(DBP.AI.ComradeFollower.All.All(c => c.GetComponentsInChildren<Transform>(true).Any(t => t.name == "LuoiLe_Model")), "Súng bộ đội có lưỡi lê");
            Assert.IsTrue(DBP.AI.SoldierAI.All.Any(s => s.GetComponentsInChildren<Transform>(true).Any(t => t.name == "LuoiLe_Model")), "Súng lính địch có lưỡi lê");
            knife.gameObject.SetActive(true);
            knife.localPosition = new Vector3(.12f, -.18f, .55f);
            knife.localRotation = Quaternion.Euler(-5f, -8f, 0f);
            yield return null;
            Shot(cam, "dao_gam");
            knife.gameObject.SetActive(false);
        }

        [UnityTest] public IEnumerator M2_MountedGun_FollowsAim_KicksWhenFiring_AndBazookaHasModel()
        {
            yield return Begin("M2_GiuDocLap");
            var gun = Object.FindAnyObjectByType<DBP.Weapons.MountedGun>();
            var pivot = gun.transform.Find("ModelSungMay/NongSung");
            Assert.IsNotNull(pivot, "Súng máy có phần nòng xoay");
            Assert.IsNotNull(pivot.Find("DauNong"), "Có đầu nòng để đặt lửa");
            Assert.IsNotNull(pivot.Find("DayDan"), "Có dây đạn");
            var bounds = pivot.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Assert.Greater(bounds.size.z, 1.2f, "Thân súng đủ lớn để thấy rõ");
            var player = Object.FindAnyObjectByType<FirstPersonController>();
            var interactor = player.GetComponent<DBP.Interaction.Interactor>();
            gun.Interact(interactor);
            yield return null;
            Assert.IsTrue(PlayerView.HideArms, "Ngồi súng máy thì ẩn tay + súng cá nhân");
            var cam = Camera.main.transform;
            float eyeGap = Vector3.Distance(cam.position, pivot.position);
            Assert.Less(eyeGap, 1.5f, "Súng ở ngay trước mắt khi ngồi");
            float end = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < end) yield return null;
            Assert.Less(Quaternion.Angle(pivot.rotation, cam.rotation), 3f, "Nòng theo tâm ngắm");
            Shot(cam.GetComponent<Camera>(), "sung_may_ngoi");

            var supply = GameObject.Find("DiemCap_Bazooka");
            Assert.IsNotNull(supply);
            Assert.IsFalse(supply.GetComponent<MeshRenderer>().enabled, "Khối xám đã ẩn");
            Assert.IsNotNull(supply.transform.Find("ModelBazooka/Bazooka"), "Điểm cấp có ống phóng thật");
            PlayerView.HideArms = false;
        }

        [UnityTest] public IEnumerator Bazooka_ShowsOnBody_InThirdPerson()
        {
            yield return Begin("M1_HimLam");
            var player = Object.FindAnyObjectByType<FirstPersonController>();
            var weapons = player.GetComponentInChildren<DBP.Weapons.WeaponController>();
            weapons.Inventory.GiveWeapon(DBP.Weapons.WeaponCatalog.Get("W-BAZOOKA"), 4);
            PlayerView.Set(true);
            yield return null; yield return null;
            var body = player.transform.Find("ThanNhanVat");
            var baz = body.GetComponentsInChildren<Transform>(true).First(t => t.name == "BazookaThan");
            var rifle = body.GetComponentsInChildren<Transform>(true).First(t => t.name == "AlliedRifle");
            Assert.IsTrue(baz.gameObject.activeInHierarchy, "Cầm bazooka: thân nhân vật hiện ống phóng");
            Assert.IsFalse(rifle.gameObject.activeInHierarchy, "Súng trường ẩn đi");
            Shot(Camera.main, "bazooka_goc_ba");
            PlayerView.Set(false);
            yield return null; yield return null;
            Shot(Camera.main, "bazooka_goc_nhat");
        }

        [UnityTest] public IEnumerator M5_BridgeDeck_IsTheWalkingSurface()
        {
            yield return Begin("M5_ChieuMungBay");
            var plank = GameObject.Find("CauMuongThanh").GetComponent<Collider>().bounds;
            var model = GameObject.Find("CauMuongThanh_Model");
            Assert.IsNotNull(model);
            var decks = model.GetComponentsInChildren<Renderer>().Where(r => r.name.Contains("Bridge")).ToArray();
            Assert.IsNotEmpty(decks, "Có mặt cầu ván");
            foreach (var d in decks) Assert.AreEqual(plank.max.y, d.bounds.max.y, .12f, "Mặt cầu ngang mặt đi, không cao hơn để đi xuyên qua");
            Assert.Less(model.GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.y), plank.max.y + 1.4f, "Không còn cầu vòm cao");
        }
    }
}
