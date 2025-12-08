using UnityEngine;

public class FlourCabinet : MonoBehaviour
{
    [Header("Flour cabinet Settings")]
    public Transform flour;               // Door 2 child object
    public Transform cameraTransform;
    public float detectionRange = 2f;
    public float animationSpeed = 2f;

    // Door rotates outward (opens to right)
    public Vector3 doorOpenRotation = new Vector3(0, 0, -90);

    private Vector3 doorClosedRotation;
    private float doorOpenAmount = 0f;

    void Start()
    {
        if (flour != null)
        {
            doorClosedRotation = flour.localRotation.eulerAngles;
        }
    }

    void Update()
    {
        if (flour == null || cameraTransform == null) return;

        float distance = Vector3.Distance(cameraTransform.position, flour.position);

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

        flour.localRotation = targetRotation;
    }
}
