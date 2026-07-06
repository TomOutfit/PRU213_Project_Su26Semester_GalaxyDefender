using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Clips")]
    [Tooltip("Assign AudioClip per key name for SFX playback.")]
    public List<NamedAudioClip> sfxClips = new List<NamedAudioClip>();

    [Header("BGM Clips")]
    [Tooltip("Assign BGM AudioClips here.")]
    public List<NamedAudioClip> bgmClips = new List<NamedAudioClip>();

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource1;
    [SerializeField] private AudioSource bgmSource2;
    [SerializeField] private AudioSource sfxSource;

    [Header("Mixer Groups")]
    public AudioMixerGroup bgmMixerGroup;
    public AudioMixerGroup sfxMixerGroup;

    private bool isSource1Active = true;
    private Dictionary<string, AudioClip> sfxMap = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> bgmMap = new Dictionary<string, AudioClip>();
    private Dictionary<string, float> sfxCooldowns = new Dictionary<string, float>();
    private Coroutine fadeRoutine;

    [Header("Settings")]
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
    private const float MIN_SFX_INTERVAL = 0.05f; // Prevent rapid overlap of same sound

    [System.Serializable]
    public class NamedAudioClip
    {
        public string key;
        public AudioClip clip;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.MergeClips(sfxClips, bgmClips);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeMaps();
        InitializeSources();
    }

    public void MergeClips(List<NamedAudioClip> newSfx, List<NamedAudioClip> newBgm)
    {
        if (newSfx != null)
        {
            foreach (var entry in newSfx)
            {
                if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
                {
                    if (!sfxMap.ContainsKey(entry.key))
                    {
                        sfxMap[entry.key] = entry.clip;
                        sfxClips.Add(entry);
                    }
                }
            }
        }

        if (newBgm != null)
        {
            foreach (var entry in newBgm)
            {
                if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
                {
                    if (!bgmMap.ContainsKey(entry.key))
                    {
                        bgmMap[entry.key] = entry.clip;
                        bgmClips.Add(entry);
                    }
                }
            }
        }
    }

    private void InitializeMaps()
    {
        foreach (var entry in sfxClips)
        {
            if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
                sfxMap[entry.key] = entry.clip;
        }

        foreach (var entry in bgmClips)
        {
            if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
                bgmMap[entry.key] = entry.clip;
        }
    }

    private void InitializeSources()
    {
        AudioSource[] existing = GetComponents<AudioSource>();
        int index = 0;

        if (bgmSource1 == null)
        {
            if (index < existing.Length) bgmSource1 = existing[index++];
            else bgmSource1 = gameObject.AddComponent<AudioSource>();
        }
        if (bgmSource2 == null)
        {
            if (index < existing.Length) bgmSource2 = existing[index++];
            else bgmSource2 = gameObject.AddComponent<AudioSource>();
        }
        if (sfxSource == null)
        {
            if (index < existing.Length) sfxSource = existing[index++];
            else sfxSource = gameObject.AddComponent<AudioSource>();
        }

        SetupBgmSource(bgmSource1);
        SetupBgmSource(bgmSource2);
        
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        if (sfxMixerGroup != null) sfxSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    private void SetupBgmSource(AudioSource src)
    {
        src.loop = true;
        src.playOnAwake = false;
        if (bgmMixerGroup != null) src.outputAudioMixerGroup = bgmMixerGroup;
    }

    public void PlaySFX(string key)
    {
        if (!sfxMap.TryGetValue(key, out AudioClip clip)) return;

        // Prevent overlapping too many instances of the same sound
        if (sfxCooldowns.TryGetValue(key, out float lastPlayTime))
        {
            if (Time.time - lastPlayTime < MIN_SFX_INTERVAL) return;
        }
        sfxCooldowns[key] = Time.time;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayBGM(string key, bool immediate = false)
    {
        if (!bgmMap.TryGetValue(key, out AudioClip clip))
        {
            if (!sfxMap.TryGetValue(key, out clip)) return;
        }

        // If the target clip is already playing, do nothing to prevent restart/stutter
        if (bgmSource1.isPlaying && bgmSource1.clip == clip) return;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        // Stop all BGM sources to guarantee only one BGM is active at a time
        bgmSource1.Stop();
        bgmSource2.Stop();

        bgmSource1.clip = clip;
        bgmSource1.volume = bgmVolume;
        bgmSource1.Play();

        isSource1Active = true;
    }

    public void StopBGM()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        bgmSource1.Stop();
        bgmSource2.Stop();
    }

    public void StopAllLevelSounds()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        // Stop our own BGM sources
        if (bgmSource1 != null) bgmSource1.Stop();
        if (bgmSource2 != null) bgmSource2.Stop();

        // Stop our own SFX source
        if (sfxSource != null) sfxSource.Stop();

        // Find all other active AudioSources in the scene and stop them
        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude);
        foreach (AudioSource src in allSources)
        {
            if (src != null && src != bgmSource1 && src != bgmSource2 && src != sfxSource)
            {
                src.Stop();
            }
        }
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        AudioSource active = isSource1Active ? bgmSource1 : bgmSource2;
        if (active != null) active.volume = bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetSFXVolumePersist(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (Instance != this) return;

        if (scene.name == "Victory")
        {
            StopAllLevelSounds();
            PlayBGM("bgm_winner", true);
        }
        else if (scene.name == "GameOver")
        {
            StopAllLevelSounds();
            PlayBGM("bgm_gameover", true);
        }
        else if (scene.name == "MainMenu")
        {
            StopBGM();
            if (bgmMap.ContainsKey("bgm_menu"))
            {
                PlayBGM("bgm_menu", true);
            }
        }
    }
}
