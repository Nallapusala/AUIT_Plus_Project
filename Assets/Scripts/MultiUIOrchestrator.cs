using UnityEngine;
using System.Collections.Generic;

public class NoOverlapOrchestrator : MonoBehaviour
{
    public enum LayoutMode { SideBySide, Stacked }

    [Header("Layout Settings")]
    public LayoutMode layoutMode = LayoutMode.SideBySide;

    [Tooltip("Distance from the headset")]
    public float distance = 1.5f;

    [Tooltip("Gap between the panels in meters")]
    public float spacing = 0.1f; // Increased default gap

    [Header("Manual Size Overrides (Set > 0 to force size)")]
    [Tooltip("If auto-size fails, type the width of Panel 1 here (e.g., 0.8 for video)")]
    public float panel1ManualSize = 0f;
    [Tooltip("If auto-size fails, type the width of Panel 2 here")]
    public float panel2ManualSize = 0f;

    [Header("References")]
    public Transform head;
    public Transform panel1; // The Video
    public Transform panel2; // The Instructions

    [Header("Smoothing")]
    public float movementSmoothTime = 0.2f;
    public float rotationSmoothTime = 0.1f;

    // Internal State
    private Vector3 currentCenterPos;
    private Quaternion currentRotation;
    private Vector3 velocity;

    void Start()
    {
        if (head == null) head = Camera.main.transform;

        // Initialize position to avoid flying in from (0,0,0)
        currentCenterPos = GetTargetPosition();
        currentRotation = Quaternion.LookRotation(head.forward);
    }

    void LateUpdate()
    {
        if (!head) return;

        // 1. Calculate the ideal "Group Center" in front of the user
        Vector3 targetPos = GetTargetPosition();

        // Smoothly move the center point
        currentCenterPos = Vector3.SmoothDamp(currentCenterPos, targetPos, ref velocity, movementSmoothTime);

        // 2. Rotate to face user (Billboarding)
        Vector3 dirToHead = (head.position - currentCenterPos).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dirToHead, Vector3.up);
        // Correct for UI usually facing backwards, or just LookRotation(dirToHead) if using 3D plane
        // If your UI is invisible, flip the sign below:
        currentRotation = Quaternion.Slerp(currentRotation, Quaternion.LookRotation(-dirToHead, Vector3.up), Time.deltaTime / rotationSmoothTime);

        // 3. Calculate Layout
        bool p1Active = panel1 != null && panel1.gameObject.activeInHierarchy;
        bool p2Active = panel2 != null && panel2.gameObject.activeInHierarchy;

        if (!p1Active && !p2Active) return;

        if (p1Active && !p2Active)
        {
            // Only Panel 1
            PositionPanel(panel1, currentCenterPos, currentRotation);
        }
        else if (!p1Active && p2Active)
        {
            // Only Panel 2
            PositionPanel(panel2, currentCenterPos, currentRotation);
        }
        else
        {
            // Both Active - SEPARATE THEM
            ApplySeparation(panel1, panel2);
        }
    }

    void ApplySeparation(Transform p1, Transform p2)
    {
        // Get sizes (Use manual override if provided!)
        float size1 = (panel1ManualSize > 0) ? panel1ManualSize : GetAutoWorldSize(p1);
        float size2 = (panel2ManualSize > 0) ? panel2ManualSize : GetAutoWorldSize(p2);

        float totalSpan = size1 + size2 + spacing;

        // Calculate the "Right" vector based on the group rotation
        Vector3 right = currentRotation * Vector3.right;
        Vector3 up = currentRotation * Vector3.up;

        if (layoutMode == LayoutMode.SideBySide)
        {
            // Move Left relative to center
            Vector3 p1Pos = currentCenterPos - (right * (totalSpan / 2f)) + (right * (size1 / 2f));
            // Move Right relative to center
            Vector3 p2Pos = currentCenterPos + (right * (totalSpan / 2f)) - (right * (size2 / 2f));

            PositionPanel(p1, p1Pos, currentRotation);
            PositionPanel(p2, p2Pos, currentRotation);
        }
        else // Stacked
        {
            // Move Up
            Vector3 p1Pos = currentCenterPos + (up * (totalSpan / 2f)) - (up * (size1 / 2f));
            // Move Down
            Vector3 p2Pos = currentCenterPos - (up * (totalSpan / 2f)) + (up * (size2 / 2f));

            PositionPanel(p1, p1Pos, currentRotation);
            PositionPanel(p2, p2Pos, currentRotation);
        }
    }

    void PositionPanel(Transform t, Vector3 pos, Quaternion rot)
    {
        t.position = pos;
        t.rotation = rot;
    }

    // Helper to try and guess size if manual size isn't set
    float GetAutoWorldSize(Transform t)
    {
        // 1. Try RectTransform (UI)
        var rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            if (layoutMode == LayoutMode.SideBySide)
                return rt.rect.width * t.lossyScale.x;
            else
                return rt.rect.height * t.lossyScale.y;
        }

        // 2. Try Mesh Renderer (3D Objects / Quads)
        var rend = t.GetComponent<Renderer>();
        if (rend != null)
        {
            if (layoutMode == LayoutMode.SideBySide)
                return rend.bounds.size.x;
            else
                return rend.bounds.size.y;
        }

        return 0.5f; // Fallback
    }

    Vector3 GetTargetPosition()
    {
        Vector3 flatForward = head.forward;
        flatForward.y = 0;
        return head.position + flatForward.normalized * distance;
    }
}