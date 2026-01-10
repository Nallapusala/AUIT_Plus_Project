using UnityEngine;
using AUIT.AdaptationObjectives;

/// <summary>
/// Condition controller for the instruction UI.
/// - Supports Static vs. Semantic (AUIT) condition.
/// - Supports Fatigue toggle (F1).
/// - Integrates TaskStepManager (per-step distances).
/// - Integrates AttentionMonitor (bring UI closer when attention is low).
/// </summary>
public class SemanticUIAdapter : MonoBehaviour
{
    // ----------------- Condition toggle -----------------

    [Header("Condition Toggle (Guide-compatible)")]
    [Tooltip("If true => Semantic (AUIT). If false => Static UI (no AUIT).")]
    public bool isSemanticMode = true;

    // ----------------- References -----------------

    [Header("References")]
    [Tooltip("The UI panel that should be adapted (e.g. VideoPanel rect transform).")]
    public RectTransform videoPanel;

    [Tooltip("User camera, usually the XR Origin's Main Camera.")]
    public Transform userCamera;

    [Tooltip("AUIT DistanceIntervalObjective on the same UI (VideoPanel).")]
    public DistanceIntervalObjective distanceObjective;

    [Tooltip("Logical baking steps (Intro, GatherIngredients, ...).")]
    public TaskStepManager taskStepManager;

    [Tooltip("Monitors whether the user is looking at the current semantic anchor.")]
    public AttentionMonitor attentionMonitor;

    // ----------------- Static mode distances -----------------

    [Header("Distance Settings (Static mode)")]
    [Tooltip("Base distance in static mode when not fatigued.")]
    public float staticNormalDistance = 1.2f;

    [Tooltip("Closer distance in static mode when fatigued.")]
    public float staticCloseDistance = 0.8f;

    // ----------------- Semantic mode distances -----------------

    [Header("Global distances (Semantic mode fallback)")]
    [Tooltip("Fallback distance when no step is configured.")]
    public float globalNormalDistance = 1.0f;

    [Tooltip("Fallback close distance for fatigue in semantic mode.")]
    public float globalCloseDistance = 0.5f;

    [Header("Per-step distances (Semantic mode)")]
    public float gatherNormalDistance = 1.0f;
    public float gatherCloseDistance = 0.5f;

    public float mixNormalDistance = 1.2f;
    public float mixCloseDistance = 0.6f;

    public float ovenNormalDistance = 1.5f;
    public float ovenCloseDistance = 0.7f;

    public float pourNormalDistance = 1.2f;
    public float pourCloseDistance = 0.6f;

    public float bakeNormalDistance = 1.6f;
    public float bakeCloseDistance = 0.8f;

    // ----------------- Attention-based adaptation -----------------

    [Header("Attention-based adaptation")]
    [Tooltip("If true, attention state influences the desired distance.")]
    public bool useAttention = true;

    [Tooltip("Target distance when attention is low (LostFocus).")]
    public float attentionCloseDistance = 0.5f;

    // ----------------- Input settings -----------------

    [Header("Input Settings")]
    [Tooltip("Key to toggle fatigue state (for testing in editor).")]
    public KeyCode triggerFatigueKey = KeyCode.F1;

    [Tooltip("Optional key to toggle between Static and Semantic mode.")]
    public KeyCode toggleModeKey = KeyCode.F2;

    // ----------------- Runtime state -----------------

    /// <summary>Current fatigue flag (can be toggled by key or by other scripts).</summary>
    public bool IsFatigued { get; private set; } = false;

    private bool isAttentionLow = false;

    // For optimization: remember last applied distance so we don't spam AUIT.
    private float lastAppliedGoalDistance = -1f;
    private bool hasLastAppliedGoalDistance = false;

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        // --- Auto-assign / subscribe TaskStepManager ---
        if (taskStepManager == null)
        {
            taskStepManager = FindObjectOfType<TaskStepManager>();
        }

        if (taskStepManager != null)
        {
            taskStepManager.OnStepChanged += HandleStepChanged;
        }

        // --- Auto-assign / subscribe AttentionMonitor ---
        if (attentionMonitor == null)
        {
            attentionMonitor = FindObjectOfType<AttentionMonitor>();
        }

        if (attentionMonitor != null)
        {
            attentionMonitor.OnAttentionStateChanged += HandleAttentionStateChanged;

           
            isAttentionLow =
                (attentionMonitor.CurrentState == AttentionMonitor.AttentionState.LostFocus);
        }
    }

    private void OnDisable()
    {
        if (attentionMonitor != null)
        {
            attentionMonitor.OnAttentionStateChanged -= HandleAttentionStateChanged;
        }

        if (taskStepManager != null)
        {
            taskStepManager.OnStepChanged -= HandleStepChanged;
        }
    }

    private void Start()
    {
        // Initial goal distance
        UpdateGoalDistance(forceUpdate: true);
    }

    private void Update()
    {
        // ------------- Keyboard testing -------------

        if (Input.GetKeyDown(triggerFatigueKey))
        {
            IsFatigued = !IsFatigued;
            Debug.Log($"[SemanticUIAdapter] Fatigue toggled = {IsFatigued}");
            UpdateGoalDistance(forceUpdate: true);
        }

        if (Input.GetKeyDown(toggleModeKey))
        {
            isSemanticMode = !isSemanticMode;
            Debug.Log($"[SemanticUIAdapter] Mode toggled. Now isSemanticMode = {isSemanticMode}");
            UpdateGoalDistance(forceUpdate: true);
        }

        // In static mode, we directly place the panel relative to the user
        if (!isSemanticMode)
        {
            UpdateStaticPlacement();
        }
        // In semantic mode, AUIT handles placement.
    }

    // =====================================================================
    // Validation
    // =====================================================================

    private void ValidateReferences()
    {
        bool ok = true;

        if (videoPanel == null)
        {
            Debug.LogWarning("[SemanticUIAdapter] VideoPanel reference is missing.");
            ok = false;
        }

        if (userCamera == null && Camera.main != null)
        {
            userCamera = Camera.main.transform;
        }

        if (distanceObjective == null && videoPanel != null)
        {
            distanceObjective = videoPanel.GetComponent<DistanceIntervalObjective>();
            if (distanceObjective != null)
            {
                Debug.Log("[SemanticUIAdapter] Auto-assigned DistanceIntervalObjective from VideoPanel.");
            }
        }

        // Optional: try to auto-assign managers
        if (taskStepManager == null)
        {
            taskStepManager = FindObjectOfType<TaskStepManager>();
        }

        if (attentionMonitor == null)
        {
            attentionMonitor = FindObjectOfType<AttentionMonitor>();
        }

        if (distanceObjective == null)
        {
            Debug.LogError("[SemanticUIAdapter] DistanceIntervalObjective reference is missing. Semantic mode cannot work.", this);
            ok = false;
        }

        if (!ok)
        {
            // Component stays enabled; static mode will still work.
        }
    }

    // =====================================================================
    // Attention & step integration
    // =====================================================================

    private void HandleAttentionStateChanged(AttentionMonitor.AttentionState newState)
    {
        
        isAttentionLow = (newState == AttentionMonitor.AttentionState.LostFocus);

        // Only semantic mode uses attention right now.
        if (isSemanticMode && useAttention)
        {
            UpdateGoalDistance(forceUpdate: true);
        }
    }

    private void HandleStepChanged(TaskStepManager.StepConfig cfg)
    {
        // Whenever the logical task step changes, recompute distance.
        if (isSemanticMode)
        {
            UpdateGoalDistance(forceUpdate: true);
        }
    }

    // =====================================================================
    // Static mode: directly move the panel
    // =====================================================================

    private void UpdateStaticPlacement()
    {
        if (videoPanel == null || userCamera == null)
            return;

        float distance = IsFatigued ? staticCloseDistance : staticNormalDistance;

        Vector3 forward = userCamera.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        forward.Normalize();

        Vector3 targetPos = userCamera.position + forward * distance;
        videoPanel.position = targetPos;

        // Face the camera in yaw only
        Vector3 toCamera = userCamera.position - videoPanel.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude > 0.0001f)
        {
            videoPanel.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }

    // =====================================================================
    // Semantic mode: drive AUIT DistanceIntervalObjective
    // =====================================================================

    /// <summary>
    /// Compute and apply the desired goal distance.
    /// If forceUpdate = false, we only update AUIT when the target value changed.
    /// </summary>
    private void UpdateGoalDistance(bool forceUpdate = false)
    {
        if (distanceObjective == null)
            return;

        float targetDistance = ComputeDesiredGoalDistance();

        if (!forceUpdate && hasLastAppliedGoalDistance &&
            Mathf.Approximately(targetDistance, lastAppliedGoalDistance))
        {
            return; // nothing changed
        }

        SetGoalDistance(targetDistance);
        lastAppliedGoalDistance = targetDistance;
        hasLastAppliedGoalDistance = true;

        Debug.Log(
            $"[SemanticUIAdapter] Applied GoalXYDistance = {targetDistance} m. " +
            $"Mode={(isSemanticMode ? "Semantic" : "Static")}, " +
            $"Step={GetCurrentStepName()}, Fatigued={IsFatigued}, AttentionLow={isAttentionLow}",
            this);
    }

    /// <summary>
    /// Decide what the target goal distance should be,
    /// based on task step + fatigue + attention.
    /// </summary>
    private float ComputeDesiredGoalDistance()
    {
        // If we are in static mode, we do not really use AUIT, but we return a sane value.
        if (!isSemanticMode || distanceObjective == null)
        {
            float baseDist = IsFatigued ? globalCloseDistance : globalNormalDistance;
            if (useAttention && isAttentionLow)
                baseDist = Mathf.Min(baseDist, attentionCloseDistance);
            return baseDist;
        }

        // Semantic mode: per-step distances
        float stepNormal = globalNormalDistance;
        float stepClose = globalCloseDistance;

        if (taskStepManager != null)
        {
            switch (taskStepManager.CurrentStep)
            {
                case TaskStepManager.TaskStep.GatherIngredients:
                    stepNormal = gatherNormalDistance;
                    stepClose = gatherCloseDistance;
                    break;

                case TaskStepManager.TaskStep.MixIngredients:
                    stepNormal = mixNormalDistance;
                    stepClose = mixCloseDistance;
                    break;

                case TaskStepManager.TaskStep.PreheatOven:
                    stepNormal = ovenNormalDistance;
                    stepClose = ovenCloseDistance;
                    break;

                case TaskStepManager.TaskStep.PourMixture:
                    stepNormal = pourNormalDistance;
                    stepClose = pourCloseDistance;
                    break;

                case TaskStepManager.TaskStep.BakeCake:
                    stepNormal = bakeNormalDistance;
                    stepClose = bakeCloseDistance;
                    break;

                default:
                    stepNormal = globalNormalDistance;
                    stepClose = globalCloseDistance;
                    break;
            }
        }

        float result = IsFatigued ? stepClose : stepNormal;

        // Attention override: if attention is low, we bring UI at least as close
        if (useAttention && isAttentionLow)
        {
            result = Mathf.Min(result, attentionCloseDistance);
        }

        return result;
    }

    /// <summary>
    /// Writes the desired distance into the AUIT DistanceIntervalObjective.
    /// Requires that DistanceIntervalObjective exposes a public
    /// "GoalXYDistance" property mapped to its internal field.
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
