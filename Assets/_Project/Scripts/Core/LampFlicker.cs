using UnityEngine;

namespace DBP.Core
{
    /// Đèn dầu trong hầm chữ A: lửa chập chờn, thỉnh thoảng rung khi pháo nổ xa. Chạy cả khi timeScale = 0
    /// (hầm hiện sau giao diện chuẩn bị).
    [RequireComponent(typeof(Light))]
    public class LampFlicker : MonoBehaviour
    {
        public float baseIntensity = 2.2f;
        public float flicker = 0.6f;
        public float shakeGap = 9f;

        Light lamp;
        float nextShake, shakeUntil;

        void Awake()
        {
            lamp = GetComponent<Light>();
            nextShake = Time.unscaledTime + shakeGap * 0.5f;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (now >= nextShake) { shakeUntil = now + 0.6f; nextShake = now + shakeGap + Random.Range(0f, 5f); }
            float shake = now < shakeUntil ? Random.Range(-0.8f, 0.4f) : 0f;
            lamp.intensity = Mathf.Max(0.2f, baseIntensity + (Mathf.PerlinNoise(now * 6f, 0.3f) - 0.5f) * 2f * flicker + shake);
        }
    }
}
