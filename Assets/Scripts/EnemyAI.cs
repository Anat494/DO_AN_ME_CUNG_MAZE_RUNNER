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

    [Header("4. Cấu hình Tấn công Nhảy (Jump Attack)")]
    [SerializeField] private float attackRange = 4.0f;      // Tầm nhảy lao tới
    [SerializeField] private float attackDuration = 1.2f;   // Tổng thời gian animation đánh
    [SerializeField] private float jumpImpactDelay = 0.6f;  // Thời điểm giậm xuống Player
    [SerializeField] private float jumpSpeed = 12.0f;        // Tốc độ lao vút tới
    [SerializeField] private float rotationSpeed = 10.0f;   // Tốc độ xoay hướng khi tấn công
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private LayerMask obstacleMask;        // Layer các vật cản (Tường, Đá...)

    private NavMeshAgent agent;
    private SphereCollider detectionTrigger;
    private Animator animator;

    private bool isChasing = false;
    private bool isSearching = false;
    private bool isAttacking = false;

    // Biến lưu Coroutine tìm kiếm để dừng chính xác
    private Coroutine searchCoroutine;

    // Cache Hash ID cho Animator
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    // Cache Wait For Seconds tránh rác GC
    private readonly WaitForSeconds searchWaitTime = new WaitForSeconds(3.0f);

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        detectionTrigger = GetComponent<SphereCollider>();
        animator = GetComponent<Animator>();

        FindPlayerIfNull();
        SetPatrolState();

        // Đã xóa phần tự tìm Waypoint rườm rà. 
        // Waypoint sẽ được truyền vào tự động thông qua hàm SetupWaypoints từ TimeManager.
    }

    public void SetupWaypoints(Transform[] areaWaypoints)
    {
        waypoints = areaWaypoints;

        if (agent != null && waypoints != null && waypoints.Length > 0)
        {
            GoToNearestWaypoint();
        }
    }

    void Update()
    {
        // Cập nhật Animation Speed
        if (animator != null && agent != null)
        {
            float speedValue = (isAttacking || isSearching) ? 0f : agent.velocity.magnitude;
            animator.SetFloat(SpeedHash, speedValue);
        }

        if (isAttacking) return;

        if (playerTransform == null)
        {
            FindPlayerIfNull();
        }

        // RƯỢT ĐUỔI HOẶC TẤN CÔNG
        if (isChasing && playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer <= attackRange)
            {
                isAttacking = true;
                StartCoroutine(PerformAttack());
            }
            else
            {
                agent.SetDestination(playerTransform.position);
            }
        }
        // ĐI TUẦN
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

    IEnumerator PerformAttack()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (playerTransform != null)
        {
            Vector3 targetPosition = playerTransform.position;

            // 1. Kích hoạt Animation Attack
            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }

            // 2. HIỆU ỨNG LAO TỚI & XOAY HƯỚNG
            float elapsedTime = 0f;
            while (elapsedTime < jumpImpactDelay)
            {
                Vector3 currentTarget = playerTransform != null ? playerTransform.position : targetPosition;

                // Xoay mượt về hướng mục tiêu
                Vector3 lookDirection = (currentTarget - transform.position).normalized;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                // Di chuyển vị trí có kiểm tra tường
                Vector3 nextPos = Vector3.MoveTowards(transform.position, currentTarget, jumpSpeed * Time.deltaTime);
                Vector3 moveDir = nextPos - transform.position;
                float moveDist = moveDir.magnitude;

                if (moveDist > 0.001f)
                {
                    if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, moveDir.normalized, moveDist, obstacleMask))
                    {
                        transform.position = nextPos;
                    }
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 3. ĐỒNG BỘ NAVMESH
            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(transform.position);
            }

            // 4. GÂY SÁT THƯƠNG
            if (playerTransform != null)
            {
                PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                }
            }
        }

        // 5. CHỜ THU CHÂN ANIMATION
        float remainingTime = attackDuration - jumpImpactDelay;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        // 6. RESET TRẠNG THÁI
        isAttacking = false;

        if (agent != null)
        {
            agent.isStopped = false;
        }

        if (playerTransform == null || Vector3.Distance(transform.position, playerTransform.position) > chaseDetectionRadius)
        {
            SetPatrolState();
            GoToNearestWaypoint();
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

        int attempts = 0;
        while (attempts < waypoints.Length)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            if (waypoints[currentWaypointIndex] != null)
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.SetDestination(waypoints[currentWaypointIndex].position);
                }
                return;
            }
            attempts++;
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

        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
            searchCoroutine = null;
        }

        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
        }
        if (detectionTrigger != null) detectionTrigger.radius = chaseDetectionRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isAttacking)
        {
            SetChaseState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !isAttacking)
        {
            if (searchCoroutine != null) StopCoroutine(searchCoroutine);
            searchCoroutine = StartCoroutine(LostPlayerRoutine());
        }
    }

    IEnumerator LostPlayerRoutine()
    {
        isChasing = false;
        isSearching = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        yield return searchWaitTime;

        if (agent != null) agent.isStopped = false;
        SetPatrolState();
        GoToNearestWaypoint();
        isSearching = false;
        searchCoroutine = null;
    }

    private void FindPlayerIfNull()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isChasing ? Color.red : Color.yellow;
        float currentRadius = isChasing ? chaseDetectionRadius : patrolDetectionRadius;
        Gizmos.DrawWireSphere(transform.position, currentRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}