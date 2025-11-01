using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoveTimerUI : MonoBehaviour
{
    [Header("References")]
    public Stove targetStove;         
    public Slider timerSlider;        
    public TMP_Text timerText;        

    private float burnTime;
    private float elapsed;

    void Start()
    {
        if (targetStove != null)
        {
            burnTime = targetStove.burnTime;
            timerSlider.value = 1f;
            if (timerText != null) timerText.text = "";
        }

        // Hide UI elements at start
        SetUIVisible(false);
    }

    void Update()
    {
        if (targetStove == null || timerSlider == null)
            return;

        bool hasIngredients = HasIngredients();
        SetUIVisible(hasIngredients); // 🔥 toggle visibility, not GameObject

        if (!hasIngredients)
        {
            elapsed = 0f;
            return;
        }

        bool stoveActive = targetStove.gameObject.activeSelf && !targetStove.isCooking && targetStove.burnTime > 0;

        if (IsBurned())
        {
            SetSliderColor(Color.red);
            timerSlider.value = 0f;
            if (timerText != null)
                timerText.text = "🔥 Burned!";
            return;
        }

        if (targetStove.isCooking)
        {
            ResetUI();
            return;
        }

        if (stoveActive)
        {
            elapsed += Time.deltaTime;
            float remaining = Mathf.Clamp01(1f - (elapsed / burnTime));
            timerSlider.value = remaining;

            if (timerText != null)
                timerText.text = $"{(burnTime - elapsed):0.0}s";

            SetSliderColor(Color.Lerp(Color.red, Color.green, remaining));
        }
    }

    private bool IsBurned()
    {
        var burnedField = targetStove.GetType().GetField("isBurned",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return burnedField != null && (bool)burnedField.GetValue(targetStove);
    }

    private bool HasIngredients()
    {
        var ingredientsField = targetStove.GetType().GetField("currentIngredients",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (ingredientsField != null)
        {
            var list = ingredientsField.GetValue(targetStove) as System.Collections.ICollection;
            return list != null && list.Count > 0;
        }
        return false;
    }

    private void ResetUI()
    {
        elapsed = 0f;
        timerSlider.value = 1f;
        SetSliderColor(Color.green);
        if (timerText != null)
            timerText.text = "";
    }

    private void SetSliderColor(Color color)
    {
        if (timerSlider.fillRect != null)
        {
            var fill = timerSlider.fillRect.GetComponent<Image>();
            if (fill != null)
                fill.color = color;
        }
    }

    private void SetUIVisible(bool visible)
    {
        if (timerSlider != null)
            timerSlider.gameObject.SetActive(visible);

        if (timerText != null)
            timerText.gameObject.SetActive(visible);
    }
}
