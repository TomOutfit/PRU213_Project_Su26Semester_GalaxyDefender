using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuEffectsInitializer : MonoBehaviour
{
    private void Start()
    {
        InitializeEffects();
    }

    public void InitializeEffects()
    {
        Debug.Log("Initializing Main Menu Art Effects...");

        // 1. Setup Background Particles
        GameObject bgGO = GameObject.Find("Background");
        if (bgGO != null)
        {
            if (bgGO.GetComponent<MenuBackgroundParticles>() == null)
            {
                bgGO.AddComponent<MenuBackgroundParticles>();
                Debug.Log("Added MenuBackgroundParticles to Background GameObject.");
            }
        }
        else
        {
            // If background isn't found by name, try to attach to the canvas or create one
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                // Find a GameObject called Background or Image under canvas
                Transform bgTransform = canvas.transform.Find("Background");
                if (bgTransform != null)
                {
                    if (bgTransform.GetComponent<MenuBackgroundParticles>() == null)
                    {
                        bgTransform.gameObject.AddComponent<MenuBackgroundParticles>();
                    }
                }
            }
        }

        // 2. Setup Title/Logo VFX
        // Look for Logo GameObject or any text element containing "GALAXY" or "DEFENDER"
        GameObject logoGO = GameObject.Find("Logo");
        if (logoGO != null)
        {
            if (logoGO.GetComponent<TitleVfx>() == null)
            {
                logoGO.AddComponent<TitleVfx>();
                Debug.Log("Added TitleVfx to Logo GameObject.");
            }
        }
        else
        {
            // Search all TMP Text elements for title text
            TMP_Text[] allTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
            foreach (TMP_Text txt in allTexts)
            {
                string lower = txt.text.ToLower();
                if (lower.Contains("galaxy") || lower.Contains("defender"))
                {
                    if (txt.GetComponent<TitleVfx>() == null)
                    {
                        txt.gameObject.AddComponent<TitleVfx>();
                        Debug.Log($"Added TitleVfx to text object: {txt.gameObject.name} (Text: '{txt.text}')");
                    }
                }
            }
        }

        // 3. Setup Button Effects
        // We find the buttons by name or from MainMenuController references
        MainMenuController menuController = FindAnyObjectByType<MainMenuController>();
        if (menuController != null)
        {
            SetupButtonEffect(menuController.startButton);
            SetupButtonEffect(menuController.loadButton);
            SetupButtonEffect(menuController.optionsButton);
            SetupButtonEffect(menuController.highScoreButton);
            SetupButtonEffect(menuController.exitButton);
        }

        // Fallback or double-check buttons by name
        string[] buttonNames = { "StartButton", "LoadButton", "OptionsButton", "HighScoreButton", "ExitButton" };
        foreach (string btnName in buttonNames)
        {
            GameObject btnGO = GameObject.Find(btnName);
            if (btnGO != null)
            {
                Button btn = btnGO.GetComponent<Button>();
                if (btn != null)
                {
                    SetupButtonEffect(btn);
                }
            }
        }
    }

    private void SetupButtonEffect(Button button)
    {
        if (button == null) return;
        
        UIButtonEffects eff = button.GetComponent<UIButtonEffects>();
        if (eff == null)
        {
            button.gameObject.AddComponent<UIButtonEffects>();
            Debug.Log($"Added UIButtonEffects to button: {button.gameObject.name}");
        }
    }
}
