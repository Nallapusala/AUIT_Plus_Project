using UnityEngine;

/// <summary>
/// Very small manager that tracks the current task step in the cooking task.
/// Other scripts (like SemanticUIAdapter) can query CurrentStep to adapt behavior.
/// 
/// For now we drive it by keyboard:
/// - Key 1 ¡ú GatherIngredients (Step 1)
/// - Key 2 ¡ú MixIngredients   (Step 2)
/// - Key 3 ¡ú PrepareOven      (Step 3)
/// </summary>
public class TaskStepManager : MonoBehaviour
{
    /// <summary>
    /// Logical steps in the task. Names are for code only.
    /// </summary>
    public enum TaskStep
    {
        GatherIngredients, // Step 1 ¨C walking around counters
        MixIngredients,    // Step 2 ¨C at the mixing bowl
        PrepareOven        // Step 3 ¨C at the oven
    }

    [Header("Initial Step")]
    [Tooltip("Which step should be active when the scene starts.")]
    [SerializeField]
    private TaskStep initialStep = TaskStep.GatherIngredients;

    /// <summary>
    /// Public read-only property so other scripts can query the current step.
    /// </summary>
    public TaskStep CurrentStep { get; private set; }

    private void Awake()
    {
        // Initialize with the configured initial step.
        SetStep(initialStep, true);
    }

    private void Update()
    {
        // Simple keyboard control for debugging / prototyping.
        // Later you can replace this with trigger volumes around
        // FlourZone, MixingBowl, Oven, etc.

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetStep(TaskStep.GatherIngredients);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetStep(TaskStep.MixIngredients);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetStep(TaskStep.PrepareOven);
        }
    }

    /// <summary>
    /// Internal helper to change the current step and print a debug message.
    /// </summary>
    private void SetStep(TaskStep newStep, bool skipIfSame = false)
    {
        if (skipIfSame && newStep == CurrentStep)
            return;

        if (newStep == CurrentStep)
            return;

        CurrentStep = newStep;

        Debug.Log($"[TaskStepManager] Current step changed to: {CurrentStep}", this);
    }
}
