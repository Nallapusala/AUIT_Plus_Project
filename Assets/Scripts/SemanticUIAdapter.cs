using UnityEngine;

public class SemanticUIAdapter : MonoBehaviour
{
    [Header("References")]
    public Transform videoPanel;   // Drag InstructionCanvas or VideoPanel here in the Inspector
    public Transform userCamera;   // Drag the Main Camera under XR Origin here in the Inspector

    [Header("Settings")]
    public bool isSemanticMode = false;  // Toggle between static mode and semantic (adaptive) mode
    public float adaptationSpeed = 2f;
    public float closeDistance = 1.5f;   // Distance to the user when "fatigued" (closer)
    public float normalDistance = 2.5f;  // Distance to the user in normal state (farther)

    [Header("Fatigue Simulation")]
    public KeyCode triggerFatigueKey = KeyCode.F;  // Press numeric key 1 to simulate fatigue
    private bool isFatigued = false;

    private Vector3 targetPosition;
    private Vector3 initialPanelPosition;
    private float panelHeight;   // Stores the initial vertical height of the panel

    void Start()
    {
        if (videoPanel == null || userCamera == null)
        {
            Debug.LogError("SemanticUIAdapter: Missing references!");
            enabled = false;
            return;
        }

        initialPanelPosition = videoPanel.position;
        panelHeight = initialPanelPosition.y;   // Remember the initial height
        targetPosition = initialPanelPosition;
    }

    void Update()
    {
        // Toggle fatigue state using the trigger key
        if (Input.GetKeyDown(triggerFatigueKey))
        {
            isFatigued = !isFatigued;
            Debug.Log("Fatigue state: " + isFatigued);
        }

        if (isSemanticMode)
        {
            // Target distance: closer when fatigued, farther when normal
            float targetDistance = isFatigued ? closeDistance : normalDistance;

            // Use the camera's forward direction, projected onto the horizontal plane
            Vector3 flatForward = userCamera.forward;
            flatForward.y = 0f;

            // Fallback if the direction is too small
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = Vector3.forward;
            }

            flatForward.Normalize();

            // Target position = camera position + forward * distance, with fixed height
            targetPosition = userCamera.position + flatForward * targetDistance;
            targetPosition.y = panelHeight;

            // Smoothly move towards the target position
            videoPanel.position = Vector3.Lerp(
                videoPanel.position,
                targetPosition,
                Time.deltaTime * adaptationSpeed
            );

            // Make the panel always face the user
            videoPanel.LookAt(userCamera);
            videoPanel.Rotate(0f, 180f, 0f);
        }
        else
        {
            // Static mode: keep the panel at its initial position
            videoPanel.position = initialPanelPosition;
        }
    }
}
