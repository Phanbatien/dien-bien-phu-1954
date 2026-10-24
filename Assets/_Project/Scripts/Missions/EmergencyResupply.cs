using System.Collections.Generic;
using DBP.AI;
using DBP.Core;
using DBP.Interaction;
using UnityEngine;

namespace DBP.Missions
{
    /// Điểm cấp bổ sung khi hết vật tư (T30, BR-17):
    /// Theo dõi các MissionSupplyPoint và Bunker trong màn chơi.
    /// Nếu còn Bunker chưa phá (Health > 0) mà người chơi không còn SatchelCharge (count <= 0),
    /// tự động kích hoạt MissionSupplyPoint dự phòng gần nhất để đảm bảo luôn hoàn thành được nhiệm vụ.
    public class EmergencyResupply : MonoBehaviour
    {
        [Tooltip("Danh sách các điểm tiếp tế dự phòng bố trí sẵn trong màn chơi.")]
        [SerializeField] public List<MissionSupplyPoint> emergencyPoints = new List<MissionSupplyPoint>();

        [Tooltip("Tham chiếu Interactor của người chơi (tự tìm nếu để trống).")]
        [SerializeField] Interactor player;

        public List<MissionSupplyPoint> EmergencyPoints => emergencyPoints;
        public Interactor Player { get => player; set => player = value; }

        float timer;

        void Start()
        {
            if (!player) player = FindAnyObjectByType<Interactor>();
            InvokeRepeating(nameof(CheckAndRestoreSupply), 2f, 2f);
        }

        void Update()
        {
            if (!player) player = FindAnyObjectByType<Interactor>();

            // Fallback định kỳ nếu InvokeRepeating không chạy (ví dụ trong môi trường test)
            if (!IsInvoking(nameof(CheckAndRestoreSupply)))
            {
                timer += Time.deltaTime;
                if (timer >= 2f)
                {
                    timer = 0f;
                    if (player != null) CheckAndRestoreSupply(player);
                }
            }
        }

        /// Overload không tham số phục vụ InvokeRepeating.
        public void CheckAndRestoreSupply()
        {
            if (!player) player = FindAnyObjectByType<Interactor>();
            if (player != null) CheckAndRestoreSupply(player);
        }

        /// Kiểm tra tình trạng lô cốt và bộc phá của người chơi; kích hoạt điểm dự phòng nếu hết vật tư (BR-17).
        public void CheckAndRestoreSupply(Interactor targetPlayer)
        {
            if (targetPlayer == null) return;

            // 1. Kiểm tra xem còn Bunker nào chưa bị phá hủy không
            bool hasAliveBunker = false;
            var bunkers = FindObjectsOfType<Bunker>();
            foreach (var b in bunkers)
            {
                if (b == null) continue;
                var hp = b.GetComponent<Health>();
                if (hp != null && !hp.IsDead && hp.Current > 0f)
                {
                    hasAliveBunker = true;
                    break;
                }
            }

            if (!hasAliveBunker) return; // Toàn bộ lô cốt đã bị phá, không cần cấp bù

            // 2. Kiểm tra số lượng bộc phá (SatchelCharge) còn lại của người chơi
            var satchel = targetPlayer.GetComponent<SatchelCharge>()
                          ?? targetPlayer.GetComponentInChildren<SatchelCharge>()
                          ?? targetPlayer.GetComponentInParent<SatchelCharge>()
                          ?? FindAnyObjectByType<SatchelCharge>();

            int satchelCount = satchel != null ? satchel.Count : 0;
            if (satchelCount > 0) return; // Người chơi vẫn còn bộc phá

            // 3. Nếu đã có một điểm tiếp tế dự phòng đang hoạt động và chưa được nhặt, đợi người chơi đến lấy
            if (emergencyPoints != null)
            {
                foreach (var pt in emergencyPoints)
                {
                    if (pt != null && pt.gameObject.activeInHierarchy && !pt.Consumed)
                    {
                        return;
                    }
                }
            }

            // 4. Tìm điểm tiếp tế dự phòng gần nhất chưa bị tiêu thụ (consumed)
            MissionSupplyPoint nearest = null;
            float minDistance = float.MaxValue;
            Vector3 playerPos = targetPlayer.transform.position;

            if (emergencyPoints != null)
            {
                foreach (var pt in emergencyPoints)
                {
                    if (pt == null || pt.Consumed) continue;
                    float dist = Vector3.Distance(playerPos, pt.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = pt;
                    }
                }
            }

            // 5. Kích hoạt điểm dự phòng tìm được
            if (nearest != null)
            {
                nearest.Activate();
                Debug.Log($"[EmergencyResupply] Kích hoạt điểm tiếp tế dự phòng '{nearest.name}' ({nearest.SupplyType} × {nearest.Quantity}) tại {nearest.transform.position} (cách người chơi {minDistance:F1}m).");
            }
            else
            {
                Debug.LogWarning("[EmergencyResupply] Còn lô cốt chưa phá và người chơi đã hết bộc phá, nhưng không còn điểm tiếp tế dự phòng nào trong danh sách!");
            }
        }
    }
}
