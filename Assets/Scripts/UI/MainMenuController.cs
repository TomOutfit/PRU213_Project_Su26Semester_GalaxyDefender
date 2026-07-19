using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionsPanel;
    public GameObject highScorePanel;
    public GameObject showroomPanel;

    [Header("Buttons")]
    public Button startButton;
    public Button loadButton;
    public Button optionsButton;
    public Button highScoreButton;
    public Button showroomButton;
    public Button exitButton;

    [Header("High Score Labels (5 TMP labels in order)")]
    public TMP_Text[] highScoreLabels = new TMP_Text[5];

    [Header("Level Indicator")]
    public TMP_Text levelIndicatorText;

    private void Awake()
    {
        // Optimization: Lock frame rate and enable VSync to prevent micro-stuttering/tearing on external displays, TV screens and projectors.
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;

        // Auto-resolve references if not set
        if (levelIndicatorText == null) levelIndicatorText = GameObject.Find("LevelIndicator")?.GetComponent<TMP_Text>();
        if (optionsPanel == null) optionsPanel = GameObject.Find("OptionsPanel");
        if (highScorePanel == null) highScorePanel = GameObject.Find("HighScorePanel");
        if (showroomPanel == null) showroomPanel = GameObject.Find("ShowroomPanel");

        // Dynamically build Showroom Button and Panel if they do not exist
        if (showroomPanel == null || showroomButton == null)
        {
            TryGenerateShowroomUI();
        }


        if (highScorePanel != null)
        {
            TMP_Text[] allTexts = highScorePanel.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < 5; i++)
            {
                if (highScoreLabels[i] == null)
                {
                    string targetName = $"highScore{i + 1}";
                    foreach (var txt in allTexts)
                    {
                        if (txt.gameObject.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
                        {
                            highScoreLabels[i] = txt;
                            break;
                        }
                    }
                }
            }
        }

        // Populate high score labels dynamically
        RefreshMenuScores();

        // Show level indicator
        if (levelIndicatorText != null)
        {
            bool hasVictory  = SaveManager.Instance != null
                                   ? SaveManager.Instance.IsVictoryCleared()
                                   : PlayerPrefs.GetInt("GameCleared", 0) == 1;
            bool hasLastLevel = PlayerPrefs.HasKey("LastLevel");

            if (hasVictory)
            {
                // Người chơi đã hoàn thành tất cả level → hiện huy hiệu đặc biệt
                levelIndicatorText.text = "<color=#FFD700>★ ALL LEVELS CLEARED ★</color>";
                levelIndicatorText.gameObject.SetActive(true);
            }
            else if (hasLastLevel)
            {
                // Đang chơi dở → hiện level gần nhất với tên thân thiện
                int lastBuildIndex = PlayerPrefs.GetInt("LastLevel", 1);
                string levelName   = BuildIndexToLevelName(lastBuildIndex);
                levelIndicatorText.text = $"Last played: {levelName}";
                levelIndicatorText.gameObject.SetActive(true);
            }
            else
            {
                // Chưa có save nào → ẩn indicator
                levelIndicatorText.gameObject.SetActive(false);
            }
        }

        // Initial panel state
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (highScorePanel != null) highScorePanel.SetActive(false);
        if (showroomPanel != null) showroomPanel.SetActive(false);
    }

    private void Start()
    {
        // Find buttons directly
        if (startButton == null) startButton = FindButton("MenuButtons/StartButton");
        if (loadButton == null) loadButton = FindButton("MenuButtons/LoadButton");
        if (optionsButton == null) optionsButton = FindButton("MenuButtons/OptionsButton");
        if (highScoreButton == null) highScoreButton = FindButton("MenuButtons/HighScoreButton");
        if (showroomButton == null) showroomButton = FindButton("MenuButtons/ShowroomButton");
        if (exitButton == null) exitButton = FindButton("MenuButtons/ExitButton");

        if (startButton != null) startButton.onClick.RemoveListener(OnStartClick);
        if (loadButton != null) loadButton.onClick.RemoveListener(OnLoadClick);
        if (optionsButton != null) optionsButton.onClick.RemoveListener(OnOptionsClick);
        if (highScoreButton != null) highScoreButton.onClick.RemoveListener(OnHighScoreClick);
        if (showroomButton != null) showroomButton.onClick.RemoveListener(OnShowroomClick);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClick);

        if (startButton != null) startButton.onClick.AddListener(OnStartClick);
        if (loadButton != null) loadButton.onClick.AddListener(OnLoadClick);
        if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsClick);
        if (highScoreButton != null) highScoreButton.onClick.AddListener(OnHighScoreClick);
        if (showroomButton != null) showroomButton.onClick.AddListener(OnShowroomClick);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClick);

        // Ensure SceneTransitionManager exists so the menu fade-in plays
        if (SceneTransitionManager.Instance == null)
        {
            GameObject tm = new GameObject("[SceneTransitionManager]");
            tm.AddComponent<SceneTransitionManager>();
        }

        // Fade in the overlay from black (for when entering MainMenu via SceneTransition)
        StartCoroutine(FadeInMenuOverlay());
    }

    private System.Collections.IEnumerator FadeInMenuOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) yield break;

        // Find FadeOverlay → CanvasGroup
        Transform fadeT = canvas.transform.Find("FadeOverlay");
        CanvasGroup cg = fadeT != null ? fadeT.GetComponent<CanvasGroup>() : null;

        // Also check SceneTransitionManager's own overlay
        if (SceneTransitionManager.Instance != null)
        {
            // The STM creates its own overlay; just wait a moment then return
            yield break;
        }

        if (cg != null)
        {
            // Wait one frame for the scene to fully settle
            yield return null;

            float duration = 0.7f;
            float elapsed = 0f;
            float startAlpha = cg.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
        }
    }

    private static Button FindButton(string path)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;
        Transform t = canvas.transform.Find(path);
        return t != null ? t.GetComponent<Button>() : null;
    }

    public void OnStartClick()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetSession();
            GameManager.Instance.SetState(GameManager.State.Playing);
        }

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene("Level1");
        else
            SceneManager.LoadScene("Level1");
    }

    public void OnLoadClick()
    {
        // Đọc save data — luôn đi qua SaveManager để pendingResumeWave được set đúng
        int savedLevel = PlayerPrefs.GetInt("LastLevel", 1);
        int savedScore = PlayerPrefs.GetInt("LastScore", 0);
        int savedWave  = SaveManager.Instance != null
                             ? SaveManager.Instance.GetLastWave()
                             : PlayerPrefs.GetInt("LastWave", 0);

        // Khôi phục score và trạng thái game
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.currentScore = savedScore;

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.State.Playing);

        // Đặt flag wave TRƯỚC khi load scene — WaveManager.Start() sẽ đọc flag này
        WaveManager.pendingResumeWave = savedWave;

        // Load scene qua transition (cinematic) hoặc trực tiếp
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(savedLevel);
        else
            SceneManager.LoadScene(savedLevel);
    }

    public void OnOptionsClick()
    {
        if (optionsPanel != null)
        {
            bool nextState = !optionsPanel.activeSelf;
            if (nextState)
            {
                if (highScorePanel != null) highScorePanel.SetActive(false);
                if (showroomPanel != null) showroomPanel.SetActive(false);
            }
            optionsPanel.SetActive(nextState);
        }
    }

    public void OnHighScoreClick()
    {
        if (highScorePanel != null)
        {
            bool nextState = !highScorePanel.activeSelf;
            if (nextState)
            {
                if (optionsPanel != null) optionsPanel.SetActive(false);
                if (showroomPanel != null) showroomPanel.SetActive(false);
                RefreshMenuScores();
            }
            highScorePanel.SetActive(nextState);
        }
    }

    public void OnShowroomClick()
    {
        if (showroomPanel != null)
        {
            bool nextState = !showroomPanel.activeSelf;
            if (nextState)
            {
                if (optionsPanel != null) optionsPanel.SetActive(false);
                if (highScorePanel != null) highScorePanel.SetActive(false);
            }
            showroomPanel.SetActive(nextState);
        }
    }

    private void RefreshMenuScores()
    {
        int[] scores = SaveManager.Instance != null ? SaveManager.Instance.GetHighScores() : new int[5];
        if (SaveManager.Instance == null)
        {
            for (int i = 0; i < 5; i++) scores[i] = PlayerPrefs.GetInt("HighScore_" + i, 0);
        }

        for (int i = 0; i < highScoreLabels.Length; i++)
        {
            if (highScoreLabels[i] != null)
                highScoreLabels[i].text = $"{i + 1}. {scores[i]:N0}";
        }
    }

    private void CleanLayoutComponents(GameObject go)
    {
        if (go == null) return;
        foreach (var comp in go.GetComponents<LayoutGroup>())
        {
            if (Application.isPlaying) Destroy(comp);
            else DestroyImmediate(comp);
        }
        foreach (var comp in go.GetComponents<ContentSizeFitter>())
        {
            if (Application.isPlaying) Destroy(comp);
            else DestroyImmediate(comp);
        }
        foreach (var comp in go.GetComponents<LayoutElement>())
        {
            if (Application.isPlaying) Destroy(comp);
            else DestroyImmediate(comp);
        }
        foreach (var comp in go.GetComponents<AspectRatioFitter>())
        {
            if (Application.isPlaying) Destroy(comp);
            else DestroyImmediate(comp);
        }
    }

    private void TryGenerateShowroomUI()
    {
        // 1. Find Canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[MainMenuController] Canvas not found. Cannot generate Showroom UI.");
            return;
        }

        // 2. Generate Showroom Button
        if (showroomButton == null)
        {
            GameObject btnGO = GameObject.Find("ShowroomButton");
            if (btnGO != null)
            {
                showroomButton = btnGO.GetComponent<Button>();
            }
            else
            {
                // Clone HighScoreButton or StartButton
                Button templateBtn = highScoreButton != null ? highScoreButton : startButton;
                if (templateBtn == null)
                {
                    templateBtn = canvas.GetComponentInChildren<Button>(true);
                }

                if (templateBtn != null)
                {
                    btnGO = Instantiate(templateBtn.gameObject, templateBtn.transform.parent);
                    btnGO.name = "ShowroomButton";
                    CleanLayoutComponents(btnGO);
                    
                    // Position it before ExitButton if exists
                    if (exitButton != null)
                    {
                        btnGO.transform.SetSiblingIndex(exitButton.transform.GetSiblingIndex());
                    }
                    else
                    {
                        Transform exitT = templateBtn.transform.parent.Find("ExitButton");
                        if (exitT != null)
                        {
                            btnGO.transform.SetSiblingIndex(exitT.GetSiblingIndex());
                        }
                    }

                    showroomButton = btnGO.GetComponent<Button>();
                    showroomButton.onClick.RemoveAllListeners();
                    
                    TMP_Text txt = btnGO.GetComponentInChildren<TMP_Text>();
                    if (txt != null)
                    {
                        txt.text = "SHOWROOM";
                    }
                }
            }
        }

        // 3. Generate Showroom Panel
        // Find and destroy old ShowroomPanel to force a fresh regeneration of the new layout!
        GameObject panelGO = GameObject.Find("ShowroomPanel");
        if (panelGO != null)
        {
            if (Application.isPlaying) Destroy(panelGO);
            else DestroyImmediate(panelGO);
            showroomPanel = null;
        }

        // Clone HighScorePanel
        GameObject templatePanel = highScorePanel != null ? highScorePanel : optionsPanel;
        if (templatePanel == null)
        {
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                var child = canvas.transform.GetChild(i).gameObject;
                if (child.name.Contains("Panel") && child.name != "ShowroomPanel")
                {
                    templatePanel = child;
                    break;
                }
            }
        }

        if (templatePanel != null)
        {
            panelGO = Instantiate(templatePanel, templatePanel.transform.parent);
            panelGO.name = "ShowroomPanel";
            showroomPanel = panelGO;

            // Remove cloned controllers
            HighScoreController oldHighScoreCtrl = panelGO.GetComponent<HighScoreController>();
            if (oldHighScoreCtrl != null)
            {
                if (Application.isPlaying) Destroy(oldHighScoreCtrl);
                else DestroyImmediate(oldHighScoreCtrl);
            }
            
            OptionsController oldOptionsCtrl = panelGO.GetComponent<OptionsController>();
            if (oldOptionsCtrl != null)
            {
                if (Application.isPlaying) Destroy(oldOptionsCtrl);
                else DestroyImmediate(oldOptionsCtrl);
            }

            CleanLayoutComponents(panelGO);

            // Set to Full Screen (Stretch to fill Canvas)
            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                panelRect.pivot = new Vector2(0.5f, 0.5f);
            }

            // Add ShowroomController
            ShowroomController showroomCtrl = panelGO.GetComponent<ShowroomController>();
            if (showroomCtrl == null) showroomCtrl = panelGO.AddComponent<ShowroomController>();

                    // Clean panel contents (destroy everything except background frames/borders)
                    for (int i = panelGO.transform.childCount - 1; i >= 0; i--)
                    {
                        Transform child = panelGO.transform.GetChild(i);
                        string childNameLower = child.name.ToLower();
                        
                        // Keep only the visual background/border frame
                        if (childNameLower.Contains("bg") || childNameLower.Contains("background") ||
                            childNameLower.Contains("panelbg") || childNameLower.Contains("border") ||
                            childNameLower.Contains("frame"))
                        {
                            continue;
                        }
                        
                        if (Application.isPlaying)
                            Destroy(child.gameObject);
                        else
                            DestroyImmediate(child.gameObject);
                    }

                    // Style/Create Title Text from scratch
                    var titleText = CreateText(panelGO.transform, "TitleText", "SHOWROOM & CODEX", 28f, new Color(0f, 1f, 1f, 1f), TextAlignmentOptions.Center);
                    RectTransform titleRect = titleText.GetComponent<RectTransform>();
                    titleRect.anchorMin = new Vector2(0.5f, 1f);
                    titleRect.anchorMax = new Vector2(0.5f, 1f);
                    titleRect.pivot = new Vector2(0.5f, 1f);
                    titleRect.anchoredPosition = new Vector2(0f, -30f);
                    titleRect.sizeDelta = new Vector2(600f, 50f);

                    // Create Close Button from templateBtn (or fallback to startButton)
                    Button closeBtn = null;
                    Button buttonTemplate = startButton != null ? startButton : showroomButton;
                    if (buttonTemplate != null)
                    {
                        GameObject closeBtnGO = Instantiate(buttonTemplate.gameObject, panelGO.transform);
                        closeBtnGO.name = "BackButton";
                        CleanLayoutComponents(closeBtnGO);
                        
                        // Destroy any other script that is not Button or UIButtonEffects
                        foreach (var script in closeBtnGO.GetComponents<MonoBehaviour>())
                        {
                            if (script != null && !(script is Button) && !(script is UIButtonEffects))
                            {
                                if (Application.isPlaying) Destroy(script);
                                else DestroyImmediate(script);
                            }
                        }

                        closeBtn = closeBtnGO.GetComponent<Button>();
                        closeBtn.onClick.RemoveAllListeners();
                        showroomCtrl.closeButton = closeBtn;

                        // Position at bottom center of the panel
                        RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
                        closeRect.anchorMin = new Vector2(0.5f, 0f);
                        closeRect.anchorMax = new Vector2(0.5f, 0f);
                        closeRect.pivot = new Vector2(0.5f, 0f);
                        closeRect.anchoredPosition = new Vector2(0f, 25f);
                        closeRect.sizeDelta = new Vector2(160f, 40f);

                        // Style text inside closeBtn (Force horizontal layout)
                        TMP_Text closeText = closeBtn.GetComponentInChildren<TMP_Text>();
                        if (closeText != null)
                        {
                            CleanLayoutComponents(closeText.gameObject);
                            closeText.text = "BACK";
                            closeText.fontSize = 16f;
                            closeText.color = Color.white;
                            closeText.alignment = TextAlignmentOptions.Center;
                            
                            RectTransform closeTxtRect = closeText.GetComponent<RectTransform>();
                            closeTxtRect.anchorMin = Vector2.zero;
                            closeTxtRect.anchorMax = Vector2.one;
                            closeTxtRect.offsetMin = Vector2.zero;
                            closeTxtRect.offsetMax = Vector2.zero;
                        }

                        // Style close button background image to Hot Pink/Magenta
                        Image closeImg = closeBtn.GetComponent<Image>();
                        if (closeImg != null)
                        {
                            closeImg.color = new Color(1f, 0f, 0.6f, 0.8f); // Neon Hot Pink/Magenta
                        }
                        
                        Outline closeOutline = closeBtn.GetComponent<Outline>();
                        if (closeOutline == null) closeOutline = closeBtn.gameObject.AddComponent<Outline>();
                        closeOutline.effectColor = new Color(1f, 0f, 0.6f, 1f); // Hot Pink border
                        closeOutline.effectDistance = new Vector2(1.5f, 1.5f);

                        // ── Tạo FooterFrame bọc quanh nút Back ──────────────────────────
                        GameObject footerFrameGO = new GameObject("FooterFrame", typeof(RectTransform), typeof(Image));
                        footerFrameGO.transform.SetParent(panelGO.transform, false);
                        CleanLayoutComponents(footerFrameGO);

                        RectTransform footerFrameRect = footerFrameGO.GetComponent<RectTransform>();
                        footerFrameRect.anchorMin     = new Vector2(0.05f, 0f);
                        footerFrameRect.anchorMax     = new Vector2(0.95f, 0f);
                        footerFrameRect.pivot         = new Vector2(0.5f, 0f);
                        footerFrameRect.offsetMin     = new Vector2(0f, 8f);
                        footerFrameRect.offsetMax     = new Vector2(0f, 0f);
                        footerFrameRect.sizeDelta     = new Vector2(0f, 65f);

                        Image footerImg = footerFrameGO.GetComponent<Image>();
                        footerImg.color = new Color(0.08f, 0.02f, 0.12f, 0.82f); // Deep magenta dark

                        Outline footerOutline = footerFrameGO.AddComponent<Outline>();
                        footerOutline.effectColor    = new Color(1f, 0f, 0.6f, 0.9f); // Hot Pink glow
                        footerOutline.effectDistance = new Vector2(2f, 2f);

                        // Di chuyển nút Back vào trong FooterFrame và căn giữa
                        closeBtnGO.transform.SetParent(footerFrameGO.transform, false);
                        closeRect.anchorMin        = new Vector2(0.5f, 0.5f);
                        closeRect.anchorMax        = new Vector2(0.5f, 0.5f);
                        closeRect.pivot            = new Vector2(0.5f, 0.5f);
                        closeRect.anchoredPosition = Vector2.zero;
                        closeRect.sizeDelta        = new Vector2(180f, 42f);
                    }


                    // Create Content Container
                    GameObject contentGO = new GameObject("ShowroomContent", typeof(RectTransform));
                    contentGO.transform.SetParent(panelGO.transform, false);
                    CleanLayoutComponents(contentGO);
                    
                    RectTransform contentRect = contentGO.GetComponent<RectTransform>();
                    contentRect.anchorMin = new Vector2(0f, 0f);
                    contentRect.anchorMax = new Vector2(1f, 1f);
                    contentRect.pivot = new Vector2(0.5f, 0.5f);
                    contentRect.offsetMin = new Vector2(40f, 75f);  // left, bottom
                    contentRect.offsetMax = new Vector2(-40f, -80f); // right, top

                    // 1. Create Tab Navigation Bar
                    GameObject tabsBarGO = new GameObject("TabBar", typeof(RectTransform));
                    tabsBarGO.transform.SetParent(contentRect, false);
                    CleanLayoutComponents(tabsBarGO);
                    
                    RectTransform tabsBarRect = tabsBarGO.GetComponent<RectTransform>();
                    tabsBarRect.anchorMin = new Vector2(0.5f, 1f);
                    tabsBarRect.anchorMax = new Vector2(0.5f, 1f);
                    tabsBarRect.pivot = new Vector2(0.5f, 1f);
                    tabsBarRect.anchoredPosition = new Vector2(0f, -10f);
                    tabsBarRect.sizeDelta = new Vector2(500f, 40f);

                    // Thêm background + border cho Tab Bar
                    Image tabBarImg = tabsBarGO.AddComponent<Image>();
                    tabBarImg.color = new Color(0f, 0.08f, 0.15f, 0.78f); // Dark navy translucent

                    Outline tabBarOutline = tabsBarGO.AddComponent<Outline>();
                    tabBarOutline.effectColor    = new Color(0f, 1f, 1f, 0.8f); // Neon Cyan border
                    tabBarOutline.effectDistance = new Vector2(2f, 2f);

                    Button tabTemplateBtn = closeBtn != null ? closeBtn : (highScoreButton != null ? highScoreButton : startButton);
                    if (tabTemplateBtn != null)
                    {
                        showroomCtrl.shipsTabButton = CreateTabButton(tabsBarRect, "ShipsTab", "SPACESHIPS", 0, tabTemplateBtn);
                        showroomCtrl.arsenalTabButton = CreateTabButton(tabsBarRect, "ArsenalTab", "ARSENAL", 1, tabTemplateBtn);
                        showroomCtrl.enemiesTabButton = CreateTabButton(tabsBarRect, "EnemiesTab", "ENEMIES", 2, tabTemplateBtn);
                    }

                    // 2. Create Split Content Area
                    GameObject displayAreaGO = new GameObject("DisplayArea", typeof(RectTransform));
                    displayAreaGO.transform.SetParent(contentRect, false);
                    CleanLayoutComponents(displayAreaGO);
                    
                    RectTransform displayAreaRect = displayAreaGO.GetComponent<RectTransform>();
                    displayAreaRect.anchorMin = Vector2.zero;
                    displayAreaRect.anchorMax = Vector2.one;
                    displayAreaRect.offsetMin = new Vector2(10f, 10f);
                    displayAreaRect.offsetMax = new Vector2(-10f, -65f); // Leave top space for Tabs

                    // Left Column (Visual Frame)
                    GameObject leftColGO = new GameObject("LeftColumn", typeof(RectTransform), typeof(Image));
                    leftColGO.transform.SetParent(displayAreaRect, false);
                    CleanLayoutComponents(leftColGO);
                    
                    RectTransform leftColRect = leftColGO.GetComponent<RectTransform>();
                    leftColRect.anchorMin = Vector2.zero;
                    leftColRect.anchorMax = new Vector2(0.42f, 1f);
                    leftColRect.offsetMin = Vector2.zero;
                    leftColRect.offsetMax = new Vector2(-15f, 0f);

                    Image leftColImg = leftColGO.GetComponent<Image>();
                    leftColImg.color = new Color(0f, 0.05f, 0.1f, 0.65f); // Neon dark translucent background
                    
                    Outline leftColOutline = leftColGO.AddComponent<Outline>();
                    leftColOutline.effectColor = new Color(0f, 1f, 1f, 0.35f); // Neon Cyan border
                    leftColOutline.effectDistance = new Vector2(1.5f, 1.5f);
                    // Item Main Sprite View
                    GameObject mainImgGO = new GameObject("ItemMainImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    mainImgGO.transform.SetParent(leftColRect, false);
                    CleanLayoutComponents(mainImgGO);
                    
                    RectTransform mainImgRect = mainImgGO.GetComponent<RectTransform>();
                    // Stretch anchor: lấp đầy phần trên left column (10% lề mỗi phía, từ 33%→93% chiều cao)
                    mainImgRect.anchorMin = new Vector2(0.1f, 0.33f);
                    mainImgRect.anchorMax = new Vector2(0.9f, 0.93f);
                    mainImgRect.pivot     = new Vector2(0.5f, 0.5f);
                    mainImgRect.offsetMin = Vector2.zero;
                    mainImgRect.offsetMax = Vector2.zero;
                    
                    showroomCtrl.itemMainImage = mainImgGO.GetComponent<Image>();
                    showroomCtrl.itemMainImage.preserveAspect = true;

                    // Prev / Next Navigation Buttons inside Left Column
                    if (tabTemplateBtn != null)
                    {
                        showroomCtrl.prevButton = CreateNavButton(leftColRect, "PrevButton", "<", new Vector2(0f, 0.62f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), tabTemplateBtn);
                        showroomCtrl.nextButton = CreateNavButton(leftColRect, "NextButton", ">", new Vector2(1f, 0.62f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), tabTemplateBtn);
                    }




                    // Extra Image Container
                    GameObject extraContainerGO = new GameObject("ExtraContainer", typeof(RectTransform), typeof(Image));
                    extraContainerGO.transform.SetParent(leftColRect, false);
                    CleanLayoutComponents(extraContainerGO);
                                        RectTransform extraContainerRect = extraContainerGO.GetComponent<RectTransform>();
                    extraContainerRect.anchorMin = new Vector2(0.05f, 0.03f);
                    extraContainerRect.anchorMax = new Vector2(0.95f, 0.31f);
                    extraContainerRect.offsetMin = Vector2.zero;
                    extraContainerRect.offsetMax = Vector2.zero;
                    
                    Image extraBg = extraContainerGO.GetComponent<Image>();
                    extraBg.color = new Color(0f, 0f, 0f, 0.4f);
                    
                    Outline extraOutline = extraContainerGO.AddComponent<Outline>();
                    extraOutline.effectColor = new Color(1f, 0f, 1f, 0.3f);
                    extraOutline.effectDistance = new Vector2(1f, 1f);
                    
                    showroomCtrl.extraImageContainer = extraContainerGO;

                    // Extra Sprite Label Text
                    var extraLabel = CreateText(extraContainerRect, "ExtraLabel", "PROJECTILE TYPE", 10f, new Color(0f, 1f, 1f, 0.8f), TextAlignmentOptions.Center);
                    RectTransform extraLabelRect = extraLabel.GetComponent<RectTransform>();
                    extraLabelRect.anchorMin = new Vector2(0f, 0.72f);
                    extraLabelRect.anchorMax = new Vector2(1f, 0.95f);
                    extraLabelRect.offsetMin = Vector2.zero;
                    extraLabelRect.offsetMax = Vector2.zero;

                    // Extra Sprite Image View
                    GameObject extraImgGO = new GameObject("ItemExtraImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    extraImgGO.transform.SetParent(extraContainerRect, false);
                    CleanLayoutComponents(extraImgGO);
                    
                    RectTransform extraImgRect = extraImgGO.GetComponent<RectTransform>();
                    // Stretch anchor trong ExtraContainer: chiếm phần trung tâm dưới label
                    extraImgRect.anchorMin = new Vector2(0.5f, 0.1f);
                    extraImgRect.anchorMax = new Vector2(0.5f, 0.65f);
                    extraImgRect.pivot     = new Vector2(0.5f, 0.5f);
                    extraImgRect.sizeDelta = new Vector2(120f, 0f); // chiều rộng cố định, chiều cao theo anchor

                    showroomCtrl.itemExtraImage = extraImgGO.GetComponent<Image>();
                    showroomCtrl.itemExtraImage.preserveAspect = true;

                    // Right Column (Info Display)
                    GameObject rightColGO = new GameObject("RightColumn", typeof(RectTransform));
                    rightColGO.transform.SetParent(displayAreaRect, false);
                    CleanLayoutComponents(rightColGO);
                    
                    RectTransform rightColRect = rightColGO.GetComponent<RectTransform>();
                    rightColRect.anchorMin = new Vector2(0.42f, 0f);
                    rightColRect.anchorMax = new Vector2(1f, 1f);
                    rightColRect.offsetMin = new Vector2(15f, 0f);
                    rightColRect.offsetMax = Vector2.zero;

                    // Apply Vertical Layout to Right Column
                    VerticalLayoutGroup rightColVGroup = rightColGO.AddComponent<VerticalLayoutGroup>();
                    rightColVGroup.padding = new RectOffset(20, 20, 15, 15);
                    rightColVGroup.spacing = 15f;
                    rightColVGroup.childAlignment = TextAnchor.UpperLeft;
                    rightColVGroup.childControlWidth = true;
                    rightColVGroup.childControlHeight = true;
                    rightColVGroup.childForceExpandWidth = true;
                    rightColVGroup.childForceExpandHeight = false;

                    // Item Name
                    var nameText = CreateText(rightColRect, "ItemNameText", "Item Name", 26f, new Color(0f, 1f, 1f, 1f));
                    LayoutElement nameLE = nameText.gameObject.AddComponent<LayoutElement>();
                    nameLE.preferredHeight = 35f;
                    nameLE.flexibleHeight = 0f;
                    showroomCtrl.itemNameText = nameText;

                    // Item Subtitle
                    var subtitleText = CreateText(rightColRect, "ItemSubtitleText", "Subtitle Class", 14f, new Color(1f, 0.4f, 1f, 0.9f));
                    LayoutElement subtitleLE = subtitleText.gameObject.AddComponent<LayoutElement>();
                    subtitleLE.preferredHeight = 22f;
                    subtitleLE.flexibleHeight = 0f;
                    showroomCtrl.itemSubtitleText = subtitleText;

                    // Stats Frame Box
                    GameObject statsBoxGO = new GameObject("StatsBoxFrame", typeof(RectTransform), typeof(Image));
                    statsBoxGO.transform.SetParent(rightColRect, false);
                    CleanLayoutComponents(statsBoxGO);
                    
                    LayoutElement statsBoxLE = statsBoxGO.AddComponent<LayoutElement>();
                    statsBoxLE.preferredHeight = 145f;
                    statsBoxLE.flexibleHeight = 0f;

                    Image statsBoxImg = statsBoxGO.GetComponent<Image>();
                    statsBoxImg.color = new Color(0f, 0.06f, 0.12f, 0.8f); // Soft glass dark cyan
                    
                    Outline statsBoxOutline = statsBoxGO.AddComponent<Outline>();
                    statsBoxOutline.effectColor = new Color(0f, 1f, 0.5f, 0.35f); // Soft Neon Green border
                    statsBoxOutline.effectDistance = new Vector2(1f, 1f);

                    // Stats Text
                    var statsText = CreateText(statsBoxGO.transform, "ItemStatsText", "Stats details...", 13.5f, new Color(0f, 1f, 0.5f, 1f));
                    RectTransform statsTextRect = statsText.GetComponent<RectTransform>();
                    statsTextRect.anchorMin = Vector2.zero;
                    statsTextRect.anchorMax = Vector2.one;
                    statsTextRect.offsetMin = new Vector2(15f, 10f);
                    statsTextRect.offsetMax = new Vector2(-15f, -10f);
                    
                    showroomCtrl.itemStatsText = statsText;

                    // Description text
                    var descText = CreateText(rightColRect, "ItemDescriptionText", "Description flavor text...", 14f, new Color(0.85f, 0.9f, 1f, 1f));
                    LayoutElement descLE = descText.gameObject.AddComponent<LayoutElement>();
                    descLE.preferredHeight = 180f;
                    descLE.flexibleHeight = 1f;
                    showroomCtrl.itemDescriptionText = descText;

#if UNITY_EDITOR
                    showroomCtrl.PopulateDefaultData();
#endif

                    // Strip any stray TitleVfx components that might have been cloned or added
                    foreach (var vfx in panelGO.GetComponentsInChildren<TitleVfx>(true))
                    {
                        if (Application.isPlaying) Destroy(vfx);
                        else DestroyImmediate(vfx);
                    }
                }
    }

    private void TryGenerateHighScoreUI()
    {
        if (highScorePanel == null)
        {
            Debug.LogWarning("[MainMenuController] highScorePanel reference is null. Cannot generate HighScore UI.");
            return;
        }

        // Clean highScorePanel (destroy everything except backgrounds/frames)
        for (int i = highScorePanel.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = highScorePanel.transform.GetChild(i);
            string childNameLower = child.name.ToLower();
            if (childNameLower.Contains("bg") || childNameLower.Contains("background") ||
                childNameLower.Contains("panelbg") || childNameLower.Contains("border") ||
                childNameLower.Contains("frame"))
            {
                continue;
            }
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        CleanLayoutComponents(highScorePanel);

        // Set to Full Screen (Stretch to fill Canvas)
        RectTransform panelRect = highScorePanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
        }

        // Add/Get HighScoreController
        HighScoreController hsCtrl = highScorePanel.GetComponent<HighScoreController>();
        if (hsCtrl == null) hsCtrl = highScorePanel.AddComponent<HighScoreController>();

        // 1. Create Title Text from scratch
        var titleText = CreateText(highScorePanel.transform, "TitleText", "HIGH SCORES", 28f, new Color(0f, 1f, 1f, 1f), TextAlignmentOptions.Center);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -30f);
        titleRect.sizeDelta = new Vector2(600f, 50f);

        // 2. Create Close (Back) & Clear Buttons
        Button closeBtn = null;
        Button clearBtn = null;
        Button buttonTemplate = startButton != null ? startButton : showroomButton;
        if (buttonTemplate != null)
        {
            // --- BACK BUTTON ---
            GameObject closeBtnGO = Instantiate(buttonTemplate.gameObject, highScorePanel.transform);
            closeBtnGO.name = "BackButton";
            CleanLayoutComponents(closeBtnGO);

            foreach (var script in closeBtnGO.GetComponents<MonoBehaviour>())
            {
                if (script != null && !(script is Button) && !(script is UIButtonEffects))
                {
                    if (Application.isPlaying) Destroy(script);
                    else DestroyImmediate(script);
                }
            }

            closeBtn = closeBtnGO.GetComponent<Button>();
            closeBtn.onClick.RemoveAllListeners();
            hsCtrl.backButton = closeBtn;

            // Position at bottom center-left
            RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(-100f, 25f);
            closeRect.sizeDelta = new Vector2(160f, 40f);

            TMP_Text closeText = closeBtn.GetComponentInChildren<TMP_Text>();
            if (closeText != null)
            {
                CleanLayoutComponents(closeText.gameObject);
                closeText.text = "BACK";
                closeText.fontSize = 16f;
                closeText.color = Color.white;
                closeText.alignment = TextAlignmentOptions.Center;

                RectTransform closeTxtRect = closeText.GetComponent<RectTransform>();
                closeTxtRect.anchorMin = Vector2.zero;
                closeTxtRect.anchorMax = Vector2.one;
                closeTxtRect.offsetMin = Vector2.zero;
                closeTxtRect.offsetMax = Vector2.zero;
            }

            Image closeImg = closeBtn.GetComponent<Image>();
            if (closeImg != null)
            {
                closeImg.color = new Color(1f, 0f, 0.6f, 0.8f); // Neon Hot Pink/Magenta
            }

            Outline closeOutline = closeBtn.GetComponent<Outline>();
            if (closeOutline == null) closeOutline = closeBtn.gameObject.AddComponent<Outline>();
            closeOutline.effectColor = new Color(1f, 0f, 0.6f, 1f); // Hot Pink border
            closeOutline.effectDistance = new Vector2(1.5f, 1.5f);

            // --- CLEAR BUTTON ---
            GameObject clearBtnGO = Instantiate(buttonTemplate.gameObject, highScorePanel.transform);
            clearBtnGO.name = "ClearButton";
            CleanLayoutComponents(clearBtnGO);

            foreach (var script in clearBtnGO.GetComponents<MonoBehaviour>())
            {
                if (script != null && !(script is Button) && !(script is UIButtonEffects))
                {
                    if (Application.isPlaying) Destroy(script);
                    else DestroyImmediate(script);
                }
            }

            clearBtn = clearBtnGO.GetComponent<Button>();
            clearBtn.onClick.RemoveAllListeners();
            hsCtrl.clearButton = clearBtn;

            // Position at bottom center-right
            RectTransform clearRect = clearBtn.GetComponent<RectTransform>();
            clearRect.anchorMin = new Vector2(0.5f, 0f);
            clearRect.anchorMax = new Vector2(0.5f, 0f);
            clearRect.pivot = new Vector2(0.5f, 0f);
            clearRect.anchoredPosition = new Vector2(100f, 25f);
            clearRect.sizeDelta = new Vector2(160f, 40f);

            TMP_Text clearText = clearBtn.GetComponentInChildren<TMP_Text>();
            if (clearText != null)
            {
                CleanLayoutComponents(clearText.gameObject);
                clearText.text = "CLEAR";
                clearText.fontSize = 16f;
                clearText.color = Color.white;
                clearText.alignment = TextAlignmentOptions.Center;

                RectTransform clearTxtRect = clearText.GetComponent<RectTransform>();
                clearTxtRect.anchorMin = Vector2.zero;
                clearTxtRect.anchorMax = Vector2.one;
                clearTxtRect.offsetMin = Vector2.zero;
                clearTxtRect.offsetMax = Vector2.zero;
            }

            Image clearImg = clearBtn.GetComponent<Image>();
            if (clearImg != null)
            {
                clearImg.color = new Color(1f, 0.25f, 0f, 0.8f); // Neon Orange-Red
            }

            Outline clearOutline = clearBtn.GetComponent<Outline>();
            if (clearOutline == null) clearOutline = clearBtn.gameObject.AddComponent<Outline>();
            clearOutline.effectColor = new Color(1f, 0.25f, 0f, 1f); // Orange-Red border
            clearOutline.effectDistance = new Vector2(1.5f, 1.5f);
        }

        // 3. Create Content Container
        GameObject contentGO = new GameObject("HighScoreContent", typeof(RectTransform));
        contentGO.transform.SetParent(highScorePanel.transform, false);
        CleanLayoutComponents(contentGO);

        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.offsetMin = new Vector2(100f, 90f);  // left, bottom margin
        contentRect.offsetMax = new Vector2(-100f, -90f); // right, top margin

        // Apply Vertical Layout to Content
        VerticalLayoutGroup contentVGroup = contentGO.AddComponent<VerticalLayoutGroup>();
        contentVGroup.padding = new RectOffset(20, 20, 10, 10);
        contentVGroup.spacing = 15f;
        contentVGroup.childAlignment = TextAnchor.MiddleCenter;
        contentVGroup.childControlWidth = true;
        contentVGroup.childControlHeight = true;
        contentVGroup.childForceExpandWidth = true;
        contentVGroup.childForceExpandHeight = false;

        // 4. Create 5 High Score Rows
        GameObject[] rowGOs = new GameObject[5];
        TMP_Text[] scoreTexts = new TMP_Text[5];

        for (int i = 0; i < 5; i++)
        {
            int rank = i + 1;
            string rowName = $"ScoreTop{rank}Panel";
            GameObject rowGO = new GameObject(rowName, typeof(RectTransform));
            rowGO.transform.SetParent(contentRect, false);
            CleanLayoutComponents(rowGO);

            LayoutElement rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 45f;
            rowLE.flexibleHeight = 0f;

            // Row Background image (glassmorphism matching StatsBox)
            Image rowImg = rowGO.AddComponent<Image>();
            rowImg.color = new Color(0f, 0.06f, 0.12f, 0.75f); // Soft glass dark cyan

            // Outline (glowing border)
            Outline rowOutline = rowGO.AddComponent<Outline>();
            rowOutline.effectColor = new Color(0f, 1f, 0.8f, 0.4f); // Cyan border
            rowOutline.effectDistance = new Vector2(1f, 1f);

            // Row Text (highScoreX)
            string labelName = $"highScore{rank}";
            var txt = CreateText(rowGO.transform, labelName, $"{rank}.  000,000", 18f, new Color(0f, 1f, 0.8f, 1f), TextAlignmentOptions.Left);
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(30f, 0f); // Indent text
            txtRect.offsetMax = new Vector2(-30f, 0f);

            rowGOs[i] = rowGO;
            scoreTexts[i] = txt;
        }

        // Bind references to HighScoreController
        hsCtrl.scoreTop1Panel = rowGOs[0];
        hsCtrl.scoreTop2Panel = rowGOs[1];
        hsCtrl.scoreTop3Panel = rowGOs[2];
        hsCtrl.scoreTop4Panel = rowGOs[3];
        hsCtrl.scoreTop5Panel = rowGOs[4];

        hsCtrl.highScore1 = scoreTexts[0];
        hsCtrl.highScore2 = scoreTexts[1];
        hsCtrl.highScore3 = scoreTexts[2];
        hsCtrl.highScore4 = scoreTexts[3];
        hsCtrl.highScore5 = scoreTexts[4];

        // Strip any stray TitleVfx components
        foreach (var vfx in highScorePanel.GetComponentsInChildren<TitleVfx>(true))
        {
            if (Application.isPlaying) Destroy(vfx);
            else DestroyImmediate(vfx);
        }
    }

    private Button CreateTabButton(Transform parent, string name, string text, int catIndex, Button template)
    {
        GameObject go = Instantiate(template.gameObject, parent);
        go.name = name;
        
        CleanLayoutComponents(go);
        
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(catIndex / 3f, 0f);
        rect.anchorMax = new Vector2((catIndex + 1) / 3f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(5f, 2f);
        rect.offsetMax = new Vector2(-5f, -2f);
        
        Button btn = go.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();

        // Add Outline component to Tab Buttons for a glowing neon border
        Outline outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectDistance = new Vector2(1.2f, 1.2f);
        
        TMP_Text txt = go.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            txt.text = text;
            txt.fontSize = 12f;
            txt.color = Color.white;
            CleanLayoutComponents(txt.gameObject);
        }
        
        return btn;
    }

    private Button CreateNavButton(Transform parent, string name, string text, Vector2 anchor, Vector2 pivot, Vector2 offset, Button template)
    {
        GameObject go = Instantiate(template.gameObject, parent);
        go.name = name;
        
        CleanLayoutComponents(go);
        
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(35f, 45f);
        
        Button btn = go.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        
        TMP_Text txt = go.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            txt.text = text;
            txt.fontSize = 20f;
            txt.color = new Color(0f, 1f, 1f, 1f);
            CleanLayoutComponents(txt.gameObject);
        }
        
        return btn;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        
        if (levelIndicatorText != null)
        {
            tmp.font = levelIndicatorText.font;
            tmp.fontSharedMaterial = levelIndicatorText.fontSharedMaterial;
        }
        else if (highScoreLabels != null && highScoreLabels.Length > 0 && highScoreLabels[0] != null)
        {
            tmp.font = highScoreLabels[0].font;
            tmp.fontSharedMaterial = highScoreLabels[0].fontSharedMaterial;
        }
        
        return tmp;
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Showroom UI")]
    public void ForceGenerateShowroomUI()
    {
        if (showroomPanel != null) DestroyImmediate(showroomPanel);
        showroomPanel = null;
        
        GameObject oldButton = GameObject.Find("ShowroomButton");
        if (oldButton != null) DestroyImmediate(oldButton);
        showroomButton = null;
        
        TryGenerateShowroomUI();
        
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[MainMenuController] Showroom UI generated and scene marked dirty!");
    }

    [ContextMenu("Generate HighScore UI")]
    public void ForceGenerateHighScoreUI()
    {
        TryGenerateHighScoreUI();
        
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[MainMenuController] HighScore UI generated and scene marked dirty!");
    }
#endif

    public void OnExitClick()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Chuyển Build Index thành tên level thân thiện để hiển thị trên Level Indicator.
    /// Cập nhật mapping này nếu bạn thêm/đổi thứ tự scene trong Build Settings.
    /// </summary>
    private static string BuildIndexToLevelName(int buildIndex)
    {
        // Thử lấy tên scene từ Build Settings trước (tự động, không cần hardcode)
        string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(buildIndex);
        if (!string.IsNullOrEmpty(path))
        {
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

            // Ẩn các scene không phải level thực sự
            if (sceneName.Equals("MainMenu",  System.StringComparison.OrdinalIgnoreCase) ||
                sceneName.Equals("Victory",   System.StringComparison.OrdinalIgnoreCase) ||
                sceneName.Equals("GameOver",  System.StringComparison.OrdinalIgnoreCase) ||
                sceneName.Equals("EndCredit", System.StringComparison.OrdinalIgnoreCase))
            {
                return $"Level {buildIndex}"; // fallback an toàn
            }

            // "Level1" → "Level 1",  "Level2" → "Level 2", etc.
            if (sceneName.StartsWith("Level", System.StringComparison.OrdinalIgnoreCase))
            {
                string numPart = sceneName.Substring(5); // cắt "Level"
                return $"Level {numPart}";
            }

            return sceneName; // Giữ nguyên tên gốc nếu không match
        }

        // Fallback nếu Build Index không hợp lệ
        return $"Level {buildIndex}";
    }
}
