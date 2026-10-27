using UnityEngine;

namespace DBP.Core
{
    /// Camera trong Hầm chữ A đưa nhẹ qua lại như người đứng nhìn quanh hầm (chạy cả khi timeScale = 0).
    public class BunkerCameraSway : MonoBehaviour
    {
        public float yawDegrees = 7f, pitchDegrees = 2f, period = 14f;
        Quaternion rest;

        void Start() => rest = transform.localRotation;

        void LateUpdate()
        {
            float t = Time.unscaledTime * Mathf.PI * 2f / period;
            transform.localRotation = rest * Quaternion.Euler(Mathf.Sin(t * 0.5f) * pitchDegrees, Mathf.Sin(t) * yawDegrees, 0f);
        }
    }
}
