using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class StoveIngredientTrackerUI : MonoBehaviour
{
    [Header("References")]
    public Stove targetStove;
    public Transform ingredientContainer;
    public GameObject ingredientSlotPrefab;

    private List<GameObject> activeSlots = new List<GameObject>();

    void OnEnable()
    {
        if (targetStove != null)
        {
            targetStove.OnIngredientsChanged += UpdateIngredientIcons;
            targetStove.OnRecipeMatched += ShowCookingPreview;
        }
    }

    void OnDisable()
    {
        if (targetStove != null)
        {
            targetStove.OnIngredientsChanged -= UpdateIngredientIcons;
            targetStove.OnRecipeMatched -= ShowCookingPreview;
        }
    }

    public void UpdateIngredientIcons(List<string> ingredients)
    {
        // Clear old icons
        foreach (var slot in activeSlots)
            Destroy(slot);
        activeSlots.Clear();

        // Add ingredient icons
        foreach (string ingredient in ingredients)
        {
            GameObject newSlot = Instantiate(ingredientSlotPrefab, ingredientContainer);
            newSlot.transform.localScale = Vector3.one;

            Image img = newSlot.transform.Find("Icon").GetComponent<Image>();
            Sprite icon = targetStove.GetIngredientIcon(ingredient);

            if (img != null && icon != null)
                img.sprite = icon;
            else
                Debug.LogWarning($"No icon found for ingredient: {ingredient}");

            activeSlots.Add(newSlot);
        }
    }

    public void ShowCookingPreview(Sprite dishSprite)
    {
        Debug.Log($"🍳 ShowCookingPreview called! Sprite: {dishSprite?.name ?? "null"}");

        // Clear ingredient slots
        foreach (var slot in activeSlots)
            Destroy(slot);
        activeSlots.Clear();

        // Spawn a single preview slot using the same prefab
        if (dishSprite != null)
        {
            GameObject previewSlot = Instantiate(ingredientSlotPrefab, ingredientContainer);
            previewSlot.transform.localScale = Vector3.one;

            Image img = previewSlot.transform.Find("Icon").GetComponent<Image>();
            if (img != null)
                img.sprite = dishSprite;

            activeSlots.Add(previewSlot);
        }
    }
}