using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class Stove : NetworkBehaviour
{
    public Transform cookedFoodPoint;

    public GameObject cookedHotsilog;
    public GameObject cookedTocilog;
    public GameObject cookedTapsilog;

    private List<string> addedIngredients = new();
    private Dictionary<string, List<string>> recipeBook;
    private Dictionary<string, GameObject> cookedPrefabs;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;
    private GameObject currentCookedFood;

    void Start()
    {
        recipeBook = new()
        {
            { "Hotsilog", new List<string> { "Hotdog", "Sinangag", "Itlog" } },
            { "Tocilog", new List<string> { "Tocino", "Sinangag", "Itlog" } },
            { "Tapsilog", new List<string> { "Tapa", "Sinangag", "Itlog" } }
        };

        cookedPrefabs = new()
        {
            { "Hotsilog", cookedHotsilog },
            { "Tocilog", cookedTocilog },
            { "Tapsilog", cookedTapsilog }
        };
    }

    void Update()
    {
        if (!playerInRange || playerInventory == null || !playerInventory.IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Space) && playerInventory.HasIngredient())
        {
            AddIngredientServerRpc(playerInventory.NetworkObjectId);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ResetStoveServerRpc();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ClearCookedFoodServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void AddIngredientServerRpc(ulong playerId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out var netObj)) return;

        PlayerInventory player = netObj.GetComponent<PlayerInventory>();
        if (player == null || !player.HasIngredient()) return;

        addedIngredients.Add(player.heldIngredient.Value.ToString());
        player.PlaceIngredient();
        CheckRecipes();
    }

    void CheckRecipes()
    {
        foreach (var recipe in recipeBook)
        {
            var expected = recipe.Value.OrderBy(i => i).ToList();
            var actual = addedIngredients.OrderBy(i => i).ToList();

            if (expected.SequenceEqual(actual))
            {
                if (cookedPrefabs.TryGetValue(recipe.Key, out GameObject prefab))
                {
                    GameObject cooked = Instantiate(prefab, cookedFoodPoint.position, cookedFoodPoint.rotation);
                    cooked.GetComponent<NetworkObject>().Spawn();
                    currentCookedFood = cooked;
                }

                addedIngredients.Clear();
                Debug.Log("Cooked " + recipe.Key);
                return;
            }
        }

        if (addedIngredients.Count >= 3)
        {
            Debug.Log("Invalid recipe");
            addedIngredients.Clear();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void ResetStoveServerRpc()
    {
        addedIngredients.Clear();
        Debug.Log("Stove reset");
    }

    [ServerRpc(RequireOwnership = false)]
    void ClearCookedFoodServerRpc()
    {
        if (currentCookedFood != null)
        {
            Destroy(currentCookedFood);
            currentCookedFood = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInventory = other.GetComponent<PlayerInventory>();
            if (playerInventory != null) playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInventory = null;
            playerInRange = false;
        }
    }
}
