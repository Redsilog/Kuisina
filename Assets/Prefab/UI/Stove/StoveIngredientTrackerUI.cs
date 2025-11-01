using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StoveIngredientTrackerUI : MonoBehaviour
{
    [Header("References")]
    public Stove targetStove;
    public Transform ingredientContainer;   // The parent object for icons
    public GameObject ingredientSlotPrefab; // The prefab with Image

    private List<GameObject> activeSlots = new List<GameObject>();

    void OnEnable()
    {
        if (targetStove != null)
            targetStove.OnIngredientsChanged += UpdateIngredientIcons;
    }

    void OnDisable()
    {
        if (targetStove != null)
            targetStove.OnIngredientsChanged -= UpdateIngredientIcons;
    }

    public void UpdateIngredientIcons(List<string> ingredients)
    {
        // Clear old icons
        foreach (var slot in activeSlots)
            Destroy(slot);
        activeSlots.Clear();

        // Add new icons
        foreach (string ingredient in ingredients)
        {
            GameObject newSlot = Instantiate(ingredientSlotPrefab, ingredientContainer);
            newSlot.transform.localScale = Vector3.one;
            newSlot.transform.localPosition = Vector3.zero;
            newSlot.transform.localRotation = Quaternion.identity;

            Image img = newSlot.transform.Find("Icon").GetComponent<Image>();

            Sprite icon = targetStove.GetIngredientIcon(ingredient);
            if (img != null && icon != null)
                img.sprite = icon;
            else
                Debug.LogWarning($"No icon found for ingredient: {ingredient}");

            activeSlots.Add(newSlot);
        }
    }

}
