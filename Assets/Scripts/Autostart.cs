using UnityEngine;

public class StepAutoStart : MonoBehaviour
{
    public TaskStepManager stepManager;

    [Tooltip("Seconds to show Welcome before moving to AddMilk.")]
    public float welcomeDuration = 2.0f;

    private void Start()
    {
        if (stepManager == null) return;

        // Only auto-advance if we are currently at Welcome
        if (stepManager.CurrentStep == TaskStepManager.TaskStep.Welcome)
            Invoke(nameof(GoToMilkStep), welcomeDuration);
    }

    private void GoToMilkStep()
    {
        if (stepManager == null) return;

        if (stepManager.CurrentStep == TaskStepManager.TaskStep.Welcome)
            stepManager.SetStep(TaskStepManager.TaskStep.Milk);
    }
}