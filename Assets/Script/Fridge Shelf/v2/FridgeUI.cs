using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;


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

    [Header("Input Settings")]
    public int playerID = 1;
    private Controls controls;
    private InputActionMap activeMap;

    public bool isOpen = false;

    //AUDIO
    [SerializeField] AudioClip openFridgeClip;
    [SerializeField] AudioClip closeFridgeClip;

    [SerializeField] AudioClip openFreezerClip;
    [SerializeField] AudioClip closeFreezerClip;

    [SerializeField] AudioClip openPantryClip;
    [SerializeField] AudioClip closePantryClip;

    [SerializeField] AudioClip openCondimentsClip;
    [SerializeField] AudioClip closeCondimentsClip;

    [SerializeField] AudioClip getItemClip;
    [SerializeField] AudioClip selectItemClip;

    [Header("Animation")]
    public Animator fridgeAnimator;
    public Animator fridgeAnimator2;
    private PlayerController player;

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
        activeMap = (playerID == 1)
            ? controls.FridgeFreezerPantry1
            : controls.FridgeFreezerPantry2;

        activeMap.Enable();

        activeMap.FindAction("Left").performed += OnMoveLeft;
        activeMap.FindAction("Right").performed += OnMoveRight;
        activeMap.FindAction("Select").performed += OnSelect;
        activeMap.FindAction("Back").performed += OnBack;
    }

    private void OnDisable()
    {
        if (activeMap == null) return;

        activeMap.FindAction("Left").performed -= OnMoveLeft;
        activeMap.FindAction("Right").performed -= OnMoveRight;
        activeMap.FindAction("Select").performed -= OnSelect;
        activeMap.FindAction("Back").performed -= OnBack;

        activeMap.Disable();
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

        Animator activeAnimator = currentPlayerID == 1 ? fridgeAnimator : fridgeAnimator2;
        if (activeAnimator != null)
            activeAnimator.SetBool("isOpen", true);
        else
            Debug.LogWarning($"No animator assigned for Player {currentPlayerID}");

        isOpen = true;

        //eto yung di nagpapagalaw sa player pag open fridge
        PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
        if (playerController != null)
            playerController.enabled = false;

        Rigidbody rb = currentPlayer.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        // if (!controls.FridgeFreezerPantry.enabled)
        // {
        //     controls.FridgeFreezerPantry.Enable();
        //     Debug.Log("Fridge controls enabled");
        // }

        switch (type)
        {
            case FridgeShelfManager.StorageType.Fridge:
                PlayClipSafe(openFridgeClip, "openFridgeClip", type);
                break;

            case FridgeShelfManager.StorageType.Freezer:
                PlayClipSafe(openFreezerClip, "openFreezerClip", type);
                break;

            case FridgeShelfManager.StorageType.Pantry:
                PlayClipSafe(openPantryClip, "openPantryClip", type);
                break;

            case FridgeShelfManager.StorageType.Condiments:
                PlayClipSafe(openCondimentsClip, "openCondimentsClip", type);
                break;

            default:
                Debug.LogWarning($"[FridgeUI] Unknown storage type: {type}");
                break;
        }

        if (TutorialManager.IsTutorialActive)
        {
            switch (type)
            {
                case FridgeShelfManager.StorageType.Fridge:
                    TutorialManager.NotifyTrigger(TutorialManager.TutorialTriggerType.OpenFridge);
                    break;
                case FridgeShelfManager.StorageType.Freezer:
                    TutorialManager.NotifyTrigger(TutorialManager.TutorialTriggerType.OpenFreezer);
                    break;
                case FridgeShelfManager.StorageType.Pantry:
                    TutorialManager.NotifyTrigger(TutorialManager.TutorialTriggerType.OpenPantry);
                    break;
                case FridgeShelfManager.StorageType.Condiments:
                    TutorialManager.NotifyTrigger(TutorialManager.TutorialTriggerType.OpenCondiments);
                    break;
            }
        }

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
        if (!isOpen) return;

        Animator activeAnimator = currentPlayerID == 1 ? fridgeAnimator : fridgeAnimator2;
        if (activeAnimator != null)
            activeAnimator.SetBool("isOpen", false);
        else
            Debug.LogWarning($"No animator assigned for Player {currentPlayerID}");

        isOpen = false;
        
        //nagpapagalaw ulit 
        if (currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
                playerController.enabled = true;
        }

        // if (controls.FridgeFreezerPantry.enabled)
        // {
        //     controls.FridgeFreezerPantry.Disable();
        //     Debug.Log("Fridge controls disabled");
        // }

        switch (type)
        {
            case FridgeShelfManager.StorageType.Fridge:
                PlayClipSafe(closeFridgeClip, "closeFridgeClip", type);
                break;

            case FridgeShelfManager.StorageType.Freezer:
                PlayClipSafe(closeFreezerClip, "closeFreezerClip", type);
                break;

            case FridgeShelfManager.StorageType.Pantry:
                PlayClipSafe(closePantryClip, "closePantryClip", type);
                break;

            case FridgeShelfManager.StorageType.Condiments:
                PlayClipSafe(closeCondimentsClip, "closeCondimentsClip", type);
                break;

            default:
                Debug.LogWarning($"[FridgeUI] Unknown storage type: {type}");
                break;
        }

        StartCoroutine(HideAfterAnimation(type));

        currentFridge = null;
        currentPlayer = null;
    }


    private void PlayClipSafe(AudioClip clip, string clipName, FridgeShelfManager.StorageType type)
    {
        if (clip == null)
        {
            Debug.LogWarning($"[FridgeUI] {clipName} is missing for {type} on {gameObject.name}");
            return;
        }

        if (SoundFXManager.instance == null)
        {
            Debug.LogError("[FridgeUI] SoundFXManager.instance is NULL!");
            return;
        }

        SoundFXManager.instance.PlaySoundFXClip(clip, transform, 1f);
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

    private IEnumerator HideAfterAnimation(FridgeShelfManager.StorageType type)
    {
        yield return new WaitForSeconds(.5f);
        fridgePanel.SetActive(false);

        currentFridge = null;
        currentPlayer = null;
    }

}

