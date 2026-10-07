using System;
using DBP.Core;
using DBP.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace DBP.Weapons
{
    /// Bắn raycast (T05), thay đạn (T06), ngắm ADS + độ giật (T12), ống ngắm (T44), đổi súng (T21).
    /// Chuột trái: bắn · Chuột phải: ngắm · R: thay đạn · 1/2 hoặc lăn chuột: đổi súng.
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] FirstPersonController body;
        [SerializeField] string startingWeaponId = "W-MOSIN";
        [SerializeField] LayerMask hitMask = ~0;
        [Tooltip("Hiệu ứng trúng đích (T67 – Long). Bỏ trống thì không sinh.")]
        [SerializeField] GameObject impactPrefab;
        [Tooltip("Mô hình súng góc nhìn thứ nhất (T16 – Khôi). Ẩn khi soi ống ngắm.")]
        [SerializeField] GameObject viewModel;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip fireClip, reloadClip, dryClip;
        [SerializeField] float adsLerpSpeed = 12f;

        public static event Action Fired;
        public static event Action Reloaded;
        public event Action<RaycastHit> Hit;

        public Inventory Inventory { get; } = new Inventory();
        public bool IsAiming { get; private set; }
        public bool IsReloading => reloadEndsAt > 0f;
        public float ReloadProgress01 => IsReloading ? 1f - (reloadEndsAt - Time.time) / reloadDuration : 0f;
        /// Nâng cấp Tốc độ thay đạn (BR-35): ×0,9 / ×0,8 / ×0,7.
        public float ReloadMultiplier { get; set; } = 1f;
        public bool Scoped => IsAiming && Inventory.Current != null && Inventory.Current.Def.stats.scopeZoom > 1f
                              && cam && cam.fieldOfView < baseFov * 0.6f;

        float baseFov = 70f, nextShotAt, reloadEndsAt, reloadDuration = 1f;

        void Start()
        {
            if (!cam) cam = Camera.main;
            if (!body) body = GetComponentInParent<FirstPersonController>();
            if (cam) baseFov = cam.fieldOfView;

            var def = WeaponCatalog.Get(startingWeaponId);
            if (def != null) Inventory.GiveWeapon(def, def.stats.reserve * 2); // đầy trần đạn khi xuất trận
        }

        void OnDisable()
        {
            IsAiming = false;
            CancelReload();
            if (cam) cam.fieldOfView = baseFov;
            if (body) { body.SprintBlocked = false; body.LookSensitivityMultiplier = 1f; }
            if (viewModel) viewModel.SetActive(true);
        }

        void Update()
        {
            var weapon = Inventory.Current;
            if (InputLock.Locked || weapon == null) return;
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null) return;

            HandleSwitch(keyboard, mouse);
            weapon = Inventory.Current;

            IsAiming = mouse.rightButton.isPressed && !IsReloading;
            UpdateAim(weapon);

            if (IsReloading)
            {
                if (Time.time >= reloadEndsAt)
                {
                    weapon.CompleteReload();
                    reloadEndsAt = 0f;
                    Reloaded?.Invoke();
                }
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame && weapon.CanReload)
            {
                StartReload(weapon);
                return;
            }

            bool trigger = weapon.Def.Automatic ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
            if (!trigger || Time.time < nextShotAt) return;

            if (weapon.CanFire) Fire(weapon);
            else
            {
                Play(dryClip);
                nextShotAt = Time.time + 0.25f;
                if (weapon.CanReload) StartReload(weapon);
            }
        }

        void HandleSwitch(Keyboard keyboard, Mouse mouse)
        {
            int before = Inventory.CurrentIndex;
            if (keyboard.digit1Key.wasPressedThisFrame) Inventory.Switch(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) Inventory.Switch(1);
            else if (Mathf.Abs(mouse.scroll.ReadValue().y) > 0.01f) Inventory.Next();
            if (Inventory.CurrentIndex != before) CancelReload();
        }

        void UpdateAim(Weapon weapon)
        {
            if (!cam) return;
            var stats = weapon.Def.stats;
            float targetFov = IsAiming ? stats.adsFov / Mathf.Max(1f, stats.scopeZoom) : baseFov;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-adsLerpSpeed * Time.deltaTime));
            if (body)
            {
                body.SprintBlocked = IsAiming;
                body.LookSensitivityMultiplier = cam.fieldOfView / baseFov;
            }
            if (viewModel) viewModel.SetActive(!Scoped);
        }

        void Fire(Weapon weapon)
        {
            var stats = weapon.Def.stats;
            weapon.TryConsume();
            nextShotAt = Time.time + 60f / Mathf.Max(1f, stats.rpm);

            float spread = IsAiming ? stats.adsSpread : stats.hipSpread;
            if (body && body.IsSprinting) spread *= 2f;
            var r = Random.insideUnitCircle * spread;
            var origin = cam.transform.position;
            var direction = cam.transform.rotation * (Quaternion.Euler(r.x, r.y, 0f) * Vector3.forward);

            if (Physics.Raycast(origin, direction, out var hit, stats.range, hitMask, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponentInParent<IDamageable>();
                target?.TakeDamage(stats.damage, DamageType.Bullet, hit.point);
                if (impactPrefab) Destroy(Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal)), 5f);
                Hit?.Invoke(hit);
            }

            float recoilScale = IsAiming ? 0.6f : 1f;
            if (body) body.AddRecoil(stats.recoilPitch * recoilScale, Random.Range(-1f, 1f) * stats.recoilYaw * recoilScale);
            Play(fireClip);
            Fired?.Invoke();
        }

        void StartReload(Weapon weapon)
        {
            reloadDuration = weapon.Def.stats.reloadSec * ReloadMultiplier;
            reloadEndsAt = Time.time + reloadDuration;
            Play(reloadClip);
        }

        void CancelReload() => reloadEndsAt = 0f;

        void Play(AudioClip clip)
        {
            if (audioSource && clip) audioSource.PlayOneShot(clip);
        }

        void OnGUI()
        {
            if (InputLock.Locked || Inventory.Current == null) return;
            float cx = Screen.width / 2f, cy = Screen.height / 2f;
            if (Scoped)
            {
                // Ống ngắm: viền đen + chữ thập (T44). Khôi có thể thay bằng texture.
                float r = Screen.height * 0.45f;
                var black = Texture2D.whiteTexture;
                var old = GUI.color;
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0, 0, cx - r, Screen.height), black);
                GUI.DrawTexture(new Rect(cx + r, 0, cx - r, Screen.height), black);
                GUI.DrawTexture(new Rect(cx - r, 0, 2 * r, cy - r), black);
                GUI.DrawTexture(new Rect(cx - r, cy + r, 2 * r, cy - r), black);
                GUI.DrawTexture(new Rect(cx - r, cy - 0.5f, 2 * r, 1), black);
                GUI.DrawTexture(new Rect(cx - 0.5f, cy - r, 1, 2 * r), black);
                GUI.color = old;
            }
            else if (!IsAiming)
            {
                GUI.Label(new Rect(cx - 4, cy - 10, 20, 20), "+");
            }
        }
    }
}
