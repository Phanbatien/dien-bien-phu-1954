using System;
using DBP.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DBP.Player
{
    /// Điều khiển góc nhìn thứ nhất (T01, T12): đi, chạy, nhảy, ngồi/bò, nhìn chuột, thể lực, độ giật.
    /// Tự viết thay Starter Assets: không phụ thuộc Asset Store, đọc thẳng bàn phím/chuột (Input System).
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Di chuyển")]
        public float walkSpeed = 4f;
        public float sprintSpeed = 6.5f;
        public float crouchSpeed = 1.8f;
        public float jumpHeight = 1f;
        public float gravity = -20f;

        [Header("Nhìn")]
        public Transform cameraPivot;
        public float lookSensitivity = 0.1f;
        public float minPitch = -85f;
        public float maxPitch = 85f;

        [Header("Ngồi / bò dưới làn đạn")]
        public float standHeight = 1.8f;
        public float crouchHeight = 1f;
        public float eyeOffset = 0.15f;

        [Header("Thể lực (T12)")]
        public float baseMaxStamina = 100f;

        [Header("Độ giật (T12)")]
        [Tooltip("Tốc độ tâm ngắm trở lại sau mỗi phát (độ/giây).")]
        public float recoilRecoverSpeed = 12f;

        public static event Action Moved;

        public StaminaModel Stamina { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        /// Đặt bởi vũ khí khi ngắm (ADS): không cho chạy.
        public bool SprintBlocked { get; set; }
        /// Đặt bởi vũ khí khi ngắm: độ nhạy theo tỉ lệ FOV.
        public float LookSensitivityMultiplier { get; set; } = 1f;
        /// Ụ súng máy: khóa di chuyển, chỉ cho xoay trong một cung.
        public bool MovementLocked { get; set; }
        public float? YawCenter { get; set; }
        public float YawHalfArc { get; set; } = 60f;

        CharacterController controller;
        float pitch, yaw, verticalVelocity, staminaMultiplier = 1f;
        Vector2 recoil; // x = pitch lên, y = yaw

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            Stamina = new StaminaModel(baseMaxStamina * staminaMultiplier);
            yaw = transform.eulerAngles.y;
        }

        void OnEnable()
        {
            WorldFreeze.Frozen += UnlockCursor;
            WorldFreeze.Resumed += LockCursor;
        }

        void OnDisable()
        {
            WorldFreeze.Frozen -= UnlockCursor;
            WorldFreeze.Resumed -= LockCursor;
        }

        void Start() => LockCursor();

        public static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// Nâng cấp Thể lực (BR-35): thay trần, không cộng dồn.
        public void SetStaminaMultiplier(float multiplier)
        {
            staminaMultiplier = multiplier;
            Stamina?.SetMax(baseMaxStamina * multiplier);
        }

        public void AddRecoil(float pitchUp, float yawKick) => recoil += new Vector2(pitchUp, yawKick);

        public void SnapTo(Vector3 position, float newYaw)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            yaw = newYaw;
        }

        void Update()
        {
            if (InputLock.Locked) return; // BR-09: khóa đi, ngắm, xoay camera khi đóng băng
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            Look(mouse.delta.ReadValue());
            if (!MovementLocked) Move(keyboard);
            else IsSprinting = false;
        }

        void Look(Vector2 delta)
        {
            float sens = lookSensitivity * LookSensitivityMultiplier;
            yaw += delta.x * sens;
            pitch = Mathf.Clamp(pitch - delta.y * sens, minPitch, maxPitch);
            if (YawCenter.HasValue)
                yaw = YawCenter.Value + Mathf.Clamp(Mathf.DeltaAngle(YawCenter.Value, yaw), -YawHalfArc, YawHalfArc);

            recoil = Vector2.MoveTowards(recoil, Vector2.zero, recoilRecoverSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, yaw + recoil.y, 0f);
            if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(Mathf.Clamp(pitch - recoil.x, minPitch, maxPitch), 0f, 0f);
        }

        void Move(Keyboard keyboard)
        {
            float dt = Time.deltaTime;
            var input = new Vector2(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            input = Vector2.ClampMagnitude(input, 1f);

            UpdateCrouch(keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed, dt);

            bool wantsSprint = keyboard.leftShiftKey.isPressed && input.y > 0f && !IsCrouching && !SprintBlocked;
            IsSprinting = wantsSprint && Stamina.CanSprint;
            Stamina.Tick(dt, IsSprinting);

            float speed = IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * speed;

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (keyboard.spaceKey.wasPressedThisFrame && !IsCrouching)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * dt;
            velocity.y = verticalVelocity;
            controller.Move(velocity * dt);

            if (input.sqrMagnitude > 0f) Moved?.Invoke();
        }

        void UpdateCrouch(bool wantsCrouch, float dt)
        {
            if (!wantsCrouch && IsCrouching)
            {
                // Không đứng dậy được nếu đang ở dưới vật cản (rào, mái hầm).
                var head = transform.position + Vector3.up * crouchHeight;
                if (Physics.Raycast(head, Vector3.up, standHeight - crouchHeight + 0.05f, ~0, QueryTriggerInteraction.Ignore))
                    wantsCrouch = true;
            }
            IsCrouching = wantsCrouch;

            float height = IsCrouching ? crouchHeight : standHeight;
            controller.height = Mathf.MoveTowards(controller.height, height, 6f * dt);
            controller.center = Vector3.up * (controller.height / 2f);
            if (cameraPivot)
            {
                var p = cameraPivot.localPosition;
                p.y = controller.height - eyeOffset;
                cameraPivot.localPosition = p;
            }
        }
    }
}
