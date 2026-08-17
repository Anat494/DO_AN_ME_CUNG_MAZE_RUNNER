using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Tốc độ di chuyển")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;

    [Header("Lực nhảy")]
    [SerializeField] private float jumpForce = 5f;

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        // Lấy thành phần Rigidbody được gắn trên Player
        rb = GetComponent<Rigidbody>();

        // Đóng băng trục xoay vật lý để Player không bị ngã lăn lộn khi đụng tường
        rb.freezeRotation = true;
    }

    void Update()
    {
        // 1. Nhận đầu vào từ bàn phím (A, W, S, D hoặc các phím mũi tên)
        float moveX = Input.GetAxisRaw("Horizontal"); // A/D hoặc Mũi tên Trái/Phải
        float moveZ = Input.GetAxisRaw("Vertical");   // W/S hoặc Mũi tên Lên/Xuống

        // 2. Kiểm tra xem người chơi có đang giữ phím Shift hay không để chọn tốc độ
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        // 3. Tính toán hướng di chuyển
        Vector3 moveDirection = new Vector3(moveX, 0f, moveZ).normalized;

        // 4. Áp dụng vận tốc di chuyển vào Rigidbody (giữ nguyên vận tốc Y để không ảnh hưởng trọng lực)
        rb.velocity = new Vector3(moveDirection.x * currentSpeed, rb.velocity.y, moveDirection.z * currentSpeed);

        // 5. Nhấn phím Space để nhảy (Chỉ nhảy được khi đang đứng trên sàn)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
            isGrounded = false; // Rời đất rồi thì không được nhảy tiếp cho đến khi chạm sàn
        }
    }

    // Kiểm tra va chạm với Sàn nhà (để biết khi nào được nhảy tiếp)
    private void OnCollisionEnter(Collision collision)
    {
        // Nếu chạm vào bất kỳ vật thể nào (Ví dụ như Ground/Floor) thì cho phép nhảy tiếp
        // Bạn có thể đặt Tag cho sàn là "Ground" nếu muốn kiểm tra chính xác hơn
        if (collision.gameObject.name.Contains("Ground") || collision.gameObject.name.Contains("Floor"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.name.Contains("Ground") || collision.gameObject.name.Contains("Floor"))
        {
            isGrounded = true;
        }
    }
}