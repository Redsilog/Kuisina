using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;


public class FridgeUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject fridgePanel; // Background
    public Transform slotsParent;  // "Slots" under Left
    public GameObject slotPrefab;  // Prefab for slot buttons

    [Header("Right Panel")]
    public Image itemPicture;
    public TMP_Text itemName;  
    public TMP_Text itemDescription;

    public RectTransform leftAnchor;
    public RectTransform rightAnchor;

    private FridgeShelfManager currentFridge;
    private List<GameObject> spawnedSlots = new List<GameObject>();

    private PlayerInventory currentPlayer;

    private int currentPlayerID;

    private FridgeSlot[] slots;
    private int scrollOffset = 0;
    private int globalIndex = 0;

    private FridgeShelfManager.StorageType currentStorageType;

    
    private Controls controls;
    private bool isOpen = false;

    //AUDIO
    [SerializeField] AudioClip openFridgeClip;
    [SerializeField] AudioClip closeFridgeClip;
    [SerializeField] AudioClip openPantryClip;
    [SerializeField] AudioClip closePantryClip;
    [SerializeField] AudioClip getItemClip;
    [SerializeField] AudioClip selectItemClip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created


    void Awake()
    {
        controls = new Controls();
    }

    void Start()
    {
        fridgePanel.SetActive(false);
        slots = slotsParent.GetComponentsInChildren<FridgeSlot>(true);
    }

    private void OnEnable()
    {
        controls.FridgeFreezerPantry.Left.performed += OnMoveLeft;
        controls.FridgeFreezerPantry.Right.performed += OnMoveRight;
        controls.FridgeFreezerPantry.Select.performed += OnSelect;
        controls.FridgeFreezerPantry.Back.performed += OnBack;
    }

    private void OnDisable()
    {
        controls.FridgeFreezerPantry.Left.performed -= OnMoveLeft;
        controls.FridgeFreezerPantry.Right.performed -= OnMoveRight;
        controls.FridgeFreezerPantry.Select.performed -= OnSelect;
        controls.FridgeFreezerPantry.Back.performed -= OnBack;
    }

    private void OnBack(InputAction.CallbackContext ctx)
    {
        if (!isOpen || currentFridge == null) return;
        CloseFridge(currentStorageType);
    }

    private void OnMoveRight(InputAction.CallbackContext ctx)
    {
        if (!isOpen || currentFridge == null) return;

        globalIndex = (globalIndex + 1) % currentFridge.storedIngredients.Count;
        SoundFXManager.instance.PlaySoundFXClip(selectItemClip, transform, .3f);
        AdjustScrollOffset();
        RefreshSlots();
    }

    private void OnMoveLeft(InputAction.CallbackContext ctx)
    {
        if (!isOpen || currentFridge == null) return;

        globalIndex = (globalIndex - 1 + currentFridge.storedIngredients.Count) % currentFridge.storedIngredients.Count;
        SoundFXManager.instance.PlaySoundFXClip(selectItemClip, transform, .3f);
        AdjustScrollOffset();
        RefreshSlots();
    }


    private void OnSelect(InputAction.CallbackContext ctx)
    {
        if (!isOpen || currentFridge == null) return;
        TakeSelectedIngredient();
    }

    public void OpenFridge(FridgeShelfManager fridge, PlayerInventory playerInv, bool openOnLeft, FridgeShelfManager.StorageType type)
    {

        if (isOpen && currentFridge != null)
        {
            CloseFridge(currentStorageType);
        }
        currentFridge = fridge;
        currentPlayer = playerInv;
        currentPlayerID = playerInv.playerID;
        currentStorageType = type;
        globalIndex = 0;
        scrollOffset = 0;

        foreach (var slot in slots)
        {
            slot.Clear();
        }

        fridgePanel.SetActive(true);
        isOpen = true;

        if (!controls.FridgeFreezerPantry.enabled)
        {
            controls.Gameplay.Disable();
            controls.FridgeFreezerPantry.Enable();
        }

        if (type == FridgeShelfManager.StorageType.Fridge)
            SoundFXManager.instance.PlaySoundFXClip(openFridgeClip, transform, 1f);
        else
            SoundFXManager.instance.PlaySoundFXClip(openPantryClip, transform, 1f);

        RefreshSlots();
        HighlightSlot(globalIndex);

        if (currentFridge != null && currentFridge.storedIngredients.Count > 0)
        {
            Ingredients firstItem = currentFridge.storedIngredients[globalIndex];
            itemPicture.sprite = firstItem.ingredientIcon;
            itemName.text = firstItem.ingredientName;
            itemDescription.text = firstItem.ingredientDescription;
        }
        else
        {
            itemPicture.sprite = null;
            itemName.text = "";
            itemDescription.text = "";
        }
    }

    public void CloseFridge(FridgeShelfManager.StorageType type)
    {
        fridgePanel.SetActive(false);
        isOpen = false;
        
        if (!controls.Gameplay.enabled)
        {
            controls.FridgeFreezerPantry.Disable();
            controls.Gameplay.Enable();
        }

        if (type == FridgeShelfManager.StorageType.Fridge)
            SoundFXManager.instance.PlaySoundFXClip(closeFridgeClip, transform, 1f);
        else
            SoundFXManager.instance.PlaySoundFXClip(closePantryClip, transform, 1f);

        if (currentPlayer != null)
        {
            playerMovement movement = currentPlayer.GetComponent<playerMovement>();
            if (movement != null) movement.enabled = true;
            playerMovement2 movement2 = currentPlayer.GetComponent<playerMovement2>();
            if (movement2 != null) movement2.enabled = true;
        }

        currentFridge.SetCooldown(0.25f);
        currentFridge = null;
        currentPlayer = null;
    }

    void AdjustScrollOffset()
    {
        //shift window left
        if (globalIndex < scrollOffset)
            scrollOffset = globalIndex;

        //shift window right
        if (globalIndex >= scrollOffset + slots.Length)
            scrollOffset = globalIndex - slots.Length + 1;
    }

    public void RefreshSlots()
    {
        foreach (var slot in slots)
            slot.Clear();

        for (int i = 0; i < slots.Length; i++)
        {
            int fridgeIndex = i + scrollOffset; // map slot to fridge item
            if (fridgeIndex < currentFridge.storedIngredients.Count)
            {
                slots[i].SetIngredient(currentFridge.storedIngredients[fridgeIndex]);
            }
        }

        HighlightSlot(globalIndex);
    }

    void TakeSelectedIngredient()
    {
        if (globalIndex < 0 || globalIndex >= currentFridge.storedIngredients.Count)
            return;

        Ingredients selected = currentFridge.storedIngredients[globalIndex];
        Debug.Log("Picked up " + selected.ingredientName);

        SoundFXManager.instance.PlaySoundFXClip(getItemClip, transform, .75f);

        currentPlayer.PickUpIngredient(selected.ingredientName, selected.ingredientPrefab);

        CloseFridge(currentStorageType);
    }

    void HighlightSlot(int fridgeIndex)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int mappedIndex = scrollOffset + i;
            slots[i].SetHighlight(mappedIndex == fridgeIndex);
        }

        if (fridgeIndex < currentFridge.storedIngredients.Count)
        {
            Ingredients ing = currentFridge.storedIngredients[fridgeIndex];
            itemPicture.sprite = ing.ingredientIcon;
            itemName.text = ing.ingredientName;
            itemDescription.text = ing.ingredientDescription;
        }
        else
        {
            itemPicture.sprite = null;
            itemName.text = "";
            itemDescription.text = "";
        }
    }
}
