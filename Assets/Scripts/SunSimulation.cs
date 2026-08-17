using UnityEngine;

public class SunSimulation : MonoBehaviour
{
    [Header("Sun Settings")]
    private Light sunLight;
    public float targetIntensity = 0f;
    public float fadeSpeed = 0.1f;

    [Header("Rotation Settings (Sunset)")]
    public float rotationSpeed = 5f;

    void Start()
    {
        sunLight = GetComponent<Light>();
        if (sunLight == null)
        {
            Debug.LogWarning("Vui lòng gắn Script này vào GameObject có thành phần Light!");
        }
    }

    void Update()
    {
        if (sunLight == null) return;

        // Xoay mặt trời
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);

        // Giảm độ sáng từ từ
        sunLight.intensity = Mathf.MoveTowards(sunLight.intensity, targetIntensity, fadeSpeed * Time.deltaTime);

        // Kiểm tra điều kiện mặt trời lặn hẳn (Dùng Vector3.Dot để chính xác hơn eulerAngles)
        bool isUnderground = Vector3.Dot(transform.forward, Vector3.down) <= 0;

        if (sunLight.intensity <= 0.01f || isUnderground)
        {
            sunLight.intensity = 0f;
            sunLight.shadows = LightShadows.None;
        }
    }
}