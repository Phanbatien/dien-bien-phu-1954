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
            // Bụi cỏ, đá nhỏ không được che mất hòm; vật lớn (vách hào, tường, mặt đất) thì vẫn chắn.
            var hits = Physics.RaycastAll(DBP.Player.PlayerView.AimOrigin(cam.transform), cam.transform.forward, range, mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null) return interactable.CanInteract(this) ? interactable : null;
                var size = hit.collider.bounds.size;
                if (size.x > 1.2f || size.y > 1.2f || size.z > 1.2f) return null;
            }
            return null;
        }
    }
}
