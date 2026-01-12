using UnityEngine;

public class ScaleAndFitOVR : MonoBehaviour
{
    [Header("Physical Play Area (meters)")]
    public float physicalWidth = 2f;   // X dimension of your VR boundary
    public float physicalDepth = 2.5f; // Z dimension of your VR boundary

    [Header("Parent of all virtual objects")]
    public Transform kitchenParent;    // Assign the parent of fridge, eggs, bowl, oven

    [Header("OVR Camera Rig")]
    public Transform OVRCameraRig;     // Assign your OVRCameraRig root

    [Header("Optional Padding (meters)")]
    public float padding = 0.2f;       // Extra space from boundary edges

    void Start()
    {
        if (kitchenParent == null || OVRCameraRig == null)
        {
            Debug.LogError("Please assign both kitchenParent and OVRCameraRig!");
            return;
        }

        // Step 1: Calculate virtual bounds
        Bounds bounds = new Bounds(kitchenParent.position, Vector3.zero);
        Renderer[] renderers = kitchenParent.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("No Renderer components found in children!");
            return;
        }

        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        float virtualWidth = bounds.size.x;
        float virtualDepth = bounds.size.z;

        // Step 2: Calculate scale factor with optional padding
        float scaleX = (physicalWidth - padding) / virtualWidth;
        float scaleZ = (physicalDepth - padding) / virtualDepth;
        float scaleFactor = Mathf.Min(scaleX, scaleZ); // maintain proportions

        // Step 3: Apply scaling
        kitchenParent.localScale *= scaleFactor;

        // Step 4: Recalculate bounds after scaling
        bounds = new Bounds(kitchenParent.position, Vector3.zero);
        foreach (Renderer r in kitchenParent.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(r.bounds);

        // Step 5: Center the kitchen around the player's real-world position
        Vector3 playerPosition = OVRCameraRig.position; // OVR camera root
        Vector3 offset = bounds.center - playerPosition;
        kitchenParent.position -= offset;

        Debug.Log($"Kitchen scaled by {scaleFactor:F2} and centered around player.");
    }
}
