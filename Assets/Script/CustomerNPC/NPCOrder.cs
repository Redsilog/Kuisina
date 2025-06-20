using UnityEngine;

public class NPCOrder : MonoBehaviour
{
    [Header("Order Data")]
    [Tooltip("Prefabs the NPC can request (not used for terminal test)")]
    public GameObject[] orderPrefabs;
    [Tooltip("Names matching each prefab (must align by index)")]
    public string[] orderNames;

    [Header("Settings")]
    [Tooltip("Key to press when in range")]
    public KeyCode interactKey = KeyCode.E;

    int currentOrder = -1;
    bool playerInRange;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Waiter"))
            playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Waiter"))
            playerInRange = false;
    }

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (currentOrder < 0)
                AskForRandomOrder();
            else
                Debug.Log("NPC: You already have an order. Bring it back!");
        }
    }

    void AskForRandomOrder()
    {
        if (orderNames == null || orderNames.Length == 0)
        {
            Debug.LogWarning("NPCOrderGiver: No orderNames set!");
            return;
        }

        // pick one
        currentOrder = Random.Range(0, orderNames.Length);
        string name = orderNames[currentOrder];

        // log to Unity console / terminal
        Debug.Log($"NPC requests: «{name}»");
        // If you want to see it in a standalone build’s terminal:
        // System.Console.WriteLine($"NPC requests: «{name}»");
    }
}
