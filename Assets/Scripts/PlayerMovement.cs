using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("1. Tốc độ di chuyển")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;

    [Header("2. Lực nhảy & Trọng lực rơi")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float fallMultiplier = 2.5f; // Hệ số nhân giúp rơi nhanh hơn, không bị bồng bềnh

    [Header("3. Cấu hình Camera & Chuột")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float mouseSensitivity = 200f;

    private Rigidbody rb;
    private bool isGrounded;
    private float xRotation = 0f; // Góc ngẩng/cúi mặt

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        // Tự động tìm Main Camera trong con của Player nếu chưa gán ở Inspector
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        // Khóa con trỏ chuột vào giữa màn hình và ẩn chuột đi khi chơi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- A. XOAY CAMERA THEO CHUỘT ---
        if (playerCamera != null)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            // Xoay Camera theo trục X (Ngẩng lên / Cúi xuống)
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Giới hạn không cho xoay quá đà lộn ngược đầu
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Xoay toàn bộ thân Player theo trục Y (Quay Trái / Quay Phải)
            transform.Rotate(Vector3.up * mouseX);
        }

        // --- B. DI CHUYỂN THEO HƯỚNG MẮT NHÌN ---
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        // Tính hướng đi theo HƯỚNG QUAY HIỆN TẠI của Player (TransformDirection)
        Vector3 moveDirection = (transform.right * moveX + transform.forward * moveZ).normalized;

        rb.velocity = new Vector3(moveDirection.x * currentSpeed, rb.velocity.y, moveDirection.z * currentSpeed);

        // --- C. XỬ LÝ NHẢY ---
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
            isGrounded = false;
        }

        // --- D. XỬ LÝ RƠI NHANH (TRIỆT TIÊU HIỆU ỨNG TRỌNG LỰC MẶT TRĂNG) ---
        if (rb.velocity.y < 0)
        {
            // Tăng trọng lực kéo xuống khi nhân vật đang trên không và rơi xuống
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
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