using UnityEngine;

public class PanelOpener : MonoBehaviour
{
    [Tooltip("Drag your panel GameObject here in the Inspector.")]
    [SerializeField] private GameObject panel;
    public bool isPanelOpen = false;

    [Tooltip("Key used to open the panel.")]
    [SerializeField] private KeyCode openKey = KeyCode.E;

    void Start()
    {
        // Make sure the panel starts closed
        if (panel != null)
            panel.SetActive(false);
    }

    void Update()
    {
        if (panel != null && Input.GetKeyDown(openKey))
        {
            if (isPanelOpen)
            {
                isPanelOpen = false;
                panel.SetActive(false);
                Debug.Log("Closing Recipe Book");
            }
            else
            {
                isPanelOpen = true;
                panel.SetActive(true);
                Debug.Log("Opening Recipe Book");
            }
        }
    }
}
