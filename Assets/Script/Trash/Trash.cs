using UnityEngine;

public class Trash : MonoBehaviour
{
    private float trashCooldown = 0f;
    [SerializeField] AudioClip trashClip;

    private OutlineHighlighter highlighter;
    private bool player1InRange = false;
    private bool player2InRange = false;

    private void Awake()
    {
        highlighter = GetComponent<OutlineHighlighter>();
        if (highlighter == null)
            highlighter = GetComponentInParent<OutlineHighlighter>();
    }

    private void Update()
    {
        if (trashCooldown > 0f)
            trashCooldown -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player1InRange = true;
            highlighter?.SetHighlight(true, "Player");
        }
        else if (other.CompareTag("Player2"))
        {
            player2InRange = true;
            highlighter?.SetHighlight(true, "Player2");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player1InRange = false;
            highlighter?.SetHighlight(false, "Player");
        }
        else if (other.CompareTag("Player2"))
        {
            player2InRange = false;
            highlighter?.SetHighlight(false, "Player2");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        PlayerInventory player = other.GetComponent<PlayerInventory>();
        if (player == null) return;

        if (!player.IsHoldingItem()) return;

        if (trashCooldown > 0f) return;

        bool pressed = false;

        // Player 1 uses Space
        if (player.playerID == 1 && Input.GetKey(KeyCode.Space))
        {
            SoundFXManager.instance.PlaySoundFXClip(trashClip, transform, 1f);
            pressed = true;
        }

        // Player 2 uses Return (Enter)
        else if (player.playerID == 2 && Input.GetKey(KeyCode.Return))
        {
            SoundFXManager.instance.PlaySoundFXClip(trashClip, transform, 1f);
            pressed = true;
        }

        if (!pressed) return;

        string itemName = player.heldIngredient != "" ? player.heldIngredient : player.heldDish;
        Debug.Log($"Player {player.playerID} trashed: {itemName}");

        Destroy(player.heldVisual);

        player.heldIngredient = "";
        player.heldDish = "";
        player.heldVisual = null;

        trashCooldown = 0.5f;
    }
}