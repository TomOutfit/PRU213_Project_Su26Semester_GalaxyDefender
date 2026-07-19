using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Medal colors for Top 1 / 2 / 3
// Easily adjustable here without touching the Inspector.

public class HighScoreController : MonoBehaviour
{
    // ── Medal colour palette ──────────────────────────────────────────────
    private static readonly Color ColorGold   = new Color(1.00f, 0.84f, 0.00f, 1f); // #FFD700
    private static readonly Color ColorSilver = new Color(0.75f, 0.75f, 0.75f, 1f); // #C0C0C0
    private static readonly Color ColorBronze = new Color(0.80f, 0.50f, 0.20f, 1f); // #CC8033

    // Panel background tint (25 % alpha so the original art still shows)
    private static readonly Color BgGold   = new Color(1.00f, 0.84f, 0.00f, 0.18f);
    private static readonly Color BgSilver = new Color(0.75f, 0.75f, 0.75f, 0.18f);
    private static readonly Color BgBronze = new Color(0.80f, 0.50f, 0.20f, 0.18f);
    // ─────────────────────────────────────────────────────────────────────

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
    public Button clearButton;

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

        if (clearButton == null)
        {
            Transform clearT = transform.Find("ClearButton");
            if (clearT != null) clearButton = clearT.GetComponent<Button>();
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveListener(ClearScores);
            clearButton.onClick.AddListener(ClearScores);
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
        GameObject[] panels = new GameObject[] { scoreTop1Panel, scoreTop2Panel, scoreTop3Panel, scoreTop4Panel, scoreTop5Panel };

        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null)
            {
                labels[i].text = $"{i + 1}.  {scores[i]:N0}";

                // Apply medal colour to text (Top 1 / 2 / 3 only)
                switch (i)
                {
                    case 0: labels[i].color = ColorGold;   break;
                    case 1: labels[i].color = ColorSilver; break;
                    case 2: labels[i].color = ColorBronze; break;
                    default: labels[i].color = Color.white; break;
                }
            }

            // Apply subtle background tint to the panel Image
            if (panels[i] != null)
            {
                Image panelImg = panels[i].GetComponent<Image>();
                if (panelImg != null)
                {
                    switch (i)
                    {
                        case 0: panelImg.color = BgGold;   break;
                        case 1: panelImg.color = BgSilver; break;
                        case 2: panelImg.color = BgBronze; break;
                        // Top 4 & 5: leave unchanged
                    }
                }
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

    public void ClearScores()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ClearHighScores();
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                PlayerPrefs.DeleteKey("HighScore_" + i);
            }
            PlayerPrefs.Save();
        }

        RefreshScores();
        Debug.Log("High Scores Cleared!");
    }
}
