using UnityEngine;

public class PanelOpener : MonoBehaviour
{
    [Tooltip("Drag the UI Panel GameObject here")]
    public GameObject panel;

    private bool player1InRange = false;
    private bool player2InRange = false;

    [SerializeField] AudioClip openBookClip;
    [SerializeField] AudioClip closeBookClip;

    void Start()
    {
        if (panel != null)
            panel.SetActive(false);
    }



    void Update()
    {

        if (player1InRange && Input.GetKeyDown(KeyCode.Space))
        {
            TogglePanel();
        }

        if (player2InRange && Input.GetKeyDown(KeyCode.Return))
        {
            TogglePanel();
        }
    }

    
    private void TogglePanel()
    {
        bool isOpening = !panel.activeSelf;
        panel.SetActive(isOpening);

        if (isOpening)
        {
            SoundFXManager.instance.PlaySoundFXClip(openBookClip, transform, .75f);
        }
        else
        {
            SoundFXManager.instance.PlaySoundFXClip(closeBookClip, transform, .75f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player1InRange = true;
        }
        else if (other.CompareTag("Player2"))
        {
            player2InRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player1InRange = false;
        }
        else if (other.CompareTag("Player2"))
        {
            player2InRange = false;
        }

        if (!player1InRange && !player2InRange && panel.activeSelf)
        {
            panel.SetActive(false);

            if (closeBookClip != null)
                SoundFXManager.instance.PlaySoundFXClip(closeBookClip, transform, 1f);
        }
    }
}
