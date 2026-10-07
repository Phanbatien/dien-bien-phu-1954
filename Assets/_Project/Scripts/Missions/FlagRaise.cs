using System.Collections;
using UnityEngine;

namespace DBP.Missions
{
    /// Cắm cờ "Quyết chiến Quyết thắng" trên nóc hầm chỉ huy khi thắng Màn 5 (T58; BR-03).
    /// Nối vào MissionController.onWon. Khôi thay cột cờ và vải cờ bằng model thật.
    public class FlagRaise : MonoBehaviour
    {
        [SerializeField] Transform flag;
        [SerializeField] float height = 3f;
        [SerializeField] float seconds = 3f;

        public void Raise() => StartCoroutine(RaiseRoutine());

        IEnumerator RaiseRoutine()
        {
            if (!flag) yield break;
            flag.gameObject.SetActive(true);
            var start = flag.localPosition;
            var end = start + Vector3.up * height;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                flag.localPosition = Vector3.Lerp(start, end, t / seconds);
                yield return null;
            }
            flag.localPosition = end;
        }
    }
}
