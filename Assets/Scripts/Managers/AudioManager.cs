using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Clips")]
    [Tooltip("Assign AudioClip per key name for SFX playback (sfx_shoot, sfx_explosion, etc.).")]
    public List<NamedAudioClip> sfxClips = new List<NamedAudioClip>();

    [Header("BGM Clips")]
    [Tooltip("Assign BGM AudioClips here (bgm_gameplay, bgm_boss, bgm_gameover, bgm_winner, etc.).")]
    public List<NamedAudioClip> bgmClips = new List<NamedAudioClip>();

    [Header("BGM")]
    public AudioSource bgmSource;
    public UnityEngine.Audio.AudioMixerGroup bgmMixerGroup;

    private AudioSource bgmSource1;
    private AudioSource bgmSource2;
    private bool isSource1Active = true;
    private Dictionary<string, AudioClip> sfxMap = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> bgmMap = new Dictionary<string, AudioClip>();
    private Coroutine fadeRoutine;

    [HideInInspector]
    public float bgmVolume = 1f;

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
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

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

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }
        bgmSource1 = bgmSource;
        bgmSource1.loop = true;
        bgmSource1.playOnAwake = false;
        if (bgmMixerGroup != null)
            bgmSource1.outputAudioMixerGroup = bgmMixerGroup;

        bgmSource2 = gameObject.AddComponent<AudioSource>();
        bgmSource2.loop = true;
        bgmSource2.playOnAwake = false;
        if (bgmMixerGroup != null)
            bgmSource2.outputAudioMixerGroup = bgmMixerGroup;
    }

    [HideInInspector]
    public float sfxVolume = 1f;


    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void PlaySFX(string key)
    {
        if (!sfxMap.TryGetValue(key, out AudioClip clip)) return;
 
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        src.playOnAwake = false;
        if (bgmMixerGroup != null)
            src.outputAudioMixerGroup = bgmMixerGroup;
        src.volume = sfxVolume;
        src.Play();
        StartCoroutine(CleanupSFXSource(src));
    }

    private IEnumerator CleanupSFXSource(AudioSource src)
    {
        yield return new WaitUntil(() => !src.isPlaying);
        Destroy(src);
    }

    public void PlayBGM(string key)
    {
        // Try bgmMap first; fall back to sfxMap for backward-compatibility
        if (!bgmMap.TryGetValue(key, out AudioClip clip))
        {
            if (!sfxMap.TryGetValue(key, out clip)) return;
        }

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossfadeBGM(clip));
    }

    private IEnumerator CrossfadeBGM(AudioClip newClip)
    {
        AudioSource activeSource = isSource1Active ? bgmSource1 : bgmSource2;
        AudioSource newSource = isSource1Active ? bgmSource2 : bgmSource1;

        newSource.clip = newClip;
        newSource.volume = 0f;
        newSource.Play();

        float duration = 1.0f;
        float elapsed = 0f;
        float startVol = activeSource.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pct = elapsed / duration;
            activeSource.volume = Mathf.Lerp(startVol, 0f, pct);
            newSource.volume = Mathf.Lerp(0f, bgmVolume, pct);
            yield return null;
        }

        activeSource.volume = 0f;
        activeSource.Stop();
        newSource.volume = bgmVolume;

        isSource1Active = !isSource1Active;
        bgmSource = newSource; // Keep referencing the active one
    }

    public void StopBGM()
    {
        bgmSource1.Stop();
        bgmSource2.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        // Only apply to the currently-playing source so we don't
        // override a crossfade that is still in progress.
        AudioSource active = isSource1Active ? bgmSource1 : bgmSource2;
        if (active != null) active.volume = bgmVolume;
    }

    public void SetSFXVolumePersist(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }
}
