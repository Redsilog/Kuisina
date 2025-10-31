using UnityEngine;
using UnityEngine.AI;

public class NPCWaypointMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    private Transform[] waypoints;
    private int currentIndex;
    private bool routeReady;

    private NPCSpawner23 spawnerRef;   // <-- changed to NPCSpawner23
    private WaypointSet routeRef;

    private const float arrivalEpsilon = 0.1f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!routeReady || waypoints == null || waypoints.Length == 0) return;

        UpdateAnimation();

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + arrivalEpsilon)
        {
            if (currentIndex < waypoints.Length - 1)
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

    private void UpdateAnimation()
    {
        if (animator == null) return;
        bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
        animator.SetBool("IsMoving", isMoving);
    }

    public void InitializeRoute(Transform[] route, NPCSpawner23 spawner, WaypointSet routeOwner)
    {
        if (route == null || route.Length == 0)
        {
            Debug.LogWarning($"[NPCWaypointMovement] {name} received no waypoints!");
            return;
        }

        spawnerRef = spawner;
        routeRef = routeOwner;
        waypoints = route;
        currentIndex = 0;
        routeReady = true;
        MoveTo(currentIndex);
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
            spawnerRef.OnNPCCompletedRoute(this, routeRef); // <-- expects NPCWaypointMovement

        if (animator != null) animator.SetBool("IsMoving", false);
        Destroy(gameObject);
    }
}
