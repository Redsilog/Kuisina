using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class OutlineHighlighter : MonoBehaviour
{
    [Header("Player-specific outline materials")]
    public Material player1Outline;
    public Material player2Outline;

    private Renderer rend;
    private Material[] baseMaterials;
    private HashSet<string> activePlayers = new HashSet<string>();

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
            baseMaterials = rend.materials;
    }

    public void SetHighlight(bool state, string playerTag)
    {
        if (rend == null) return;

        if (state)
        {
            if (!activePlayers.Contains(playerTag))
                activePlayers.Add(playerTag);
        }
        else
        {
            activePlayers.Remove(playerTag);
        }

        ApplyHighlight();
    }

    private void ApplyHighlight()
    {
        List<Material> newMats = new List<Material>(baseMaterials);

        // Add each player's outline material if they're in range
        foreach (var tag in activePlayers)
        {
            if (tag == "Player" && player1Outline != null && !newMats.Contains(player1Outline))
                newMats.Add(player1Outline);
            else if (tag == "Player2" && player2Outline != null && !newMats.Contains(player2Outline))
                newMats.Add(player2Outline);
        }

        rend.materials = newMats.ToArray();
    }
}