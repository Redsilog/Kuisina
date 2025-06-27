using UnityEngine;
using System.Collections.Generic;

public class IngredientPrefabRegistry : MonoBehaviour
{
    public static IngredientPrefabRegistry Instance { get; private set; }

    [System.Serializable]
    public struct NamedPrefab
    {
        public string name;
        public GameObject prefab;
    }

    public List<NamedPrefab> ingredients = new List<NamedPrefab>();

    private Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (var item in ingredients)
        {
            if (!prefabDict.ContainsKey(item.name))
            {
                prefabDict.Add(item.name, item.prefab);
            }
        }
    }

    public GameObject GetPrefabByName(string name)
    {
        if (prefabDict.TryGetValue(name, out var prefab))
        {
            return prefab;
        }

        return null;
    }
}
