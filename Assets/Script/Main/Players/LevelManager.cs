using UnityEngine;
using System;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Stars")]
    public int starsToNextLevel = 5;
    public int currentStars = 0;

    public Action OnStarsReached;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void AddStars(int amount)
    {
        currentStars += amount;
        Debug.Log($"Earned {amount} stars. Total: {currentStars}/{starsToNextLevel}");

        if (currentStars >= starsToNextLevel)
            Debug.Log("Finished Level");
    }

    public void ResetStars()
    {
        currentStars = 0;
    }
}
