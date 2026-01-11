using UnityEngine;

public class UIBillboard : MonoBehaviour
{
    public Transform userCamera;
    [Tooltip("If your UI appears mirrored, enable this to flip 180 degrees.")]
    public bool flip180 = true;

    private void LateUpdate()
    {
        if (userCamera == null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            userCamera = cam.transform;
        }

        var camPos = userCamera.position;
        var uiPos = transform.position;

        Vector3 dir = (uiPos - camPos);
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;

        // This makes the UI look at the camera (front facing camera)
        var rot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (flip180) rot *= Quaternion.Euler(0f, 180f, 0f);

        transform.rotation = rot;
    }
}
