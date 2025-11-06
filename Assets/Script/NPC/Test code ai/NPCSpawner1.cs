using System.Collections;
using System.Collections.Generic;
using System.Linq;          
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
        public float spawnDelay = 0f;
    }

    [Header("Per-Route Entries (these are cycled in shuffled order)")]
    public RouteEntry[] routeEntries;

    [Header("Spawner Settings")]
    public bool spawnOnStart = true;
    public bool reshuffleEveryCycle = true;
    public bool avoidConsecutivePrefabRepeat = true;

    [Header("Max Concurrency Per Route")]
    [Min(1)] public int maxConcurrencyPerRoute = 3;

    // --- Internal tracking ---
    private readonly List<RouteEntry> _validEntries = new();
    private readonly Dictionary<WaypointSet, int> _activeCountByRoute = new();
    private readonly Dictionary<WaypointSet, GameObject> _lastPrefabPerRoute = new();

    // NEW: only the very first cycle will prioritize the lowest delay first
    private bool _isFirstCycle = true;

    void Start()
    {
        BuildValidEntries();
        if (spawnOnStart && _validEntries.Count > 0)
        {
            StartCoroutine(CycleLoop());
        }
    }

    private void BuildValidEntries()
    {
        _validEntries.Clear();
        _activeCountByRoute.Clear();

        foreach (var e in routeEntries)
        {
            if (e == null || e.route == null || e.spawnPoint == null) continue;
            _validEntries.Add(e);
            if (!_activeCountByRoute.ContainsKey(e.route))
                _activeCountByRoute[e.route] = 0;
        }
    }

    private bool IsRouteClearForSpawning(WaypointSet route)
    {
        if (_activeCountByRoute.ContainsKey(route))
        {
            return _activeCountByRoute[route] == 0; // Route is clear if no active NPCs are present
        }
        return true; // If route has no entries, consider it clear
    }

    private IEnumerator CycleLoop()
    {
        var work = new List<RouteEntry>(_validEntries);

        while (true)
        {
            if (reshuffleEveryCycle) FisherYatesShuffle(work);

            // --- NEW: First cycle = force the lowest spawnDelay entry to index 0 ---
            if (_isFirstCycle && work.Count > 1)
            {
                var fastest = work
                    .Where(e => e != null)
                    .OrderBy(e => e.spawnDelay)
                    .FirstOrDefault();

                if (fastest != null)
                {
                    // Move fastest to front; keep the rest in their (shuffled) order
                    work.Remove(fastest);
                    work.Insert(0, fastest);
                }
            }

            // One spawn attempt per entry (keeps �all routes get an NPC� per cycle)
            for (int i = 0; i < work.Count; i++)
            {
                var entry = work[i];
                if (entry == null) continue;

                if (entry.spawnDelay > 0f)
                    yield return new WaitForSeconds(entry.spawnDelay);

                // Check if the route is clear for spawning a new NPC
                if (!IsRouteClearForSpawning(entry.route))
                {
                    continue; // Skip spawning if route is not clear
                }

                yield return StartCoroutine(WaitForFreeSlot(entry.route));
                TrySpawn(entry);
            }

            // after finishing the first full pass, disable the �first cycle� rule
            _isFirstCycle = false;
        }
    }

    private IEnumerator WaitForFreeSlot(WaypointSet route)
    {
        if (route == null) yield break;
        while (_activeCountByRoute.TryGetValue(route, out int count) && count >= maxConcurrencyPerRoute)
            yield return null;
    }

    private void TrySpawn(RouteEntry entry)
    {
        if (entry == null || entry.route == null || entry.spawnPoint == null) return;
        if (_activeCountByRoute[entry.route] >= maxConcurrencyPerRoute) return;

        var prefab = PickRandomPrefabForRoute(entry.route);
        if (prefab == null)
        {
            Debug.LogWarning("[NPCSpawner1] No NPC prefabs assigned.");
            return;
        }

        _activeCountByRoute[entry.route]++;

        var npcObj = Instantiate(prefab, entry.spawnPoint.position, entry.spawnPoint.rotation);
        var move = npcObj.GetComponent<NPCMovement>();
        if (move != null)
            move.InitializeRoute(entry.route.waypoints, entry.waitIndex, this, entry.route);
        else
            Debug.LogWarning($"[NPCSpawner1] Spawned prefab {prefab.name} has no NPCMovement.");
    }

    public void TrySpawnTutorial()
    {
        if (_validEntries.Count == 0)
        {
            Debug.LogWarning("[NPCSpawner1] No valid RouteEntries to spawn from.");
            return;
        }

        var entry = _validEntries[0];

        TrySpawn(entry);
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

        var last = _lastPrefabPerRoute[route];
        var chosenPrefab = last;
        int safety = 20;
        while (chosenPrefab == last && safety-- > 0)
            chosenPrefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];

        _lastPrefabPerRoute[route] = chosenPrefab;
        return chosenPrefab;
    }

    public void OnNPCCompletedRoute(NPCMovement npc, WaypointSet route)
    {
        if (route == null) return;
        if (!_activeCountByRoute.ContainsKey(route)) return;
        _activeCountByRoute[route] = Mathf.Max(0, _activeCountByRoute[route] - 1);
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
