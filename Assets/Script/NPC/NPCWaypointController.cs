using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NPCWaypointController : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform[] waypoints;

    [Header("Order Stop")]
    [Tooltip("Zero-based index of the waypoint where NPC will wait for order")]
    public int orderStopIndex = 0;

    [Header("Settings")]
    public float arrivalTolerance = 0.5f;
    public float postOrderDelay    = 10f;

    NavMeshAgent agent;
    NPCOrder    npcOrder;
    int         currentWaypoint = 0;
    bool        waitingForOrder = false;

    void Start()
    {
        agent    = GetComponent<NavMeshAgent>();
        npcOrder = GetComponent<NPCOrder>();
        if (npcOrder != null)
            npcOrder.onOrderComplete.AddListener(OnOrderComplete);

        if (waypoints.Length > 0)
            MoveTo(currentWaypoint);
    }

    void Update()
    {
        if (waitingForOrder || waypoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= arrivalTolerance)
        {
            // if this is our designated order waypoint, stop & wait
            if (currentWaypoint == orderStopIndex)
            {
                agent.isStopped     = true;
                waitingForOrder     = true;
                Debug.Log($"Arrived at order waypoint #{orderStopIndex}, waiting for order...");
            }
            else
            {
                // otherwise immediately go to next
                AdvanceWaypoint();
            }
        }
    }

    void MoveTo(int idx)
    {
        agent.isStopped = false;
        agent.SetDestination(waypoints[idx].position);
    }

    void AdvanceWaypoint()
    {
        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        MoveTo(currentWaypoint);
    }

    void OnOrderComplete()
    {
        StartCoroutine(AfterOrder());
    }

    IEnumerator AfterOrder()
    {
        yield return new WaitForSeconds(postOrderDelay);
        waitingForOrder = false;
        Debug.Log("Order done—moving on");
        AdvanceWaypoint();
    }
}
