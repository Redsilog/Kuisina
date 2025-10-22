using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handles NPC movement along a series of waypoints, stopping at a sit/wait point
/// to interact with the player and resuming after.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NPCInteractable))]
public class NPCMovement : MonoBehaviour
{
    public enum NPCState
    {
        Moving,     // Walking between points
        Sitting,    // Waiting for player interaction
        Ordering,   // Order phase (player interaction in progress)
        Thanking,   // Short dwell after order success
        Leaving     // Leaving after thank or timeout
    }

    [Header("Waypoint Route (Injected by Spawner)")]

    private int sitIndex = 0;

    [Header("Timing")]
    [Tooltip("Time NPC stays in 'Thanking' phase before leaving.")]
    public float thankYouDelay = 5f;

    public NPCState CurrentState { get; private set; } = NPCState.Moving;

    // --- Internal references ---
    private NavMeshAgent agent;
    private NPCInteractable npcInteractable;

    // --- Route control ---
    private Transform[] waypoints; // private because spawner injects it
    private int currentIndex;
    private bool routeReady;
    private float stateTimer;

    // Small constant to help with arrival precision
    private const float arrivalEpsilon = 0.1f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        npcInteractable = GetComponent<NPCInteractable>();
    }

    void Update()
    {
        if (!routeReady || waypoints == null || waypoints.Length == 0)
            return;

        switch (CurrentState)
        {
            case NPCState.Moving:
                HandleMoving();
                break;

            case NPCState.Sitting:
            case NPCState.Ordering:
                agent.isStopped = true;
                break;

            case NPCState.Thanking:
                HandleThanking();
                break;

            case NPCState.Leaving:
                HandleLeaving();
                break;
        }
    }

    // ----------- Public API -----------

    /// <summary>
    /// Called by the spawner right after NPC is created.
    /// Sets up the waypoints and starting movement.
    /// </summary>
    public void InitializeRoute(Transform[] route, int sitWaypoint)
    {
        if (route == null || route.Length == 0)
        {
            Debug.LogWarning($"[NPCMovement] {name} received no waypoints!");
            return;
        }

        waypoints = route;
        sitIndex = Mathf.Clamp(sitWaypoint, 0, waypoints.Length - 1);
        currentIndex = 0; // Always start from the first waypoint

        routeReady = true;
        MoveTo(currentIndex);
        CurrentState = NPCState.Moving;
    }

    /// <summary>
    /// Called by NPCInteractable when player starts an order.
    /// </summary>
    public void BeginOrdering()
    {
        CurrentState = NPCState.Ordering;
        agent.isStopped = true;
        stateTimer = 0f;
    }

    /// <summary>
    /// Called when correct item is delivered.
    /// </summary>
    public void BeginThanking()
    {
        CurrentState = NPCState.Thanking;
        stateTimer = 0f;
        agent.isStopped = true;
    }

    /// <summary>
    /// Called when thank phase or timeout finishes.
    /// </summary>
    public void StartLeaving()
    {
        CurrentState = NPCState.Leaving;
        stateTimer = 0f;

        int next = (sitIndex + 1) % waypoints.Length;
        MoveTo(next);
    }

    // ----------- Internal Handlers -----------

    private void HandleMoving()
    {
        if (!agent.hasPath)
            MoveTo(currentIndex);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + arrivalEpsilon)
        {
            if (currentIndex == sitIndex)
            {
                StartSitting();
            }
            else
            {
                // Continue to next waypoint
                currentIndex = (currentIndex + 1) % waypoints.Length;
                MoveTo(currentIndex);
            }
        }
    }

    private void HandleThanking()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= thankYouDelay)
            StartLeaving();
    }

    private void HandleLeaving()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + arrivalEpsilon)
        {
            // Resume loop after leaving
            currentIndex = (currentIndex + 1) % waypoints.Length;
            MoveTo(currentIndex);
            CurrentState = NPCState.Moving;
        }
    }

    private void StartSitting()
    {
        CurrentState = NPCState.Sitting;
        agent.isStopped = true;
        stateTimer = 0f;

        npcInteractable?.StartWaitingForPlayer();
    }

    private void MoveTo(int index)
    {
        currentIndex = index;
        agent.isStopped = false;
        agent.SetDestination(waypoints[currentIndex].position);
    }
}
