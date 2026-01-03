using UnityEngine;
using TMPro;

/// <summary>
/// Keeps a UI text element in sync with the current task step.
/// Subscribes to TaskStepManager.OnStepChanged and updates the instruction label.
/// </summary>
public class TaskStepUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("TaskStepManager in the scene (usually on SceneManager GameObject).")]
    public TaskStepManager stepManager;

    [Tooltip("Text component that displays the instruction text.")]
    public TMP_Text instructionLabel;

    private void Awake()
    {
        // If the text is on the same GameObject, auto-grab it.
        if (instructionLabel == null)
        {
            instructionLabel = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        if (stepManager == null)
        {
            stepManager = FindObjectOfType<TaskStepManager>();
        }

        if (stepManager == null)
        {
            Debug.LogWarning("[TaskStepUI] No TaskStepManager found in the scene.", this);
            return;
        }

        // Subscribe to step change events
        stepManager.OnStepChanged += HandleStepChanged;

        // Also apply the current step once, in case it was already set in Awake()
        if (stepManager.CurrentStepConfig != null)
        {
            ApplyStep(stepManager.CurrentStepConfig);
        }
    }

    private void OnDisable()
    {
        if (stepManager != null)
        {
            stepManager.OnStepChanged -= HandleStepChanged;
        }
    }

    /// <summary>
    /// Called whenever TaskStepManager reports a new step.
    /// </summary>
    private void HandleStepChanged(TaskStepManager.StepConfig config)
    {
        ApplyStep(config);
    }

    /// <summary>
    /// Actually writes the instruction text into the label.
    /// </summary>
    private void ApplyStep(TaskStepManager.StepConfig config)
    {
        if (config == null || instructionLabel == null)
            return;

        // Here we simply use the instructionText from the config.
        // If you want "Step 1: ..." style text, just include that in the
        // Instruction Text field in the Inspector.
        instructionLabel.text = config.instructionText;
    }
}
