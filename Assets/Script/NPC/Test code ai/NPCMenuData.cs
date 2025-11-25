using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Kusina/NPC Menu", fileName = "NPCMenu")]
public class NPCMenuData : ScriptableObject
{
    [Header("Single Items")]
    public List<GameObject> singleItems = new List<GameObject>();   // ?? IMPORTANT: new List<>

    [Header("Prefab Combos")]
    public List<OrderCombo> prefabCombos = new List<OrderCombo>();  // ?? also initialised
}

[Serializable]
public class OrderCombo
{
    public string displayName;
    public List<GameObject> items = new List<GameObject>();
}
