using UnityEngine;

public class NPCSpawner1 : MonoBehaviour
{
    [System.Serializable]
    public class NPCSpawnData
    {
        [Header("NPC Setup")]
        [Tooltip("The NPC prefab to spawn.")]
        public GameObject npcPrefab;

        [Tooltip("The route this NPC will follow (WaypointSet with child waypoints).")]
        public WaypointSet route;

        [Tooltip("Spawn point where this NPC will appear.")]
        public Transform spawnPoint;

        [Tooltip("Which waypoint index the NPC will sit/wait at.")]
        [Range(0, 20)] public int waitIndex = 0;

        [Tooltip("Where this NPC's food will appear (table Display transform).")]
        public Transform tableDisplayPoint;
    }

    [Header("Spawner Settings")]
    [Tooltip("List of all NPCs to spawn with their own prefab, route, and spawn point.")]
    public NPCSpawnData[] npcEntries;

    [Tooltip("If true, NPCs will spawn automatically at Start.")]
    public bool spawnOnStart = true;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnAllNPCs();
        }
    }

    /// <summary>
    /// Spawns all NPCs defined in the npcEntries list.
    /// </summary>
    public void SpawnAllNPCs()
    {
        if (npcEntries == null || npcEntries.Length == 0)
        {
            Debug.LogWarning("[NPCSpawner1] No NPC entries set!");
            return;
        }

        foreach (var entry in npcEntries)
        {
            if (entry.npcPrefab == null || entry.route == null || entry.spawnPoint == null)
            {
                Debug.LogWarning("[NPCSpawner1] Missing prefab, route, or spawn point in entry.");
                continue;
            }

            // Instantiate the NPC prefab
            GameObject npc = Instantiate(
                entry.npcPrefab,
                entry.spawnPoint.position,
                entry.spawnPoint.rotation
            );

            // Set order display point for the NPC's food (handled by NPCOrder1)
            var order = npc.GetComponent<NPCOrder1>();
            if (order != null)
            {
                order.SetOrderDisplayPoint(entry.tableDisplayPoint);
            }

            // Inject route and sit index into NPCMovement
            var move = npc.GetComponent<NPCMovement>();
            if (move != null)
            {
                Transform[] waypoints = entry.route.GetPoints();
                if (waypoints != null && waypoints.Length > 0)
                {
                    move.InitializeRoute(waypoints, entry.waitIndex);
                }
                else
                {
                    Debug.LogWarning($"[NPCSpawner1] Route '{entry.route.name}' has no waypoints!");
                }
            }
            else
            {
                Debug.LogWarning($"[NPCSpawner1] NPC prefab '{entry.npcPrefab.name}' is missing NPCMovement component!");
            }

            // After NPC is instantiated, set the player's transform for NPC's headLookAt
            var headLookAt = npc.GetComponent<NPCHeadLookAt>();
            if (headLookAt != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");  // Find the player in the scene
                if (player != null)
                {
                    headLookAt.SetPlayerTransform(player.transform);  // Assign the player transform
                    headLookAt.EnableFollowing(true);  // Start following when interacting
                }
            }
        }
    }
}
