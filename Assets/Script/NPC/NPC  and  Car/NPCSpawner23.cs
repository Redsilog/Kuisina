using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner23 : MonoBehaviour
{
    [Header("All Possible NPC Prefabs")]
    public GameObject[] npcPrefabs;

    [System.Serializable]
    public class RouteEntry
    {
        [Header("Pathing Setup")]
        public WaypointSet route;
        public Transform spawnPoint;

        [Header("Spawn Timing")]
        [Tooltip("Delay before this entry spawns (in seconds).")]
        public float spawnDelay = 0f;
    }

    [Header("Route Entries")]
    public RouteEntry[] routeEntries;

    [Header("Spawner Settings")]
    public bool spawnOnStart = true;
    public bool reshuffleEveryCycle = true;

    private List<RouteEntry> activeRoutes = new();

    void Start()
    {
        BuildRoutes();
        if (spawnOnStart && activeRoutes.Count > 0)
            StartCoroutine(SpawnCycle());
    }

    private void BuildRoutes()
    {
        activeRoutes.Clear();
        foreach (var entry in routeEntries)
        {
            if (entry == null || entry.route == null || entry.spawnPoint == null)
                continue;
            activeRoutes.Add(entry);
        }
    }

    private IEnumerator SpawnCycle()
    {
        var workList = new List<RouteEntry>(activeRoutes);

        while (true)
        {
            if (reshuffleEveryCycle)
                FisherYatesShuffle(workList);

            foreach (var entry in workList)
            {
                if (entry == null || entry.route == null || entry.spawnPoint == null)
                    continue;

                // Wait for this route's individual spawn delay
                if (entry.spawnDelay > 0f)
                    yield return new WaitForSeconds(entry.spawnDelay);

                SpawnNPC(entry);
            }

            // Optional delay between cycles to prevent constant respawn flood
            yield return new WaitForSeconds(1f);
        }
    }

    private void SpawnNPC(RouteEntry entry)
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0) return;

        var prefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
        var npcObj = Instantiate(prefab, entry.spawnPoint.position, entry.spawnPoint.rotation);

        var movement = npcObj.GetComponent<NPCWaypointMovement>();
        if (movement != null)
            movement.InitializeRoute(entry.route.waypoints, this, entry.route);
        else
            Debug.LogWarning($"[NPCSpawner23] Spawned prefab {prefab.name} has no NPCWaypointMovement.");
    }

    public void OnNPCCompletedRoute(NPCWaypointMovement npc, WaypointSet route)
    {
        // No-op callback for future use
    }

    private void FisherYatesShuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
