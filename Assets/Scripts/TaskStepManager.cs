using System;
using UnityEngine;
using UnityEngine.Video;

public class TaskStepManager : MonoBehaviour
{
    // All logical steps of the baking task (UPDATED)
    public enum TaskStep
    {
        None = 0,

        Welcome,
        Milk,
        Flour,
        Sugar,
        Eggs,
        MixingBowl,

        End
    }

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
    public TaskStep initialStep = TaskStep.Welcome;

    public TaskStep CurrentStep { get; private set; } = TaskStep.None;
    public StepConfig CurrentStepConfig { get; private set; }

    public event Action<StepConfig> OnStepChanged;

    private void Start()
    {
        SetStep(initialStep, force: true);
    }

    public void SetStep(TaskStep newStep, bool force = false)
    {
        if (!force && newStep == CurrentStep)
            return;

        CurrentStep = newStep;

        CurrentStepConfig = FindConfigForStep(newStep);
        if (CurrentStepConfig == null)
        {
            Debug.LogWarning(
                $"TaskStepManager: No StepConfig found for step {newStep}. UI may not update correctly.");
        }
        else
        {
            Debug.Log($"[TaskStepManager] Step changed to: {newStep}");
        }

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

#if UNITY_EDITOR
    private void Update()
    {
        // Optional keyboard shortcuts for quick testing in Editor (UPDATED)
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetStep(TaskStep.Welcome);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetStep(TaskStep.Milk);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetStep(TaskStep.Flour);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetStep(TaskStep.Sugar);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetStep(TaskStep.Eggs);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetStep(TaskStep.MixingBowl);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SetStep(TaskStep.End);
    }
#endif
}