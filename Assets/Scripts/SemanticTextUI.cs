using TMPro;
using UnityEngine;

public class SemanticTextUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the TaskStepManager in the scene")]
    public TaskStepManager taskStepManager;

    [Tooltip("TMP text element that displays the instruction")]
    public TextMeshProUGUI instructionText;

    [Header("Formatting")]
    public bool showStepTitle = true;

    void Awake()
    {
        if (!instructionText)
        {
            Debug.LogError("SemanticTextUI: InstructionText is not assigned.");
        }
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
        if (cfg == null || instructionText == null)
            return;

        if (showStepTitle)
        {
            instructionText.text =
                $"<b>{cfg.step}</b>\n\n{cfg.instructionText}";
        }
        else
        {
            instructionText.text = cfg.instructionText;
        }
    }
}
