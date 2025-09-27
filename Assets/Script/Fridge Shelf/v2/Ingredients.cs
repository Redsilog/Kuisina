using UnityEngine;

[CreateAssetMenu(fileName = "Ingredient", menuName = "Scriptable Objects/Ingredient")]
public class Ingredients : ScriptableObject
{
    public string ingredientName;
    public Sprite ingredientIcon;
    public GameObject ingredientPrefab;
    [TextArea] public string ingredientDescription;
}
