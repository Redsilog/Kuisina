using UnityEngine;
using System.Collections;

[System.Serializable]
public class CarSpawnData
{
    [Header("Spawn Setup")]
    public Transform spawnPoint;     // Where to spawn the car
    public Transform[] waypoints;    // Path this car follows
}

public class CarSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject carPrefab;
    [Tooltip("Time before a new car respawns after the previous one is destroyed.")]
    public float respawnDelay = 1f;

    [Header("Spawn Configurations")]
    public CarSpawnData[] carSpawns; // Each spawn point + path pair

    private GameObject[] activeCars;

    private void Start()
    {
        activeCars = new GameObject[carSpawns.Length];
        // Spawn only one car per spawner entry at the start
        for (int i = 0; i < carSpawns.Length; i++)
        {
            SpawnCarAtIndex(i);
        }
    }

    private void SpawnCarAtIndex(int index)
    {
        var data = carSpawns[index];
        if (data == null || data.spawnPoint == null || data.waypoints == null || data.waypoints.Length == 0)
            return;

        // If there’s already a car active at this spawner, skip spawning
        if (activeCars[index] != null)
            return;

        // Spawn and initialize the car
        GameObject car = Instantiate(carPrefab, data.spawnPoint.position, data.spawnPoint.rotation);
        var moveScript = car.GetComponent<CarWaypointMovement>();
        if (moveScript != null)
        {
            moveScript.waypoints = data.waypoints;
            moveScript.spawner = this;
        }

        activeCars[index] = car;
    }

    public void OnCarDestroyed(CarWaypointMovement car)
    {
        // Called by the car when it reaches the destroy index or is removed
        for (int i = 0; i < activeCars.Length; i++)
        {
            if (activeCars[i] == car.gameObject)
            {
                activeCars[i] = null;
                StartCoroutine(RespawnAfterDelay(i));
                break;
            }
        }
    }

    private IEnumerator RespawnAfterDelay(int index)
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnCarAtIndex(index); // Only spawns if no car exists for that slot
    }
}
