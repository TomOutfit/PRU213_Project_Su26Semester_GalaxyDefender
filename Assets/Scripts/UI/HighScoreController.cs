using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HighScoreController : MonoBehaviour
{
    [Header("Score Panels")]
    public GameObject scoreTop1Panel;
    public GameObject scoreTop2Panel;
    public GameObject scoreTop3Panel;
    public GameObject scoreTop4Panel;
    public GameObject scoreTop5Panel;

    [Header("Score Labels")]
    public TMP_Text highScore1;
    public TMP_Text highScore2;
    public TMP_Text highScore3;
    public TMP_Text highScore4;
    public TMP_Text highScore5;

    [Header("Navigation")]
    public Button backButton;

    private UIPanelEffects panelEffects;

    private void Awake()
    {
        panelEffects = GetComponent<UIPanelEffects>();
    }

    private void Start()
    {
        AutoBind();
        RefreshScores();
    }

    private void OnEnable()
    {
        AutoBind();
        RefreshScores();
    }

    public void AutoBind()
    {
        if (scoreTop1Panel == null) scoreTop1Panel = transform.Find("ScoreTop1Panel")?.gameObject;
        if (scoreTop2Panel == null) scoreTop2Panel = transform.Find("ScoreTop2Panel")?.gameObject;
        if (scoreTop3Panel == null) scoreTop3Panel = transform.Find("ScoreTop3Panel")?.gameObject;
        if (scoreTop4Panel == null) scoreTop4Panel = transform.Find("ScoreTop4Panel")?.gameObject;
        if (scoreTop5Panel == null) scoreTop5Panel = transform.Find("ScoreTop5Panel")?.gameObject;

        if (highScore1 == null) highScore1 = GetLabel(scoreTop1Panel, "highScore1");
        if (highScore2 == null) highScore2 = GetLabel(scoreTop2Panel, "highScore2");
        if (highScore3 == null) highScore3 = GetLabel(scoreTop3Panel, "highScore3");
        if (highScore4 == null) highScore4 = GetLabel(scoreTop4Panel, "highScore4");
        if (highScore5 == null) highScore5 = GetLabel(scoreTop5Panel, "highScore5");

        if (backButton == null)
        {
            Transform backT = transform.Find("BackButton");
            if (backT != null) backButton = backT.GetComponent<Button>();
            if (backButton == null) backButton = GetComponentInChildren<Button>();
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(GoBack);
            backButton.onClick.AddListener(GoBack);
        }
    }

    private TMP_Text GetLabel(GameObject panel, string labelName)
    {
        if (panel != null)
        {
            Transform t = panel.transform.Find(labelName);
            if (t != null) return t.GetComponent<TMP_Text>();
            return panel.GetComponentInChildren<TMP_Text>();
        }
        return GameObject.Find(labelName)?.GetComponent<TMP_Text>();
    }

    public void RefreshScores()
    {
        int[] scores;
        if (SaveManager.Instance != null)
        {
            scores = SaveManager.Instance.GetHighScores();
        }
        else
        {
            scores = new int[5];
            for (int i = 0; i < 5; i++)
            {
                scores[i] = PlayerPrefs.GetInt("HighScore_" + i, 0);
            }
        }

        TMP_Text[] labels = new TMP_Text[] { highScore1, highScore2, highScore3, highScore4, highScore5 };
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null)
            {
                labels[i].text = $"{i + 1}.  {scores[i]:N0}";
            }
        }
    }

    public void GoBack()
    {
        if (panelEffects != null)
        {
            panelEffects.Hide();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
