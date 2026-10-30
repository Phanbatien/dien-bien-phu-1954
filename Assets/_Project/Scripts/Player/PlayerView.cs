using System.Collections.Generic;
using DBP.Core;
using DBP.Missions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DBP.Player
{
    /// Góc nhìn thứ nhất / thứ ba (phím T, nhớ trên máy). Người chơi có thân nhân vật thật (ThanNhanVat, model bộ đội
    /// của Long) cùng hoạt ảnh đi, bắn, đâm, ném, ngồi, trèo: góc thứ nhất chỉ đổ bóng, góc thứ ba hiện đầy đủ và camera
    /// lùi ra sau vai phải, gặp vách hào thì kéo vào gần. Tia bắn / tương tác vẫn theo tâm ngắm, nhưng xuất phát từ
    /// trước người nhân vật (AimOrigin) để không trúng chính mình hay vật sau lưng.
    public sealed class PlayerView : MonoBehaviour
    {
        const string Key = "dbp.view";
        static readonly Vector3 ShoulderOffset = new Vector3(.55f, .25f, -2.6f);
        static PlayerView current;

        public static bool ThirdPerson { get; private set; }
        /// Điểm xuất phát của tia ngắm từ camera.
        public static Vector3 AimOrigin(Transform cam) => cam.position + cam.forward * (current ? current.aimSkip : 0f);
        /// Đầu nòng súng trên thân nhân vật (góc thứ ba) để vẽ lửa đầu nòng, đạn vạch.
        public static Transform Muzzle => current && ThirdPerson ? current.ActiveMuzzle : null;
        /// Đang ngồi súng máy: ẩn tay + súng cá nhân, chỉ thấy khẩu súng máy.
        public static bool HideArms { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            var body = FindAnyObjectByType<FirstPersonController>();
            if (body && !body.GetComponent<PlayerView>()) body.gameObject.AddComponent<PlayerView>();
        }

        Transform pivot, cam, muzzle, bodyRifle, bodyBazooka, bazookaMuzzle;
        DBP.Weapons.WeaponController weapons;
        Transform ActiveMuzzle => bodyBazooka && bodyBazooka.gameObject.activeSelf && bazookaMuzzle ? bazookaMuzzle : muzzle;
        Vector3 camRest;
        float aimSkip, nextArmsScan;
        readonly List<Renderer> bodyRenderers = new List<Renderer>();
        Renderer[] armsRenderers = new Renderer[0];

        void Awake()
        {
            current = this;
            HideArms = false; // màn mới không còn ngồi súng máy
            pivot = transform.Find("CameraPivot");
            var c = GetComponentInChildren<Camera>();
            if (c) { cam = c.transform; camRest = cam.localPosition; }
            if (!pivot && cam) pivot = cam.parent;
            var body = transform.Find("ThanNhanVat");
            if (body)
            {
                bodyRenderers.AddRange(body.GetComponentsInChildren<Renderer>(true));
                foreach (var t in body.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "AlliedRifle") bodyRifle = t;
                    else if (t.name == "BazookaThan") bodyBazooka = t;
                }
                if (bodyRifle) muzzle = bodyRifle.Find("Muzzle");
                if (bodyBazooka) bazookaMuzzle = bodyBazooka.Find("Muzzle");
                weapons = GetComponentInChildren<DBP.Weapons.WeaponController>();
            }
            ThirdPerson = PlayerPrefs.GetInt(Key, 0) == 1;
        }

        void OnDestroy() { if (current == this) current = null; }

        public static void Set(bool third)
        {
            ThirdPerson = third;
            PlayerPrefs.SetInt(Key, third ? 1 : 0); PlayerPrefs.Save();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || InputLock.Locked || !MissionController.Current || !kb.tKey.wasPressedThisFrame) return;
            Set(!ThirdPerson);
            DBP.UI.CampaignNotices.Show(ThirdPerson ? "Góc nhìn thứ ba (phím T)" : "Góc nhìn thứ nhất (phím T)");
        }

        void LateUpdate()
        {
            if (!cam || !pivot) return;
            foreach (var r in bodyRenderers)
                if (r) r.shadowCastingMode = ThirdPerson ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            if (Time.unscaledTime >= nextArmsScan)
            {
                nextArmsScan = Time.unscaledTime + .25f;
                var arms = FindAnyObjectByType<FirstPersonArms>();
                armsRenderers = arms ? arms.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            }
            foreach (var r in armsRenderers) if (r) r.forceRenderingOff = ThirdPerson || HideArms;
            bool holdsBazooka = weapons && weapons.Inventory != null && weapons.Inventory.Current != null && weapons.Inventory.Current.Def.id == "W-BAZOOKA";
            if (bodyBazooka && bodyBazooka.gameObject.activeSelf != holdsBazooka) bodyBazooka.gameObject.SetActive(holdsBazooka);
            if (bodyRifle && bodyRifle.gameObject.activeSelf == holdsBazooka) bodyRifle.gameObject.SetActive(!holdsBazooka);

            if (!ThirdPerson)
            {
                cam.localPosition = camRest;
                aimSkip = 0f;
                return;
            }
            // Lùi ra sau vai; vướng vách / vật thì kéo camera vào trước chỗ vướng
            var from = pivot.position + pivot.right * .3f;
            var want = pivot.TransformPoint(ShoulderOffset);
            var dir = want - from;
            float dist = dir.magnitude;
            float allowed = dist;
            foreach (var hit in Physics.SphereCastAll(from, .2f, dir / dist, dist, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && hit.distance < allowed) allowed = Mathf.Max(.3f, hit.distance - .05f);
            cam.position = from + dir / dist * allowed;
            aimSkip = Mathf.Max(0f, Vector3.Dot(pivot.position - cam.position, cam.forward)) + .45f;
        }
    }
}
