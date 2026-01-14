using TMPro;
using UnityEngine;

public class SemanticTextUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the TaskStepManager in the scene")]
    public TaskStepManager taskStepManager;

    [Tooltip("TMP text element that displays the step title (e.g., 'Welcome', 'Add milk')")]
    public TextMeshProUGUI titleText;

    [Tooltip("TMP text element that displays the instruction body")]
    public TextMeshProUGUI bodyText;

    [Header("Formatting")]
    [Tooltip("If true, titleText will be shown and updated from cfg.step.")]
    public bool showStepTitle = true;

    [Tooltip("Optional: override the title shown for each enum step (leave blank to use cfg.step.ToString()).")]
    public bool useFriendlyTitles = true;

    void Awake()
    {
        if (taskStepManager == null)
            Debug.LogError("SemanticTextUI: TaskStepManager is not assigned.");

        if (bodyText == null)
            Debug.LogError("SemanticTextUI: BodyText is not assigned.");

        if (showStepTitle && titleText == null)
            Debug.LogWarning("SemanticTextUI: ShowStepTitle is ON but TitleText is not assigned.");
    }

    void OnEnable()
    {
        if (taskStepManager != null)
            taskStepManager.OnStepChanged += HandleStepChanged;
    }

    void OnDisable()
    {
        if (taskStepManager != null)
            taskStepManager.OnStepChanged -= HandleStepChanged;
    }

    private void HandleStepChanged(TaskStepManager.StepConfig cfg)
    {
        if (cfg == null) return;

        // Update title (optional)
        if (titleText != null)
        {
            if (showStepTitle)
            {
                titleText.gameObject.SetActive(true);
                titleText.text = useFriendlyTitles ? GetFriendlyTitle(cfg.step) : cfg.step.ToString();
            }
            else
            {
                titleText.gameObject.SetActive(false);
            }
        }

        // Update body
        if (bodyText != null)
        {
            bodyText.text = cfg.instructionText ?? string.Empty;
        }
    }

    private string GetFriendlyTitle(TaskStepManager.TaskStep step)
    {
        // Adjust these to your exact enum names
        switch (step)
        {
            case TaskStepManager.TaskStep.Welcome: return "Welcome";
            case TaskStepManager.TaskStep.Milk: return "Add Milk";
            case TaskStepManager.TaskStep.Flour: return "Add Flour";
            case TaskStepManager.TaskStep.Sugar: return "Add Sugar";
            case TaskStepManager.TaskStep.Eggs: return "Add Eggs";
            case TaskStepManager.TaskStep.MixingBowl: return "Move Bowl";
            case TaskStepManager.TaskStep.End: return "Done";
            default: return step.ToString();
        }
    }
}