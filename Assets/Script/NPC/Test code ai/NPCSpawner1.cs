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

    [Tooltip("Which waypoint index is the SIT/WAIT spot (NPC will stop here).")]
    public int initialWaitIndex = 0;

    [Tooltip("If true, NPC walks through waypoints in order until reaching initialWaitIndex. If false, it starts directly at that index.")]
    public bool approachWaitIndexSequentially = true;

    [Tooltip("If true, ignore initialWaitIndex and randomly pick a sit/stop waypoint from the route.")]
    public bool randomizeWaitIndex = false;

    [Header("Auto")]
    public bool spawnOnStart = true;
    [Min(1)] public int spawnCount = 1;

    void Start()
    {
        if (spawnOnStart)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnOne();
            }
        }
    }

    /// <summary>
    /// Spawns a single NPC, injects waypoints + sit index, and returns the instance.
    /// </summary>
    public GameObject SpawnOne(Transform overrideSpawnPoint = null, WaypointSet overrideRoute = null, int? overrideWaitIndex = null, bool? overrideApproachSequentially = null)
    {
        if (npcPrefab == null)
        {
            Debug.LogWarning("[NPCSpawner1] Missing npcPrefab.");
            return null;
        }

        // pick spawn point
        Transform spawn = overrideSpawnPoint != null
            ? overrideSpawnPoint
            : PickRandomSpawnPoint();

        if (spawn == null)
        {
            Debug.LogWarning("[NPCSpawner1] No spawn points assigned.");
            return null;
        }

        // instantiate
        GameObject npc = Instantiate(npcPrefab, spawn.position, spawn.rotation);

        // choose route
        WaypointSet route = overrideRoute != null ? overrideRoute : routeToUse;
        if (route == null)
        {
            Debug.LogWarning("[NPCSpawner1] No routeToUse assigned. NPC will have no waypoints.");
            return npc;
        }

        Transform[] points = route.GetPoints();
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning("[NPCSpawner1] routeToUse has no points.");
            return npc;
        }

        // compute wait index
        int waitIndex = overrideWaitIndex.HasValue ? overrideWaitIndex.Value :
                        randomizeWaitIndex ? Random.Range(0, points.Length) :
                        Mathf.Clamp(initialWaitIndex, 0, points.Length - 1);

        // inject into NPCMovement
        var movement = npc.GetComponent<NPCMovement>();
        if (movement != null)
        {
            movement.approachWaitIndexSequentially = overrideApproachSequentially ?? approachWaitIndexSequentially;
            movement.SetWaypoints(points, waitIndex);
        }
        else
        {
            Debug.LogWarning("[NPCSpawner1] Spawned NPC has no NPCMovement component.");
        }

        return npc;
    }

    private Transform PickRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
