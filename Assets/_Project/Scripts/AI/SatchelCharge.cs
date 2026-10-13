using DBP.Core;
using DBP.Interaction;
using UnityEngine;

namespace DBP.AI
{
    /// Bộc phá (Satchel Charge) — T15 – Đạt; BR-20, BR-17:
    ///   Người chơi giữ E 1 giây để đặt lên → báo Bunker trong tầm → Bunker phát nổ sau delay.
    ///   Đây là vật dùng một lần (consumed khi đặt thành công).
    ///   Bộc phá phải ở trong tầm satchelDetectRadius của Bunker mới tác dụng.
    [RequireComponent(typeof(Collider))]
    public class SatchelCharge : MonoBehaviour, IInteractable
    {
        [Tooltip("Số lượng bộc phá mang theo; giảm khi dùng (BR-17: vẫn còn đủ cho tất cả lô cốt).")]
        [SerializeField] int count = 2;
        [SerializeField] float holdSeconds = 1f;

        public string Prompt => count > 0 ? $"Giữ E: đặt bộc phá ({count})" : "Hết bộc phá";
        public float HoldSeconds => holdSeconds;
        public int Count { get => count; set => count = value; }
        public void AddCount(int amount) => count += Mathf.Max(0, amount);

        public bool CanInteract(Interactor who) => count > 0;

        public void Interact(Interactor who)
        {
            // Tìm lô cốt trong tầm
            var bunker = FindNearestBunker(who.transform.position);
            if (bunker == null)
            {
                Debug.Log("[Satchel] Không có lô cốt trong tầm đặt bộc phá.");
                return;
            }

            count--;
            Debug.Log($"[Satchel] Đặt bộc phá lên {bunker.name}. Còn {count} quả.");
            bunker.OnSatchelPlanted();
        }

        Bunker FindNearestBunker(Vector3 pos)
        {
            Bunker best = null;
            float bestDist = float.MaxValue;
            foreach (var b in FindObjectsOfType<Bunker>())
            {
                float dist = Vector3.Distance(pos, b.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = b;
                }
            }
            // Kiểm tra ngưỡng: Bunker có field satchelDetectRadius nhưng là private → dùng 8 m mặc định
            return (best != null && bestDist <= 8f) ? best : null;
        }
    }
}
