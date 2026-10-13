using DBP.AI;
using DBP.Core;
using DBP.Interaction;
using DBP.Weapons;
using UnityEngine;

namespace DBP.Missions
{
    /// Vật tư nhiệm vụ không qua câu hỏi (T30, BR-17):
    /// Cung cấp vật tư bắt buộc để hoàn thành nhiệm vụ (bộc phá phá lô cốt, đạn bazooka, lựu đạn).
    /// Nhặt bằng cách nhấn E (không cần giữ), không qua câu hỏi, không chiếm chỗ súng, không quy đổi thành điểm.
    public class MissionSupplyPoint : MonoBehaviour, IInteractable
    {
        [Tooltip("Loại vật tư nhiệm vụ: 'Satchel', 'Bazooka', hoặc 'Grenade'.")]
        [SerializeField] string supplyType = "Satchel";
        [Tooltip("Số lượng vật tư cấp.")]
        [SerializeField] int quantity = 1;
        [Tooltip("Đã nhận vật tư tại điểm này chưa.")]
        [SerializeField] bool consumed;

        public string supplyTypeField => supplyType;
        public string SupplyType { get => supplyType; set => supplyType = value; }
        public int Quantity { get => quantity; set => quantity = value; }
        public bool Consumed { get => consumed; set => consumed = value; }

        public string Prompt => $"E: lấy {supplyType} × {quantity}";
        public float HoldSeconds => 0f;

        public bool CanInteract(Interactor who)
        {
            if (consumed) return false;
            if (who == null) return false;
            if (who.Health != null && who.Health.IsDead) return false;
            return true;
        }

        public void Interact(Interactor who)
        {
            if (!CanInteract(who)) return;

            switch (supplyType)
            {
                case "Satchel":
                    var satchel = who.GetComponent<SatchelCharge>()
                                  ?? who.GetComponentInChildren<SatchelCharge>()
                                  ?? who.GetComponentInParent<SatchelCharge>()
                                  ?? FindAnyObjectByType<SatchelCharge>();
                    if (satchel != null)
                    {
                        satchel.AddCount(quantity);
                        Debug.Log($"[MissionSupplyPoint] Trao {quantity} bộc phá (Satchel). Tổng hiện có: {satchel.Count}.");
                    }
                    else
                    {
                        Debug.LogWarning("[MissionSupplyPoint] Không tìm thấy SatchelCharge trên người chơi.");
                    }
                    break;

                case "Bazooka":
                    if (who.Weapons != null && who.Weapons.Inventory != null)
                    {
                        who.Weapons.Inventory.AddAmmo("Bazooka", quantity);
                        Debug.Log($"[MissionSupplyPoint] Trao {quantity} đạn Bazooka.");
                    }
                    else
                    {
                        Debug.LogWarning("[MissionSupplyPoint] Không tìm thấy Inventory để trao đạn Bazooka.");
                    }
                    break;

                case "Grenade":
                    if (who.Weapons != null && who.Weapons.Inventory != null)
                    {
                        who.Weapons.Inventory.AddGrenades(quantity);
                        Debug.Log($"[MissionSupplyPoint] Trao {quantity} lựu đạn.");
                    }
                    else
                    {
                        Debug.LogWarning("[MissionSupplyPoint] Không tìm thấy Inventory để trao lựu đạn.");
                    }
                    break;

                default:
                    Debug.LogWarning($"[MissionSupplyPoint] Loại vật tư không xác định: {supplyType}");
                    break;
            }

            consumed = true;
        }

        /// Kích hoạt lại điểm cấp (dùng cho hệ thống cấp bổ sung khẩn cấp EmergencyResupply).
        public void Activate()
        {
            consumed = false;
            gameObject.SetActive(true);
        }
    }
}
