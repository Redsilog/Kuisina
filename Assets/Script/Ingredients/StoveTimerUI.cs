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
    private Image fillImage;

    void Start()
    {
        if (targetStove != null)
        {
            burnTime = targetStove.burnTime;
            if (timerText != null) timerText.text = "";
        }

        // Cache the image from the fill area
        if (timerSlider != null && timerSlider.fillRect != null)
            fillImage = timerSlider.fillRect.GetComponent<Image>();

        // Make sure slider doesn’t mess with our image
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

        bool stoveActive = targetStove.gameObject.activeSelf && !targetStove.isCooking && targetStove.burnTime > 0;

        if (IsBurned())
        {
            fillImage.fillAmount = 0f;
            return;
        }

        if (targetStove.IsCooking)
        {
            float cookProgress = targetStove.GetCookProgress();
            float remaining = Mathf.Clamp01(1f - cookProgress);
            fillImage.fillAmount = remaining;

            return;
        }

        if (stoveActive)
        {
            float progress = targetStove.GetBurnProgress();
            float remaining = Mathf.Clamp01(1f - progress);

            fillImage.fillAmount = remaining;
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
        if (fillImage != null)
            fillImage.fillAmount = 1f;

        if (timerText != null)
            timerText.text = "";
    }

    private void SetUIVisible(bool visible)
    {
        if (timerSlider != null)
            timerSlider.gameObject.SetActive(visible);

        if (timerText != null)
            timerText.gameObject.SetActive(visible);
    }
}
