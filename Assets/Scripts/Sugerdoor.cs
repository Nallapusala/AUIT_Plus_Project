using UnityEngine;

public class SugerCabinet : MonoBehaviour
{
    [Header("Suger Settings")]
    public Transform Suger;               // Door 2 child object
    public Transform cameraTransform;
    public float detectionRange = 2f;
    public float animationSpeed = 2f;

    // Door rotates outward (opens to right)
    public Vector3 doorOpenRotation = new Vector3(180, 0, 0);

    private Vector3 doorClosedRotation;
    private float doorOpenAmount = 0f;

    void Start()
    {
        if (Suger != null)
        {
            doorClosedRotation = Suger.localRotation.eulerAngles;
        }
    }

    void Update()
    {
        if (Suger == null || cameraTransform == null) return;

        float distance = Vector3.Distance(cameraTransform.position, Suger.position);

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

        Suger.localRotation = targetRotation;
    }
}
