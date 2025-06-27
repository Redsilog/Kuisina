using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections;

public class PlayerInventory : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> heldIngredient = new NetworkVariable<FixedString64Bytes>();
    public GameObject heldVisual;
    public Transform holdPoint;

    void Update()
    {
        // Optional: visuals synced locally
        if (IsOwner && heldIngredient.Value.ToString() == "" && heldVisual != null)
        {
            Destroy(heldVisual);
        }
    }

    public void PickUpIngredient(string ingredientName)
    {
        if (!IsOwner || heldIngredient.Value.ToString() != "") return;

        heldIngredient.Value = new FixedString64Bytes(ingredientName);
        PickUpIngredientServerRpc(ingredientName);
    }

    [ServerRpc]
    void PickUpIngredientServerRpc(string prefabName)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabName);
        GameObject spawned = Instantiate(prefab, holdPoint.position, Quaternion.identity);

        NetworkObject netObj = spawned.GetComponent<NetworkObject>();
        netObj.Spawn(true); // true = give ownership to client

        StartCoroutine(ParentAfterSpawn(spawned));
    }

    IEnumerator ParentAfterSpawn(GameObject obj)
    {
        // Wait one frame
        yield return null;

        obj.transform.SetParent(holdPoint);
        heldVisual = obj;

        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    public void PlaceIngredient()
    {
        if (!IsOwner || heldIngredient.Value.ToString() == "") return;

        heldIngredient.Value = new FixedString64Bytes("");
        PlaceIngredientServerRpc();
    }

    [ServerRpc]
    void PlaceIngredientServerRpc()
    {
        if (heldVisual != null)
        {
            Destroy(heldVisual);
            heldVisual = null;
        }
    }

    public bool HasIngredient()
    {
        return heldIngredient.Value.ToString() != "";
    }
}
