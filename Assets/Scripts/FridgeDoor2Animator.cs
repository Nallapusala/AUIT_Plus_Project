using UnityEngine;

public class FridgeDoor2Animator : MonoBehaviour
{
    [Header("Door 2 Settings")]
    public Transform door2;               // Door 2 child object
    public Transform cameraTransform;
    public float detectionRange = 2f;
    public float animationSpeed = 2f;

    // Door rotates outward (opens to right)
    public Vector3 doorOpenRotation = new Vector3(0, 0, -180);

    private Vector3 doorClosedRotation;
    private float doorOpenAmount = 0f;

    void Start()
    {
        if (door2 != null)
        {
            doorClosedRotation = door2.localRotation.eulerAngles;
        }
    }

    void Update()
    {
        if (door2 == null || cameraTransform == null) return;

        float distance = Vector3.Distance(cameraTransform.position, door2.position);

        if (distance < detectionRange)
        {
            doorOpenAmount = Mathf.Lerp(doorOpenAmount, 1f, Time.deltaTime * animationSpeed);
        }
        else
        {
            doorOpenAmount = Mathf.Lerp(doorOpenAmount, 0f, Time.deltaTime * animationSpeed);
        }

        Quaternion targetRotation = Quaternion.Lerp(
            Quaternion.Euler(doorClosedRotation),
            Quaternion.Euler(doorClosedRotation + doorOpenRotation),
            doorOpenAmount
        );

        door2.localRotation = targetRotation;
    }
}