using UnityEngine;
using UnityEngine.UI;

public class OptionsController : MonoBehaviour
{
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
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        if (musicSlider != null)
        {
            musicSlider.value = musicVolume;
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = sfxVolume;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFullscreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBack);
        }

        // Apply immediately
        SetMasterVolume(masterVolume);
        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);
        SetFullscreen(isFullscreen);
    }

    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume_Master", value);
    }

    public void SetMusicVolume(float value)
    {
        AudioManager.Instance?.SetBGMVolume(value);
        PlayerPrefs.SetFloat("Volume_Music", value);
    }

    public void SetSFXVolume(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
        PlayerPrefs.SetFloat("Volume_SFX", value);
    }

    public void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt("Fullscreen", value ? 1 : 0);
    }

    public void GoBack()
    {
        gameObject.SetActive(false);
    }
}
