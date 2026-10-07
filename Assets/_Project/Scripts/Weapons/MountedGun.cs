using DBP.Core;
using DBP.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DBP.Weapons
{
    /// Súng máy tại ụ cho Màn 2 Giữ Độc Lập (T43). Dùng tại chỗ, không chiếm chỗ mang súng (BR-36).
    /// E: lên/xuống ụ · Chuột trái: bắn. Khi lên ụ chỉ xoay trong cung bắn.
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
        float nextShotAt;

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
        }

        void Dismount()
        {
            user.Body.MovementLocked = false;
            user.Body.YawCenter = null;
            if (user.Weapons) user.Weapons.enabled = true;
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
            var r = Random.insideUnitCircle * spread;
            var dir = cam.rotation * (Quaternion.Euler(r.x, r.y, 0f) * Vector3.forward);
            if (Physics.Raycast(cam.position, dir, out var hit, range, ~0, QueryTriggerInteraction.Ignore))
                hit.collider.GetComponentInParent<IDamageable>()?.TakeDamage(damage, DamageType.Bullet, hit.point);
            user.Body.AddRecoil(recoilPitch, Random.Range(-0.3f, 0.3f));
            if (audioSource && fireClip) audioSource.PlayOneShot(fireClip);
        }
    }
}
