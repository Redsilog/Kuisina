using UnityEngine;

public class WaypointSet : MonoBehaviour
{
    public Transform[] waypoints;

    void Reset()
    {
        // Auto-fill with children for convenience
        int n = transform.childCount;
        waypoints = new Transform[n];
        for (int i = 0; i < n; i++) waypoints[i] = transform.GetChild(i);
    }
}
