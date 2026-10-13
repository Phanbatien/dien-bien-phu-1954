using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DBP.DebugTools
{
    /// Đo FPS từng màn trên máy thật (T63). Chạy bản build với tham số -perf-probe:
    ///   DienBienPhu1954.exe -perf-probe -logFile perf.log
    /// Mỗi màn: chờ 2 giây cho ổn định, đo 6 giây, ghi "[PERF] …" vào log rồi sang màn sau; xong thì thoát.
    public class PerfProbe : MonoBehaviour
    {
        const float Warmup = 2f, Measure = 6f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Environment.GetCommandLineArgs().Contains("-perf-probe")) return;
            var go = new GameObject("PerfProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfProbe>();
        }

        IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0; // đo FPS thật, không bị khóa ở tần số màn hình
            Application.targetFrameRate = -1;
            Debug.Log($"[PERF] Máy: {SystemInfo.processorType} · {SystemInfo.graphicsDeviceName} · RAM {SystemInfo.systemMemorySize} MB · {Screen.width}x{Screen.height}");

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var load = SceneManager.LoadSceneAsync(i);
                while (!load.isDone) yield return null;
                yield return new WaitForSecondsRealtime(Warmup);

                int frames = 0;
                float worst = 0f, start = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - start < Measure)
                {
                    worst = Mathf.Max(worst, Time.unscaledDeltaTime);
                    frames++;
                    yield return null;
                }
                float avg = frames / (Time.realtimeSinceStartup - start);
                Debug.Log($"[PERF] {SceneManager.GetActiveScene().name}: trung bình {avg:0} FPS · khung chậm nhất {worst * 1000f:0} ms");
            }
            Application.Quit();
        }
    }
}
