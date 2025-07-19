using UnityEngine;

public class PanelOpener : MonoBehaviour
{
    [Tooltip("Drag the UI Panel GameObject here")]
    public GameObject panel;

    bool isPlayerNear = false;

    void Start()
    {
        if (panel != null)
            panel.SetActive(false);  // ensure it's closed at start
    }

    void Update()
    {
        // If player is in range and presses Tab, toggle the panel
        if (isPlayerNear && Input.GetKeyDown(KeyCode.Tab))
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            // optional: auto-close when leaving
            if (panel.activeSelf)
                panel.SetActive(false);
        }
    }
}
