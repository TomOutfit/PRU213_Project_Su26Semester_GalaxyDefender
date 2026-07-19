using UnityEngine;

public class VerticalParallaxManager : MonoBehaviour
{
    [System.Serializable]
    public struct ParallaxLayer
    {
        public Transform layerTransform; // Object của layer này
        public float speedMultiplier;    // Hệ số tốc độ (Ví dụ: 1 = nhanh, 0.2 = rất chậm ở xa)
        [HideInInspector] public float imageHeight; // Chiều cao tự động tính
        [HideInInspector] public SpriteRenderer spriteRenderer; // Cached SpriteRenderer
    }

    [Header("Cấu hình chung")]
    [SerializeField] private float baseMoveSpeed = 2f; // Tốc độ gốc (nền trôi xuống dưới)

    [Header("Danh sách các Layers")]
    [SerializeField] private ParallaxLayer[] parallaxLayers;

    void Start()
    {
        // Tự động tính chiều cao cho từng Layer dựa vào SpriteRenderer của nó
        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            if (parallaxLayers[i].layerTransform != null)
            {
                SpriteRenderer spriteRenderer = parallaxLayers[i].layerTransform.GetComponent<SpriteRenderer>();
                parallaxLayers[i].spriteRenderer = spriteRenderer;
                if (spriteRenderer != null)
                {
                    parallaxLayers[i].imageHeight = (spriteRenderer.sprite.texture.height / spriteRenderer.sprite.pixelsPerUnit) * spriteRenderer.transform.localScale.y;
                }
                else
                {
                    Debug.LogError($"Layer {parallaxLayers[i].layerTransform.name} thiếu SpriteRenderer!");
                }
            }
        }
    }

    void Update()
    {
        // Lặp qua từng layer để di chuyển và reset
        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            ParallaxLayer layer = parallaxLayers[i];
            if (layer.layerTransform == null) continue;

            // 1. Tính toán vận tốc trôi xuống cho từng layer dựa vào hệ số speedMultiplier
            // Dùng dấu trừ (-) để ép nền luôn trôi xuống dưới
            float currentSpeed = -baseMoveSpeed * layer.speedMultiplier;
            float moveY = currentSpeed * Time.deltaTime;
            
            layer.layerTransform.position += new Vector3(0, moveY, 0);

            // 2. Kiểm tra nếu layer trôi xuống quá chiều cao của nó -> Reset lên trên
            if (layer.layerTransform.position.y <= -layer.imageHeight)
            {
                // Bù trừ sai số khung hình để không bị hở
                layer.layerTransform.position += new Vector3(0, layer.imageHeight, 0);
            }

            // 3. Hiệu ứng lung linh ánh sáng vũ trụ (Color Shimmer)
            if (layer.spriteRenderer != null)
            {
                float cycleSpeed = 0.25f + i * 0.08f;
                float sinVal = Mathf.Sin(Time.time * cycleSpeed);
                float cosVal = Mathf.Cos(Time.time * (cycleSpeed * 0.7f) + i);

                // Dịch chuyển nhẹ màu sắc qua lại giữa các tông xanh neon, tím violet cực đẹp
                float r = Mathf.Lerp(0.75f, 1.0f, (sinVal + 1f) / 2f);
                float g = Mathf.Lerp(0.70f, 0.95f, (cosVal + 1f) / 2f);
                float b = Mathf.Lerp(0.85f, 1.0f, (sinVal + 1f) / 2f);

                layer.spriteRenderer.color = new Color(r, g, b, layer.spriteRenderer.color.a);
            }
        }
    }
}