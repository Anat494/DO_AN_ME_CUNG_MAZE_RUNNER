using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public struct EnemySpawnArea
{
    public string areaName;           // Tên khu vực
    public Transform spawnPoint;    // Điểm quái xuất hiện
    public Transform[] waypoints;   // Danh sách Waypoint đi tuần riêng của khu vực này
}

public class DayNightClock : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("Thời gian xuất phát (0 - 24h)")]
    [Range(0f, 24f)]
    public float currentHour = 8f;
    private float timeMultiplier = 24f / (15f * 60f);

    [Header("Sun Settings")]
    public Light sunLight;
    public float maxSunIntensity = 1.2f;

    [Header("UI Settings")]
    public TextMeshProUGUI clockText;

    [Header("Monster Night Settings (Cấu hình Ban Đêm & Quái)")]
    public bool isNight = false;
    private bool wasNight = false;
    [Range(0f, 24f)] public float nightStartHour = 18f;
    [Range(0f, 24f)] public float nightEndHour = 6f;

    [Space(10)]
    public GameObject enemyPrefab;
    public EnemySpawnArea[] spawnAreas; // Danh sách các khu vực spawn

    public float spawnInterval = 10f;
    private float spawnTimer = 0f;

    // Danh sách lưu vết các con quái đã spawn
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    // Dictionary để theo dõi xem khu vực (index) nào đang có quái sinh sống
    private Dictionary<int, GameObject> activeEnemiesPerArea = new Dictionary<int, GameObject>();

    void Update()
    {
        currentHour += Time.deltaTime * timeMultiplier;
        if (currentHour >= 24f) currentHour -= 24f;

        isNight = (currentHour >= nightStartHour || currentHour < nightEndHour);

        if (!isNight && wasNight)
        {
            ClearAllNightEnemies();
        }
        wasNight = isNight;

        HandleMonsterSpawning();
        UpdateSunPosition();
        UpdateClockUI();
    }

    void HandleMonsterSpawning()
    {
        if (isNight)
        {
            // Kiểm tra xem còn khu vực nào trống không trước khi tiếp tục tích lũy thời gian spawn
            if (HasAvailableSpawnArea())
            {
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= spawnInterval)
                {
                    SpawnEnemy();
                    spawnTimer = 0f;
                }
            }
            else
            {
                // Nếu tất cả khu vực đã đầy, reset timer về 0 để dừng hoàn toàn việc cố gắng spawn liên tục
                spawnTimer = 0f;
            }
        }
        else
        {
            spawnTimer = 0f;
        }
    }

    bool HasAvailableSpawnArea()
    {
        if (spawnAreas == null || spawnAreas.Length == 0) return false;

        for (int i = 0; i < spawnAreas.Length; i++)
        {
            if (!activeEnemiesPerArea.ContainsKey(i) || activeEnemiesPerArea[i] == null)
            {
                return true; // Vẫn còn ít nhất một khu vực trống
            }
        }
        return false; // Tất cả khu vực đã có quái
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null || spawnAreas == null || spawnAreas.Length == 0) return;

        // 1. Tìm tất cả các index khu vực hiện đang TRỐNG (chưa có quái hoặc quái cũ đã bị tiêu diệt)
        List<int> availableAreaIndices = new List<int>();

        for (int i = 0; i < spawnAreas.Length; i++)
        {
            // Kiểm tra xem khu vực này đã có quái chưa
            if (!activeEnemiesPerArea.ContainsKey(i) || activeEnemiesPerArea[i] == null)
            {
                availableAreaIndices.Add(i);
            }
        }

        // 2. Nếu tất cả các điểm Spawn đều đã có quái -> Không spawn nữa!
        if (availableAreaIndices.Count == 0)
        {
            return;
        }

        // 3. Chọn ngẫu nhiên 1 khu vực trong danh sách các khu vực TRỐNG
        int selectedIndex = availableAreaIndices[Random.Range(0, availableAreaIndices.Count)];
        EnemySpawnArea selectedArea = spawnAreas[selectedIndex];

        if (selectedArea.spawnPoint == null) return;

        // 4. Sinh quái tại điểm Spawn được chọn
        GameObject newEnemy = Instantiate(enemyPrefab, selectedArea.spawnPoint.position, selectedArea.spawnPoint.rotation);

        // 5. Truyền Waypoints cho quái
        EnemyAI enemyAI = newEnemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.SetupWaypoints(selectedArea.waypoints);
        }

        // 6. Lưu vết con quái vào hệ thống quản lý
        spawnedEnemies.Add(newEnemy);
        activeEnemiesPerArea[selectedIndex] = newEnemy; // Đánh dấu khu vực này ĐÃ CÓ QUÁI

        Debug.Log($"Đã sinh 1 con quái tại khu vực trống: {selectedArea.areaName}");
    }

    void ClearAllNightEnemies()
    {
        Debug.Log("Trời đã sáng! Toàn bộ quái vật ban đêm biến mất!");

        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }

        spawnedEnemies.Clear();
        activeEnemiesPerArea.Clear(); // Reset lại toàn bộ khu vực thành TRỐNG
    }

    void UpdateSunPosition()
    {
        if (sunLight == null) return;

        float sunAngle = (currentHour - 6f) / 24f * 360f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 50f, 0f);

        float dotProduct = Vector3.Dot(sunLight.transform.forward, Vector3.down);

        if (dotProduct > -0.1f)
        {
            float intensityFactor = Mathf.Clamp01(dotProduct + 0.15f);
            sunLight.intensity = Mathf.Lerp(0.1f, maxSunIntensity, intensityFactor);
            sunLight.shadows = LightShadows.Soft;
        }
        else
        {
            sunLight.intensity = 0f;
            sunLight.shadows = LightShadows.None;
        }
    }

    void UpdateClockUI()
    {
        if (clockText == null) return;

        int hours = Mathf.FloorToInt(currentHour);
        int minutes = Mathf.FloorToInt((currentHour - hours) * 60f);
        clockText.text = string.Format("{0:00}:{1:00}", hours, minutes);
    }
}