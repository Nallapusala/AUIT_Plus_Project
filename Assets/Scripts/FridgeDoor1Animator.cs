using UnityEngine;

public class FridgeDoor1Animator : MonoBehaviour
{
    [Header("Door 1 Settings")]
    public Transform door1;               // Door 1 child object
    public Transform cameraTransform;
    public float detectionRange = 2f;
    public float animationSpeed = 2f;

    // Door rotates outward (opens to left)
    public Vector3 doorOpenRotation = new Vector3(0, 0, -180);

    private Vector3 doorClosedRotation;
    private float doorOpenAmount = 0f;

    void Start()
    {
        if (door1 != null)
        {
            doorClosedRotation = door1.localRotation.eulerAngles;
        }
    }

    void Update()
    {
        if (door1 == null || cameraTransform == null) return;

        float distance = Vector3.Distance(cameraTransform.position, door1.position);

        if (distance < detectionRange)
        {
            doorOpenAmount = Mathf.Lerp(doorOpenAmount, 1f, Time.deltaTime * animationSpeed);
        }
        else
        {
            doorOpenAmount = Mathf.Lerp(doorOpenAmount, 0f, Time.deltaTime * animationSpeed);
        }

        // ← IMPORTANT: Use localRotation!
        Quaternion targetRotation = Quaternion.Lerp(
            Quaternion.Euler(doorClosedRotation),
            Quaternion.Euler(doorClosedRotation + doorOpenRotation),
            doorOpenAmount
        );

        door1.localRotation = targetRotation;
    }
}