using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class OptionsController : MonoBehaviour
{
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("UI Elements")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle fullscreenToggle;
    public Button backButton;

    private void Start()
    {
        // Load settings or default to 1
        float masterVolume = PlayerPrefs.GetFloat("Volume_Master", 1f);
        float musicVolume = PlayerPrefs.GetFloat("Volume_Music", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("Volume_SFX", 1f);
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

        // Try to find UI elements programmatically if not assigned
        if (masterSlider == null) masterSlider = GameObject.Find("MasterVolumeSlider")?.GetComponent<Slider>();
        if (musicSlider == null) musicSlider = GameObject.Find("MusicVolumeSlider")?.GetComponent<Slider>();
        if (sfxSlider == null) sfxSlider = GameObject.Find("SFXVolumeSlider")?.GetComponent<Slider>();
        if (fullscreenToggle == null) fullscreenToggle = GameObject.Find("FullscreenToggle")?.GetComponent<Toggle>();
        if (backButton == null) backButton = GameObject.Find("BackButton")?.GetComponent<Button>();

        if (masterSlider != null)
        {
            masterSlider.value = masterVolume;
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
        }
        if (musicSlider != null)
        {
            musicSlider.value = musicVolume;
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = sfxVolume;
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFullscreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggle);
        }
        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBack);
        }

        // Apply immediately
        OnMasterChanged(masterVolume);
        OnMusicChanged(musicVolume);
        OnSFXChanged(sfxVolume);
        OnFullscreenToggle(isFullscreen);
    }

    public void OnMasterChanged(float v)
    {
        float db = Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", db);
        }
        else
        {
            AudioListener.volume = v;
        }
        PlayerPrefs.SetFloat("Volume_Master", v);
        OnAnyChange();
    }

    public void OnMusicChanged(float v)
    {
        float db = Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", db);
        }
        else
        {
            AudioManager.Instance?.SetBGMVolume(v);
        }
        PlayerPrefs.SetFloat("Volume_Music", v);
        OnAnyChange();
    }

    public void OnSFXChanged(float v)
    {
        float db = Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;
        if (audioMixer != null)
        {
            audioMixer.SetFloat("SFXVolume", db);
        }
        else
        {
            AudioManager.Instance?.SetSFXVolume(v);
        }
        PlayerPrefs.SetFloat("Volume_SFX", v);
        OnAnyChange();
    }

    public void OnFullscreenToggle(bool v)
    {
        Screen.fullScreen = v;
        PlayerPrefs.SetInt("Fullscreen", v ? 1 : 0);
        OnAnyChange();
    }

    private void OnAnyChange()
    {
        PlayerPrefs.Save();
    }

    public void GoBack()
    {
        gameObject.SetActive(false);
    }
}
