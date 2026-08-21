using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    // Biến lưu trữ vị trí bắt đầu
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    void Start()
    {
        currentHealth = maxHealth;

        // Lưu lại vị trí và hướng ban đầu của Player khi game bắt đầu
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("Player nhận " + damage + " sát thương! Máu còn: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player đã chết! Đang hồi sinh về điểm bắt đầu...");
        Respawn();
    }

    private void Respawn()
    {
        // Hồi đầy lại máu
        currentHealth = maxHealth;

        // Nếu Player dùng CharacterController, cần tắt đi trước khi đổi vị trí để tránh kẹt lỗi va chạm
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        // Đưa Player về vị trí xuất phát ban đầu
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        // Bật lại CharacterController sau khi đã dịch chuyển xong
        if (controller != null) controller.enabled = true;
    }
}