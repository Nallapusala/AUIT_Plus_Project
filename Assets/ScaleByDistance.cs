using UnityEngine;

public class ScaleByDistance : MonoBehaviour
{
    public Transform cameraTransform;
    public float baseDistance = 1.2f;
    public float baseWidth = 0.8f;   // meters
    public float aspectRatio = 16f / 9f;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        float dist = Vector3.Distance(transform.position, cameraTransform.position);
        float scaleFactor = dist / baseDistance;

        float width = baseWidth * scaleFactor;
        float height = width / aspectRatio;

        transform.localScale = new Vector3(width, height, 1f);
    }
}
