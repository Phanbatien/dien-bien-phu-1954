using DBP.Missions;
using DBP.Quiz;
using UnityEngine;

namespace DBP.Interaction
{
    /// T23 (BR-16): Cơ chế bảo hiểm cho hòm câu hỏi ngoài trận.
    /// Sai 3 hòm liên tiếp → hòm tiếp theo kích hoạt câu bảo hiểm (cấp 1, 12 giây).
    /// Sau câu bảo hiểm (dù đúng hay sai), chuỗi sai liên tiếp reset về 0.
    /// Chỉ đếm kết quả của hòm vũ khí, không đếm súng rơi hay điện thoại gọi pháo.
    public class InsuranceTracker : MonoBehaviour
    {
        static InsuranceTracker instance;

        public static InsuranceTracker Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<InsuranceTracker>();
                    if (instance == null)
                    {
                        var go = new GameObject("InsuranceTracker");
                        instance = go.AddComponent<InsuranceTracker>();
                    }
                }
                return instance;
            }
        }

        [SerializeField] int streak;

        public int CurrentStreak => streak;
        public bool IsInsuranceActive => streak >= 3;
        public int InsuranceLevel => 1;
        public float InsuranceTimeLimit => 12f;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            streak = 0;
        }

        void OnEnable()
        {
            MissionController.Ended += OnMissionEnded;
        }

        void OnDisable()
        {
            MissionController.Ended -= OnMissionEnded;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void OnMissionEnded(Attempt _)
        {
            streak = 0;
        }

        /// Ghi nhận kết quả mở hòm (BR-16): đúng reset về 0, sai/hết giờ tăng streak.
        public void RecordCrateResult(QuizOutcome outcome)
        {
            if (outcome == QuizOutcome.Correct)
            {
                streak = 0;
            }
            else
            {
                streak++;
            }
        }

        /// Tiêu thụ bảo hiểm sau câu bảo hiểm: đưa chuỗi về 0.
        public void ConsumeInsurance()
        {
            streak = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
        }
    }
}
