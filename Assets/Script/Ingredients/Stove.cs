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

        foreach (var recipe in recipes)
        {
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

        yield return new WaitForSeconds(5f);

        Debug.Log($"{recipe.dishName} is ready!");

        if (recipe.cookedDishPrefab != null && spawnPoint != null)
        {
            cookedFood = Instantiate(recipe.cookedDishPrefab, spawnPoint.position, recipe.cookedDishPrefab.transform.rotation);

            var refComp = cookedFood.AddComponent<DishReference>();
            refComp.prefab = recipe.cookedDishPrefab;

            cookedFood.transform.SetParent(spawnPoint);

            if (cookedFood.TryGetComponent<Collider>(out var col))
                col.isTrigger = true;
            if (cookedFood.TryGetComponent<Rigidbody>(out var rb))
                rb.isKinematic = true;
        }

        currentIngredients.Clear();
        isCooking = false;
    }
}
