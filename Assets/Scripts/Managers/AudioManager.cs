using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Clips")]
    [Tooltip("Assign AudioClip per key name for SFX playback.")]
    public List<NamedAudioClip> sfxClips = new List<NamedAudioClip>();

    [Header("BGM")]
    public AudioSource bgmSource;
    public AudioMixerGroup bgmMixerGroup;

    private Dictionary<string, AudioClip> sfxMap = new Dictionary<string, AudioClip>();
    private Coroutine fadeRoutine;

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

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            if (bgmMixerGroup != null)
                bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        }
    }

    public void PlaySFX(string key)
    {
        if (!sfxMap.TryGetValue(key, out AudioClip clip)) return;

        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        src.playOnAwake = false;
        src.PlayOneShot(clip);
        StartCoroutine(CleanupSFXSource(src));
    }

    private IEnumerator CleanupSFXSource(AudioSource src)
    {
        yield return new WaitUntil(() => !src.isPlaying);
        Destroy(src);
    }

    public void PlayBGM(string key)
    {
        if (!sfxMap.TryGetValue(key, out AudioClip clip)) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossfadeBGM(clip));
    }

    private IEnumerator CrossfadeBGM(AudioClip newClip)
    {
        // Fade out
        float t = 0f;
        float duration = 1f;
        float startVol = bgmSource.volume;

        while (t < duration)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.volume = 0f;
        bgmSource.Play();

        // Fade in
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }

        bgmSource.volume = 1f;
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = Mathf.Clamp01(volume);
    }
}
