using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner1 : MonoBehaviour
{
    [Header("All Possible NPC Prefabs (pick at spawn time)")]
    public GameObject[] npcPrefabs;

    [System.Serializable]
    public class RouteEntry
    {
        [Header("Pathing Setup")]
        public WaypointSet route;
        public Transform spawnPoint;
        [Range(0, 50)] public int waitIndex = 0;

        [Header("Spawn Timing")]
        [Tooltip("Delay before this entry spawns (seconds).")]
        public float spawnDelay = 0f; // float as requested
    }

    [Header("Per-Route Entries (order controls spawn sequence on each route)")]
    public RouteEntry[] routeEntries;

    [Header("Spawner Settings")]
    public bool spawnOnStart = true;
    [Tooltip("If true, routes loop back to their first entry after finishing the last.")]
    public bool loopPerRoute = true;
    [Tooltip("Prevent picking the exact same prefab twice in a row on the same route.")]
    public bool avoidConsecutivePrefabRepeat = true;

    // --- Internal tracking per route ---
    private readonly Dictionary<WaypointSet, List<RouteEntry>> _entriesByRoute = new();
    private readonly Dictionary<WaypointSet, int> _nextIndexByRoute = new();
    private readonly HashSet<WaypointSet> _routeHasActiveNPC = new();
    private readonly Dictionary<WaypointSet, GameObject> _lastPrefabPerRoute = new();

    // Per-route max concurrency (number of active NPCs allowed on each route at a time)
    [Header("Max Concurrency Per Route")]
    public int maxConcurrency = 3;  // Default max concurrency for all routes
    private readonly Dictionary<WaypointSet, int> _activeNPCCountByRoute = new();

    void Start()
    {
        BuildGroups();

        if (spawnOnStart)
        {
            foreach (var route in _entriesByRoute.Keys)
            {
                StartCoroutine(SpawnWithDelay(route));
            }
        }
    }

    private void BuildGroups()
    {
        _entriesByRoute.Clear();
        _nextIndexByRoute.Clear();
        _routeHasActiveNPC.Clear();
        _activeNPCCountByRoute.Clear();

        foreach (var entry in routeEntries)
        {
            if (entry == null || entry.route == null || entry.spawnPoint == null)
                continue;

            if (!_entriesByRoute.ContainsKey(entry.route))
                _entriesByRoute[entry.route] = new List<RouteEntry>();

            _entriesByRoute[entry.route].Add(entry);
            _activeNPCCountByRoute[entry.route] = 0;  // Initialize NPC count for each route
        }

        foreach (var kv in _entriesByRoute)
            _nextIndexByRoute[kv.Key] = 0;
    }

    private IEnumerator SpawnWithDelay(WaypointSet route)
    {
        if (route == null || !_entriesByRoute.ContainsKey(route)) yield break;

        var list = _entriesByRoute[route];
        if (list == null || list.Count == 0) yield break;

        int idx = _nextIndexByRoute[route];
        if (!loopPerRoute && idx >= list.Count) yield break;
        if (loopPerRoute && idx >= list.Count) idx = 0;

        var entry = list[idx];

        // Per-entry float delay
        if (entry.spawnDelay > 0f)
            yield return new WaitForSeconds(entry.spawnDelay);

        TrySpawnNextForRoute(route);
    }

    private void TrySpawnNextForRoute(WaypointSet route)
    {
        if (route == null || !_entriesByRoute.ContainsKey(route)) return;

        // Check the number of active NPCs for this route and compare to max concurrency
        if (_activeNPCCountByRoute[route] >= maxConcurrency)
        {
            // Max concurrency reached, do not spawn another NPC for this route
            return;
        }

        var list = _entriesByRoute[route];
        if (list == null || list.Count == 0) return;

        // Which entry (route config) to use next
        int idx = _nextIndexByRoute[route];
        if (!loopPerRoute && idx >= list.Count) return;
        if (loopPerRoute && idx >= list.Count) idx = 0;
        var entry = list[idx];

        // Advance pointer for the next time
        _nextIndexByRoute[route] = idx + 1;

        // Pick a random prefab from the global pool
        var prefab = PickRandomPrefabForRoute(route);
        if (prefab == null)
        {
            Debug.LogWarning("[NPCSpawner1] No NPC prefabs assigned.");
            return;
        }

        // Spawn
        _routeHasActiveNPC.Add(route);
        _activeNPCCountByRoute[route]++;

        GameObject npcObj = Instantiate(prefab, entry.spawnPoint.position, entry.spawnPoint.rotation);

        var move = npcObj.GetComponent<NPCMovement>();
        if (move != null)
            move.InitializeRoute(entry.route.waypoints, entry.waitIndex, this, entry.route);
        else
            Debug.LogWarning($"[NPCSpawner1] Spawned prefab {prefab.name} has no NPCMovement.");
    }

    private GameObject PickRandomPrefabForRoute(WaypointSet route)
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0) return null;

        if (!avoidConsecutivePrefabRepeat || npcPrefabs.Length == 1 || !_lastPrefabPerRoute.ContainsKey(route))
        {
            var chosen = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
            _lastPrefabPerRoute[route] = chosen;
            return chosen;
        }

        // Avoid repeating the last one if possible
        GameObject last = _lastPrefabPerRoute[route];
        GameObject chosenPrefab = last;
        int safety = 20;
        while (chosenPrefab == last && safety-- > 0)
        {
            chosenPrefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
        }
        _lastPrefabPerRoute[route] = chosenPrefab;
        return chosenPrefab;
    }

    // Called by NPCMovement when an NPC finishes its route
    public void OnNPCCompletedRoute(NPCMovement npc, WaypointSet route)
    {
        if (route != null && _routeHasActiveNPC.Contains(route))
            _routeHasActiveNPC.Remove(route);

        // Decrease the active NPC count for this route
        _activeNPCCountByRoute[route]--;

        // Schedule the next spawn on that route using that entry's delay
        StartCoroutine(SpawnWithDelay(route));
    }
}
