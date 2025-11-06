using System.Collections;
using UnityEngine;
using TMPro;

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
    public GameObject player; // assign in inspector

    [Header("Behaviour Settings")]
    [Tooltip("Delay before the tutorial starts (seconds, real time).")]
    public float startDelay = 2.5f;
    [Tooltip("Key the player presses to continue a 'requiresInput' step.")]
    public KeyCode continueKey = KeyCode.Space;

    private int currentStep = 0;
    private bool isPlaying = false;
    private float previousTimeScale = 1f;

    public static bool IsTutorialActive { get; private set; } = false;

    private static TutorialTriggerType latestTrigger = TutorialTriggerType.None;

    [Header("NPC Spawner")]
    public NPCSpawner1 npcSpawner;

    public static void NotifyTrigger(TutorialTriggerType type)
    {
        latestTrigger = type;
    }


    void Start()
    {
        StartCoroutine(StartAfterDelayRealtime(startDelay));
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

        if (guidePanel != null)
            guidePanel.SetActive(true);

        currentStep = 0;
        while (currentStep < dialogueSteps.Length)
        {
            DialogueStep step = dialogueSteps[currentStep];

            // ✅ Freeze or unfreeze game
            Time.timeScale = step.pauseDuringStep ? 0f : previousTimeScale;

            // ✅ Update text
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
                yield return new WaitForSecondsRealtime(0.12f);
                while (!Input.GetKeyDown(continueKey))
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

        // ✅ End tutorial
        if (guidePanel != null)
            guidePanel.SetActive(false);

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
