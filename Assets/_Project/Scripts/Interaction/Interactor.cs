using DBP.Core;
using DBP.Player;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DBP.Interaction
{
    /// Gắn trên người chơi: nhìn vào vật tương tác trong tầm, nhấn/giữ E (T13).
    /// Trong lúc giữ E chiến trường vẫn chạy và người chơi vẫn trúng đạn.
    public class Interactor : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] float range = 2.5f;
        [SerializeField] LayerMask mask = ~0;

        public Health Health { get; private set; }
        public WeaponController Weapons { get; private set; }
        public FirstPersonController Body { get; private set; }
        public IInteractable Focus { get; private set; }
        public float HoldProgress01 => Focus != null && Focus.HoldSeconds > 0f ? Mathf.Clamp01(hold.Elapsed / Focus.HoldSeconds) : 0f;

        readonly HoldProgress hold = new HoldProgress();

        void Awake()
        {
            Health = GetComponent<Health>();
            Weapons = GetComponentInChildren<WeaponController>();
            Body = GetComponent<FirstPersonController>();
            if (!cam) cam = GetComponentInChildren<Camera>();
        }

        void Update()
        {
            if (InputLock.Locked)
            {
                hold.Cancel();
                Focus = null;
                return;
            }

            Focus = FindFocus();
            var keyboard = Keyboard.current;
            bool alive = Health == null || !Health.IsDead;
            bool holding = alive && keyboard != null && keyboard.eKey.isPressed;

            if (Focus != null && Focus.HoldSeconds <= 0f)
            {
                hold.Cancel();
                if (alive && keyboard != null && keyboard.eKey.wasPressedThisFrame) Focus.Interact(this);
                return;
            }

            if (hold.Tick(Focus, holding, Time.deltaTime, Focus?.HoldSeconds ?? 0f))
                Focus.Interact(this);
        }

        IInteractable FindFocus()
        {
            if (!cam) return null;
            if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, range, mask, QueryTriggerInteraction.Ignore))
                return null;
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            return interactable != null && interactable.CanInteract(this) ? interactable : null;
        }
    }
}
