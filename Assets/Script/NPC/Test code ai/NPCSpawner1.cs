using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner1 : MonoBehaviour
{
    [System.Serializable]
    public class NPCSpawnData
    {
        [Header("NPC Setup")]
        public GameObject npcPrefab;
        public WaypointSet route;
        public Transform spawnPoint;
        [Range(0, 20)] public int waitIndex = 0;

        [Header("Spawn Timing")]
        [Tooltip("Delay before this NPC prefab spawns (in seconds).")]
        public float spawnDelay = 0f;  //  you can manually set this in the Inspector
    }

    [Header("Spawner Settings")]
    public NPCSpawnData[] npcEntries;
    public bool spawnOnStart = true;

    [Tooltip("If true, after the last NPC on a route finishes, it loops back to the first prefab on that route.")]
    public bool loopPerRoute = true;

    // Route tracking
    private Dictionary<WaypointSet, List<NPCSpawnData>> _routeLists = new();
    private Dictionary<WaypointSet, int> _nextIndex = new();
    private HashSet<WaypointSet> _routeHasActiveNPC = new();

    void Start()
    {
        BuildRouteGroups();

        if (spawnOnStart)
        {
            // Spawn first NPC per route, each with their own delay
            foreach (var route in _routeLists.Keys)
                StartCoroutine(SpawnWithDelay(route));
        }
    }

    private void BuildRouteGroups()
    {
        _routeLists.Clear();
        _nextIndex.Clear();
        _routeHasActiveNPC.Clear();

        foreach (var entry in npcEntries)
        {
            if (entry == null || entry.npcPrefab == null || entry.route == null || entry.spawnPoint == null)
                continue;

            if (!_routeLists.ContainsKey(entry.route))
                _routeLists[entry.route] = new List<NPCSpawnData>();

            _routeLists[entry.route].Add(entry);
        }

        foreach (var kv in _routeLists)
            _nextIndex[kv.Key] = 0;
    }

    // Handles waiting for that NPC's spawn delay
    private IEnumerator SpawnWithDelay(WaypointSet route)
    {
        if (!_routeLists.ContainsKey(route)) yield break;

        var list = _routeLists[route];
        if (list == null || list.Count == 0) yield break;

        // Pick the next NPC in order
        int idx = _nextIndex[route];
        if (!loopPerRoute && idx >= list.Count) yield break;
        if (loopPerRoute && idx >= list.Count) idx = 0;

        var entry = list[idx];

        // Wait for its custom delay
        if (entry.spawnDelay > 0)
            yield return new WaitForSeconds(entry.spawnDelay);

        // Then spawn
        TrySpawnNextForRoute(route);
    }

    private void TrySpawnNextForRoute(WaypointSet route)
    {
        if (route == null || !_routeLists.ContainsKey(route)) return;
        if (_routeHasActiveNPC.Contains(route)) return;

        var list = _routeLists[route];
        if (list == null || list.Count == 0) return;

        int idx = _nextIndex[route];

        // Stop if not looping and finished all NPCs
        if (!loopPerRoute && idx >= list.Count) return;
        if (loopPerRoute && idx >= list.Count) idx = 0;

        var entry = list[idx];
        _nextIndex[route] = idx + 1;
        _routeHasActiveNPC.Add(route);

        GameObject npcObj = Instantiate(entry.npcPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);
        var move = npcObj.GetComponent<NPCMovement>();
        if (move != null)
            move.InitializeRoute(entry.route.waypoints, entry.waitIndex, this, entry.route);
        else
            Debug.LogWarning($"[NPCSpawner1] Spawned prefab {entry.npcPrefab.name} has no NPCMovement.");
    }

    // Called when NPC finishes route
    public void OnNPCCompletedRoute(NPCMovement npc, WaypointSet route)
    {
        if (route != null && _routeHasActiveNPC.Contains(route))
            _routeHasActiveNPC.Remove(route);

        //Spawn the next NPC with its individual delay
        StartCoroutine(SpawnWithDelay(route));
    }
}
