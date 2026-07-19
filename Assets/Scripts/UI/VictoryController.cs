using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class VictoryController : MonoBehaviour
{
    [Header("Stats Text (TMP)")]
    public TMP_Text scoreTextTMP;
    public TMP_Text survivalTimeTextTMP;
    public TMP_Text enemiesKilledTextTMP;

    [Header("Buttons")]
    public Button menuButton;
    public Button playAgainButton;

    [Header("Title Pulse")]
    public TMP_Text titleText;
    public float pulseScale  = 1.12f;
    public float pulseSpeed  = 2.5f;

    private Coroutine _fireworksRoutine;
    private Coroutine _pulseTitleRoutine;

    private void Awake()
    {
        if (menuButton == null)
            menuButton = GameObject.Find("MenuButton")?.GetComponent<Button>();
        if (playAgainButton == null)
            playAgainButton = GameObject.Find("PlayAgainButton")?.GetComponent<Button>();
        if (titleText == null)
            titleText = GameObject.Find("TitleText")?.GetComponent<TMP_Text>();

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(OnMenuClick);
        }
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(OnPlayAgainClick);
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.State.Victory);
        }

        int finalScore  = ScoreManager.Instance != null ? ScoreManager.Instance.currentScore : 0;
        float timeVal   = GameManager.Instance   != null ? GameManager.Instance.survivalTime  : 0f;
        int killedCount = GameManager.Instance   != null ? GameManager.Instance.totalEnemiesKilled : 0;

        int minutes = Mathf.FloorToInt(timeVal / 60f);
        int seconds = Mathf.FloorToInt(timeVal % 60f);
        string timeStr = $"{minutes}:{seconds:D2}";

        if (scoreTextTMP         != null) scoreTextTMP.text         = $"FINAL SCORE: {finalScore:N0}";
        if (enemiesKilledTextTMP != null) enemiesKilledTextTMP.text = $"ENEMIES DEFEATED: {killedCount}";
        if (survivalTimeTextTMP  != null) survivalTimeTextTMP.text  = $"SURVIVAL TIME: {timeStr}";

        // Save high score
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveHighScore(finalScore);

        // Start title pulse animation
        if (titleText != null)
            _pulseTitleRoutine = StartCoroutine(PulseTitle());

        // Start fireworks celebration
        _fireworksRoutine = StartCoroutine(FireworksRoutine());
    }

    private IEnumerator PulseTitle()
    {
        if (titleText == null) yield break;
        RectTransform rt = titleText.GetComponent<RectTransform>();
        while (true)
        {
            float s = 1f + (pulseScale - 1f) * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
            if (rt != null) rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    private Sprite CreateGlowSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float alpha = Mathf.Clamp01(1f - dist);
                // Apply exponential curve for a smoother radial glow
                alpha = Mathf.Pow(alpha, 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private IEnumerator FireworksRoutine()
    {
        // Obtain canvas
        Canvas canvas = null;
        if (scoreTextTMP != null) canvas = scoreTextTMP.canvas;
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) yield break;

        // Find the panel background container robustly
        Transform parentPanel = scoreTextTMP != null ? scoreTextTMP.transform.parent : canvas.transform;
        Sprite glowSprite = CreateGlowSprite();

        // 1. Create a dedicated FireworksContainer to manage layering inside the panel
        GameObject containerGo = GameObject.Find("FireworksContainer");
        if (containerGo == null)
        {
            containerGo = new GameObject("FireworksContainer", typeof(RectTransform));
            RectTransform containerRt = containerGo.GetComponent<RectTransform>();
            containerRt.SetParent(parentPanel, false);
            containerRt.localScale = Vector3.one;
            containerRt.anchoredPosition = Vector2.zero;
            
            // Set as first sibling of the panel so it renders behind text/buttons but in front of panel bg
            containerGo.transform.SetSiblingIndex(0);
        }
        Transform fireworksRoot = containerGo.transform;

        // Startup delay
        yield return new WaitForSeconds(0.4f);

        while (true)
        {
            RectTransform canvasRt = canvas.GetComponent<RectTransform>();
            float width = canvasRt != null ? canvasRt.rect.width : Screen.width;
            float height = canvasRt != null ? canvasRt.rect.height : Screen.height;

            float startY = -height * 0.55f;

            // Pick a launch pattern to make it spectacular
            int pattern = Random.Range(0, 4);

            if (pattern == 0) // Single Giant Firework
            {
                float startX = Random.Range(-width * 0.35f, width * 0.35f);
                float targetY = Random.Range(height * 0.15f, height * 0.42f);
                StartCoroutine(LaunchRocket(fireworksRoot, glowSprite, startX, startY, targetY, true));
                yield return new WaitForSeconds(Random.Range(0.8f, 1.4f));
            }
            else if (pattern == 1) // V-Shape Trio
            {
                float centerX = Random.Range(-width * 0.15f, width * 0.15f);
                // Center rocket
                StartCoroutine(LaunchRocket(fireworksRoot, glowSprite, centerX, startY, Random.Range(height * 0.25f, height * 0.45f), false));
                yield return new WaitForSeconds(0.15f);
                // Left and right wing rockets
                StartCoroutine(LaunchRocket(fireworksRoot, glowSprite, centerX - width * 0.2f, startY, Random.Range(height * 0.1f, height * 0.3f), false));
                StartCoroutine(LaunchRocket(fireworksRoot, glowSprite, centerX + width * 0.2f, startY, Random.Range(height * 0.1f, height * 0.3f), false));

                yield return new WaitForSeconds(Random.Range(1.2f, 1.8f));
            }
            else if (pattern == 2) // Cross Launch Doublet
            {
                float leftX = -width * 0.35f;
                float rightX = width * 0.35f;
                float targetY = Random.Range(height * 0.15f, height * 0.35f);

                // Angled inward crossing path
                StartCoroutine(LaunchRocket(fireworksRoot, glowSprite, leftX, startY, targetY, false, 80f));
                StartCoroutine(LaunchRocket(fireworksRoot, glowSprite, rightX, startY, targetY, false, -80f));

                yield return new WaitForSeconds(Random.Range(1.0f, 1.6f));
            }
            else // Cascade Barrage (4-5 rockets climbing in sequence)
            {
                int count = Random.Range(4, 6);
                float step = (width * 0.7f) / (count - 1);
                float startLeft = -width * 0.35f;
                bool leftToRight = Random.value > 0.5f;

                for (int i = 0; i < count; i++)
                {
                    float index = leftToRight ? i : (count - 1 - i);
                    float startX = startLeft + index * step + Random.Range(-15f, 15f);
                    float targetY = Random.Range(height * 0.05f, height * 0.38f);
                    StartCoroutine(LaunchRocket(fireworksRoot, glowSprite, startX, startY, targetY, false));
                    yield return new WaitForSeconds(0.2f);
                }

                yield return new WaitForSeconds(Random.Range(1.2f, 1.8f));
            }
        }
    }

    private IEnumerator LaunchRocket(Transform parent, Sprite glowSprite, float startX, float startY, float targetY, bool isGiant, float xSpeed = 0f)
    {
        // 1. Create rocket UI GameObject
        GameObject rocketGo = new GameObject("FireworkRocket");
        RectTransform rt = rocketGo.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;

        float rocketSize = isGiant ? 15f : 10f;
        rt.sizeDelta = new Vector2(rocketSize, rocketSize);
        rt.anchoredPosition = new Vector2(startX, startY);

        Image img = rocketGo.AddComponent<Image>();
        img.sprite = glowSprite;
        // Warm glow color for rocket head
        img.color = new Color(1f, 0.75f, 0.25f, 1f);
        img.raycastTarget = false; // Prevent blocking UI inputs

        float duration = isGiant ? Random.Range(0.8f, 1.2f) : Random.Range(0.6f, 0.9f);
        float elapsed = 0f;
        float trailTimer = 0f;

        // Launch Sound
        AudioManager.Instance?.PlaySFX("sfx_shoot_player");

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            trailTimer += dt;

            float pct = elapsed / duration;
            // Ease out quad vertical curve
            float currY = Mathf.Lerp(startY, targetY, 1f - (1f - pct) * (1f - pct));
            float currX = startX + xSpeed * pct * duration;
            rt.anchoredPosition = new Vector2(currX, currY);

            // Spawn trailing sparks
            if (trailTimer >= 0.04f)
            {
                trailTimer = 0f;
                SpawnTrailParticle(parent, glowSprite, rt.anchoredPosition);
            }

            // Shimmer size
            float scale = 1.0f + 0.3f * Mathf.PingPong(Time.time * 25f, 1f);
            rt.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        Vector2 explosionPos = rt.anchoredPosition;
        Destroy(rocketGo);

        // Detonate!
        SpawnExplosion(parent, glowSprite, explosionPos, isGiant);
    }

    private void SpawnTrailParticle(Transform parent, Sprite glowSprite, Vector2 position)
    {
        GameObject trailGo = new GameObject("FireworkTrail");
        RectTransform rt = trailGo.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;

        rt.sizeDelta = new Vector2(6f, 6f);
        rt.anchoredPosition = position;

        Image img = trailGo.AddComponent<Image>();
        img.sprite = glowSprite;
        img.color = new Color(1f, 0.5f + Random.value * 0.4f, 0.1f, 0.85f);
        img.raycastTarget = false; // Prevent blocking UI inputs

        StartCoroutine(AnimateTrail(trailGo, rt, img));
    }

    private IEnumerator AnimateTrail(GameObject go, RectTransform rt, Image img)
    {
        float duration = 0.35f;
        float elapsed = 0f;
        if (rt == null) yield break;
        Vector2 startPos = rt.anchoredPosition;

        while (elapsed < duration)
        {
            if (go == null || rt == null || img == null) yield break;
            elapsed += Time.deltaTime;
            float pct = elapsed / duration;
            // Gravity downward pull
            rt.anchoredPosition = startPos + new Vector2(0f, -20f * pct);
            img.color = new Color(img.color.r, img.color.g, img.color.b, (1f - pct) * 0.85f);
            float scale = 1f - pct;
            rt.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    private void SpawnFlash(Transform parent, Sprite glowSprite, Vector2 position, Color color)
    {
        GameObject flashGo = new GameObject("FireworkFlash");
        RectTransform rt = flashGo.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;

        rt.sizeDelta = new Vector2(25f, 25f);
        rt.anchoredPosition = position;

        Image img = flashGo.AddComponent<Image>();
        img.sprite = glowSprite;
        img.color = new Color(color.r, color.g, color.b, 0.6f);
        img.raycastTarget = false; // Prevent blocking UI inputs

        StartCoroutine(AnimateFlash(flashGo, rt, img));
    }

    private IEnumerator AnimateFlash(GameObject go, RectTransform rt, Image img)
    {
        float duration = 0.18f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (go == null || rt == null || img == null) yield break;
            elapsed += Time.deltaTime;
            float pct = elapsed / duration;
            float s = 1f + pct * 7f; // Expand rapidly
            rt.localScale = new Vector3(s, s, 1f);
            img.color = new Color(img.color.r, img.color.g, img.color.b, (1f - pct) * 0.6f);
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    private void SpawnExplosion(Transform parent, Sprite glowSprite, Vector2 position, bool isGiant)
    {
        // 1. Alternate audio detonation SFX
        string sfxKey = Random.value > 0.4f ? "sfx_explosion_large" : "sfx_explosion_small";
        AudioManager.Instance?.PlaySFX(sfxKey);

        // 2. Camera shake
        float shakeAmt = isGiant ? 0.14f : 0.07f;
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.2f, shakeAmt);
        }

        // 3. Selection of neon colors
        float hue = Random.value;
        Color baseColor = Color.HSVToRGB(hue, 0.85f, 1f);
        Color accentColor = Color.HSVToRGB((hue + 0.28f) % 1f, 0.95f, 1f);

        // Initial flash
        SpawnFlash(parent, glowSprite, position, baseColor);

        int style = Random.Range(0, 4);
        int particleCount = isGiant ? Random.Range(65, 95) : Random.Range(25, 45);

        if (style == 0) // Concentric Rings
        {
            int halfCount = particleCount / 2;
            // Outer Ring
            for (int i = 0; i < halfCount; i++)
            {
                float angle = (i * 2f * Mathf.PI) / halfCount;
                float speed = isGiant ? Random.Range(320f, 420f) : Random.Range(200f, 290f);
                Vector2 velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);
                CreateSpark(parent, glowSprite, position, baseColor, velocity, 1.3f, 1.8f);
            }
            // Inner Ring
            for (int i = 0; i < halfCount; i++)
            {
                float angle = (i * 2f * Mathf.PI) / halfCount + (Mathf.PI / halfCount);
                float speed = isGiant ? Random.Range(180f, 250f) : Random.Range(110f, 180f);
                Vector2 velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);
                CreateSpark(parent, glowSprite, position, accentColor, velocity, 0.9f, 1.4f);
            }
        }
        else if (style == 1) // Glitter Willow
        {
            for (int i = 0; i < particleCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(70f, isGiant ? 340f : 230f);
                // upward drift bias
                Vector2 velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed + Random.Range(40f, 140f));
                CreateSpark(parent, glowSprite, position, baseColor, velocity, 1.6f, 2.3f, true, true);
            }
        }
        else if (style == 2) // Cardinal Starburst
        {
            int directions = 8;
            int perDir = particleCount / directions;
            if (perDir < 2) perDir = 2;
            for (int d = 0; d < directions; d++)
            {
                float angle = d * (Mathf.PI / 4f);
                for (int i = 0; i < perDir; i++)
                {
                    float speed = Random.Range(90f, isGiant ? 440f : 310f);
                    Vector2 velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);
                    CreateSpark(parent, glowSprite, position, Random.value > 0.4f ? baseColor : Color.white, velocity, 0.8f, 1.5f);
                }
            }
        }
        else // Twinkling Sparklers
        {
            for (int i = 0; i < particleCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(90f, isGiant ? 360f : 240f);
                Vector2 velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);
                Color color = Random.value > 0.5f ? baseColor : accentColor;
                CreateSpark(parent, glowSprite, position, color, velocity, 1.0f, 1.7f, false, false, true);
            }
        }
    }

    private void CreateSpark(Transform parent, Sprite glowSprite, Vector2 position, Color color, Vector2 velocity, float minLife, float maxLife, bool highGravity = false, bool isWillow = false, bool twinkle = false)
    {
        GameObject sparkGo = new GameObject("FireworkSpark");
        RectTransform sparkRt = sparkGo.AddComponent<RectTransform>();
        sparkRt.SetParent(parent, false);
        sparkRt.localScale = Vector3.one;

        float sparkSize = Random.Range(8f, 20f);
        sparkRt.sizeDelta = new Vector2(sparkSize, sparkSize);
        sparkRt.anchoredPosition = position;

        Image sparkImg = sparkGo.AddComponent<Image>();
        sparkImg.sprite = glowSprite;
        sparkImg.color = color;
        sparkImg.raycastTarget = false; // Prevent blocking UI inputs

        StartCoroutine(AnimateSpark(sparkGo, sparkRt, sparkImg, velocity, Random.Range(minLife, maxLife), highGravity, isWillow, twinkle));
    }

    private IEnumerator AnimateSpark(GameObject go, RectTransform rt, Image img, Vector2 velocity, float duration, bool highGravity, bool isWillow, bool twinkle)
    {
        float elapsed = 0f;
        if (rt == null || img == null) yield break;
        Vector2 pos = rt.anchoredPosition;

        float gravityY = highGravity ? -150f : -60f;
        if (isWillow) gravityY = -110f;
        Vector2 gravity = new Vector2(0f, gravityY);
        Color startColor = img.color;

        while (elapsed < duration)
        {
            if (go == null || rt == null || img == null) yield break;
            float dt = Time.deltaTime;
            elapsed += dt;
            float pct = elapsed / duration;

            float drag = isWillow ? 1.0f : 1.7f;
            velocity = Vector2.Lerp(velocity, Vector2.zero, drag * dt);
            velocity += gravity * dt;

            pos += velocity * dt;
            rt.anchoredPosition = pos;

            float alpha = 1f - pct;
            if (twinkle)
            {
                alpha *= (0.4f + 0.6f * Mathf.PingPong(elapsed * 25f, 1f));
            }
            img.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            float s = 1f - pct;
            rt.localScale = new Vector3(s, s, 1f);

            yield return null;
        }

        if (go != null) Destroy(go);
    }

    public void OnMenuClick()
    {
        if (_fireworksRoutine != null)
        {
            StopCoroutine(_fireworksRoutine);
            _fireworksRoutine = null;
        }
        if (_pulseTitleRoutine != null)
        {
            StopCoroutine(_pulseTitleRoutine);
            _pulseTitleRoutine = null;
        }

        // Tắt toàn bộ âm thanh pháo hoa đang phát ngay lập tức
        AudioManager.Instance?.StopAllLevelSounds();

        // Phát End Credit (~73 giây) trước khi về MainMenu
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.PlayEndCredits();
        else
            SceneManager.LoadScene("MainMenu");
    }

    public void OnPlayAgainClick()
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
}
