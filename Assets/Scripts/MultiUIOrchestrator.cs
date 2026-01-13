using UnityEngine;

public class TwoPanelViewSlotterAvoidEnv : MonoBehaviour
{
    [Header("References")]
    [Tooltip("XR Main Camera / CenterEyeAnchor")]
    public Transform head;

    public Transform primaryPanel;
    public Transform secondaryPanel;

    [Header("Optional: read AUIT 'desired' poses (recommended)")]
    public Transform primaryDesiredPose;
    public Transform secondaryDesiredPose;

    [Header("View Slotting")]
    public float baseDistance = 1.2f;
    public float minDistance = 0.8f;
    public float maxDistance = 2.0f;
    public float heightOffset = -0.10f;

    public float minSlotYawDegrees = 18f;
    public float maxSlotYawDegrees = 40f;

    public float extraGapMeters = 0.10f;
    public float radiusMultiplier = 1.10f;

    [Header("Environment Avoidance")]
    [Tooltip("Layers for furniture/walls/etc. EXCLUDE any UI layers.")]
    public LayerMask environmentMask;

    [Tooltip("Extra space kept between UI and obstacles (meters).")]
    public float obstacleClearance = 0.08f;

    [Tooltip("Adds a little padding to the sphere cast radius.")]
    public float castRadiusPadding = 0.02f;

    [Tooltip("How many distance steps (pull closer) to try when blocked.")]
    public int distanceTries = 8;

    [Tooltip("How many height offsets to try when blocked (pattern: 0,+,-,+2,-2...).")]
    public int heightTries = 5;

    [Tooltip("Height step size (meters) for retries.")]
    public float heightStep = 0.12f;

    [Header("Assignment Stability")]
    public float swapHysteresisDegrees = 6f;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.08f;
    public float rotationLerpSpeed = 12f;

    [Header("Facing")]
    public bool faceUserYawOnly = true;

    private int assignment = 0; // 0: primary left, 1: primary right
    private Vector3 primaryVel, secondaryVel;

    void LateUpdate()
    {
        if (!head || !primaryPanel || !secondaryPanel) return;

        Vector3 headPos = head.position;

        // Yaw-only forward
        Vector3 flatForward = head.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 1e-6f) flatForward = Vector3.forward;
        flatForward.Normalize();

        Quaternion baseYawRot = Quaternion.LookRotation(flatForward, Vector3.up);

        // Desired poses (AUIT-driven if provided)
        Vector3 pDesiredPos = (primaryDesiredPose ? primaryDesiredPose.position : primaryPanel.position);
        Vector3 sDesiredPos = (secondaryDesiredPose ? secondaryDesiredPose.position : secondaryPanel.position);

        float pDesiredYaw = SignedYawDeg(headPos, baseYawRot, pDesiredPos);
        float sDesiredYaw = SignedYawDeg(headPos, baseYawRot, sDesiredPos);

        assignment = ChooseBestAssignment(pDesiredYaw, sDesiredYaw, assignment);

        float leftDesiredYaw = (assignment == 0) ? pDesiredYaw : sDesiredYaw;
        float rightDesiredYaw = (assignment == 0) ? sDesiredYaw : pDesiredYaw;

        float leftYaw = ClampLeft(leftDesiredYaw);
        float rightYaw = ClampRight(rightDesiredYaw);

        float usedDistance = Mathf.Clamp(baseDistance, minDistance, maxDistance);

        // Radii for separation check
        float rPrimary = GetPanelRadius(primaryPanel) * radiusMultiplier;
        float rSecondary = GetPanelRadius(secondaryPanel) * radiusMultiplier;
        float desiredSeparation = rPrimary + rSecondary + extraGapMeters;

        // Try a few iterations: (solve env) then (ensure separation) by widening yaw if needed.
        float yawAbs = Mathf.Clamp(Mathf.Max(Mathf.Abs(leftYaw), Mathf.Abs(rightYaw)), minSlotYawDegrees, maxSlotYawDegrees);

        Vector3 leftPos = Vector3.zero, rightPos = Vector3.zero;

        for (int iter = 0; iter < 6; iter++)
        {
            leftYaw = -yawAbs;
            rightYaw = yawAbs;

            // Which panel is on left/right for env solving?
            Transform leftPanel = (assignment == 0) ? primaryPanel : secondaryPanel;
            Transform rightPanel = (assignment == 0) ? secondaryPanel : primaryPanel;

            float leftRadius = GetPanelRadius(leftPanel) * radiusMultiplier;
            float rightRadius = GetPanelRadius(rightPanel) * radiusMultiplier;

            leftPos = SolveSlot(headPos, baseYawRot, leftYaw, usedDistance, heightOffset, leftRadius);
            rightPos = SolveSlot(headPos, baseYawRot, rightYaw, usedDistance, heightOffset, rightRadius);

            float sep = Vector3.Distance(leftPos, rightPos);
            if (sep >= desiredSeparation || yawAbs >= maxSlotYawDegrees) break;

            yawAbs = Mathf.Min(yawAbs + 2f, maxSlotYawDegrees);
        }

        Vector3 primaryTarget = (assignment == 0) ? leftPos : rightPos;
        Vector3 secondaryTarget = (assignment == 0) ? rightPos : leftPos;

        MoveAndFace(primaryPanel, primaryTarget, headPos, ref primaryVel);
        MoveAndFace(secondaryPanel, secondaryTarget, headPos, ref secondaryVel);
    }

    Vector3 SolveSlot(Vector3 headPos, Quaternion baseYawRot, float yawDeg, float desiredDist, float baseHeight, float radius)
    {
        Vector3 forward = (baseYawRot * Quaternion.Euler(0f, yawDeg, 0f)) * Vector3.forward;
        float castRadius = Mathf.Max(0.01f, radius + castRadiusPadding);

        // Height offsets to try: 0, +1, -1, +2, -2, ...
        for (int h = 0; h < Mathf.Max(1, heightTries); h++)
        {
            float hMul = (h == 0) ? 0f : Mathf.Ceil(h * 0.5f) * ((h % 2 == 1) ? 1f : -1f);
            float heightTry = baseHeight + hMul * heightStep;

            // Pull closer if blocked
            for (int d = 0; d < Mathf.Max(1, distanceTries); d++)
            {
                float t = (distanceTries <= 1) ? 0f : (d / (float)(distanceTries - 1));
                float distTry = Mathf.Lerp(desiredDist, minDistance, t);
                distTry = Mathf.Clamp(distTry, minDistance, maxDistance);

                Vector3 candidate = headPos + forward * distTry + Vector3.up * heightTry;

                if (IsClear(headPos, candidate, castRadius))
                    return candidate;

                // If blocked, try clipping to just before the hit (still keeping the same direction)
                if (TryClipBeforeObstacle(headPos, candidate, castRadius, out Vector3 clipped))
                {
                    // Keep height close to intended (optional). Comment out the next line if you prefer the clipped height.
                    clipped.y = headPos.y + heightTry;

                    // If that reintroduced collision, skip; otherwise accept.
                    if (IsClear(headPos, clipped, castRadius))
                        return clipped;
                }
            }
        }

        // Worst-case fallback: just return the desired position.
        return headPos + forward * Mathf.Clamp(desiredDist, minDistance, maxDistance) + Vector3.up * baseHeight;
    }

    bool IsClear(Vector3 headPos, Vector3 targetPos, float radius)
    {
        Vector3 dir = targetPos - headPos;
        float dist = dir.magnitude;
        if (dist < 0.01f) return true;
        dir /= dist;

        // 1) line-of-sight sphere cast
        if (Physics.SphereCast(headPos, radius, dir, out _, dist, environmentMask, QueryTriggerInteraction.Ignore))
            return false;

        // 2) ensure we are not intersecting environment at the target
        if (Physics.CheckSphere(targetPos, radius, environmentMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    bool TryClipBeforeObstacle(Vector3 headPos, Vector3 targetPos, float radius, out Vector3 clippedPos)
    {
        clippedPos = targetPos;

        Vector3 dir = targetPos - headPos;
        float dist = dir.magnitude;
        if (dist < 0.01f) return false;
        dir /= dist;

        if (Physics.SphereCast(headPos, radius, dir, out RaycastHit hit, dist, environmentMask, QueryTriggerInteraction.Ignore))
        {
            float safe = Mathf.Max(0.05f, hit.distance - obstacleClearance);
            clippedPos = headPos + dir * safe;
            return true;
        }

        return false;
    }

    int ChooseBestAssignment(float pYaw, float sYaw, int current)
    {
        float cost0 = Mathf.Abs(ClampLeft(pYaw) - pYaw) + Mathf.Abs(ClampRight(sYaw) - sYaw);
        float cost1 = Mathf.Abs(ClampLeft(sYaw) - sYaw) + Mathf.Abs(ClampRight(pYaw) - pYaw);

        int best = (cost1 + swapHysteresisDegrees < cost0) ? 1 : 0;

        if (best != current)
        {
            float currentCost = (current == 0) ? cost0 : cost1;
            float bestCost = (best == 0) ? cost0 : cost1;
            if (bestCost > currentCost - swapHysteresisDegrees) best = current;
        }

        return best;
    }

    float ClampLeft(float yawDeg)
    {
        float y = yawDeg;
        if (y > 0f) y = -y;
        return Mathf.Clamp(y, -maxSlotYawDegrees, -minSlotYawDegrees);
    }

    float ClampRight(float yawDeg)
    {
        float y = yawDeg;
        if (y < 0f) y = -y;
        return Mathf.Clamp(y, minSlotYawDegrees, maxSlotYawDegrees);
    }

    float SignedYawDeg(Vector3 headPos, Quaternion baseYawRot, Vector3 worldPos)
    {
        Vector3 to = worldPos - headPos;
        to.y = 0f;
        if (to.sqrMagnitude < 1e-6f) return 0f;
        to.Normalize();
        Vector3 fwd = baseYawRot * Vector3.forward;
        return Vector3.SignedAngle(fwd, to, Vector3.up);
    }

    void MoveAndFace(Transform panel, Vector3 targetPos, Vector3 headPos, ref Vector3 vel)
    {
        panel.position = Vector3.SmoothDamp(panel.position, targetPos, ref vel, positionSmoothTime);

        Quaternion targetRot;
        if (faceUserYawOnly)
        {
            Vector3 toHead = headPos - panel.position;
            toHead.y = 0f;
            if (toHead.sqrMagnitude < 1e-6f) toHead = panel.forward;
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

    float GetPanelRadius(Transform panel)
    {
        var rend = panel.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Vector3 ext = rend.bounds.extents;
            return Mathf.Max(ext.x, ext.y, ext.z);
        }

        var rt = panel.GetComponentInChildren<RectTransform>();
        if (rt != null)
        {
            Vector3 s = rt.lossyScale;
            float w = Mathf.Abs(rt.rect.width * s.x);
            float h = Mathf.Abs(rt.rect.height * s.y);
            return 0.5f * Mathf.Max(w, h);
        }

        return 0.25f;
    }
}