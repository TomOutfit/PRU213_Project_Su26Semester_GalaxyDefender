using UnityEngine;

public class VerticalScrollBridge : MonoBehaviour
{
    [Header("Cấu hình tốc độ")]
    [SerializeField] private float scrollSpeed = 0.3f;
    [SerializeField] private bool moveUp = false; // Tích chọn nếu muốn cuộn lên, bỏ tích để cuộn xuống

    private float textureUnitSizeY;
    private Vector3 startPosition;

    void Start()
    {
        // Lưu lại vị trí xuất phát ban đầu (X=0, Y=1, Z=0)
        startPosition = transform.position;

        // Lấy thông số Sprite để tính toán chiều cao chuẩn của 1 bức ảnh gốc
        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        if (sprite != null)
        {
            textureUnitSizeY = sprite.texture.height / sprite.pixelsPerUnit;
        }
        else
        {
            Debug.LogError("Chưa gán Sprite vào Sprite Renderer rồi bạn ơi!");
        }
    }

    void Update()
    {
        // Xác định hướng di chuyển: 1 là lên, -1 là xuống
        float direction = moveUp ? 1f : -1f;

        // Di chuyển dải ảnh liên tục theo thời gian
        transform.Translate(Vector3.up * direction * scrollSpeed * Time.deltaTime);

        // Tính khoảng cách hiện tại đã đi được bao xa so với điểm xuất phát ban đầu
        float distanceMoved = Mathf.Abs(transform.position.y - startPosition.y);

        // Khi đi quá hoặc bằng đúng chiều cao của 1 bức ảnh đơn lẻ
        if (distanceMoved >= textureUnitSizeY)
        {
            // Tính toán phần vị trí bị lọt lưới (sai số nhỏ do FPS trồi sụt)
            float overShoot = distanceMoved - textureUnitSizeY;

            // Đập ngược vị trí Object về điểm xuất phát + bù sai số để ảnh nối đuôi mượt mà
            float newY = moveUp ? startPosition.y + overShoot : startPosition.y - overShoot;

            transform.position = new Vector3(startPosition.x, newY, transform.position.z);
        }
    }
}