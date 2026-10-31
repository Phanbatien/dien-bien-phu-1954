using System.Collections.Generic;
using DBP.Core;
using DBP.Interaction;
using DBP.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DBP.Weapons
{
    /// Súng máy tại ụ cho Màn 2 Giữ Độc Lập (T43). Dùng tại chỗ, không chiếm chỗ mang súng (BR-36).
    /// E: lên/xuống ụ · Chuột trái: bắn. Khi lên ụ chỉ xoay trong cung bắn.
    /// Hoạt ảnh (31/10): nòng xoay theo tâm ngắm, súng giật khi bắn, dây đạn rung, vỏ đạn văng, lửa đầu nòng ở đúng đầu nòng.
    // ponytail: đạn vô hạn, không quá nhiệt; thêm nếu playtest thấy quá dễ (T65).
    public class MountedGun : MonoBehaviour, IInteractable
    {
        [SerializeField] Transform seat;
        [SerializeField] float halfArc = 60f;
        [SerializeField] float damage = 30f;
        [SerializeField] float rpm = 500f;
        [SerializeField] float spread = 1.2f;
        [SerializeField] float range = 200f;
        [SerializeField] float recoilPitch = 0.6f;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip fireClip;

        Interactor user;
        int mountedFrame;
        float nextShotAt, kick;
        Transform pivot, muzzle, belt;
        Vector3 pivotRest, beltRest;
        Quaternion idleRotation;
        Collider box;
        readonly List<Casing> casings = new List<Casing>();
        static Material brass;
        struct Casing { public Transform t; public Vector3 v; public float dieAt; }

        void Awake()
        {
            box = GetComponent<Collider>();
            pivot = transform.Find("ModelSungMay/NongSung");
            if (!pivot) return;
            muzzle = pivot.Find("DauNong");
            belt = pivot.Find("DayDan");
            pivotRest = pivot.localPosition;
            if (belt) beltRest = belt.localPosition;
            idleRotation = pivot.rotation * Quaternion.Euler(9f, 0, 0); // bỏ không: nòng chúc xuống
            pivot.rotation = idleRotation;
        }

        public bool InUse => user != null;
        public string Prompt => "E: dùng súng máy";
        public float HoldSeconds => 0f;
        public bool CanInteract(Interactor who) => user == null && who.Body != null;

        public void Interact(Interactor who)
        {
            user = who;
            mountedFrame = Time.frameCount;
            var s = seat ? seat : transform;
            who.Body.SnapTo(s.position, s.eulerAngles.y);
            who.Body.MovementLocked = true;
            who.Body.YawCenter = s.eulerAngles.y;
            who.Body.YawHalfArc = halfArc;
            if (who.Weapons) who.Weapons.enabled = false;
            if (box) box.enabled = false; // tia bắn không trúng chính khẩu súng
            PlayerView.HideArms = true;
        }

        void Dismount()
        {
            user.Body.MovementLocked = false;
            user.Body.YawCenter = null;
            if (user.Weapons) user.Weapons.enabled = true;
            if (box) box.enabled = true;
            PlayerView.HideArms = false;
            user = null;
        }

        void Update()
        {
            if (user == null || InputLock.Locked) return;
            if (user.Health && user.Health.IsDead) { Dismount(); return; }
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (keyboard.eKey.wasPressedThisFrame && Time.frameCount != mountedFrame) { Dismount(); return; }
            if (!mouse.leftButton.isPressed || Time.time < nextShotAt) return;

            nextShotAt = Time.time + 60f / rpm;
            var cam = Camera.main.transform;
            kick = 1f;
            EnemyAnimatorDriverPlay();
            EjectCasing();
            var r = Random.insideUnitCircle * spread;
            var dir = cam.rotation * (Quaternion.Euler(r.x, r.y, 0f) * Vector3.forward);
            var origin = PlayerView.AimOrigin(cam);
            bool struck = Physics.Raycast(origin, dir, out var hit, range, ~0, QueryTriggerInteraction.Ignore);
            var from = muzzle ? muzzle.position : cam.position + cam.forward * 1.2f - cam.up * .25f;
            CombatEffects.Muzzle(from, muzzle ? muzzle.rotation : cam.rotation, 1.5f);
            CombatEffects.Tracer(from, struck ? hit.point : origin + dir * range, false);
            if (struck)
                hit.collider.GetComponentInParent<IDamageable>()?.TakeDamage(damage, DamageType.Bullet, hit.point);
            user.Body.AddRecoil(recoilPitch, Random.Range(-0.3f, 0.3f));
            if (audioSource && fireClip) audioSource.PlayOneShot(fireClip);
        }

        void EnemyAnimatorDriverPlay() => DBP.AI.EnemyAnimatorDriver.Play(user.Body, "Shoot");

        void LateUpdate()
        {
            UpdateCasings();
            if (!pivot) return;
            // Theo tâm ngắm (cả giật) khi có người ngồi; không có thì về thế nghỉ
            var want = user != null ? Camera.main.transform.rotation : idleRotation;
            pivot.rotation = Quaternion.Slerp(pivot.rotation, want, 1f - Mathf.Exp(-20f * Time.deltaTime));
            kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 9f);
            pivot.localPosition = pivotRest + pivot.localRotation * Vector3.back * (.07f * kick) + Random.insideUnitSphere * (.006f * kick);
            if (belt) belt.localPosition = beltRest + Vector3.up * (Mathf.Sin(Time.time * 70f) * .012f * kick);
        }

        void EjectCasing()
        {
            if (!muzzle) return;
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(c.GetComponent<Collider>());
            if (!brass) { brass = new Material(Shader.Find("Universal Render Pipeline/Lit")); brass.SetColor("_BaseColor", new Color(.75f, .58f, .25f)); brass.SetFloat("_Smoothness", .7f); }
            c.GetComponent<Renderer>().sharedMaterial = brass;
            c.transform.localScale = new Vector3(.018f, .035f, .018f);
            c.transform.position = pivot.TransformPoint(.09f, .08f, .1f);
            casings.Add(new Casing { t = c.transform, v = pivot.right * Random.Range(1.6f, 2.6f) + Vector3.up * Random.Range(1.2f, 2f), dieAt = Time.time + 1f });
        }

        void UpdateCasings()
        {
            for (int i = casings.Count - 1; i >= 0; i--)
            {
                var c = casings[i];
                if (!c.t || Time.time >= c.dieAt) { if (c.t) Destroy(c.t.gameObject); casings.RemoveAt(i); continue; }
                c.v += Physics.gravity * Time.deltaTime;
                c.t.position += c.v * Time.deltaTime;
                c.t.Rotate(720f * Time.deltaTime, 300f * Time.deltaTime, 0f);
                casings[i] = c;
            }
        }
    }
}
