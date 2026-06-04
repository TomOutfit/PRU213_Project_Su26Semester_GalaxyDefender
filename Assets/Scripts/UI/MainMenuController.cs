using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionsPanel;
    public GameObject highScorePanel;

    public void OnStartClick()
    {
        SceneManager.LoadScene("Level1");
    }

    public void OnOptionsClick()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(!optionsPanel.activeSelf);
        }
    }

    public void OnHighScoreClick()
    {
        if (highScorePanel != null)
        {
            highScorePanel.SetActive(!highScorePanel.activeSelf);
        }
    }

    public void OnExitClick()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
