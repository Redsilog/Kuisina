using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; 

public class TutorialManager : MonoBehaviour
{
    public enum TutorialTriggerType
    {
        None,
        WaitForAction,

        OpenRecipeBook,
        OpenFridge,
        OpenFreezer,
        OpenPantry,
        OpenCondiments,

        InteractCustomer,
        AddIngredient,  
        ChopIngredient,
        CookDish,
        ServeDish
    }

    [System.Serializable]
    public class DialogueStep
    {
        [TextArea] public string text;
        public float waitTimeAfter = 1f;
        public bool requiresInput;
        public bool lockMovement;
        public bool pauseDuringStep = true;

        public TutorialTriggerType triggerType = TutorialTriggerType.None;
    }

    [Header("Dialogue Settings")]
    public TextMeshProUGUI guideText;
    public GameObject guidePanel;
    public DialogueStep[] dialogueSteps;

    [Header("Player")]
    public GameObject player1;
    public GameObject player2;

    [Header("Behaviour Settings")]
    public float startDelay = 2.5f;

    private int currentStep = 0;
    private bool isPlaying = false;
    private float previousTimeScale = 1f;

    public static bool IsTutorialActive { get; private set; } = false;

    private static TutorialTriggerType latestTrigger = TutorialTriggerType.None;

    [Header("NPC Spawner")]
    public NPCSpawner1 npcSpawner;

    private PlayerInput input1;
    private PlayerInput input2;
    private bool player1ContinuePressed = false;
    private bool player2ContinuePressed = false;

    public static bool IsInteractionLocked { get; private set; } = false;

    public static void NotifyTrigger(TutorialTriggerType type)
    {
        latestTrigger = type;
    }


    void Start()
    {
        if (player1 != null) input1 = player1.GetComponent<PlayerInput>();
        if (player2 != null) input2 = player2.GetComponent<PlayerInput>();

        if (input1 != null)
            input1.actions["Interact"].performed += OnPlayer1Continue;

        if (input2 != null)
            input2.actions["Interact"].performed += OnPlayer2Continue;

        StartCoroutine(StartAfterDelayRealtime(startDelay));
    }
    void OnDestroy()
    {
        if (input1 != null)
            input1.actions["Interact"].performed -= OnPlayer1Continue;

        if (input2 != null)
            input2.actions["Interact"].performed -= OnPlayer2Continue;
    }

    private void OnPlayer1Continue(InputAction.CallbackContext context)
    {
        player1ContinuePressed = true;
    }

    private void OnPlayer2Continue(InputAction.CallbackContext context)
    {
        player2ContinuePressed = true;
    }

    IEnumerator StartAfterDelayRealtime(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        StartCoroutine(PlayTutorial());
    }

    IEnumerator PlayTutorial()
    {
        IsTutorialActive = true;
        isPlaying = true;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        IsInteractionLocked = true; 

        if (guidePanel != null)
            guidePanel.SetActive(true);

        var controller1 = player1 != null ? player1.GetComponent<PlayerController>() : null;
        var controller2 = player2 != null ? player2.GetComponent<PlayerController>() : null;

        currentStep = 0;
        while (currentStep < dialogueSteps.Length)
        {
            DialogueStep step = dialogueSteps[currentStep];

            Time.timeScale = step.pauseDuringStep ? 0f : previousTimeScale;
            IsInteractionLocked = step.pauseDuringStep;

            if (guideText != null)
                guideText.text = step.text;

            if (step.triggerType != TutorialTriggerType.None)
            {
                latestTrigger = TutorialTriggerType.None;
                while (latestTrigger != step.triggerType)
                    yield return null;

                yield return new WaitForSecondsRealtime(0.2f);
            }
            else if (step.requiresInput)
            {
                player1ContinuePressed = false;
                player2ContinuePressed = false;

                while (!player1ContinuePressed && !player2ContinuePressed)
                    yield return null;

                yield return new WaitForSecondsRealtime(0.08f);
            }
            else
            {
                yield return new WaitForSecondsRealtime(step.waitTimeAfter);
            }

            if (currentStep == 11)
                npcSpawner.TrySpawnTutorial();

            currentStep++;
        }

        if (guidePanel != null)
            guidePanel.SetActive(false);

        if (controller1 != null) controller1.enabled = true;
        if (controller2 != null) controller2.enabled = true;

        Time.timeScale = previousTimeScale;
        IsTutorialActive = false;
        isPlaying = false;
    }

    public void StartTutorialNow()
    {
        if (!isPlaying)
        {
            StopAllCoroutines();
            StartCoroutine(PlayTutorial());
        }
    }
}
