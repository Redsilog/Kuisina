using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class NPCWaypointController : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform[] waypoints;

    [Header("Order Stop")]
    public int orderStopIndex = 0;

    [Header("Order Zone Settings")]
    public float orderZoneMaxWait        = 20f;  // initial
    public float postOrderDeliveredDelay = 5f;

    [Header("Arrival Settings")]
    public float arrivalTolerance = 0.5f;

    [Header("Patrol Completion")]
    public bool loop = false;
    public UnityEvent onPatrolComplete;

    NavMeshAgent agent;
    NPCOrder     npcOrder;
    int          currentIndex        = 0;
    bool         waitingForOrder     = false;
    bool         patrolCompleteFired = false;
    Coroutine    orderWaitCoroutine;
    float        orderTimerRemaining;

    void Start()
    {
        agent    = GetComponent<NavMeshAgent>();
        npcOrder = GetComponent<NPCOrder>();

        if (npcOrder != null)
        {
            npcOrder.onOrderRequested.AddListener(OnOrderRequested);
            npcOrder.onOrderComplete .AddListener(OnOrderComplete);
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("No waypoints assigned!");
            enabled = false;
            return;
        }

        orderStopIndex = Mathf.Clamp(orderStopIndex, 0, waypoints.Length - 1);
        MoveTo(waypoints[currentIndex]);
    }

    void Update()
    {
        if (waitingForOrder) return;
        if (agent.pathPending || agent.remainingDistance > arrivalTolerance) return;

        bool isLast = currentIndex == waypoints.Length - 1;

        if (currentIndex == orderStopIndex)
        {
            // stop and start timer
            waitingForOrder = true;
            agent.isStopped = true;
            Debug.Log($"NPC: arrived at waypoint #{orderStopIndex}, waiting for your interact…");
            orderWaitCoroutine = StartCoroutine(OrderZoneTimeout());
        }
        else if (isLast && !loop)
        {
            if (!patrolCompleteFired)
            {
                patrolCompleteFired = true;
                agent.isStopped = true;
                Debug.Log("NPC: patrol complete!");
                onPatrolComplete?.Invoke();
            }
        }
        else
        {
            AdvanceWaypoint();
        }
    }

    void MoveTo(Transform wp)
    {
        agent.isStopped = false;
        agent.SetDestination(wp.position);
    }

    void AdvanceWaypoint()
    {
        currentIndex = (currentIndex + 1) % waypoints.Length;
        MoveTo(waypoints[currentIndex]);
    }

    IEnumerator OrderZoneTimeout()
    {
        orderTimerRemaining = orderZoneMaxWait;
        int lastLogged = Mathf.CeilToInt(orderTimerRemaining);
        Debug.Log($"Order timeout starts: {lastLogged}s remaining");

        while (orderTimerRemaining > 0f && waitingForOrder)
        {
            orderTimerRemaining -= Time.deltaTime;
            int secondsLeft = Mathf.CeilToInt(orderTimerRemaining);
            if (secondsLeft != lastLogged)
            {
                //Debug.Log($"Order timeout in: {secondsLeft}s");
                lastLogged = secondsLeft;
            }
            yield return null;
        }

        if (waitingForOrder)
        {
            Debug.Log("NPC: no order delivered in time—resuming patrol.");
            waitingForOrder = false;
            AdvanceWaypoint();
        }
        orderWaitCoroutine = null;
    }

    void OnOrderRequested()
    {
        // give player +10s when they first get the order
        if (orderWaitCoroutine != null)
        {
            orderTimerRemaining += 10f;
            Debug.Log($"NPC: timer extended by 10s → {Mathf.CeilToInt(orderTimerRemaining)}s remaining");
        }
    }

    void OnOrderComplete()
    {
        // stop the timeout coroutine
        if (orderWaitCoroutine != null)
        {
            StopCoroutine(orderWaitCoroutine);
            orderWaitCoroutine = null;
        }
        // then wait a bit, then resume patrol
        StartCoroutine(PostOrderDelay());
    }

    IEnumerator PostOrderDelay()
    {
        yield return new WaitForSeconds(postOrderDeliveredDelay);
        if (waitingForOrder)
        {
            waitingForOrder = false;
            Debug.Log("NPC: order delivered—resuming patrol.");
            AdvanceWaypoint();
        }
    }
}
