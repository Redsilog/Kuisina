using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Recipe
{
    public string dishName;
    public List<string> requiredIngredients = new List<string>();
    public GameObject cookedDishPrefab;
}

public class Stove : MonoBehaviour
{
    [Header("Cooking Settings")]
    public List<Recipe> recipes = new List<Recipe>();
    [Tooltip("Point where the cooked dish will appear.")]
    public Transform spawnPoint;

    private List<string> currentIngredients = new List<string>();
    private bool isCooking = false;
    [HideInInspector] public GameObject cookedFood;

    public void PlaceIngredient(string ingredientName, GameObject ingredientObject)
    {
        if (isCooking) return;

        currentIngredients.Add(ingredientName);
        Destroy(ingredientObject);
        Debug.Log("Placed ingredient: " + ingredientName);

        CheckCookingStart();
    }

    private void CheckCookingStart()
    {
        if (isCooking) return;

        Recipe matchedRecipe = null;

        // Normalize current ingredients for case-insensitive comparison
        var normalizedCurrent = currentIngredients
            .Select(i => i.ToLower())
            .ToList();

        foreach (var recipe in recipes)
        {
            if (recipe.requiredIngredients.Count != normalizedCurrent.Count)
                continue;

            // Copy list so we can safely remove matches
            var tempList = new List<string>(normalizedCurrent);
            bool allMatch = true;

            foreach (var req in recipe.requiredIngredients)
            {
                string reqLower = req.ToLower();
                if (tempList.Contains(reqLower))
                {
                    tempList.Remove(reqLower); // remove matched occurrence
                }
                else
                {
                    allMatch = false;
                    break;
                }
            }

            // ✅ Only match when all ingredients match exactly (case-insensitive)
            if (allMatch && tempList.Count == 0)
            {
                matchedRecipe = recipe;
                break;
            }
        }

        if (matchedRecipe != null)
        {
            // Step 2: Check order accuracy
            int wrongOrderCount = CountWrongOrder(currentIngredients, matchedRecipe.requiredIngredients);
            int starRating = GetStarRating(wrongOrderCount);

            if (starRating == 0)
            {
                Debug.Log($"All ingredients are wrong! No dish for {matchedRecipe.dishName}.");
                currentIngredients.Clear();
                return;
            }

            Debug.Log($"Cooking {matchedRecipe.dishName} with {starRating} stars (wrong order count: {wrongOrderCount})!");
            StartCoroutine(CookRoutine(matchedRecipe, starRating));
        }
        else
        {
            Debug.Log("No recipe matched these ingredients (checked all).");
            Debug.Log("Current: " + string.Join(", ", currentIngredients));
            foreach (var r in recipes)
            {
                Debug.Log($"Recipe: {r.dishName} requires {string.Join(", ", r.requiredIngredients)}");
            }
        }
    }

    private int CountWrongOrder(List<string> current, List<string> required)
    {
        int wrong = 0;
        for (int i = 0; i < required.Count; i++)
        {
            if (!string.Equals(current[i], required[i], System.StringComparison.OrdinalIgnoreCase))
                wrong++;
        }
        return wrong;
    }

    private int GetStarRating(int wrongOrder)
    {
        if (wrongOrder == 0) return 5;
        if (wrongOrder == 2) return 3;
        if (wrongOrder == 3) return 2;
        if (wrongOrder >= 4) return 1;
        return 0;
    }

    private IEnumerator CookRoutine(Recipe recipe, int stars)
    {
        isCooking = true;
        Debug.Log($"Cooking {recipe.dishName}... Please wait.");

        yield return new WaitForSeconds(5f);

        if (stars > 0)
        {
            Debug.Log($"{recipe.dishName} is ready! Satisfaction: {stars} stars");
            cookedFood = Instantiate(recipe.cookedDishPrefab, spawnPoint.position, recipe.cookedDishPrefab.transform.rotation);

            var refComp = cookedFood.AddComponent<DishReference>();
            refComp.prefab = recipe.cookedDishPrefab;

            cookedFood.transform.SetParent(spawnPoint);

            if (recipe.cookedDishPrefab != null && spawnPoint != null)
            {
                cookedFood = Instantiate(recipe.cookedDishPrefab, spawnPoint.position, spawnPoint.rotation);

                if (cookedFood.TryGetComponent<Collider>(out var col))
                    col.isTrigger = true;
                if (cookedFood.TryGetComponent<Rigidbody>(out var rb))
                    rb.isKinematic = true;
            }
        }
        else
        {
            Debug.Log($"Cooking failed! No dish produced.");
        }

        currentIngredients.Clear();
        isCooking = false;
    }
}
