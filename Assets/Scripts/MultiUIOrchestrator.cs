using UnityEngine;

public class MultiUIOrchestrator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Use OVRCameraRig/TrackingSpace/CenterEyeAnchor")]
    public Transform head;

    [Tooltip("Higher priority panel (e.g., Steps/Checklist)")]
    public Transform primaryPanel;

    [Tooltip("Lower priority panel (e.g., Video)")]
    public Transform secondaryPanel;

    [Header("Placement")]
    [Tooltip("Distance of panels from the user (meters)")]
    public float distance = 1.2f;

    [Tooltip("Vertical offset relative to head position (meters). Negative means slightly below eye level.")]
    public float heightOffset = -0.10f;

    [Tooltip("Initial left/right yaw angle for slots (degrees).")]
    public float slotYawDegrees = 18f;

    [Tooltip("Max yaw if panels need more separation (degrees).")]
    public float maxSlotYawDegrees = 35f;

    [Tooltip("Extra gap added between panels (meters).")]
    public float extraGapMeters = 0.10f;

    [Header("Occlusion")]
    [Tooltip("Raycast layers that can block UI visibility. EXCLUDE the WorldUI layer.")]
    public LayerMask occlusionMask;

    [Tooltip("If true, swap panels left/right if one side is occluded.")]
    public bool swapIfOccluded = true;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.08f;
    public float rotationLerpSpeed = 12f;

    [Header("Facing")]
    [Tooltip("Yaw-only facing keeps panels upright and avoids tilting.")]
    public bool faceUserYawOnly = true;

    Vector3 primaryVel, secondaryVel;
    int assignment = 0; // 0: primary left, secondary right. 1: swapped.

    void LateUpdate()
    {
        if (!head || !primaryPanel || !secondaryPanel) return;

        Vector3 headPos = head.position;

        // Flatten forward to yaw-only so panels don't "bob" with head pitch.
        Vector3 flatForward = head.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;
        flatForward.Normalize();

        Quaternion baseYawRot = Quaternion.LookRotation(flatForward, Vector3.up);

        // Determine required separation based on approximate panel sizes.
        float rA = GetPanelRadius(primaryPanel);
        float rB = GetPanelRadius(secondaryPanel);
        float desiredSeparation = rA + rB + extraGapMeters;

        // Increase yaw if panels are too close.
        float yaw = Mathf.Clamp(slotYawDegrees, 0f, maxSlotYawDegrees);
        Vector3 leftPos, rightPos;

        for (int i = 0; i < 8; i++)
        {
            leftPos = SlotPosition(headPos, baseYawRot, -yaw);
            rightPos = SlotPosition(headPos, baseYawRot, yaw);

            float sep = Vector3.Distance(leftPos, rightPos);
            if (sep >= desiredSeparation || yaw >= maxSlotYawDegrees) break;

            yaw += 2f; // push out a bit more
        }

        leftPos = SlotPosition(headPos, baseYawRot, -yaw);
        rightPos = SlotPosition(headPos, baseYawRot, yaw);

        if (swapIfOccluded)
        {
            bool occLeft = IsOccluded(headPos, leftPos);
            bool occRight = IsOccluded(headPos, rightPos);

            // Prefer placing PRIMARY on the unoccluded side.
            // If both same, keep current assignment (stability).
            if (occLeft && !occRight) assignment = 1;      // primary goes right
            else if (!occLeft && occRight) assignment = 0; // primary goes left
        }

        // Apply assignment
        Vector3 primaryTarget = (assignment == 0) ? leftPos : rightPos;
        Vector3 secondaryTarget = (assignment == 0) ? rightPos : leftPos;

        MoveAndFace(primaryPanel, primaryTarget, headPos, ref primaryVel);
        MoveAndFace(secondaryPanel, secondaryTarget, headPos, ref secondaryVel);
    }

    Vector3 SlotPosition(Vector3 headPos, Quaternion baseYawRot, float yawDeg)
    {
        Quaternion slotRot = baseYawRot * Quaternion.Euler(0f, yawDeg, 0f);
        Vector3 forward = slotRot * Vector3.forward;
        return headPos + forward * distance + Vector3.up * heightOffset;
    }

    void MoveAndFace(Transform panel, Vector3 targetPos, Vector3 headPos, ref Vector3 vel)
    {
        panel.position = Vector3.SmoothDamp(panel.position, targetPos, ref vel, positionSmoothTime);

        Quaternion targetRot;
        if (faceUserYawOnly)
        {
            Vector3 toHead = headPos - panel.position;
            toHead.y = 0f;
            if (toHead.sqrMagnitude < 0.0001f) toHead = panel.forward;
            targetRot = Quaternion.LookRotation(toHead.normalized, Vector3.up);
        }
        else
        {
            Vector3 toHead = (headPos - panel.position).normalized;
            targetRot = Quaternion.LookRotation(toHead, Vector3.up);
        }

        float t = 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);
        panel.rotation = Quaternion.Slerp(panel.rotation, targetRot, t);
    }

    bool IsOccluded(Vector3 headPos, Vector3 panelPos)
    {
        Vector3 dir = panelPos - headPos;
        float dist = dir.magnitude;
        if (dist < 0.01f) return false;

        dir /= dist;
        // If something is between head and panel slot, treat as occluded.
        return Physics.Raycast(headPos, dir, dist, occlusionMask, QueryTriggerInteraction.Ignore);
    }

    float GetPanelRadius(Transform panel)
    {
        // 1) If it has a Renderer (e.g., Quad), use its bounds.
        var rend = panel.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Vector3 ext = rend.bounds.extents;
            return Mathf.Max(ext.x, ext.y, ext.z);
        }

        // 2) If it is a Canvas/RectTransform, approximate from rect size + lossy scale.
        var rt = panel.GetComponentInChildren<RectTransform>();
        if (rt != null)
        {
            Vector3 s = rt.lossyScale;
            float w = Mathf.Abs(rt.rect.width * s.x);
            float h = Mathf.Abs(rt.rect.height * s.y);
            return 0.5f * Mathf.Max(w, h);
        }

        // Fallback
        return 0.25f;
    }
}
