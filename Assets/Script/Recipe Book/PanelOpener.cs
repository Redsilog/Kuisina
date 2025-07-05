using UnityEngine;

public class PanelOpener : MonoBehaviour
{
    [Tooltip("Drag your panel GameObject here in the Inspector.")]
    [SerializeField] private GameObject panel;

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
            panel.SetActive(true);
        }
    }
}
