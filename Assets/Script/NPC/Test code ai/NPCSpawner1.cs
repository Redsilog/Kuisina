using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner1 : MonoBehaviour
{
    [System.Serializable]
    public class NPCSpawnData
    {
        public GameObject npcPrefab;
        public WaypointSet route;
        public Transform spawnPoint;
        [Range(0, 20)] public int waitIndex = 0;
    }

    [Header("Spawner Settings")]
    public NPCSpawnData[] npcEntries;
    public bool spawnOnStart = true;

    [Tooltip("If true, after the last NPC on a route finishes, it loops back to the first prefab on that route.")]
    public bool loopPerRoute = true;

    // Per-route state
    private Dictionary<WaypointSet, List<NPCSpawnData>> _routeLists = new();
    private Dictionary<WaypointSet, int> _nextIndex = new();    // next prefab to spawn for that route
    private HashSet<WaypointSet> _routeHasActiveNPC = new();    // enforce 1 active per route

    void Start()
    {
        BuildRouteGroups();

        if (spawnOnStart)
        {
            // Spawn the first NPC for each route only
            foreach (var route in _routeLists.Keys)
                TrySpawnNextForRoute(route);
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

        // Preserve inspector order per route
        foreach (var kv in _routeLists)
            _nextIndex[kv.Key] = 0;
    }

    private void TrySpawnNextForRoute(WaypointSet route)
    {
        if (route == null || !_routeLists.ContainsKey(route)) return;

        // Only one active NPC per route
        if (_routeHasActiveNPC.Contains(route)) return;

        var list = _routeLists[route];
        if (list == null || list.Count == 0) return;

        int idx = _nextIndex[route];

        // If not looping and we've reached the end, stop spawning
        if (!loopPerRoute && idx >= list.Count) return;

        // Wrap index when looping
        if (loopPerRoute && idx >= list.Count) idx = 0;

        var entry = list[idx];

        // Advance pointer for next time
        _nextIndex[route] = idx + 1;

        // Mark route busy
        _routeHasActiveNPC.Add(route);

        // Spawn
        GameObject npcObj = Instantiate(entry.npcPrefab, entry.spawnPoint.position, entry.spawnPoint.rotation);
        var move = npcObj.GetComponent<NPCMovement>();
        if (move != null)
            move.InitializeRoute(entry.route.waypoints, entry.waitIndex, this, entry.route);
        else
            Debug.LogWarning($"[NPCSpawner1] Spawned prefab {entry.npcPrefab.name} has no NPCMovement.");
    }

    // Called by NPCMovement when an NPC finishes its route and despawns
    public void OnNPCCompletedRoute(NPCMovement npc, WaypointSet route)
    {
        if (route != null && _routeHasActiveNPC.Contains(route))
            _routeHasActiveNPC.Remove(route);

        // Spawn the next NPC for that same route
        TrySpawnNextForRoute(route);
    }
}
