using DBP.Core;
using UnityEngine;

namespace DBP.DebugTools
{
    // ponytail: hình nộm greybox biến mất khi chết. T08 (Đạt) thay bằng ngã xuống rồi mờ dần (BR-21).
    [RequireComponent(typeof(Health))]
    public class DisableOnDeath : MonoBehaviour
    {
        void Awake() => GetComponent<Health>().Died += () => gameObject.SetActive(false);
    }
}
