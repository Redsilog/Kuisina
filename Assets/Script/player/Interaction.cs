using UnityEngine;

public interface Interaction
{
    /// Text to show in the prompt (empty or null hides the UI).
    string GetDescription();

    /// Which key to press for this interaction (e.g. KeyCode.E, KeyCode.Space).
    KeyCode GetKey();

    /// Called when the player presses the interaction key.
    void Interact();
}