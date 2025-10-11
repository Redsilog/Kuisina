using UnityEngine;

public class playerPermissions : MonoBehaviour
{
    [Header("Storage Access Permissions")]
    public bool canUseFridge = true;
    public bool canUseFreezer = false;
    public bool canUsePantry = false;
    public bool canUseCondiments = false;

    public bool CanUse(FridgeShelfManager.StorageType type)
    {
        return type switch
        {
            FridgeShelfManager.StorageType.Fridge => canUseFridge,
            FridgeShelfManager.StorageType.Freezer => canUseFreezer,
            FridgeShelfManager.StorageType.Pantry => canUsePantry,
            FridgeShelfManager.StorageType.Condiments => canUseCondiments,
            _ => false
        };
    }
}