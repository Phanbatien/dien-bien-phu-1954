using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DBP.Core
{
    /// "Thế giới đứng yên" khi trả lời câu hỏi ngoài trận (T13; BR-09, BR-12).
    /// Đạn, AI, vật lý, pháo, hạt lửa dùng thời gian game nên dừng theo Time.timeScale = 0.
    /// UI câu hỏi và đồng hồ phải dùng thời gian thực (unscaledDeltaTime / realtimeSinceStartup).
    public class WorldFreeze : MonoBehaviour
    {
        public static WorldFreeze Instance { get; private set; }
        public static bool IsFrozen { get; private set; }
        public static event Action Frozen;
        public static event Action Resumed;

        [Tooltip("Volume toàn cục: ngả nâu xám + tối viền. Weight 0 khi chơi, 1 khi đứng yên.")]
        [SerializeField] Volume freezeVolume;
        [SerializeField] float transitionSeconds = 0.5f;

        Coroutine resuming;
        float listenerVolume = 1f;

        public bool IsTransitioning => resuming != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsFrozen = false;
            Frozen = null;
            Resumed = null;
        }

        void Awake()
        {
            Instance = this;
            if (freezeVolume) freezeVolume.weight = 0f;
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            if (IsFrozen) Release();
            Instance = null;
        }

        public void SetVolume(Volume volume) => freezeVolume = volume;

        public void Freeze()
        {
            if (IsFrozen) return;
            IsFrozen = true;
            InputLock.Push();
            Time.timeScale = 0f;
            listenerVolume = AudioListener.volume;
            AudioListener.pause = true; // âm UI: đặt AudioSource.ignoreListenerPause = true
            if (freezeVolume) freezeVolume.weight = 1f;
            Frozen?.Invoke();
        }

        /// BR-12: trong 0,5 s chuyển tiếp chiến trường vẫn đứng yên và người chơi vẫn bị khóa;
        /// hình ảnh và âm thanh trở lại dần; cuối cùng mở khóa người chơi và chạy lại thế giới cùng một khung hình.
        public void Resume(Action onResumed = null)
        {
            if (!IsFrozen || resuming != null) return;
            resuming = StartCoroutine(ResumeRoutine(onResumed));
        }

        IEnumerator ResumeRoutine(Action onResumed)
        {
            AudioListener.pause = false;
            for (float t = 0f; t < transitionSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / transitionSeconds;
                if (freezeVolume) freezeVolume.weight = 1f - k;
                AudioListener.volume = listenerVolume * k;
                yield return null;
            }
            resuming = null;
            Release();
            onResumed?.Invoke();
        }

        void Release()
        {
            if (freezeVolume) freezeVolume.weight = 0f;
            AudioListener.pause = false;
            AudioListener.volume = listenerVolume;
            Time.timeScale = 1f;
            InputLock.Pop();
            IsFrozen = false;
            Resumed?.Invoke();
        }
    }
}
