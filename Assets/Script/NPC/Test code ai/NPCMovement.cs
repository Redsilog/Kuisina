using UnityEngine;
using UnityEngine.AI;


public class NPCMovement : MonoBehaviour
{
    public enum NPCState { Moving, Sitting, Ordering, Thanking, Leaving }

    [Header("Timing")]
    public float thankYouDelay = 5f;

    public NPCState CurrentState { get; private set; } = NPCState.Moving;

    private NavMeshAgent agent;
    private NPCInteractable npcInteractable;
    private Animator animator;

    private Transform[] waypoints;
    private int sitIndex;
    private int currentIndex;
    private bool routeReady;
    private float stateTimer;
    private const float arrivalEpsilon = 0.1f;

    private NPCSpawner1 spawnerRef;
    private WaypointSet routeRef;

    [HideInInspector] public bool HasReachedWaitIndex = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        npcInteractable = GetComponent<NPCInteractable>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!routeReady || waypoints == null || waypoints.Length == 0) return;

        UpdateAnimation();

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

    private void UpdateAnimation()
    {
        //  New fix: force movement animation OFF when interacting or sitting
        bool isInteracting =
            CurrentState == NPCState.Ordering ||
            CurrentState == NPCState.Thanking ||
            CurrentState == NPCState.Sitting;

        bool isMoving = !isInteracting && agent.velocity.magnitude > 0.1f && !agent.isStopped;
        bool isSitting = (CurrentState == NPCState.Sitting || CurrentState == NPCState.Ordering);

        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsSitting", isSitting);
    }

    public void InitializeRoute(Transform[] route, int sitWaypoint, NPCSpawner1 spawner, WaypointSet routeOwner)
    {
        if (route == null || route.Length == 0)
        {
            Debug.LogWarning($"[NPCMovement] {name} received no waypoints!");
            return;
        }

        spawnerRef = spawner;
        routeRef = routeOwner;

        waypoints = route;
        sitIndex = Mathf.Clamp(sitWaypoint, 0, waypoints.Length - 1);
        currentIndex = 0;
        routeReady = true;
        MoveTo(currentIndex);
        CurrentState = NPCState.Moving;
    }

    public void BeginOrdering()
    {
        CurrentState = NPCState.Ordering;
        agent.isStopped = true;
        stateTimer = 0f;
        UpdateAnimation();
    }

    public void BeginThanking()
    {
        CurrentState = NPCState.Thanking;
        agent.isStopped = true;
        stateTimer = 0f;
        UpdateAnimation();
    }

    public void StartLeaving()
    {
        CurrentState = NPCState.Leaving;
        stateTimer = 0f;
        int next = (sitIndex + 1) % waypoints.Length;
        MoveTo(next);
        UpdateAnimation();
    }

    private void HandleMoving()
    {
        if (!agent.hasPath) MoveTo(currentIndex);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + arrivalEpsilon)
        {
            if (currentIndex == sitIndex)
            {
                StartSitting();
            }
            else if (currentIndex < waypoints.Length - 1)
            {
                currentIndex++;
                MoveTo(currentIndex);
            }
            else
            {
                FinishRoute();
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
            if (currentIndex < waypoints.Length - 1)
            {
                currentIndex++;
                MoveTo(currentIndex);
                CurrentState = NPCState.Moving;
            }
            else
            {
                FinishRoute();
            }
        }
    }

    private void StartSitting()
    {
        CurrentState = NPCState.Sitting;
        agent.isStopped = true;
        stateTimer = 0f;
        npcInteractable?.StartWaitingForPlayer();
        UpdateAnimation();
    }

    private void MoveTo(int index)
    {
        currentIndex = index;
        agent.isStopped = false;
        agent.SetDestination(waypoints[currentIndex].position);
        UpdateAnimation();
    }

    private void FinishRoute()
    {
        if (spawnerRef != null)
            spawnerRef.OnNPCCompletedRoute(this, routeRef);
        Destroy(gameObject);
    }
}
