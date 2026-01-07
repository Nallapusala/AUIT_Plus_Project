using UnityEngine;

public class AdaptiveVideoCanvas : MonoBehaviour
{
    public Transform head;
    public float preferredDistance = 1.5f;
    public float wallBuffer = 0.3f;
    public LayerMask environment;

    private RectTransform rect;
    private float halfDepth = 0.01f; // fake thickness

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        Vector3 dir = head.forward;
        Vector3 desiredPos = head.position + dir * preferredDistance;

        float maxDistance = preferredDistance + wallBuffer;

        if (Physics.Raycast(head.position, dir, out RaycastHit hit,
            maxDistance, environment))
        {
            // push canvas fully in front of wall
            desiredPos = hit.point - dir * wallBuffer;
        }

        MoveCanvas(desiredPos);
    }

    void MoveCanvas(Vector3 target)
    {
        transform.position =
            Vector3.Lerp(transform.position, target, Time.deltaTime * 4f);

        // Always face user
        transform.rotation =
            Quaternion.LookRotation(transform.position - head.position);
    }
}
