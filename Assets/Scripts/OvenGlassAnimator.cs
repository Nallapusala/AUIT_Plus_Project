using UnityEngine;

public class OvenGlassAnimator : MonoBehaviour
{
    [Header("Glass Settings")]
    public Transform ovenGlass;           // The glass child object
    public Transform cameraTransform;     // Main camera
    public float detectionRange = 2f;
    public float animationSpeed = 2f;

    // Glass opens by rotating (e.g., 90 degrees forward)
    public Vector3 glassTiltRotation = new Vector3(90, 0, 0);  // Tilt up

    private Vector3 glassClosedRotation;
    private float glassOpenAmount = 0f;

    void Start()
    {
        if (ovenGlass != null)
        {
            // Remember starting rotation (closed position)
            glassClosedRotation = ovenGlass.localRotation.eulerAngles;
        }
    }

    void Update()
    {
        if (ovenGlass == null || cameraTransform == null) return;

        // Check distance from oven
        float distance = Vector3.Distance(cameraTransform.position, ovenGlass.position);

        // If close → glass tilts open
        if (distance < detectionRange)
        {
            glassOpenAmount = Mathf.Lerp(glassOpenAmount, 1f, Time.deltaTime * animationSpeed);
        }
        else
        {
            glassOpenAmount = Mathf.Lerp(glassOpenAmount, 0f, Time.deltaTime * animationSpeed);
        }

        // Apply rotation - IMPORTANT: Use localRotation for child!
        Quaternion targetRotation = Quaternion.Lerp(
            Quaternion.Euler(glassClosedRotation),
            Quaternion.Euler(glassClosedRotation + glassTiltRotation),
            glassOpenAmount
        );

        ovenGlass.localRotation = targetRotation;  // ← localRotation for nested objects!
    }
}   