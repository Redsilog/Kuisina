using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Tooltip("Prefabs to spawn, in order")]
    public GameObject[] npcPrefabs;

    [Tooltip("Where in the scene each NPC should appear")]
    public Transform spawnPoint;

    [Header("Patrol Settings")]
    [Tooltip("Shared waypoint transforms")]
    public Transform[] patrolWaypoints;

    [Tooltip("Which waypoint index each NPC stops at")]
    public int[] stopIndices;  // match length of npcPrefabs

    [Header("Order Display")]
    [Tooltip("Drag the Transform where you want the order ghost to appear")]
    public Transform displayPoint;

    [Header("Order Timing Overrides")]
    [Tooltip("Max seconds an NPC will wait for its order before moving on")]
    public float npcMaxWaitForOrder = 20f;

    [Tooltip("Seconds to wait *after* an order is delivered before resuming")]
    public float npcPostOrderDelay = 5f;

    int     cur;
    GameObject current;

    void Start()
    {
        SpawnNext();
    }

    void SpawnNext()
    {
        if (cur >= npcPrefabs.Length) return;

        // 1) Instantiate NPC
        current = Instantiate(npcPrefabs[cur], spawnPoint.position, spawnPoint.rotation);

        // 2) Configure its waypoint controller
        var wp = current.GetComponent<NPCWaypointController>();
        if (wp != null)
        {
            wp.waypoints         = patrolWaypoints;
            wp.orderStopIndex    = (cur < stopIndices.Length) ? stopIndices[cur] : 0;
            wp.loop              = false;
            wp.maxWaitForOrder   = npcMaxWaitForOrder;   // ← public override
            wp.postOrderDelay    = npcPostOrderDelay;    // ← public override
            wp.onPatrolComplete.AddListener(OnFinished);
        }

        // 3) Wire up its inventory storage (optional)
        var inv = current.GetComponent<NPCInventory>();
        if (inv != null)
        {
            var sp = current.transform.Find("StoragePoint");
            if (sp != null)
                inv.storagePoint = sp;
        }

        // 4) Assign the display point for its order ghost
        var order = current.GetComponent<NPCOrder>();
        if (order != null && displayPoint != null)
        {
            order.orderDisplayPoint = displayPoint;
        }
    }

    void OnFinished()
    {
        Destroy(current);
        cur++;
        SpawnNext();
    }
}
