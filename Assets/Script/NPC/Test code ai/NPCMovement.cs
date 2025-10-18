using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCMovement : MonoBehaviour
{
    public enum NPCState
    {
        Moving,   // walking along waypoints
        Sitting,  // at sit waypoint, waiting for player to interact
        Ordering, // order phase
        Thanking, // thank dwell
        Leaving   // going to next waypoint after sit/order/thank/timeout
    }

    [Header("Runtime Route (injected by spawner)")]
    public Transform[] waypoints;          // injected at runtime
    public int waitIndex = 0;              // which waypoint is the sit spot
    public bool approachWaitIndexSequentially = true; // spawner can set this

    [Header("Movement Settings")]
    public float arrivalEpsilon = 0.1f;    // extra slack beyond agent.stoppingDistance

    [Header("Timers")]
    public float interactionTimeout = 10f; // Order timer (handled by Interactable)
    public float thankYouDelay = 5f;       // Thank dwell (handled here)

    public NPCState currentState = NPCState.Moving;

    private NavMeshAgent agent;
    private NPCInteractable npcInteractable;

    private int currentIndex = 0;
    private bool routeReady = false;
    private float stateTimer = 0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        npcInteractable = GetComponent<NPCInteractable>();
    }

    void Update()
    {
        if (!routeReady || waypoints == null || waypoints.Length == 0) return;

        switch (currentState)
        {
            case NPCState.Moving:
                TickMoving();
                break;

            case NPCState.Sitting:
                // Interactable controls SIT/ORDER timers. We only park the agent here.
                agent.isStopped = true;
                break;

            case NPCState.Ordering:
                // Ordering handled by Interactable; keep the agent parked.
                agent.isStopped = true;
                break;

            case NPCState.Thanking:
                agent.isStopped = true;
                stateTimer += Time.deltaTime;
                if (stateTimer >= thankYouDelay)
                {
                    StartLeaving();
                }
                break;

            case NPCState.Leaving:
                TickLeaving();
                break;
        }
    }

    // --------- Public API (called by Spawner / Interactable) ---------

    // Spawner calls this immediately after instantiate
    public void SetWaypoints(Transform[] points, int initialWaitIndex)
    {
        waypoints = points;
        waitIndex = Mathf.Clamp(initialWaitIndex, 0, waypoints.Length - 1);

        currentIndex = approachWaitIndexSequentially ? 0 : waitIndex;
        routeReady = (waypoints != null && waypoints.Length > 0);

        if (routeReady)
        {
            agent.isStopped = false;
            agent.SetDestination(waypoints[currentIndex].position);
            currentState = NPCState.Moving;
        }
    }

    // Called by NPCInteractable when player starts order phase
    public void StartOrdering()
    {
        currentState = NPCState.Ordering;
        stateTimer = 0f;
        agent.isStopped = true;
    }

    // Called by NPCInteractable when order fulfilled
    public void FulfillOrder()
    {
        currentState = NPCState.Thanking;
        stateTimer = 0f;
    }

    // Called by NPCInteractable on timeout / after thank
    public void StartLeaving()
    {
        // Move to the next waypoint after the sit point
        currentIndex = (waitIndex + 1) % waypoints.Length;
        agent.isStopped = false;
        agent.SetDestination(waypoints[currentIndex].position);
        currentState = NPCState.Leaving;
        stateTimer = 0f;
    }

    // Called by NPCInteractable when the NPC reaches the sit spot (or by us)
    public void StartSitting()
    {
        currentState = NPCState.Sitting;
        agent.isStopped = true;
        stateTimer = 0f;

        // Tell Interactable to begin SIT phase timers & (optionally) randomize order later
        if (npcInteractable) npcInteractable.StartWaitingForPlayer();
    }

    // --------- Internals ---------

    void TickMoving()
    {
        if (!agent.hasPath) agent.SetDestination(waypoints[currentIndex].position);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + arrivalEpsilon)
        {
            // reached currentIndex
            if (currentIndex == waitIndex)
            {
                // sit here
                StartSitting();
            }
            else
            {
                // continue along route toward waitIndex
                currentIndex = (currentIndex + 1) % waypoints.Length;
                agent.isStopped = false;
                agent.SetDestination(waypoints[currentIndex].position);
            }
        }
    }

    void TickLeaving()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + arrivalEpsilon)
        {
            // After leaving, resume normal waypoint walk (loop)
            currentIndex = (currentIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentIndex].position);
            currentState = NPCState.Moving;
        }
    }
}
