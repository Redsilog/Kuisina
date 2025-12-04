﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Recipe
{
    public string dishName;
    [TextArea] public string description;
    public List<string> requiredIngredients = new List<string>();
    public GameObject cookedDishPrefab;
}

public class Stove : MonoBehaviour
{

    [Header("Cooking Settings")]
    public List<Recipe> recipes = new List<Recipe>();
    [Tooltip("Point where the cooked dish will appear.")]
    public Transform spawnPoint;
    public Transform smokeSpawnPoint;

    [Header("Cooking Duration")]
    public float cookTime = 5f;
    public float ingredientCookTime = 5f;
    private float ingredientCookElapsed;
    private float cookTimerElapsed;
    public bool IsCooking => isCooking;


    [Header("Smoke Timing Settings")]
    [Tooltip("How long the smoke takes to fade in when cooking starts.")]
    public float smokeFadeInTime = 1f;

    [Tooltip("How long the smoke takes to fade out when cooking ends.")]
    public float smokeFadeOutTime = 2f;

    private List<string> currentIngredients = new List<string>();

    [Header("Ingredient Icons")]
    public List<IngredientIcon> ingredientIcons = new List<IngredientIcon>();
    public System.Action<Sprite> OnRecipeMatched;

    [System.Serializable]
    public class IngredientIcon
    {
        public string ingredientName;
        public Sprite icon;
    }

    public Sprite GetIngredientIcon(string ingredientName)
    {
        var match = ingredientIcons.Find(i =>
            i.ingredientName.Equals(ingredientName, System.StringComparison.OrdinalIgnoreCase));
        return match != null ? match.icon : null;
    }
    
    public bool isCooking = false;
    [HideInInspector] public GameObject cookedFood;

    [Header("Visual Effects")]
    public GameObject smokePrefab;
    public Material smokeMaterialNormal;
    public Material smokeMaterialBlack;
    private GameObject activeSmoke;  

    [Header("Cooking Sounds")]
    [SerializeField] private AudioClip whileCookingClip;
    [SerializeField] private AudioClip finishedCookingClip;
    [SerializeField] private AudioClip burnedFoodClip;
    [SerializeField] private float cookingVolume = 0.8f;
    [SerializeField] private float finishedVolume = 1f;
    [SerializeField] private float burnedVolume = 1f;

    [Header("Burn Settings")]
    public float burnTime = 15f;
    private Coroutine burnTimerRoutine;
    private bool isBurned = false;
    private bool isSmokePermanent = false;
    private float burnTimerElapsed;

    public System.Action<List<string>> OnIngredientsChanged;

    private void EnsureCookingSoundPlaying()
    {
        if (SoundFXManager.instance == null) return;

        // Check if this stove already has an active loop
        bool hasActiveLoop = SoundFXManager.instance.HasActiveLoop(transform);

        if (!hasActiveLoop)
        {
            StartCoroutine(SoundFXManager.instance.FadeInLoop(whileCookingClip, transform, cookingVolume, 1f));
        }
    }

    public void PlaceIngredient(string ingredientName, GameObject ingredientObject)
    {
        if (isCooking) return;

        if (isBurned)
        {
            Debug.Log("Stove is burned! Clear it before using again.");
            return;
        }
        EnsureCookingSoundPlaying();

        currentIngredients.Add(ingredientName);
        OnIngredientsChanged?.Invoke(currentIngredients);
        Destroy(ingredientObject);
        Debug.Log("Placed ingredient: " + ingredientName);

        TutorialManager.NotifyTrigger(TutorialManager.TutorialTriggerType.AddIngredient);

        RestartBurnTimer();

        if (activeSmoke == null)
        {
            activeSmoke = Instantiate(smokePrefab, smokeSpawnPoint.position, smokeSpawnPoint.rotation, smokeSpawnPoint);
            var renderer = activeSmoke.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && smokeMaterialNormal != null)
            {
                renderer.material = smokeMaterialNormal;
            }

            var ps = activeSmoke.GetComponent<ParticleSystem>();
            if (ps != null)
                StartCoroutine(FadeInSmoke(ps, smokeFadeInTime));

            isSmokePermanent = true;
        }

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
            
            if (starRating > 0)
            {
                // ✅ Fire the event so the UI updates immediately
                Sprite dishIcon = GetIngredientIcon(matchedRecipe.dishName); 
                OnRecipeMatched?.Invoke(dishIcon);

                StartCoroutine(CookRoutine(matchedRecipe, starRating));

                Debug.Log($"Cooking {matchedRecipe.dishName} with {starRating} stars (wrong order count: {wrongOrderCount})!");
            }
            else
            {
                Debug.Log($"All ingredients are wrong! No dish for {matchedRecipe.dishName}.");
                currentIngredients.Clear();
            }
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

    public void ClearStove()
    {
        if (SoundFXManager.instance != null)
            StartCoroutine(SoundFXManager.instance.FadeOutAndStopLoop(transform, 0.5f));
        currentIngredients.Clear();
        OnIngredientsChanged?.Invoke(currentIngredients);
        isCooking = false;

        if (cookedFood != null)
        {
            Destroy(cookedFood);
            cookedFood = null;
        }

        if (activeSmoke != null)
        {
            Destroy(activeSmoke);
            activeSmoke = null;
        }

        if (burnTimerRoutine != null)
        {
            StopCoroutine(burnTimerRoutine);
            burnTimerRoutine = null;
        }

        isBurned = false;

        Debug.Log("Stove cleared!");
    }
    
    public float GetCookProgress()
    {
        if (!isCooking || cookTime <= 0f) return 0f;
        return Mathf.Clamp01(cookTimerElapsed / cookTime);
    }

    private IEnumerator CookRoutine(Recipe recipe, int stars)
    {
        isCooking = true;
        SoundFXManager.instance.PlayLoopingSound(whileCookingClip, transform, cookingVolume);
        Debug.Log($"Cooking {recipe.dishName}... Please wait.");

        cookTimerElapsed = 0f;
        while (cookTimerElapsed < cookTime)
        {
            cookTimerElapsed += Time.deltaTime;
            yield return null;
        }
        cookTimerElapsed = cookTime;

        if (burnTimerRoutine != null)
        {
            StopCoroutine(burnTimerRoutine);
            burnTimerRoutine = null;
        }

        if (stars > 0)
        {
            StartCoroutine(SoundFXManager.instance.FadeOutAndStopLoop(transform, 0.5f));
            SoundFXManager.instance.PlaySoundFXClip(finishedCookingClip, transform, finishedVolume);
            Debug.Log($"{recipe.dishName} is ready! Satisfaction: {stars} stars");

            // ✅ Instantiate dish only ONCE
            if (recipe.cookedDishPrefab != null && spawnPoint != null)
            {
                cookedFood = Instantiate(recipe.cookedDishPrefab, spawnPoint.position, spawnPoint.rotation);

                if (activeSmoke != null && isSmokePermanent)
                {
                    var ps = activeSmoke.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        ps.Stop(); // stop emission
                        StartCoroutine(FadeOutSmoke(ps, smokeFadeOutTime));
                    }
                    Destroy(activeSmoke, 2f); // delay actual destruction
                    activeSmoke = null;
                    isSmokePermanent = false;
                }

                // ✅ Add reference to prefab data
                // ✅ Add this line after creating DishReference
                var refComp = cookedFood.AddComponent<DishReference>();
                refComp.prefab = recipe.cookedDishPrefab;
                refComp.starsEarned = stars;
                refComp.description = recipe.description;
                Debug.Log(recipe.description);

                // ✅ Parent to stove spawn
                cookedFood.transform.SetParent(spawnPoint, worldPositionStays: true);

                // ✅ Physics cleanup: prevent falling
                if (cookedFood.TryGetComponent<Collider>(out var col))
                    col.isTrigger = true;

                if (cookedFood.TryGetComponent<Rigidbody>(out var rb))
                    rb.isKinematic = true;

                // ✅ Face the same way as stove (optional fine-tune)
                cookedFood.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            }
        }
        else
        {
            Debug.Log($"Cooking failed! No dish produced.");
        }
        if (burnTimerRoutine != null)
        {
            StopCoroutine(burnTimerRoutine);
            burnTimerRoutine = null;
        }

        currentIngredients.Clear();
        OnIngredientsChanged?.Invoke(currentIngredients);
        isCooking = false;
        OnRecipeMatched?.Invoke(null);
    }

    private IEnumerator FadeInSmoke(ParticleSystem ps, float duration)
    {
        var emission = ps.emission;
        float startRate = 0f;
        float targetRate = emission.rateOverTime.constant;
        emission.rateOverTime = startRate;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            emission.rateOverTime = Mathf.Lerp(startRate, targetRate, t);
            yield return null;
        }

        emission.rateOverTime = targetRate;
    }
    private IEnumerator FadeOutSmoke(ParticleSystem ps, float duration)
    {
        var main = ps.main;
        Color startColor = main.startColor.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            main.startColor = Color.Lerp(startColor, targetColor, elapsed / duration);
            yield return null;
        }
    }
    private IEnumerator BurnTimer()
    {
        burnTimerElapsed = 0f;
        Debug.Log("⏱ Ingredient cooking timer started...");

        while (ingredientCookElapsed  < ingredientCookTime)
        {
            ingredientCookElapsed += Time.deltaTime;
            yield return null;
        }

        // Cooking complete
        Debug.Log("✅ Ingredient finished cooking! Starting burn countdown...");
        SoundFXManager.instance.PlaySoundFXClip(finishedCookingClip, transform, finishedVolume);

        burnTimerElapsed = 0f;

        while (burnTimerElapsed < burnTime)
        {
            burnTimerElapsed += Time.deltaTime;
            yield return null;
        }

        burnTimerElapsed = burnTime;

        if (!isBurned)
        {
            BurnIngredients();
            Debug.Log("🔥 Ingredient burned after being cooked too long!");
        }
    }
    private void BurnIngredients()
    {
        isBurned = true;
        StartCoroutine(SoundFXManager.instance.FadeOutAndStopLoop(transform, 0.5f));
        SoundFXManager.instance.PlaySoundFXClip(burnedFoodClip, transform, burnedVolume);

        if (activeSmoke != null)
        {
            StartCoroutine(CrossfadeToBlackSmoke(2f));
        }

        Debug.Log("Ingredients burned! Must be cleared with trash bag.");
        currentIngredients.Clear();
        OnIngredientsChanged?.Invoke(currentIngredients);
        isCooking = false;
    }

    private IEnumerator CrossfadeToBlackSmoke(float duration)
    {
        if (activeSmoke == null) yield break;

        var lightSmoke = activeSmoke.GetComponent<ParticleSystem>();
        var lightRenderer = activeSmoke.GetComponent<ParticleSystemRenderer>();
        if (lightSmoke == null || lightRenderer == null)
            yield break;

        GameObject blackSmoke = Instantiate(smokePrefab, smokeSpawnPoint.position, smokeSpawnPoint.rotation, smokeSpawnPoint);
        var blackRenderer = blackSmoke.GetComponent<ParticleSystemRenderer>();
        var blackPS = blackSmoke.GetComponent<ParticleSystem>();

        if (blackRenderer != null && smokeMaterialBlack != null)
            blackRenderer.material = smokeMaterialBlack;

        var blackMain = blackPS.main;
        Color blackStart = blackRenderer.material.GetColor("_Color");
        blackRenderer.material.SetColor("_Color", new Color(blackStart.r, blackStart.g, blackStart.b, 0f));

        blackPS.Play();

        float elapsed = 0f;
        Material lightMat = lightRenderer.material;
        Color lightStart = lightMat.GetColor("_Color");
        Color lightEnd = new Color(lightStart.r, lightStart.g, lightStart.b, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            lightMat.SetColor("_Color", Color.Lerp(lightStart, lightEnd, t));

            blackRenderer.material.SetColor("_Color", new Color(blackStart.r, blackStart.g, blackStart.b, t));

            yield return null;
        }

        Destroy(activeSmoke);
        activeSmoke = blackSmoke;
    }
    private void RestartBurnTimer()
    {
        if (burnTimerRoutine != null)
            StopCoroutine(burnTimerRoutine);
            
        ingredientCookElapsed = 0f;
        burnTimerElapsed = 0f;

        burnTimerRoutine = StartCoroutine(BurnTimer());
    }
    public float GetIngredientCookProgress()
    {
        return Mathf.Clamp01(ingredientCookElapsed / ingredientCookTime);
    }
    public float GetBurnProgress()
    {
        if (burnTime <= 0f) return 0f;

        return Mathf.Clamp01(burnTimerElapsed / burnTime);
    }
}