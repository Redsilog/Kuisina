using UnityEngine;

public class CarWaypointMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float reachThreshold = 0.5f;

    [HideInInspector] public Transform[] waypoints;
    [HideInInspector] public CarSpawner spawner; // back-reference

    private int currentWaypointIndex = 0;

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        MoveToWaypoint();
    }

    void MoveToWaypoint()
    {
        Transform target = waypoints[currentWaypointIndex];
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        transform.LookAt(target);

        if (Vector3.Distance(transform.position, target.position) < reachThreshold)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                spawner.OnCarDestroyed(this);
                Destroy(gameObject);
            }
        }
    }
}
