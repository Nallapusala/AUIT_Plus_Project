using UnityEngine;
using AUIT.AdaptationObjectives;

/// <summary>
/// Static:
/// - UI snapped to deskAnchor and stays fixed.
/// - No AUIT, no gaze-follow, no mirror.
///
/// Semantic:
/// - On enter: UI is placed once in front of the user at 1.8m.
/// - AUIT DistanceIntervalObjective is enabled (distance semantics only).
/// - If 5s inactivity is detected: UI is snapped ONCE back to user's front.
/// </summary>
public class SemanticUIAdapter : MonoBehaviour
{
    // =====================================================
    // Mode
    // =====================================================
    [Header("Mode")]
    public bool isSemanticMode = false;

    // =====================================================
    // References
    // =====================================================
    [Header("References")]
    [Tooltip("Root transform that is moved (e.g. InstructionUI_Rig).")]
    public Transform uiRig;

    [Tooltip("User camera (XR Origin -> Main Camera).")]
    public Transform userCamera;

    [Tooltip("AUIT DistanceIntervalObjective on uiRig.")]
    public DistanceIntervalObjective distanceObjective;

    // =====================================================
    // Static (baseline)
    // =====================================================
    [Header("Static Baseline")]
    public Transform deskAnchor;
    public Vector3 deskLocalPositionOffset = Vector3.zero;
    public Vector3 deskLocalRotationOffsetEuler = Vector3.zero;

    // =====================================================
    // Semantic parameters
    // =====================================================
    [Header("Semantic Placement")]
    [Tooltip("Initial semantic distance in front of user (meters).")]
    public float semanticFrontDistance = 1.8f;

    [Tooltip("Distance used by AUIT (baseline semantic distance).")]
    public float semanticBaselineDistance = 1.8f;

    [Header("Inactivity (already defined by you)")]
    [Tooltip("Seconds with no movement / interaction => distracted.")]
    public float inactivitySecondsToDrift = 5.0f;

    // =====================================================
    // Internal state
    // =====================================================
    private float inactivityTimer = 0f;
    private bool isDistracted = false;

    private Vector3 lastCamPos;
    private Quaternion lastCamRot;

    private bool lastSemanticMode;

    // =====================================================
    // Unity lifecycle
    // =====================================================
    private void Awake()
    {
        if (userCamera == null && Camera.main != null)
            userCamera = Camera.main.transform;

        if (userCamera != null)
        {
            lastCamPos = userCamera.position;
            lastCamRot = userCamera.rotation;
        }
    }

    private void Start()
    {
        lastSemanticMode = isSemanticMode;
        ApplyMode(isSemanticMode);
    }

    private void Update()
    {
        // Detect mode change
        if (isSemanticMode != lastSemanticMode)
        {
            ApplyMode(isSemanticMode);
            lastSemanticMode = isSemanticMode;
        }

        // Static mode: NOTHING happens
        if (!isSemanticMode)
            return;

        TickInactivity();
    }

    // =====================================================
    // Mode switching
    // =====================================================
    private void ApplyMode(bool semantic)
    {
        inactivityTimer = 0f;
        isDistracted = false;

        if (semantic)
        {
            // --- Semantic mode entry ---
            if (distanceObjective != null)
            {
                distanceObjective.enabled = true;
                distanceObjective.GoalXYDistance = semanticBaselineDistance;
            }

            PlaceUIInFrontOfUser();

            Debug.Log("[SemanticUIAdapter] Entered Semantic mode: UI placed in front at 1.8m.", this);
        }
        else
        {
            // --- Static mode entry ---
            if (distanceObjective != null)
                distanceObjective.enabled = false;

            SnapUIToDesk();

            Debug.Log("[SemanticUIAdapter] Entered Static mode: UI fixed on desk.", this);
        }
    }

    // =====================================================
    // Static helpers
    // =====================================================
    private void SnapUIToDesk()
    {
        if (uiRig == null || deskAnchor == null)
            return;

        uiRig.position = deskAnchor.TransformPoint(deskLocalPositionOffset);
        uiRig.rotation = deskAnchor.rotation * Quaternion.Euler(deskLocalRotationOffsetEuler);
    }

    // =====================================================
    // Semantic helpers
    // =====================================================
    private void PlaceUIInFrontOfUser()
    {
        if (uiRig == null || userCamera == null)
            return;

        Vector3 targetPos =
            userCamera.position +
            userCamera.forward * semanticFrontDistance;

        uiRig.position = targetPos;

        // Ensure front-facing (no mirror)
        Vector3 toCam = userCamera.position - uiRig.position;
        toCam.y = 0f;
        if (toCam.sqrMagnitude > 0.0001f)
            uiRig.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
    }

    // =====================================================
    // Inactivity / distraction detection (your definition)
    // =====================================================
    private void TickInactivity()
    {
        if (userCamera == null)
            return;

        bool moved =
            Vector3.Distance(userCamera.position, lastCamPos) > 0.02f ||
            Quaternion.Angle(userCamera.rotation, lastCamRot) > 2.0f;

        if (moved)
        {
            inactivityTimer = 0f;
            isDistracted = false;

            lastCamPos = userCamera.position;
            lastCamRot = userCamera.rotation;
        }
        else
        {
            inactivityTimer += Time.deltaTime;

            if (!isDistracted && inactivityTimer >= inactivitySecondsToDrift)
            {
                isDistracted = true;
                OnAttentionDrift();
            }
        }
    }

    // =====================================================
    // Semantic event: attention drift
    // =====================================================
    private void OnAttentionDrift()
    {
        // IMPORTANT: one-shot behavior
        PlaceUIInFrontOfUser();

        Debug.Log("[SemanticUIAdapter] Attention drift detected: UI returned to user's view.", this);
    }
}
