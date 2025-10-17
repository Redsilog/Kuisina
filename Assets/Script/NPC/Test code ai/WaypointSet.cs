using UnityEngine;

public class WaypointSet : MonoBehaviour
{
    [Tooltip("If left empty, this will auto-use the children of this object in hierarchy order.")]
    public Transform[] points;

    public Transform[] GetPoints()
    {
        if (points != null && points.Length > 0) return points;

        int n = transform.childCount;
        var arr = new Transform[n];
        for (int i = 0; i < n; i++)
            arr[i] = transform.GetChild(i);
        return arr;
    }
}
