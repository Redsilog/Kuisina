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
    private FridgeShelfManager currentFridge;
    private int selectedIndex = 0;
    private List<GameObject> spawnedSlots = new List<GameObject>();

    private PlayerInventory currentPlayer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fridgePanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (fridgePanel.activeSelf && spawnedSlots.Count > 0)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                selectedIndex = (selectedIndex + 1) % spawnedSlots.Count;
                HighlightSlot(selectedIndex);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                selectedIndex = (selectedIndex - 1 + spawnedSlots.Count) % spawnedSlots.Count;
                HighlightSlot(selectedIndex);
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("SPACE pressed while fridge open");
                TakeSelectedIngredient();
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                Debug.Log("SPACE pressed while fridge open");
                TakeSelectedIngredient();
            }
        }
    }

    public void OpenFridge(FridgeShelfManager fridge, PlayerInventory playerInv)
    {
        currentFridge = fridge;
        currentPlayer = playerInv;
        fridgePanel.SetActive(true);

        // clear old slots
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }
        spawnedSlots.Clear();

        // create new slots
        foreach (Ingredients ing in fridge.storedIngredients)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotsParent);

            // Set the child UI elements
            Image icon = slotGO.transform.Find("Icon").GetComponent<Image>();
            TMP_Text nameText = slotGO.transform.Find("Name").GetComponent<TMP_Text>();

            icon.sprite = ing.ingredientIcon;
            nameText.text = ing.ingredientName;

            spawnedSlots.Add(slotGO);
        }
        selectedIndex = 0; // reset selection to first slot
        HighlightSlot(selectedIndex);

        Debug.Log("Fridge opened with " + fridge.storedIngredients.Count + " items.");
    }

    public void CloseFridge()
    {
        fridgePanel.SetActive(false);
        currentFridge = null;
    }

    void TakeSelectedIngredient()
    {
        if (selectedIndex < 0 || selectedIndex >= currentFridge.storedIngredients.Count)
        {
            Debug.LogWarning("Invalid selected index: " + selectedIndex);
            return;
        }

        Ingredients selected = currentFridge.storedIngredients[selectedIndex];
        Debug.Log("Trying to take ingredient: " + selected.ingredientName);

        PlayerInventory playerInv = FindObjectOfType<PlayerInventory>();

        // take ing
        currentPlayer.PickUpIngredient(selected.ingredientName, selected.ingredientPrefab);
        Debug.Log("Picked up " + selected.ingredientName);

        HighlightSlot(selectedIndex);
    }
    void ShowItemDetails(Ingredients ingredient)
    {
        // update right side
        itemPicture.sprite = ingredient.ingredientIcon;
        itemDescription.text = ingredient.ingredientName;
        Debug.Log("Selected: " + ingredient.ingredientName);
    }


    void HighlightSlot(int index)
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            // root background
            Image bg = spawnedSlots[i].transform.Find("Background").GetComponent<Image>();
            if (bg != null)
                bg.color = (i == index) ? Color.yellow : Color.white;
        }

        // update right panel
        Ingredients ing = currentFridge.storedIngredients[index];
        itemPicture.sprite = ing.ingredientIcon;
        itemName.text = ing.ingredientName;
        itemDescription.text = ing.ingredientDescription; 
    }
}
