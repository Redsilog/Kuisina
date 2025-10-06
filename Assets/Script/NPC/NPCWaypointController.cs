using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;


public class NPCWaypointController : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] waypoints;
    [Tooltip("Zero-based index where NPC stops for its order")]
    public int orderStopIndex = 0;
    [Tooltip("If false, stops at last and fires onPatrolComplete")]
    public bool loop = false;

    [Header("Order Timing")]
    public float maxWaitForOrder = 20f;
    public float postOrderDelay = 5f;

    [Header("Completion Event")]
    public UnityEvent onPatrolComplete;

    NavMeshAgent agent;
    NPCOrder order;
    NPCInventory inventory;
    Animator animator;

    int idx;
    bool waitingForOrder;
    bool firedComplete;
    Coroutine timeoutRoutine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        order = GetComponent<NPCOrder>();
        inventory = GetComponent<NPCInventory>();
        animator = GetComponent<Animator>();

        order.onOrderRequested.AddListener(OnOrderRequested);
        order.onOrderComplete.AddListener(OnOrderComplete);

        // Clamp index
        orderStopIndex = Mathf.Clamp(orderStopIndex, 0, waypoints.Length - 1);
        MoveTo(0);
    }

    void Update()
    {
        // Set animation based on whether the agent is moving
        animator.SetBool("IsMoving", !agent.isStopped && agent.velocity.magnitude > 0.1f);

        if (waitingForOrder) return;
        if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance) return;

        bool isLast = idx == waypoints.Length - 1;

        if (idx == orderStopIndex)
            BeginWaiting();
        else if (!loop && isLast && !firedComplete)
            FinishPatrol();
        else
            MoveTo((idx + 1) % waypoints.Length);
    }

    void MoveTo(int i)
    {
        idx = i;
        firedComplete = false;
        agent.isStopped = false;
        agent.SetDestination(waypoints[idx].position);
    }

    void BeginWaiting()
    {
        waitingForOrder = true;
        agent.isStopped = true;
        timeoutRoutine = StartCoroutine(WaitForOrder());
    }

    IEnumerator WaitForOrder()
    {
        float t = maxWaitForOrder;
        while (t > 0f && waitingForOrder)
        {
            t -= Time.deltaTime;
            yield return null;
        }
        if (waitingForOrder) // gave up
            ResumePatrol();
        timeoutRoutine = null;
    }

    void OnOrderRequested()
    {
        // reset the timer
        if (timeoutRoutine != null)
        {
            StopCoroutine(timeoutRoutine);
            timeoutRoutine = StartCoroutine(WaitForOrder());
        }
    }

    void OnOrderComplete()
    {
        if (timeoutRoutine != null)
        {
            StopCoroutine(timeoutRoutine);
            timeoutRoutine = null;
        }
        StartCoroutine(DelayedResume());
    }

    IEnumerator DelayedResume()
    {
        yield return new WaitForSeconds(postOrderDelay);
        if (waitingForOrder) ResumePatrol();
    }

    void ResumePatrol()
    {
        waitingForOrder = false;
        MoveTo((idx + 1) % waypoints.Length);
    }

    void FinishPatrol()
    {
        firedComplete = true;
        agent.isStopped = true;
        onPatrolComplete?.Invoke();
    }
}
