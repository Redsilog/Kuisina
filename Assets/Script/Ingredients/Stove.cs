using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    public void PlaceIngredient(string ingredientName, GameObject ingredientObject)
    {
        if (isCooking) return;

        currentIngredients.Add(ingredientName);
        Destroy(ingredientObject); // remove visual from player
        Debug.Log("Placed ingredient: " + ingredientName);

        CheckCookingStart();
    }

    private void CheckCookingStart()
    {
        if (isCooking) return;

        foreach (var recipe in recipes)
        {
            // Check if all required ingredients are present
            bool allPresent = true;
            foreach (var req in recipe.requiredIngredients)
            {
                if (!currentIngredients.Contains(req))
                {
                    allPresent = false;
                    break;
                }
            }

            if (allPresent)
            {
                Debug.Log($"All ingredients for {recipe.dishName} placed. Starting to cook!");
                StartCoroutine(CookRoutine(recipe));
                break;
            }
        }
    }

    private IEnumerator CookRoutine(Recipe recipe)
    {
        isCooking = true;
        Debug.Log($"Cooking {recipe.dishName}...");

        yield return new WaitForSeconds(5f); // you can customize per recipe if needed

        Debug.Log($"{recipe.dishName} is ready!");

        if (recipe.cookedDishPrefab != null && spawnPoint != null)
        {
            Instantiate(recipe.cookedDishPrefab, spawnPoint.position, spawnPoint.rotation);
        }

        currentIngredients.Clear();
        isCooking = false;
    }
}
