using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MixingBowlFill : MonoBehaviour
{
    [Header("Fill Stages (ordered)")]
    [Tooltip("Stage 0 can be empty (optional). Example: Empty, 25%, 50%, 75%, Full")]
    public GameObject[] fillStagePrefabs;

    [Tooltip("How many distinct ingredients are needed to reach FULL.")]
    public int ingredientsForFull = 4;

    [Header("Fill Spawn")]
    public Transform fillSpawnPoint;
    public Vector3 fillLocalOffset = new Vector3(0f, 0.05f, 0f);

    [Header("Ingredient Handling")]
    [Tooltip("If true, each ingredient only increases fill once. It will still be returned on later drops.")]
    public bool countEachIngredientOnce = true;

    [Tooltip("If true, ingredient entering while still grabbed will be force-released.")]
    public bool forceReleaseIfGrabbed = false;

    int ingredientCount = 0;
    int currentStageIndex = -1;
    GameObject currentFillInstance;

    // Track ingredients currently inside the bowl trigger
    readonly HashSet<int> inside = new HashSet<int>();

    // Track ingredients already counted (consumed)
    readonly HashSet<int> counted = new HashSet<int>();

    // Prevent duplicate coroutines per ingredient
    readonly HashSet<int> processing = new HashSet<int>();

    void Start()
    {
        ApplyFillStage(CalcStageIndex());
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ingredient")) return;

        GameObject ingredient = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        int id = ingredient.GetInstanceID();

        inside.Add(id);

        if (!processing.Contains(id))
            StartCoroutine(HandleIngredient(ingredient, id));
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Ingredient")) return;

        GameObject ingredient = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        inside.Remove(ingredient.GetInstanceID());
    }

    IEnumerator HandleIngredient(GameObject ingredient, int id)
    {
        processing.Add(id);

        // Wait until released while still inside the bowl (prevents accidental counts).
        OVRGrabbable grabbable = ingredient.GetComponent<OVRGrabbable>();

        if (grabbable != null && grabbable.isGrabbed && forceReleaseIfGrabbed && grabbable.grabbedBy != null)
        {
            grabbable.grabbedBy.ForceRelease(grabbable);
        }

        // If it was grabbed, wait for user to release (or force release above).
        while (grabbable != null && grabbable.isGrabbed)
        {
            // If user moved it out without dropping, stop processing.
            if (!inside.Contains(id))
            {
                processing.Remove(id);
                yield break;
            }
            yield return null;
        }

        // Still inside after release?
        if (!inside.Contains(id))
        {
            processing.Remove(id);
            yield break;
        }

        // Count (optionally only once per ingredient)
        bool alreadyCounted = counted.Contains(id);
        if (!alreadyCounted || !countEachIngredientOnce)
        {
            ingredientCount++;
            if (countEachIngredientOnce) counted.Add(id);

            ApplyFillStage(CalcStageIndex());
        }

        // Return ingredient home (always)
        var returner = ingredient.GetComponent<ReturnToHome>();
        if (returner != null) returner.Return();

        // Cooldown so it can exit trigger without reprocessing immediately
        yield return new WaitForSeconds(0.2f);
        processing.Remove(id);
    }

    int CalcStageIndex()
    {
        if (fillStagePrefabs == null || fillStagePrefabs.Length == 0) return -1;
        if (ingredientsForFull <= 0) ingredientsForFull = 1;

        float ratio = Mathf.Clamp01((float)ingredientCount / ingredientsForFull);
        int last = fillStagePrefabs.Length - 1;

        // Empty -> ... -> Full thresholds aligned to number of stages.
        int idx = Mathf.FloorToInt(ratio * last + 1e-6f);
        return Mathf.Clamp(idx, 0, last);
    }

    void ApplyFillStage(int stageIndex)
    {
        if (stageIndex < 0) return;
        if (stageIndex == currentStageIndex) return;

        currentStageIndex = stageIndex;

        if (currentFillInstance != null)
            Destroy(currentFillInstance);

        Transform sp = fillSpawnPoint != null ? fillSpawnPoint : transform;

        GameObject prefab = fillStagePrefabs[stageIndex];
        if (prefab == null) return;

        currentFillInstance = Instantiate(
            prefab,
            sp.position + sp.TransformVector(fillLocalOffset),
            sp.rotation,
            transform
        );
    }
}
