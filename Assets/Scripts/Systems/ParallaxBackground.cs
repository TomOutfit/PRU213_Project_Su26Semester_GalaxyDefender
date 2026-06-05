using UnityEngine;

public class VerticalParallaxManager : MonoBehaviour
{
    [System.Serializable]
    public struct ParallaxLayer
    {
        public Transform layerTransform; // Object của layer này
        public float speedMultiplier;    // Hệ số tốc độ (Ví dụ: 1 = nhanh, 0.2 = rất chậm ở xa)
        [HideInInspector] public float imageHeight; // Chiều cao tự động tính
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
        }
    }
}