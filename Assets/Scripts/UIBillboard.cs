using UnityEngine;

/// <summary>
/// Simple billboard: keeps this object facing the user camera on the horizontal plane.
/// Position is fully controlled by AUIT; this script only adjusts rotation.
/// </summary>
public class UIBillboard : MonoBehaviour
{
    [Tooltip("User camera, usually the Main Camera under the XR Origin.")]
    public Transform userCamera;

    private void LateUpdate()
    {
        // Try to auto-assign the main camera if none is set.
        if (userCamera == null)
        {
            if (Camera.main != null)
                userCamera = Camera.main.transform;
            else
                return;
        }

        // Direction from this object to the camera (horizontal only).
        Vector3 toCamera = userCamera.position - transform.position;
        toCamera.y = 0f;

        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        // Face the user ¨C use the *opposite* direction so the front side is visible.
        transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
    }
}
