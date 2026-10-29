using System.Collections;
using DBP.Core;
using DBP.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DBP.Weapons
{
    /// Dao găm của người chơi (phím V): đâm tầm 2,4 m, 55 sát thương, trúng đầu chí mạng ×2.
    /// Tay súng hạ xuống, lưỡi dao đâm tới trước mặt rồi thu về. Tự gắn lên camera người chơi.
    public class PlayerKnife : MonoBehaviour
    {
        const float Range = 2.4f, Damage = 55f, Cooldown = 0.75f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            var weapons = FindAnyObjectByType<WeaponController>();
            if (weapons && !weapons.GetComponent<PlayerKnife>()) weapons.gameObject.AddComponent<PlayerKnife>();
        }

        float nextAt;
        Transform knife;
        FirstPersonArms arms;
        WeaponController weapons;

        void Start()
        {
            weapons = GetComponent<WeaponController>();
            arms = FindAnyObjectByType<FirstPersonArms>();
            knife = BuildKnife();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || InputLock.Locked || Time.time < nextAt || !kb.vKey.wasPressedThisFrame) return;
            if (weapons && weapons.IsReloading) return;
            nextAt = Time.time + Cooldown;
            var body = GetComponentInParent<FirstPersonController>();
            if (body) DBP.AI.EnemyAnimatorDriver.Play(body, "Stab"); // thân nhân vật đâm lê (thấy ở góc thứ ba)
            StartCoroutine(Stab());
        }

        IEnumerator Stab()
        {
            var armsT = arms ? arms.transform : null;
            var armsRest = armsT ? armsT.localPosition : Vector3.zero;
            knife.gameObject.SetActive(!PlayerView.ThirdPerson); // góc thứ ba: thân nhân vật tự đâm, không vẽ dao trước camera
            bool struck = false;
            for (float t = 0; t < 0.36f; t += Time.deltaTime)
            {
                float k = Mathf.Sin(Mathf.Clamp01(t / 0.36f) * Mathf.PI);
                if (armsT) armsT.localPosition = armsRest + new Vector3(0.08f, -0.25f, -0.05f) * k; // hạ súng
                knife.localPosition = new Vector3(0.12f, -0.18f, 0.25f + 0.45f * k);
                knife.localRotation = Quaternion.Euler(-10f + 10f * k, -8f, 0);
                if (!struck && t > 0.12f) { struck = true; Hit(); }
                yield return null;
            }
            if (armsT) armsT.localPosition = armsRest;
            knife.gameObject.SetActive(false);
        }

        void Hit()
        {
            var cam = transform;
            if (!Physics.Raycast(PlayerView.AimOrigin(cam), cam.forward, out var hit, Range, ~0, QueryTriggerInteraction.Ignore)) return;
            var health = hit.collider.GetComponentInParent<Health>();
            if (!health || health.Kind == TargetKind.Player) return;
            bool crit = Health.IsHeadshot(health, hit.point);
            health.TakeDamage(Damage * (crit ? 2f : 1f), DamageType.Bullet, hit.point, crit);
            CombatEffects.Impact(hit.point, hit.normal, false);
        }

        Transform BuildKnife()
        {
            // Model dao găm thật (Combat Knife, gắn sẵn vào camera khi nối dây); không có thì dựng bằng khối
            var model = transform.Find("DaoGam_Model");
            if (model) { model.gameObject.SetActive(false); return model; }
            var root = new GameObject("DaoGam").transform;
            root.SetParent(transform, false);
            Part(root, new Vector3(0, 0, 0.12f), new Vector3(0.02f, 0.04f, 0.22f), new Color(0.78f, 0.8f, 0.82f)); // lưỡi
            Part(root, new Vector3(0, 0, -0.03f), new Vector3(0.09f, 0.025f, 0.02f), new Color(0.3f, 0.3f, 0.3f));  // chắn tay
            Part(root, new Vector3(0, 0, -0.1f), new Vector3(0.03f, 0.035f, 0.12f), new Color(0.32f, 0.2f, 0.12f)); // cán gỗ
            root.gameObject.SetActive(false);
            return root;
        }

        static void Part(Transform parent, Vector3 pos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", color); // vật liệu mặc định của khối không chắc có trong bản build
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
