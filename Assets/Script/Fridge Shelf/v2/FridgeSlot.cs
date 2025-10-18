using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FridgeSlot : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;
    public Image highlight;

    public void SetIngredient(Ingredients ing)
    {

        if (ing == null)
        {
            Debug.LogError($"❌ Ingredient is null in {name}");
            return;
        }
        if (icon == null)
        {
            Debug.LogError($"❌ Icon Image reference missing in {name}");
            return;
        }
        icon.sprite = ing.ingredientIcon;
        nameText.text = ing.ingredientName;
        icon.enabled = true;
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        icon.sprite = null;
        nameText.text = "";
        icon.color = Color.white;
    }

    public void SetHighlight(bool active)
    {
        highlight.enabled = active;
        highlight.color = active ? Color.yellow : Color.white;
    }
}