using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoveTimerUI : MonoBehaviour
{
    [Header("References")]
    public Stove targetStove;
    public Slider timerSlider;
    public TMP_Text timerText;

    private Image fillImage;

    void Start()
    {
        if (timerSlider != null && timerSlider.fillRect != null)
            fillImage = timerSlider.fillRect.GetComponent<Image>();

        if (timerSlider != null)
            timerSlider.value = 1f;

        SetUIVisible(false);
    }

    void Update()
    {
        if (targetStove == null || fillImage == null)
            return;

        bool hasIngredients = HasIngredients();
        SetUIVisible(hasIngredients);

        if (!hasIngredients)
            return;
            
        //is burned
        if (IsBurned())
        {
            fillImage.fillAmount = 0f;
            SetTimeText("Burned");
            return;
        }

        //dish cooking timer
        if (targetStove.IsCooking)
        {
            float cookProgress = targetStove.GetCookProgress();
            float remaining = Mathf.Clamp01(1f - cookProgress);
            fillImage.fillAmount = remaining;

            SetTimeTextFromProgress(remaining, targetStove.cookTime);
            return;
        }

        //ing cook timer
        float ingredientProgress = targetStove.GetIngredientCookProgress();
        if (ingredientProgress < 1f)
        {
            float remaining = Mathf.Clamp01(1f - ingredientProgress);
            fillImage.fillAmount = remaining;

            SetTimeTextFromProgress(remaining, targetStove.ingredientCookTime);
            return;
        }

        //burn timer
        float burnProgress = targetStove.GetBurnProgress();
        float burnRemaining = Mathf.Clamp01(1f - burnProgress);
        fillImage.fillAmount = burnRemaining;

        SetTimeTextFromProgress(burnRemaining, targetStove.burnTime);
    }

    private void SetTimeTextFromProgress(float normalizedRemaining, float totalTime)
    {
        if (timerText == null)
            return;

        float timeLeft = normalizedRemaining * totalTime;
        timerText.text = timeLeft.ToString("0.0") + "s";
    }

    private void SetTimeText(string text)
    {
        if (timerText != null)
            timerText.text = text;
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

    private void SetUIVisible(bool visible)
    {
        if (timerSlider != null)
            timerSlider.gameObject.SetActive(visible);

        if (timerText != null)
            timerText.gameObject.SetActive(visible);
    }
}
