using UnityEngine;

/// <summary>
/// Very simple attention model based on head direction:
/// - FocusedOnTask: user is roughly looking at the current task anchor.
/// - LostFocus: user has looked away from the current task anchor for a certain time.
/// This is a minimal proxy for "attention / distraction / loss of focus"
/// in the semantic condition.
/// </summary>
public class AttentionMonitor : MonoBehaviour
{
    public enum AttentionState
    {
        FocusedOnTask,
        LostFocus
    }

    [Header("References")]
    [Tooltip("User camera (HMD). Usually XR Origin -> Main Camera.")]
    public Transform userCamera;

    [Tooltip("Central TaskStepManager that knows the current step and anchors.")]
    public TaskStepManager stepManager;

    [Header("Parameters")]
    [Tooltip("Max angle (degrees) between camera forward and direction to task anchor\n" +
             "to still be considered 'focused' on the task.")]
    public float focusAngleThreshold = 35f;

    [Tooltip("Seconds of looking away from the task anchor before we treat it as 'LostFocus'.")]
    public float lostFocusDelay = 3f;

    public AttentionState CurrentState { get; private set; } = AttentionState.FocusedOnTask;

    // SemanticUIAdapter Use this to determine whether ¡®attention is low¡¯.
    public bool IsAttentionLow => CurrentState == AttentionState.LostFocus;


    /// <summary>
    /// Event raised whenever the attention state changes.
    /// </summary>
    public System.Action<AttentionState> OnAttentionStateChanged;

    private float _unfocusedTimer = 0f;

    private void Awake()
    {
        if (userCamera == null && Camera.main != null)
        {
            userCamera = Camera.main.transform;
        }

        if (stepManager == null)
        {
            stepManager = FindObjectOfType<TaskStepManager>();
        }
    }

    private void Update()
    {
        if (userCamera == null || stepManager == null)
            return;

        // 1. Find current anchor for the current step
        var cfg = stepManager.CurrentStepConfig;
        Transform anchor = null;
        if (cfg != null && cfg.semanticAnchors != null)
        {
            foreach (var a in cfg.semanticAnchors)
            {
                if (a != null)
                {
                    anchor = a;
                    break;
                }
            }
        }

        // If no anchor, we fallback to "focused"
        if (anchor == null)
        {
            _unfocusedTimer = 0f;
            SetState(AttentionState.FocusedOnTask);
            return;
        }

        // 2. Compute angle between camera forward and direction to anchor (in XZ plane)
        Vector3 toAnchor = anchor.position - userCamera.position;
        toAnchor.y = 0f;

        Vector3 forward = userCamera.forward;
        forward.y = 0f;

        if (toAnchor.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
        {
            _unfocusedTimer = 0f;
            SetState(AttentionState.FocusedOnTask);
            return;
        }

        float angle = Vector3.Angle(forward, toAnchor);

        // 3. If angle is within threshold => focused, otherwise we start counting "unfocused" time
        if (angle <= focusAngleThreshold)
        {
            _unfocusedTimer = 0f;
            SetState(AttentionState.FocusedOnTask);
        }
        else
        {
            _unfocusedTimer += Time.deltaTime;
            if (_unfocusedTimer >= lostFocusDelay)
            {
                SetState(AttentionState.LostFocus);
            }
        }
    }

    private void SetState(AttentionState newState)
    {
        if (newState == CurrentState)
            return;

        CurrentState = newState;
        OnAttentionStateChanged?.Invoke(newState);

        Debug.Log($"[AttentionMonitor] Attention state changed to {newState}");
    }
}
