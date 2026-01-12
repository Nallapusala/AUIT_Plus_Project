using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    public Transform cameraTransform;
    public float rotationSpeed = 10f;
    public float maxPitch = 25f;

    void Start()
    {
        if (cameraTransform == null)
        {
            OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null)
                cameraTransform = rig.centerEyeAnchor;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 dir = cameraTransform.position - transform.position;

        // Remove roll influence completely
        dir.Normalize();

        // Extract yaw only
        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        // Optional pitch (very small!)
        float pitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        Quaternion targetRot = Quaternion.Euler(pitch, yaw+180f, 0);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationSpeed
        );
    }
}