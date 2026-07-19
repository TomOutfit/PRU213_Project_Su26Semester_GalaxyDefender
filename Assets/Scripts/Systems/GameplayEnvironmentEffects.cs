using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayEnvironmentEffects : MonoBehaviour
{
    private Camera cam;
    private Sprite glowSprite;
    private List<GameObject> activeParticles = new List<GameObject>();
    private int maxDustParticles = 40;
    
    private void Start()
    {
        cam = Camera.main;
        glowSprite = CreateGlowSprite();

        // Prefill background with some dust particles at random heights so it starts populated
        for (int i = 0; i < maxDustParticles; i++)
        {
            SpawnDustParticle(true);
        }

        // Start weather loops
        StartCoroutine(DustSpawnRoutine());
        StartCoroutine(ShootingStarRoutine());
    }

    private Sprite CreateGlowSprite()
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, 2.5f); // Soft edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private IEnumerator DustSpawnRoutine()
    {
        while (true)
        {
            // Regularly spawn new dust particles at the top of the screen
            yield return new WaitForSeconds(Random.Range(0.15f, 0.35f));
            if (activeParticles.Count < maxDustParticles + 20)
            {
                SpawnDustParticle(false);
            }
        }
    }

    private void SpawnDustParticle(bool randomY)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float zDist = Mathf.Abs(cam.transform.position.z) - 1.5f; // Place slightly in front of background plane
        float pctX = Random.Range(-0.1f, 1.1f);
        float pctY = randomY ? Random.Range(0f, 1.1f) : 1.1f; // Top of the screen, or random if prefilling

        Vector3 spawnWorldPos = cam.ViewportToWorldPoint(new Vector3(pctX, pctY, zDist));
        spawnWorldPos.z = 5f; // Sorting layer depth fallback

        GameObject dustGo = new GameObject("SpaceDust");
        dustGo.transform.position = spawnWorldPos;
        dustGo.transform.SetParent(transform);

        SpriteRenderer sr = dustGo.AddComponent<SpriteRenderer>();
        sr.sprite = glowSprite;
        sr.sortingOrder = -2; // Render behind gameplay objects

        // Give them gorgeous soft nebular shades (cyan, teal, purple, or white)
        float randColor = Random.value;
        Color c;
        if (randColor < 0.3f) c = new Color(0.2f, 0.8f, 1f, Random.Range(0.25f, 0.5f)); // Light Blue
        else if (randColor < 0.6f) c = new Color(0.6f, 0.2f, 1f, Random.Range(0.2f, 0.45f)); // Lavender purple
        else c = new Color(1f, 1f, 1f, Random.Range(0.3f, 0.6f)); // Soft starlight white

        sr.color = c;

        float scale = Random.Range(0.08f, 0.28f);
        dustGo.transform.localScale = new Vector3(scale, scale, 1f);

        activeParticles.Add(dustGo);

        float fallSpeed = Random.Range(0.4f, 1.2f);
        float driftFrequency = Random.Range(0.5f, 1.5f);
        float driftAmplitude = Random.Range(0.1f, 0.3f);
        float randomPhase = Random.Range(0f, Mathf.PI * 2f);

        StartCoroutine(AnimateDust(dustGo, fallSpeed, driftFrequency, driftAmplitude, randomPhase));
    }

    private IEnumerator AnimateDust(GameObject go, float fallSpeed, float freq, float amp, float phase)
    {
        while (go != null)
        {
            // Move downwards
            Vector3 pos = go.transform.position;
            pos.y -= fallSpeed * Time.deltaTime;
            
            // Add a gentle horizontal wind/drift oscillation
            pos.x += Mathf.Sin(Time.time * freq + phase) * amp * Time.deltaTime;

            go.transform.position = pos;

            // Check if it has drifted off-screen
            Vector3 viewportPos = cam.WorldToViewportPoint(pos);
            if (viewportPos.y < -0.1f)
            {
                break;
            }

            yield return null;
        }

        if (go != null)
        {
            activeParticles.Remove(go);
            Destroy(go);
        }
    }

    private IEnumerator ShootingStarRoutine()
    {
        while (true)
        {
            // Spawn a shooting star every 4 to 8 seconds
            yield return new WaitForSeconds(Random.Range(4f, 8f));
            SpawnShootingStar();
        }
    }

    private void SpawnShootingStar()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float zDist = Mathf.Abs(cam.transform.position.z) - 1.5f;
        // Spawn from upper-left or upper-right
        bool fromLeft = Random.value > 0.5f;
        float startPctX = fromLeft ? Random.Range(-0.2f, 0.3f) : Random.Range(0.7f, 1.2f);
        float startPctY = Random.Range(0.7f, 1.1f);

        Vector3 spawnWorldPos = cam.ViewportToWorldPoint(new Vector3(startPctX, startPctY, zDist));
        spawnWorldPos.z = 4.5f;

        GameObject starGo = new GameObject("ShootingStar");
        starGo.transform.position = spawnWorldPos;
        starGo.transform.SetParent(transform);

        // Add a trail-like effect using a LineRenderer
        LineRenderer line = starGo.AddComponent<LineRenderer>();
        line.startWidth = 0.08f;
        line.endWidth = 0.0f;
        line.positionCount = 2;
        line.sortingOrder = -1;

        // Neon blue-cyan streak
        Color neonColor = new Color(0f, 0.85f, 1f, 0.9f);
        line.startColor = neonColor;
        line.endColor = new Color(0f, 0.3f, 0.8f, 0f);

        // Use Sprites/Default material
        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null) line.material = new Material(spriteShader);

        Vector3 dir = fromLeft ? new Vector3(1.2f, -1f, 0f).normalized : new Vector3(-1.2f, -1f, 0f).normalized;
        float speed = Random.Range(8f, 15f);

        StartCoroutine(AnimateShootingStar(starGo, line, dir, speed));
    }

    private IEnumerator AnimateShootingStar(GameObject go, LineRenderer line, Vector3 dir, float speed)
    {
        float duration = 0.8f;
        float elapsed = 0f;

        Vector3 currentPos = go.transform.position;
        float trailLength = 1.2f;

        while (elapsed < duration && go != null)
        {
            elapsed += Time.deltaTime;
            currentPos += dir * speed * Time.deltaTime;
            go.transform.position = currentPos;

            // Set line positions to show a beautiful streak
            line.SetPosition(0, currentPos);
            line.SetPosition(1, currentPos - dir * trailLength);

            yield return null;
        }

        if (go != null) Destroy(go);
    }
}
