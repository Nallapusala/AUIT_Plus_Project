using UnityEngine;

public class OVRUIBillboard : MonoBehaviour
{
    [Tooltip("Center eye anchor from OVRCameraRig.")]
    public Transform centerEye;

    void Awake()
    {
        if (!centerEye)
        {
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig) centerEye = rig.centerEyeAnchor;
        }
    }

    void LateUpdate()
    {
        if (!centerEye) return;

        Vector3 toCamera = centerEye.position - transform.position;
        toCamera.y = 0f;

        if (toCamera.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
    }
}
