using UnityEngine;

public class NPCSpawner1 : MonoBehaviour
{
    [Header("Spawn")]
    [Tooltip("NPC prefab with NPCMovement + NPCInteractable + NPCOrder1 + NPCHeadLookAt")]
    public GameObject npcPrefab;

    [Tooltip("Where to spawn the NPC(s)")]
    public Transform[] spawnPoints;

    [Header("Route Injection")]
    [Tooltip("Route object that has a WaypointSet component (children = waypoints).")]
    public WaypointSet routeToUse;

    [Tooltip("Which waypoint index to use as the initial wait spot (e.g., 0 if first child is Path 2).")]
    public int initialWaitIndex = 0;

    [Header("Auto")]
    public bool spawnOnStart = true;

    void Start()
    {
        if (spawnOnStart) SpawnOne();
    }

    public GameObject SpawnOne()
    {
        if (npcPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[NPCSpawner1] Missing prefab or spawn points.");
            return null;
        }

        Transform p = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject npc = Instantiate(npcPrefab, p.position, p.rotation);

        // Inject waypoints right after spawn
        if (routeToUse != null)
        {
            var movement = npc.GetComponent<NPCMovement>();
            if (movement != null)
            {
                Transform[] points = routeToUse.GetPoints();
                movement.SetWaypoints(points, initialWaitIndex);
            }
        }
        else
        {
            Debug.LogWarning("[NPCSpawner1] No routeToUse assigned. NPC will have no waypoints.");
        }

        return npc;
    }
}
