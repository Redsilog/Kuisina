using System.Collections.Generic;
using UnityEngine;

public class NPCInventory : MonoBehaviour
{
    [Tooltip("Where to parent delivered objects for display/storage")]
    public Transform storagePoint;

    // just tracks the names of everything delivered so far
    public List<string> storedItemNames = new List<string>();

    /// <summary>
    /// Call this when the NPC accepts an item from the player.
    /// </summary>
    public void ReceiveItem(string itemName, GameObject itemObject)
    {
        // 1) Track it
        storedItemNames.Add(itemName);

        // 2) If you want the actual object to hang off the NPC:
        if (storagePoint != null && itemObject != null)
        {
            itemObject.transform.SetParent(storagePoint);
            itemObject.transform.localPosition = Vector3.zero;
            itemObject.transform.localRotation = Quaternion.identity;

            // disable physics & collider so it just sits there
            Collider col = itemObject.GetComponent<Collider>();
            if (col) col.enabled = false;
            Rigidbody rb = itemObject.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;
        }
    }
}
