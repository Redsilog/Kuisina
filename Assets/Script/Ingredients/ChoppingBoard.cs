using UnityEngine;
using UnityEngine.UI;

public class ChoppingBoard : MonoBehaviour
{
    public Transform chopPoint;
    public float chopTime = 3f; // Total "chop points" required

    [Header("Chopped Prefabs")]
    public GameObject choppedGarlicPrefab;
    public GameObject choppedOnionPrefab;
    public GameObject choppedPorkPrefab;
    public GameObject choppedChickenPrefab;
    public GameObject choppedBeefPrefab;

    public Slider choppingProgressBar;

    private bool playerInRange = false;
    private PlayerInventory playerInventory;
    private MonoBehaviour playerMovementScript; // Works for both Player1 and Player2
    private Animator playerAnimator;
    private KeyCode interactKey = KeyCode.Space;

    private bool isChopping = false;
    private bool isChopped = false;
    private string choppedIngredientName = "";
    private GameObject foodOnBoard;

    private float choppingProgress = 0f; // Current progress
    private float chopIncrement = 0.2f;  // Amount added per tap (adjust for difficulty)

    //AUDIO
    [SerializeField] private AudioClip choppingClip;

    void Start()
    {
        if (choppingProgressBar != null)
            choppingProgressBar.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange) return;

        // Start chopping or pick up chopped ingredient
        if (Input.GetKeyDown(interactKey))
        {
            if (!isChopping && !isChopped && playerInventory.HasIngredient())
            {
                StartChopping(playerInventory.heldIngredient, playerInventory.heldVisual);
                playerInventory.PlaceIngredient();
            }
            else if (isChopped && !playerInventory.HasIngredient())
            {
                PickUpChoppedIngredient();
            }

            // Increment chopping progress per tap
            if (isChopping)
            {
                choppingProgress += chopIncrement;
                SoundFXManager.instance.PlaySoundFXClip(choppingClip, transform, 1f);

                // Show progress bar
                if (choppingProgressBar != null)
                {
                    choppingProgressBar.gameObject.SetActive(true);
                    choppingProgressBar.value = choppingProgress / chopTime;
                }

                // Set chopping animation
                if (playerAnimator != null)
                    playerAnimator.SetBool("IsChopping", true);

                // Finish chopping if progress complete
                if (choppingProgress >= chopTime)
                    FinishChopping();
            }
        }

        // Stop animation if not actively tapping
        if (isChopping && !Input.GetKeyDown(interactKey))
        {
            if (playerAnimator != null)
                playerAnimator.SetBool("IsChopping", false);
        }
    }

    void StartChopping(string ingredient, GameObject rawVisual)
    {
        isChopping = true;
        choppingProgress = 0f;
        choppedIngredientName = "Chopped " + ingredient;

        // Disable player movement
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        foodOnBoard = Instantiate(rawVisual, chopPoint.position, chopPoint.rotation);
    }

    void FinishChopping()
    {
        isChopping = false;
        choppingProgress = 0f;

        // Enable player movement
        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        // Stop chopping animation
        if (playerAnimator != null)
            playerAnimator.SetBool("IsChopping", false);

        if (choppingProgressBar != null)
        {
            choppingProgressBar.value = 0;
            choppingProgressBar.gameObject.SetActive(false);
        }

        if (!string.IsNullOrEmpty(choppedIngredientName))
        {
            Destroy(foodOnBoard);
            GameObject choppedVisual = GetChoppedVisual(choppedIngredientName);
            if (choppedVisual != null)
            {
                foodOnBoard = Instantiate(choppedVisual, chopPoint.position, chopPoint.rotation);
                isChopped = true;
            }
        }
    }

    void PickUpChoppedIngredient()
    {
        if (foodOnBoard != null)
            Destroy(foodOnBoard);

        GameObject choppedVisual = GetChoppedVisual(choppedIngredientName);
        if (choppedVisual != null)
            playerInventory.PickUpIngredient(choppedIngredientName, choppedVisual);

        isChopped = false;
        choppedIngredientName = "";
    }

    GameObject GetChoppedVisual(string name)
    {
        switch (name)
        {
            case "Chopped Garlic": return choppedGarlicPrefab;
            case "Chopped Onion": return choppedOnionPrefab;
            case "Chopped Pork": return choppedPorkPrefab;
            case "Chopped Chicken": return choppedChickenPrefab;
            case "Chopped Beef": return choppedBeefPrefab;
            default: return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            playerAnimator = other.GetComponent<Animator>();

            if (other.CompareTag("Player"))
            {
                interactKey = KeyCode.Space;
                playerMovementScript = other.GetComponent<playerMovement>();
            }
            else if (other.CompareTag("Player2"))
            {
                interactKey = KeyCode.Return;
                playerMovementScript = other.GetComponent<playerMovement2>();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Player2"))
        {
            playerInRange = false;
            playerInventory = null;
            playerMovementScript = null;

            if (playerAnimator != null)
                playerAnimator.SetBool("IsChopping", false);

            playerAnimator = null;
        }
    }
}
