using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NPCWaypointController : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("Set your sequence of patrol points here")]
    public Transform[] waypoints;

    [Header("Settings")]
    [Tooltip("How close is considered 'arrived'")]
    public float arrivalTolerance = 0.5f;
    [Tooltip("Seconds to wait after order is completed")]
    public float postOrderDelay = 10f;

    NavMeshAgent agent;
    NPCOrder npcOrder;

    int currentWaypoint = 0;
    bool waitingForOrder = false;

    void Start()
    {
        agent     = GetComponent<NavMeshAgent>();
        npcOrder  = GetComponent<NPCOrder>();

        if (npcOrder != null)
        {
            // subscribe to the event
            npcOrder.onOrderComplete.AddListener(OnOrderComplete);
        }

        if (waypoints.Length > 0)
            MoveToWaypoint(0);
    }

    void Update()
    {
        if (waitingForOrder || waypoints.Length == 0) return;

        // check arrival
        if (!agent.pathPending && agent.remainingDistance <= arrivalTolerance)
        {
            // arrived: stop and wait for order
            agent.isStopped = true;
            waitingForOrder = true;

            // (optional) if you want to automatically ask for an order:
            // npcOrder.AskForRandomOrder();
        }
    }

    void MoveToWaypoint(int index)
    {
        agent.isStopped = false;
        agent.SetDestination(waypoints[index].position);
    }

    void OnOrderComplete()
    {
        // start the 10-second wait, then proceed
        StartCoroutine(ProceedAfterDelay());
    }

    IEnumerator ProceedAfterDelay()
    {
        yield return new WaitForSeconds(postOrderDelay);

        // next waypoint (wrap around)
        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        waitingForOrder = false;
        MoveToWaypoint(currentWaypoint);
    }
}
