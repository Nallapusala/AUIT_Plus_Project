using UnityEngine;

public class DrawerPull : MonoBehaviour
{
    public Transform pull;               // The drawer object (child)
    public Transform cameraTransform;    // Player / XR head
    public float detectionRange = 2f;    // Distance at which it should be fully open
    public float speed = 3f;             // Lerp speed

    public Vector3 closedPos;            // Local position when fully closed
    public Vector3 openPos;              // Local position when fully open

    void Start()
    {
        // Optional: auto-capture closed position from current state
        if (pull != null)
            closedPos = pull.localPosition;
    }

    void Update()
    {
        if (pull == null || cameraTransform == null) return;

        // Distance from camera to drawer
        float distance = Vector3.Distance(cameraTransform.position, pull.position);

        // If within range, treat as open; otherwise closed
        bool shouldOpen = distance < detectionRange;

        Vector3 targetPos = shouldOpen ? openPos : closedPos;

        // Smoothly move drawer between closed and open
        pull.localPosition = Vector3.Lerp(
            pull.localPosition,
            targetPos,
            Time.deltaTime * speed
        );
    }
}