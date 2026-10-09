// =========================================================================
//  SANCTUARY — AudioManager
//  Port de: AudioEngine.js (WebAudio synth → Unity AudioSource)
// =========================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sanctuary.Core
{
    /// <summary>
    /// Gerenciador de áudio com pools de SFX e BGM.
    /// Port do AudioEngine.js que usava WebAudio puro.
    /// No Unity usamos AudioSource + AudioClips.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Volume")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float bgmVolume = 0.5f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.7f;

        [Header("BGM Clips")]
        [SerializeField] private AudioClip bgmMenu;
        [SerializeField] private AudioClip bgmDuality;
        [SerializeField] private AudioClip bgmCatacumbas;
        [SerializeField] private AudioClip bgmPantano;
        [SerializeField] private AudioClip bgmMagma;
        [SerializeField] private AudioClip bgmCristal;
        [SerializeField] private AudioClip bgmBoss;
        [SerializeField] private AudioClip bgmDeath;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip sfxHit;
        [SerializeField] private AudioClip sfxCrit;
        [SerializeField] private AudioClip sfxDodge;
        [SerializeField] private AudioClip sfxHeal;
        [SerializeField] private AudioClip sfxLevelUp;
        [SerializeField] private AudioClip sfxLoot;
        [SerializeField] private AudioClip sfxForge;
        [SerializeField] private AudioClip sfxPotion;
        [SerializeField] private AudioClip sfxDeath;
        [SerializeField] private AudioClip sfxVictory;
        [SerializeField] private AudioClip sfxStagger;
        [SerializeField] private AudioClip sfxCombo;

        // Pool de AudioSources para SFX simultâneos
        private List<AudioSource> sfxPool = new List<AudioSource>();
        private const int SFX_POOL_SIZE = 8;

        // Mapeamento de IDs → clips
        private Dictionary<string, AudioClip> bgmMap;
        private Dictionary<string, AudioClip> sfxMap;

        // ====================== LIFECYCLE ======================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupAudioSources();
            BuildClipMaps();
            InitSFXPool();
        }

        private void OnEnable()
        {
            EventBus.OnSFXRequested += PlaySFX;
            EventBus.OnBGMRequested += PlayBGM;
        }

        private void OnDisable()
        {
            EventBus.OnSFXRequested -= PlaySFX;
            EventBus.OnBGMRequested -= PlayBGM;
        }

        private void SetupAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        private void BuildClipMaps()
        {
            bgmMap = new Dictionary<string, AudioClip>
            {
                { "bg_menu", bgmMenu },
                { "duality", bgmDuality },
                { "catacumbas", bgmCatacumbas },
                { "pantano", bgmPantano },
                { "magma", bgmMagma },
                { "cristal", bgmCristal },
                { "boss", bgmBoss },
                { "death", bgmDeath }
            };

            sfxMap = new Dictionary<string, AudioClip>
            {
                { "hit", sfxHit },
                { "crit", sfxCrit },
                { "dodge", sfxDodge },
                { "heal", sfxHeal },
                { "level_up", sfxLevelUp },
                { "loot", sfxLoot },
                { "forge", sfxForge },
                { "potion", sfxPotion },
                { "death", sfxDeath },
                { "victory_fanfare", sfxVictory },
                { "stagger", sfxStagger },
                { "combo", sfxCombo }
            };
        }

        private void InitSFXPool()
        {
            for (int i = 0; i < SFX_POOL_SIZE; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                sfxPool.Add(src);
            }
        }

        // ====================== BGM ======================

        public void PlayBGM(string bgmId)
        {
            if (!bgmMap.TryGetValue(bgmId, out AudioClip clip) || clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM não encontrada: {bgmId}");
                return;
            }

            if (bgmSource.clip == clip && bgmSource.isPlaying)
                return; // Já tocando essa música

            StartCoroutine(CrossfadeBGM(clip));
        }

        public void StopBGM()
        {
            StartCoroutine(FadeOutBGM(0.5f));
        }

        private IEnumerator CrossfadeBGM(AudioClip newClip, float duration = 1.0f)
        {
            // Fade out
            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration * 0.5f));
                yield return null;
            }

            // Troca clip
            bgmSource.clip = newClip;
            bgmSource.Play();

            // Fade in
            elapsed = 0f;
            float targetVolume = bgmVolume * masterVolume;

            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / (duration * 0.5f));
                yield return null;
            }

            bgmSource.volume = targetVolume;
        }

        private IEnumerator FadeOutBGM(float duration)
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.volume = bgmVolume * masterVolume;
        }

        // ====================== SFX ======================

        public void PlaySFX(string sfxId)
        {
            if (!sfxMap.TryGetValue(sfxId, out AudioClip clip) || clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX não encontrado: {sfxId}");
                return;
            }

            AudioSource source = GetAvailableSFXSource();
            if (source != null)
            {
                source.volume = sfxVolume * masterVolume;
                source.clip = clip;
                source.Play();
            }
        }

        /// <summary>
        /// Toca um SFX com pitch variado (bom para hits consecutivos).
        /// Port do comportamento do AudioEngine.js com playTone(freq, type, duration).
        /// </summary>
        public void PlaySFXWithPitch(string sfxId, float pitchMin = 0.9f, float pitchMax = 1.1f)
        {
            if (!sfxMap.TryGetValue(sfxId, out AudioClip clip) || clip == null)
                return;

            AudioSource source = GetAvailableSFXSource();
            if (source != null)
            {
                source.volume = sfxVolume * masterVolume;
                source.pitch = Random.Range(pitchMin, pitchMax);
                source.clip = clip;
                source.Play();
            }
        }

        private AudioSource GetAvailableSFXSource()
        {
            foreach (AudioSource src in sfxPool)
            {
                if (!src.isPlaying)
                    return src;
            }

            // Se todos em uso, reutiliza o primeiro (oldest)
            return sfxPool[0];
        }

        // ====================== VOLUME CONTROLS ======================

        public void SetMasterVolume(float vol)
        {
            masterVolume = Mathf.Clamp01(vol);
            bgmSource.volume = bgmVolume * masterVolume;
        }

        public void SetBGMVolume(float vol)
        {
            bgmVolume = Mathf.Clamp01(vol);
            bgmSource.volume = bgmVolume * masterVolume;
        }

        public void SetSFXVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
        }

        public void ToggleMute()
        {
            bgmSource.mute = !bgmSource.mute;
            foreach (var src in sfxPool) src.mute = bgmSource.mute;
            sfxSource.mute = bgmSource.mute;
        }
    }
}
