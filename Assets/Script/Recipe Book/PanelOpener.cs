using UnityEngine;

public class PanelOpener : MonoBehaviour
{
    [Tooltip("Drag your panel GameObject here in the Inspector.")]
    [SerializeField] private GameObject panel;
    public bool isPanelOpen = false;
    private bool playerInRange = false;

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
            else if(playerInRange)
            {
                isPanelOpen = true;
                panel.SetActive(true);

                Debug.Log("Opening Recipe Book");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Player in range, can open recipe book");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            isPanelOpen = false;
            panel.SetActive(false);
            Debug.Log("Player not in range, cannot open recipe book");
        }
    }
}
