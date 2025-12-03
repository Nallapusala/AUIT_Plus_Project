using Meta.XR.Simulator.Editor;
using UnityEngine;

public class SemanticUIAdapter : MonoBehaviour
{
    [Header("References")]
    public Transform videoPanel;  // Assign InstructionCanvas in Inspector
    public Transform userCamera;  // Assign Main Camera in Inspector

    [Header("Settings")]
    public bool isSemanticMode = false;  // Toggle for Static vs Semantic
    public float adaptationSpeed = 2f;
    public float closeDistance = 1.5f;  // How close panel moves when "fatigued"
    public float normalDistance = 2.5f;  // Normal distance
    private float panelHeight;// remenber initial high

    [Header("Fatigue Simulation")]
    public KeyCode triggerFatigueKey = KeyCode.F;  // Press F to simulate fatigue
    private bool isFatigued = false;

    private Vector3 targetPosition;
    private Vector3 initialPanelPosition;

    void Start()
    {
        if (videoPanel == null || userCamera == null)
        {
            Debug.LogError("SemanticUIAdapter: Missing references!");
            enabled = false;
            return;
        }

        initialPanelPosition = videoPanel.position;
        panelHeight = initialPanelPosition.y; // remenber initial high
        targetPosition = initialPanelPosition;
    }

    void Update()
    {
        // Simulate fatigue detection (press F key in editor or via Quest controller)
        if (Input.GetKeyDown(triggerFatigueKey))
        {
            isFatigued = !isFatigued;
            Debug.Log("Fatigue state: " + isFatigued);
        }

        // Only adapt if semantic mode is enabled
        if (isSemanticMode)
        {
            // Calculate direction only on the horizontal plane 
            Vector3 flatDirection = userCamera.position - videoPanel.position;
            flatDirection.y = 0;
            flatDirection = flatDirection.normalized;

            // Calculate target position based on fatigue state
            if (isFatigued)
            {
                // Move panel closer to user
                /*Vector3 directionToUser = (userCamera.position - videoPanel.position).normalized;
                targetPosition = userCamera.position + directionToUser * closeDistance;
                targetPosition.y = userCamera.position.y;  // Keep at eye level */

                targetPosition = userCamera.position + flatDirection * closeDistance;

            }
            else
            {
                // Return to normal distance
                /* Vector3 directionToUser = (userCamera.position - videoPanel.position).normalized;
                 targetPosition = userCamera.position + directionToUser * normalDistance;
                 targetPosition.y = userCamera.position.y;*/

                targetPosition = userCamera.position + flatDirection * normalDistance;
            }
            targetPosition.y = panelHeight; //Height fixed at initial height

            // Smoothly move panel to target position
            videoPanel.position = Vector3.Lerp(videoPanel.position, targetPosition, Time.deltaTime * adaptationSpeed);

            // Always face user
            videoPanel.LookAt(userCamera);
            videoPanel.Rotate(0, 180, 0);  // Flip to face user
        }
        else
        {
            // Static mode: keep panel at initial position
            videoPanel.position = initialPanelPosition;
        }
    }
}
