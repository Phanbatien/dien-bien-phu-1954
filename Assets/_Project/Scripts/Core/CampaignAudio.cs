using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DBP.Core
{
    /// Scene-owned audio: bounded voices, positional effects, and UI that survives world freeze.
    public sealed class CampaignAudio : MonoBehaviour
    {
        public static CampaignAudio Current { get; private set; }
        public bool WorldPaused { get; private set; }
        public const int VoiceLimit = 24;
        readonly List<AudioSource> world = new List<AudioSource>();
        readonly AudioSource[] voices = new AudioSource[VoiceLimit];
        readonly AudioSource[] ui = new AudioSource[2];
        int nextVoice, nextUI;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            Current = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InstallInitial() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) Ensure();
        }
        static CampaignAudio Ensure()
        {
            if (!Application.isPlaying) return null;
            if (!Current) Current = FindAnyObjectByType<CampaignAudio>();
            if (!Current) Current = new GameObject("CampaignAudio").AddComponent<CampaignAudio>();
            return Current;
        }
        void Awake()
        {
            if (Current && Current != this) { Destroy(gameObject); return; }
            Current = this;
            for (int i = 0; i < voices.Length; i++) voices[i] = Source("WorldVoice" + i, transform, false);
            for (int i = 0; i < ui.Length; i++) ui[i] = Source("UIVoice" + i, transform, true);
            var music = AudioCatalog.Music(gameObject.scene.name);
            if (music)
            {
                Loop("MissionMusic", music, transform, false, 1f);
                Loop("AirplaneAmbience", AudioCatalog.Clip("airplane_loop"), transform, false, .7f);
            }
        }
        AudioSource Source(string name, Transform parent, bool interfaceSound)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false; source.ignoreListenerPause = interfaceSound;
            source.dopplerLevel = 0; source.minDistance = 4; source.maxDistance = 130;
            source.rolloffMode = AudioRolloffMode.Linear;
            if (!interfaceSound) world.Add(source);
            return source;
        }
        AudioSource Loop(string name, AudioClip clip, Transform parent, bool positional, float volume)
        {
            if (!clip) return null;
            var source = Source(name, parent, false);
            source.clip = clip; source.loop = true; source.volume = volume;
            source.spatialBlend = positional ? 1f : 0f;
            source.Play();
            if (WorldPaused) source.Pause();
            return source;
        }
        public static AudioSource AttachLoop(string name, string clip, Transform owner, float volume = 1f)
        {
            var current = Ensure();
            return current ? current.Loop(name, AudioCatalog.Clip(clip), owner, true, volume) : null;
        }
        void LateUpdate()
        {
            // WorldFreeze already pauses the listener and fades it back in. Do not
            // pause individual voices then: they must resume during its transition.
            bool paused = Time.timeScale <= 0f && !WorldFreeze.IsFrozen;
            if (paused != WorldPaused)
            {
                WorldPaused = paused;
                foreach (var source in world)
                {
                    if (!source) continue;
                    if (paused) source.Pause(); else source.UnPause();
                }
            }
        }
        void OnDestroy() { if (Current == this) Current = null; }

        public static AudioSource Play(AudioClip clip, Vector3 position, bool positional = true, float volume = 1f)
        {
            if (!clip || Time.timeScale <= 0f || WorldFreeze.IsFrozen) return null;
            var current = Ensure();
            if (!current) return null;
            var source = current.voices[current.nextVoice];
            current.nextVoice = (current.nextVoice + 1) % VoiceLimit;
            source.Stop(); source.transform.position = position;
            source.clip = clip; source.loop = false; source.volume = volume;
            source.spatialBlend = positional ? 1f : 0f; source.pitch = 1f;
            source.Play();
            return source;
        }
        public static AudioSource Effect(string name, Vector3 position, bool positional = true, float volume = 1f) =>
            Play(AudioCatalog.Clip(name), position, positional, volume);

        public static AudioSource Interface(string name)
        {
            var clip = AudioCatalog.Clip(name);
            var current = Ensure();
            if (!clip || !current) return null;
            var source = current.ui[current.nextUI];
            current.nextUI = (current.nextUI + 1) % current.ui.Length;
            source.Stop(); source.clip = clip; source.volume = 1f; source.Play();
            return source;
        }
    }
}
