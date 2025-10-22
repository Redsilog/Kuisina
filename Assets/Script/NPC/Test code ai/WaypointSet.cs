using UnityEngine;

/// <summary>
/// Holds a list of waypoint positions for an NPC route.
/// Simply create an empty GameObject, add this script,
/// and place child objects under it to serve as waypoints.
/// </summary>
public class WaypointSet : MonoBehaviour
{
    /// <summary>
    /// Returns all child transforms as waypoint positions.
    /// </summary>
    public Transform[] GetPoints()
    {
        // Count how many children (waypoints) exist under this object
        int count = transform.childCount;
        Transform[] points = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            points[i] = transform.GetChild(i);
        }

        return points;
    }

#if UNITY_EDITOR
    // Optional: draw gizmos to visualize waypoints in the scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Transform[] points = GetPoints();
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;

            // Draw a sphere for each waypoint
            Gizmos.DrawSphere(points[i].position, 0.2f);

            // Draw a line to the next waypoint
            if (i + 1 < points.Length && points[i + 1] != null)
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
        }
    }
#endif
}
