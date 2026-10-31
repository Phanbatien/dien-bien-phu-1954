using DBP.Core;
using UnityEngine;

namespace DBP.Weapons
{
    /// Hoạt ảnh bazooka góc nhìn thứ nhất (gắn trên VM_Bazooka): bắn thì giật mạnh, lửa phụt đuôi ống, đạn trong ống biến mất;
    /// thay đạn thì xoay ống cho thấy đuôi, đạn rocket trượt vào rồi đưa ống về vai.
    public sealed class BazookaView : MonoBehaviour
    {
        WeaponController weapons;
        Transform rocket, muzzle, tail;
        Vector3 restPos, rocketLoaded;
        Quaternion restRot;
        float kick;

        void Awake()
        {
            weapons = GetComponentInParent<WeaponController>();
            rocket = transform.Find("Sung/DanRocket");
            muzzle = transform.Find("Muzzle");
            tail = transform.Find("DuoiOng");
            if (rocket) rocketLoaded = rocket.localPosition;
        }

        void Start() { restPos = transform.localPosition; restRot = transform.localRotation; }
        void OnEnable() => WeaponController.ShotFired += OnShot;
        void OnDisable()
        {
            WeaponController.ShotFired -= OnShot;
            if (restRot != default) { transform.localPosition = restPos; transform.localRotation = restRot; }
            kick = 0f;
        }

        void OnShot(Vector3 from, Vector3 to)
        {
            kick = 1f;
            if (muzzle) CombatEffects.Muzzle(muzzle.position, muzzle.rotation, 2f);
            if (tail) CombatEffects.Muzzle(tail.position, tail.rotation * Quaternion.Euler(0, 180, 0), 2.5f); // lửa phụt sau ống
        }

        void LateUpdate()
        {
            if (!weapons) return;
            var weapon = weapons.Inventory.Current;
            kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 2.6f);
            float k = kick * kick;

            // Thay đạn: đưa ống sang ngang (0–25%), đạn trượt vào từ đuôi (25–75%), đưa về (75–100%)
            float t = weapons.IsReloading ? weapons.ReloadProgress01 : 0f;
            float pose = weapons.IsReloading ? Mathf.SmoothStep(0f, 1f, Mathf.Min(t / .25f, (1f - t) / .25f)) : 0f;
            transform.localPosition = restPos + new Vector3(-.05f * pose, -.1f * pose, -.2f * k + .12f * pose);
            transform.localRotation = restRot * Quaternion.Euler(-9f * k - 8f * pose, -48f * pose, 10f * pose);

            if (!rocket) return;
            bool loaded = weapon != null && weapon.CanFire && !weapons.IsReloading;
            rocket.gameObject.SetActive(loaded || (weapons.IsReloading && t > .25f));
            float slide = weapons.IsReloading ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, .75f, t)) : 1f;
            rocket.localPosition = rocketLoaded + Vector3.back * (1.45f * (1f - slide)); // từ sau đuôi ống vào
        }
    }

    /// Đạn rocket bay thấy được, kéo khói; nổ khi tới đích. Sát thương đã tính ngay lúc bắn (đường đạn thẳng, rất nhanh).
    public sealed class RocketFlight : MonoBehaviour
    {
        const float Speed = 75f;
        Vector3 target;
        bool explode;
        float smokeAt;
        static Material body;

        public static void Launch(Vector3 from, Vector3 to, bool hit)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(go.GetComponent<Collider>());
            go.name = "DanRocketBay";
            if (!body) { body = new Material(Shader.Find("Universal Render Pipeline/Lit")); body.SetColor("_BaseColor", new Color(.2f, .24f, .16f)); }
            go.GetComponent<Renderer>().sharedMaterial = body;
            go.transform.position = from;
            go.transform.rotation = Quaternion.LookRotation(to - from) * Quaternion.Euler(90, 0, 0);
            go.transform.localScale = new Vector3(.09f, .28f, .09f);
            var r = go.AddComponent<RocketFlight>();
            r.target = to; r.explode = hit;
        }

        void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, target, Speed * Time.deltaTime);
            if (Time.time >= smokeAt) { smokeAt = Time.time + .04f; CombatEffects.Muzzle(transform.position, transform.rotation, .5f); }
            if (transform.position != target) return;
            if (explode)
            {
                CombatEffects.Explosion(target, 1.3f);
                CampaignAudio.Effect("explosion_02", target);
            }
            Destroy(gameObject);
        }
    }
}
