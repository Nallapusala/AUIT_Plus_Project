using System.Collections;
using UnityEngine;

public class ReturnToHome : MonoBehaviour
{
    public float returnDuration = 0.35f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    Vector3 homePos;
    Quaternion homeRot;

    Rigidbody rb;
    Collider[] cols;
    Coroutine running;

    void Awake()
    {
        homePos = transform.position;
        homeRot = transform.rotation;

        rb = GetComponent<Rigidbody>();
        cols = GetComponentsInChildren<Collider>(true);
    }

    public void ResetHomePoseNow()
    {
        homePos = transform.position;
        homeRot = transform.rotation;
    }

    public void Return()
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ReturnRoutine());
    }

    IEnumerator ReturnRoutine()
    {
        // Disable physics + collisions during return to avoid snagging on bowl/counters.
        bool hadRb = rb != null;
        bool wasKinematic = false;

        if (hadRb)
        {
            wasKinematic = rb.isKinematic;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        foreach (var c in cols) c.enabled = false;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float t = 0f;
        while (t < returnDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / returnDuration);
            float e = ease != null ? ease.Evaluate(u) : u;

            transform.position = Vector3.Lerp(startPos, homePos, e);
            transform.rotation = Quaternion.Slerp(startRot, homeRot, e);
            yield return null;
        }

        transform.position = homePos;
        transform.rotation = homeRot;

        foreach (var c in cols) c.enabled = true;
        if (hadRb) rb.isKinematic = wasKinematic;

        running = null;
    }
}
