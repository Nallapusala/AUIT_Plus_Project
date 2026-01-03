using UnityEngine;
using AUIT.AdaptationObjectives;

/// <summary>
/// Semantic UI Adapter compatible with the HCI Guide:
/// - Exposes a Static vs Semantic condition toggle (isSemanticMode).
/// - Uses AUIT DistanceIntervalObjective internally (no direct Transform warping).
/// - Static mode (B): panel is pinned (frozen) at the position when switching into Static.
/// - Semantic mode: goal distance depends on TaskStep + fatigue state.
/// </summary>
public class SemanticUIAdapter : MonoBehaviour
{
    [Header("Condition Toggle (Guide-compatible)")]
    [Tooltip("If false: Static condition (no semantic adaptation).\nIf true: Semantic condition (distance adapts to fatigue + task step).")]
    public bool isSemanticMode = true;

    [Header("References")]
    [Tooltip("UI panel to adapt (e.g., VideoPanel).")]
    public Transform videoPanel;

    [Tooltip("User camera, usually the Main Camera under the XR Origin.")]
    public Transform userCamera;

    [Tooltip("DistanceIntervalObjective component on the VideoPanel used by AUIT.")]
    public DistanceIntervalObjective distanceObjective;

    [Tooltip("Manager that knows which task step is currently active.")]
    public TaskStepManager taskStepManager;

    [Header("Global Fallback Distances (used if no per-step config)")]
    [Tooltip("Default target distance in a 'normal' state (meters).")]
    public float normalDistance = 2.5f;

    [Tooltip("Default target distance in a 'fatigued' state (meters).")]
    public float closeDistance = 1.5f;

    [Header("Per-step distances (Semantic mode)")]
    [Tooltip("Step 1 每 GatherIngredients: normal distance.")]
    public float gatherNormalDistance = 1.5f;

    [Tooltip("Step 1 每 GatherIngredients: fatigued distance.")]
    public float gatherCloseDistance = 1.0f;

    [Tooltip("Step 2 每 MixIngredients: normal distance.")]
    public float mixNormalDistance = 1.2f;

    [Tooltip("Step 2 每 MixIngredients: fatigued distance.")]
    public float mixCloseDistance = 0.8f;

    [Tooltip("Step 3 每 PrepareOven: normal distance.")]
    public float ovenNormalDistance = 1.5f;

    [Tooltip("Step 3 每 PrepareOven: fatigued distance.")]
    public float ovenCloseDistance = 1.0f;

    [Header("Input Settings")]
    [Tooltip("Key used to toggle fatigue (as in the Guide).")]
    public KeyCode triggerFatigueKey = KeyCode.F1;

    /// <summary>
    /// Current fatigue state. When true in Semantic mode, the panel
    /// should move closer (via AUIT distance objective).
    /// </summary>
    public bool IsFatigued { get; private set; } = false;

    // Remember the pinned panel transform for the Static condition.
    // (B) This will be updated when switching into Static to freeze the current pose.
    private Vector3 initialPanelPosition;
    private Quaternion initialPanelRotation;

    // Cache last applied goal distance, so we only update AUIT when needed.
    private float lastAppliedGoalDistance = 0f;
    private bool hasLastGoalDistance = false;

    // NEW: track runtime mode changes so we can apply them immediately.
    private bool lastSemanticMode;

    private void Reset()
    {
        // Try to auto-fill references when the component is first added.

        if (userCamera == null && Camera.main != null)
        {
            userCamera = Camera.main.transform;
        }

        if (videoPanel == null)
        {
            // If this adapter is accidentally placed on the panel itself,
            // we can try to auto-assign a RectTransform child.
            RectTransform rect = GetComponentInChildren<RectTransform>();
            if (rect != null)
                videoPanel = rect.transform;
        }

        if (distanceObjective == null && videoPanel != null)
        {
            distanceObjective = videoPanel.GetComponent<DistanceIntervalObjective>();
        }

        if (taskStepManager == null)
        {
            taskStepManager = FindObjectOfType<TaskStepManager>();
        }
    }

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        // Cache initial transform (used as initial pinned pose, but will be overwritten in Static(B) when switching).
        initialPanelPosition = videoPanel.position;
        initialPanelRotation = videoPanel.rotation;
    }

    private bool ValidateReferences()
    {
        bool ok = true;

        if (videoPanel == null)
        {
            Debug.LogError("[SemanticUIAdapter] VideoPanel reference is missing. Please assign it in the Inspector.", this);
            ok = false;
        }

        if (distanceObjective == null)
        {
            Debug.LogError(
                "[SemanticUIAdapter] DistanceIntervalObjective reference is missing. " +
                "Please drag the DistanceIntervalObjective from the VideoPanel onto this field.",
                this);
            ok = false;
        }

        if (userCamera == null && Camera.main != null)
        {
            userCamera = Camera.main.transform;
        }

        if (taskStepManager == null)
        {
            Debug.LogWarning(
                "[SemanticUIAdapter] TaskStepManager reference is missing. " +
                "Semantic adaptation will NOT be step-dependent (fallback to global distances).",
                this);
        }

        return ok;
    }

    private void Start()
    {
        if (!enabled) return;

        // Apply initial mode immediately.
        ApplyMode(isSemanticMode, forceRefresh: true);

        // NEW: remember current mode so runtime switches can be detected.
        lastSemanticMode = isSemanticMode;
    }

    private void Update()
    {
        if (!enabled) return;

        // NEW: if the mode was changed at runtime (Inspector/other script), apply it immediately.
        if (isSemanticMode != lastSemanticMode)
        {
            ApplyMode(isSemanticMode, forceRefresh: true);
            lastSemanticMode = isSemanticMode;
        }

        // Toggle fatigue state when the key is pressed.
        if (Input.GetKeyDown(triggerFatigueKey))
        {
            IsFatigued = !IsFatigued;

            if (isSemanticMode)
            {
                Debug.Log($"[SemanticUIAdapter] (Semantic mode) Fatigue toggled = {IsFatigued}.", this);
                UpdateGoalDistance(true);
            }
            else
            {
                Debug.Log(
                    $"[SemanticUIAdapter] (Static mode) Fatigue toggled = {IsFatigued}, " +
                    "but semantic adaptation is disabled.",
                    this);
            }
        }

        // In Semantic mode we continuously check whether the task step changed.
        // If so, we may need to update the goal distance even if fatigue did not change.
        if (isSemanticMode)
        {
            UpdateGoalDistance(false);
        }
    }

    private void LateUpdate()
    {
        if (!enabled) return;

        // Static(B): keep the panel pinned to the pose that was captured
        // when switching into Static mode.
        if (!isSemanticMode && videoPanel != null)
        {
            videoPanel.position = initialPanelPosition;

            // Intentionally NOT forcing rotation, so UIBillboard etc. can still control orientation.
            // If you want to freeze rotation as well, uncomment the next line:
            // videoPanel.rotation = initialPanelRotation;
        }
        // In Semantic mode we do nothing here:
        // AUIT (and optionally a billboard script) controls the transform.
    }

    /// <summary>
    /// Public API: switch modes in code (preferred over directly setting isSemanticMode).
    /// Static(B) = freeze current pose at the moment of switching to Static.
    /// </summary>
    public void SetSemanticMode(bool enableSemantic)
    {
        if (!enabled) return;

        isSemanticMode = enableSemantic;
        ApplyMode(isSemanticMode, forceRefresh: true);
        lastSemanticMode = isSemanticMode;
    }

    /// <summary>
    /// Apply mode behavior immediately.
    /// </summary>
    private void ApplyMode(bool enableSemantic, bool forceRefresh)
    {
        if (enableSemantic)
        {
            // Semantic condition: initialize/update according to current step + fatigue.
            UpdateGoalDistance(forceRefresh);
        }
        else
        {
            // Static(B): freeze CURRENT pose as the pinned pose.
            if (videoPanel != null)
            {
                initialPanelPosition = videoPanel.position;
                initialPanelRotation = videoPanel.rotation;
            }

            // Static condition: set a default distance once; no further updates.
            SetGoalDistance(normalDistance);
        }

        Debug.Log($"[SemanticUIAdapter] Mode applied: {(enableSemantic ? "Semantic" : "Static (freeze current pose)")}.", this);
    }

    /// <summary>
    /// Compute and apply the desired goal distance.
    /// If forceUpdate = false, we only update AUIT when the target value changed.
    /// </summary>
    private void UpdateGoalDistance(bool forceUpdate)
    {
        if (distanceObjective == null)
            return;

        float targetDistance = ComputeDesiredGoalDistance();

        if (!forceUpdate && hasLastGoalDistance &&
            Mathf.Approximately(targetDistance, lastAppliedGoalDistance))
        {
            return; // nothing changed
        }

        SetGoalDistance(targetDistance);
        lastAppliedGoalDistance = targetDistance;
        hasLastGoalDistance = true;

        Debug.Log(
            $"[SemanticUIAdapter] Applied GoalXYDistance = {targetDistance} m. " +
            $"Mode={(isSemanticMode ? "Semantic" : "Static")}, " +
            $"Step={GetCurrentStepName()}, Fatigued={IsFatigued}",
            this);
    }

    /// <summary>
    /// Decide what the target goal distance should be,
    /// based on task step + fatigue. Falls back to global distances
    /// if no TaskStepManager is assigned.
    /// </summary>
    private float ComputeDesiredGoalDistance()
    {
        // If we don't have a taskStepManager, fall back to global distances.
        if (taskStepManager == null)
        {
            return IsFatigued ? closeDistance : normalDistance;
        }

        TaskStepManager.TaskStep step = taskStepManager.CurrentStep;

        return step switch
        {
            TaskStepManager.TaskStep.GatherIngredients => IsFatigued ? gatherCloseDistance : gatherNormalDistance,
            TaskStepManager.TaskStep.MixIngredients => IsFatigued ? mixCloseDistance : mixNormalDistance,
            TaskStepManager.TaskStep.PreheatOven => IsFatigued ? ovenCloseDistance : ovenNormalDistance,
            _ => IsFatigued ? closeDistance : normalDistance,// Fallback 每 should not really happen.
        };
    }

    /// <summary>
    /// Writes the desired distance into the AUIT DistanceIntervalObjective.
    /// Requires that DistanceIntervalObjective exposes a public
    /// 'GoalXYDistance' property mapped to its internal field.
    /// </summary>
    private void SetGoalDistance(float distance)
    {
        if (distanceObjective == null)
            return;

        distanceObjective.GoalXYDistance = distance;
    }

    private string GetCurrentStepName()
    {
        if (taskStepManager == null)
            return "None";

        return taskStepManager.CurrentStep.ToString();
    }
}
