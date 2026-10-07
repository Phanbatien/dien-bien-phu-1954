using UnityEngine;

namespace DBP.Core
{
    /// Nền camera cùng màu sương của scene, để đường chân trời chìm vào sương (đêm Màn 3 không bị trời sáng).
    public class FogBackground : MonoBehaviour
    {
        void Start()
        {
            var cam = Camera.main;
            if (!cam) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
        }
    }
}
