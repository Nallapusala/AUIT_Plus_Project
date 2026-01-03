using System;
using UnityEngine;
using UnityEngine.Video;

public class TaskStepManager : MonoBehaviour
{
    // All logical steps of the baking task
    public enum TaskStep
    {
        None = 0,
        Intro,
        GatherIngredients,
        CrackEggs,
        AddFlour,
        MixIngredients,
        PreheatOven,
        PourMixture,
        BakeCake,
        Finished
    }

    // Configuration for each step (what to show + where UI prefers to be)
    [Serializable]
    public class StepConfig
    {
        public TaskStep step;

        [Header("Instruction content")]
        [TextArea]
        public string instructionText;

        [Header("Optional video snippet")]
        public VideoClip optionalClip;
        public float clipStartTime;
        public float clipDuration = 5f;

        [Header("Semantic anchors for this step")]
        public Transform[] semanticAnchors;
    }

    [Header("Step configuration list")]
    public StepConfig[] steps;

    [Header("Initial step")]
    public TaskStep initialStep = TaskStep.Intro;

    /// <summary>Current logical step in the task.</summary>
    public TaskStep CurrentStep { get; private set; } = TaskStep.None;

    /// <summary>Full config for the current step.</summary>
    public StepConfig CurrentStepConfig { get; private set; }

    /// <summary>
    /// Raised whenever the current step changes.
    /// Subscribers receive the new StepConfig.
    /// </summary>
    public event Action<StepConfig> OnStepChanged;

    private void Start()
    {
        // Initialize to the configured initial step
        SetStep(initialStep, force: true);
    }

    /// <summary>
    /// Change the current step of the task.
    /// </summary>
    public void SetStep(TaskStep newStep, bool force = false)
    {
        if (!force && newStep == CurrentStep)
            return;

        CurrentStep = newStep;

        // Find matching config
        CurrentStepConfig = FindConfigForStep(newStep);
        if (CurrentStepConfig == null)
        {
            Debug.LogWarning(
                $"TaskStepManager: No StepConfig found for step {newStep}. " +
                "UI may not update correctly.");
        }
        else
        {
            Debug.Log($"[TaskStepManager] Step changed to: {newStep}");
        }

        // Notify listeners (SemanticUIAdapter, extra UIs, logger, etc.)
        OnStepChanged?.Invoke(CurrentStepConfig);
    }

    private StepConfig FindConfigForStep(TaskStep step)
    {
        if (steps == null) return null;

        foreach (var cfg in steps)
        {
            if (cfg != null && cfg.step == step)
                return cfg;
        }

        return null;
    }

    // Keyboard shortcuts for quick testing in the editor.
    // You can remove this later once XR interactions are wired.
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetStep(TaskStep.GatherIngredients);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetStep(TaskStep.CrackEggs);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetStep(TaskStep.AddFlour);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            SetStep(TaskStep.PourMixture);
        if (Input.GetKeyDown(KeyCode.Alpha5))
            SetStep(TaskStep.MixIngredients);
        if (Input.GetKeyDown(KeyCode.Alpha6))
            SetStep(TaskStep.PreheatOven);
        if (Input.GetKeyDown(KeyCode.Alpha7))
            SetStep(TaskStep.BakeCake);
        if (Input.GetKeyDown(KeyCode.Alpha8))
            SetStep(TaskStep.Finished);
    }
#endif
}
