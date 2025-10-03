using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;


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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fridgePanel.SetActive(false);
        slots = slotsParent.GetComponentsInChildren<FridgeSlot>(true); 
    }
    // Update is called once per frame
    void Update()
    {
        if (fridgePanel.activeSelf && currentFridge != null && currentFridge.storedIngredients.Count > 0)
        {
            if (currentPlayerID == 1)
            {
                if (Input.GetKeyDown(KeyCode.D))
                {
                    globalIndex = (globalIndex + 1) % currentFridge.storedIngredients.Count;
                    AdjustScrollOffset();
                    RefreshSlots();
                }
                else if (Input.GetKeyDown(KeyCode.A))
                {
                    globalIndex = (globalIndex - 1 + currentFridge.storedIngredients.Count) % currentFridge.storedIngredients.Count;
                    AdjustScrollOffset();
                    RefreshSlots();
                }
                else if (Input.GetKeyDown(KeyCode.Space))
                {
                    Debug.Log("SPACE pressed while fridge open");
                    TakeSelectedIngredient();
                    //CloseFridge();
                }
            }
            else if (currentPlayerID == 2)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    globalIndex = (globalIndex + 1) % currentFridge.storedIngredients.Count;
                    AdjustScrollOffset();
                    RefreshSlots();
                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    globalIndex = (globalIndex - 1 + currentFridge.storedIngredients.Count) % currentFridge.storedIngredients.Count;
                    AdjustScrollOffset();
                    RefreshSlots();
                }
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    Debug.Log("ENTER pressed while fridge open");
                    TakeSelectedIngredient();
                    //CloseFridge();
                }
            }
            
        }
    }

    public void OpenFridge(FridgeShelfManager fridge, PlayerInventory playerInv, bool openOnLeft)
    {
        currentFridge = fridge;
        currentPlayer = playerInv;
        currentPlayerID = playerInv.playerID;
        
        fridgePanel.SetActive(true);

        playerMovement movement = currentPlayer.GetComponent<playerMovement>();
        playerMovement2 movement2 = currentPlayer.GetComponent<playerMovement2>();
        if (movement != null) movement.enabled = false;
        if (movement2 != null) movement2.enabled = false;

        if (openOnLeft)
        {
            fridgePanel.transform.SetParent(leftAnchor, false);
        }
        else
        {
            fridgePanel.transform.SetParent(rightAnchor, false);
        }
        
        RefreshSlots();

        HighlightSlot(globalIndex);

        Debug.Log("Fridge opened with " + fridge.storedIngredients.Count + " items.");
    }

    public void CloseFridge()
    {
        fridgePanel.SetActive(false);
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

        currentPlayer.PickUpIngredient(selected.ingredientName, selected.ingredientPrefab);

        CloseFridge();
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
