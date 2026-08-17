using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("1. Cấu hình Tốc độ")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float chaseSpeed = 6.0f;

    [Header("2. Lãnh thổ đi tuần (Waypoints)")]
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;

    [Header("3. Cấu hình Đuổi theo & Tầm phát hiện")]
    public Transform playerTransform;
    [SerializeField] private float patrolDetectionRadius = 6f;
    [SerializeField] private float chaseDetectionRadius = 12f;

    private NavMeshAgent agent;
    private SphereCollider detectionTrigger;
    private bool isChasing = false;
    private bool isSearching = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        detectionTrigger = GetComponent<SphereCollider>();

        // Tự động tìm Player bằng Tag nếu chưa gán
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        SetPatrolState();

        // Nếu waypoints đã được truyền vào trước khi Start() chạy thì cho di chuyển ngay
        if (waypoints != null && waypoints.Length > 0)
        {
            GoToNearestWaypoint();
        }
    }

    // Hàm nhận Waypoint từ DayNightClock truyền sang
    public void SetupWaypoints(Transform[] areaWaypoints)
    {
        waypoints = areaWaypoints;

        // Nếu NavMeshAgent đã khởi tạo thì cho đi tuần luôn
        if (agent != null && waypoints != null && waypoints.Length > 0)
        {
            GoToNearestWaypoint();
        }
    }

    void Update()
    {
        if (isChasing)
        {
            if (playerTransform != null)
            {
                agent.SetDestination(playerTransform.position);
            }
        }
        else if (!isSearching)
        {
            if (waypoints != null && waypoints.Length > 0)
            {
                if (!agent.pathPending && agent.remainingDistance < 0.8f)
                {
                    GoToNextWaypoint();
                }
            }
        }
    }

    void GoToNearestWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        int nearestIndex = 0;
        float minDistance = Mathf.Infinity;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            float distance = Vector3.Distance(transform.position, waypoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }

        currentWaypointIndex = nearestIndex;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    private void SetPatrolState()
    {
        isChasing = false;
        if (agent != null) agent.speed = patrolSpeed;
        if (detectionTrigger != null) detectionTrigger.radius = patrolDetectionRadius;
    }

    private void SetChaseState()
    {
        isChasing = true;
        isSearching = false;
        StopAllCoroutines();
        if (agent != null) agent.speed = chaseSpeed;
        if (detectionTrigger != null) detectionTrigger.radius = chaseDetectionRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SetChaseState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(LostPlayerRoutine());
        }
    }

    IEnumerator LostPlayerRoutine()
    {
        isChasing = false;
        isSearching = true;

        if (agent != null) agent.isStopped = true;
        yield return new WaitForSeconds(3.0f);

        if (agent != null) agent.isStopped = false;
        SetPatrolState();
        GoToNearestWaypoint();
        isSearching = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isChasing ? Color.red : Color.yellow;
        float currentRadius = isChasing ? chaseDetectionRadius : patrolDetectionRadius;
        Gizmos.DrawWireSphere(transform.position, currentRadius);
    }
}