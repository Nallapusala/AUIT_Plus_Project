using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    public Transform cameraTransform;
    public float rotationSpeed = 10f;
    public float maxPitch = 25f; // degrees

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 dir = transform.position - cameraTransform.position;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        Vector3 euler = targetRot.eulerAngles;

        // Convert 0–360 to -180–180
        float pitch = euler.x > 180 ? euler.x - 360 : euler.x;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        targetRot = Quaternion.Euler(pitch, euler.y, 0);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationSpeed
        );
    }
}
