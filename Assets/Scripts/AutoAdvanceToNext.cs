using UnityEngine;

public class StepAdvanceOnButton : MonoBehaviour
{
    [Header("References")]
    public TaskStepManager stepManager;

    [Header("Input (OVR)")]
    [Tooltip("Right controller B button = Button.Two")]
    public OVRInput.Button nextButton = OVRInput.Button.Two;

    [Header("Behavior")]
    [Tooltip("If true, allows skipping steps even if user isn't at the expected step.")]
    public bool allowAnyStepAdvance = false;

    private void Awake()
    {
        if (stepManager == null)
            stepManager = FindObjectOfType<TaskStepManager>();
    }

    private void Update()
    {
        if (stepManager == null) return;

        // Trigger only once per press
        if (OVRInput.GetDown(nextButton))
        {
            AdvanceToNext();
        }
    }

    public void AdvanceToNext()
    {
        var current = stepManager.CurrentStep;
        var next = GetNextStep(current);

        if (next == TaskStepManager.TaskStep.None)
        {
            Debug.Log("[StepAdvanceOnButton] No next step (already at end).");
            return;
        }

        // Optional gating: only advance if current is within the known sequence
        if (!allowAnyStepAdvance && next == current)
        {
            Debug.Log("[StepAdvanceOnButton] Advance blocked by gating.");
            return;
        }

        Debug.Log($"[StepAdvanceOnButton] Button pressed: {current} -> {next}");
        stepManager.SetStep(next);
    }

    private TaskStepManager.TaskStep GetNextStep(TaskStepManager.TaskStep current)
    {
        // Your exact sequence:
        // Welcome -> Milk -> Flour -> Sugar -> Eggs -> Oven -> End
        switch (current)
        {
            case TaskStepManager.TaskStep.Welcome:
                return TaskStepManager.TaskStep.Eggs;

            case TaskStepManager.TaskStep.Eggs:
                return TaskStepManager.TaskStep.Sugar;

            case TaskStepManager.TaskStep.Sugar:
                return TaskStepManager.TaskStep.Milk;

            case TaskStepManager.TaskStep.Milk:
                return TaskStepManager.TaskStep.Flour;

            case TaskStepManager.TaskStep.Flour:
                return TaskStepManager.TaskStep.MixingBowl;

            case TaskStepManager.TaskStep.MixingBowl:
                return TaskStepManager.TaskStep.End;

            case TaskStepManager.TaskStep.End:
                return TaskStepManager.TaskStep.None;

            // If CurrentStep is None (e.g., before init), push to Welcome
            case TaskStepManager.TaskStep.None:
            default:
                return TaskStepManager.TaskStep.Welcome;
        }
    }
}