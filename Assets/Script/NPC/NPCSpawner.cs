using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Tooltip("NPC prefabs to spawn, in order")]
    public GameObject[] npcPrefabs;

    [Tooltip("Where to spawn each NPC")]
    public Transform spawnPoint;

    [Header("Patrol Settings")]
    [Tooltip("The scene’s waypoints, in order")]
    public Transform[] patrolWaypoints;

    [Tooltip("Which waypoint index each NPC should stop at for orders")]
    public int[] orderStopIndices;  // length should match npcPrefabs.Length

    int currentIndex = 0;
    GameObject currentNPC;

    void Start()
    {
        SpawnNext();
    }

    void SpawnNext()
    {
        if (currentIndex >= npcPrefabs.Length)
            return;

        // Instantiate the next NPC
        currentNPC = Instantiate(
            npcPrefabs[currentIndex],
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Configure its waypoint controller
        var wp = currentNPC.GetComponent<NPCWaypointController>();
        if (wp != null)
        {
            // Inject the shared patrol waypoints
            wp.waypoints = patrolWaypoints;

            // Determine and clamp which waypoint it should stop at
            int stopIdx = 0;
            if (orderStopIndices != null && currentIndex < orderStopIndices.Length)
                stopIdx = orderStopIndices[currentIndex];
            wp.orderStopIndex = Mathf.Clamp(stopIdx, 0, patrolWaypoints.Length - 1);

            // Do not loop—so it will fire onPatrolComplete at the last point
            wp.loop = false;

            // When that event fires, clean up and spawn the next NPC
            wp.onPatrolComplete.AddListener(OnNPCFinishedPatrol);
        }
    }

    void OnNPCFinishedPatrol()
    {
        // Destroy the finished NPC
        if (currentNPC != null)
            Destroy(currentNPC);

        // Move to the next prefab in the list
        currentIndex++;
        SpawnNext();
    }
}
