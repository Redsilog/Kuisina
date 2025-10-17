using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCMovement : MonoBehaviour
{
    [Header("Movement")]
    public Transform[] waypoints;          // Filled by spawner via SetWaypoints()
    public float interactionDistance = 1.5f;

    [Header("Waiting")]
    [Tooltip("Waypoint index where the NPC should stop and wait.")]
    public int initialWaitIndex = 0;

    [Tooltip("If true, the NPC will walk through waypoints in order until it reaches 'initialWaitIndex'. If false, it will go straight to 'initialWaitIndex'.")]
    public bool approachWaitIndexSequentially = false;

    [Header("State")]
    public NPCState currentState = NPCState.Resting;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private NPCInteractable npcInteractable;
    private bool waitingStarted = false;

    // Private variables for animation
    private Animator animator;
    private string moveBoolName = "IsMoving";
    private string sittingBoolName = "IsSitting";  // Resting animation
    private float movingSpeedThreshold = 0.05f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        npcInteractable = GetComponent<NPCInteractable>();

        // Automatically reference the Animator component (if it exists)
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Default speed for NavMeshAgent is controlled by agent
    }

    void Start()
    {
        // If waypoints were already assigned in prefab (rare),
        // start heading there. Usually the spawner calls SetWaypoints() after instantiate.
        if (waypoints != null && waypoints.Length > 0)
        {
            agent.isStopped = false;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case NPCState.Resting:
                MoveToCurrentWaypoint();
                break;

            case NPCState.Serving:
            case NPCState.Thanking:
                agent.isStopped = true;
                break;

            case NPCState.Leaving:
                agent.isStopped = false;
                break;
        }

        SmoothRotate();
        UpdateMoveAnimation();
    }

    void MoveToCurrentWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        agent.isStopped = false;
        agent.SetDestination(waypoints[currentWaypointIndex].position);

        // Debugging line
        Debug.Log($"NPC moving to waypoint {currentWaypointIndex}");

        // Reached the waypoint
        if (!agent.pathPending && agent.remainingDistance <= interactionDistance)
        {
            // Debugging line
            Debug.Log($"NPC reached waypoint {currentWaypointIndex}");

            // Haven't started waiting yet?
            if (!waitingStarted)
            {
                // Are we at the designated wait waypoint?
                if (currentWaypointIndex == initialWaitIndex)
                {
                    waitingStarted = true;
                    currentState = NPCState.Serving;
                    if (npcInteractable != null) npcInteractable.StartWaitingForPlayer();
                    SetSittingAnimation(true);  // Start "IsSitting" when NPC is resting
                    Debug.Log("NPC reached the waiting spot and is resting");
                }
                else
                {
                    // Not at wait index yet → keep walking to next waypoint
                    currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                    agent.SetDestination(waypoints[currentWaypointIndex].position);
                }
            }
        }
    }



    public void StartLeaving()
    {
        currentState = NPCState.Leaving;
        MoveToNextWaypoint();
    }

    public void MoveToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        agent.isStopped = false;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    // Inject route + choose which index to wait at first (e.g., 0 = Path 2 / chair).
    public void SetWaypoints(Transform[] points, int initialIndex = 0)
    {
        if (points == null || points.Length == 0) return;

        waypoints = points;
        initialWaitIndex = Mathf.Clamp(initialIndex, 0, waypoints.Length - 1);

        // Start either at the beginning (sequential approach) or directly at the wait index
        currentWaypointIndex = approachWaitIndexSequentially ? 0 : initialWaitIndex;

        waitingStarted = false;
        currentState = NPCState.Resting;
        agent.isStopped = false;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    // --- Animation helper ---
    void UpdateMoveAnimation()
    {
        if (animator == null) return;

        bool isMoving = !agent.isStopped && agent.velocity.sqrMagnitude > (movingSpeedThreshold * movingSpeedThreshold);

        // Force "not moving" while Serving/Thanking
        if (currentState == NPCState.Serving || currentState == NPCState.Thanking)
            isMoving = false;

        animator.SetBool(moveBoolName, isMoving);
    }

    // ---- Set Sitting Animation (Resting State) ----
    void SetSittingAnimation(bool isSitting)
    {
        if (animator != null)
        {
            animator.SetBool(sittingBoolName, isSitting);
        }
    }

    // ---- smoothing helpers ----
    void SmoothRotate()
    {
        Vector3 v = agent.velocity;
        v.y = 0f; // ignore Y axis
        if (v.sqrMagnitude > 0.0004f) // a little motion
        {
            Quaternion target = Quaternion.LookRotation(v);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 10f * Time.deltaTime);
        }
    }
}
