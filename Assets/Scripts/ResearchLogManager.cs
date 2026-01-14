using UnityEngine;
using System.IO;
using System.Text;
using System;

public class ResearchLogManager : MonoBehaviour
{
    public enum ScenarioMode { Static, AUIT_Single, AUIT_Multi }

    [Header("1. Participant Identity")]
    public string participantID = "P01";
    public string sequenceOrder = "ABC";
    public ScenarioMode activeScenario;

    [Header("2. References (Drag from Scene)")]
    public Transform head;             // CenterEyeAnchor
    public Transform videoPanel;
    public Transform instructionPanel;
    [Tooltip("Select the layer for walls/cabinets to track occlusion")]
    public LayerMask environmentLayer;

    [Header("3. Status")]
    public bool isRecording = false;

    private string currentFilePath;
    private float trialStartTime;
    private StringBuilder buffer = new StringBuilder();

    [Header("4. Gaze Detection")]
    public LayerMask gazeUiLayer;           // set to GazeUI layer in Inspector
    public float gazeMaxDistance = 20f;
    public float gazeSphereRadius = 0.02f;  // 2 cm helps a lot in VR (slight head jitter)

    // Runs automatically when you press PLAY
    void OnEnable()
    {
        if (!head) head = Camera.main.transform;

        // Path: Assets/ExperimentData
        string folder = Path.Combine(Application.dataPath, "ExperimentData");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string fileName = $"{participantID}_{activeScenario}_{sequenceOrder}_{DateTime.Now:HHmm}.csv";
        currentFilePath = Path.Combine(folder, fileName);

        // 21 Metrics for HCI Paper (January 19th Submission)
        buffer.Clear();
        buffer.AppendLine("Time,ParticipantID,Scenario,Sequence,HeadX,HeadY,HeadZ,HeadYaw,HeadPitch," +
                        "VideoX,VideoY,VideoZ,InstX,InstY,InstZ," +
                        "GazeTarget,AngleToVideo,AngleToInst,VideoOcc,InstOcc,HitObjectName");

        trialStartTime = Time.time;
        isRecording = true;
        Debug.Log($"<color=cyan><b>LOGGING STARTED:</b></color> Scenario: {activeScenario}");
    }

    void Update()
    {
        // Capture at 10Hz (Standard for HCI study)
        if (isRecording && Time.frameCount % 10 == 0) RecordMetrics();
    }

    void RecordMetrics()
    {
        float t = Time.time - trialStartTime;

        // 1. Visibility & Comfort Angles
        float angV = Vector3.Angle(head.forward, videoPanel.position - head.position);
        float angI = (instructionPanel != null && instructionPanel.gameObject.activeInHierarchy) ?
                     Vector3.Angle(head.forward, instructionPanel.position - head.position) : -1f;

        // 2. Environment Occlusion (Linecasts)
        bool vOcc = Physics.Linecast(head.position, videoPanel.position, environmentLayer);
        bool iOcc = (instructionPanel != null && instructionPanel.gameObject.activeInHierarchy) &&
                    Physics.Linecast(head.position, instructionPanel.position, environmentLayer);

        // 3. Robust Gaze Detection
        // 3. UI-first gaze detection (prevents wall/hands stealing the hit)
        string gaze = "World";
        string hitName = "None";
        RaycastHit hit;

        // Prefer UI panels ONLY
        bool uiHit = Physics.SphereCast(
            head.position,
            gazeSphereRadius,
            head.forward,
            out hit,
            gazeMaxDistance,
            gazeUiLayer,
            QueryTriggerInteraction.Collide
        );

        if (uiHit)
        {
            hitName = hit.transform.name;

            // Video check (hit object belongs to videoPanel hierarchy)
            if (videoPanel != null && (hit.transform == videoPanel || hit.transform.IsChildOf(videoPanel)))
                gaze = "Video";

            // Instructions check
            else if (instructionPanel != null && (hit.transform == instructionPanel || hit.transform.IsChildOf(instructionPanel)))
                gaze = "Instructions";
        }
        else
        {
            // Optional: secondary raycast to log what the user is looking at in the world (for debugging)
            int worldMask = ~(1 << 2); // everything except Ignore Raycast
            if (Physics.Raycast(head.position, head.forward, out hit, gazeMaxDistance, worldMask, QueryTriggerInteraction.Ignore))
                hitName = hit.transform.name;
        }

        // MASK: ~(1 << 2) tells the raycast to hit everything EXCEPT Layer 2 (Ignore Raycast).
        // QueryTriggerInteraction.Collide ensures we hit UI/Trigger colliders even if they are set as triggers.
        int layerMask = ~(1 << 2);

        if (Physics.Raycast(head.position, head.forward, out hit, 20f, layerMask, QueryTriggerInteraction.Collide))
        {
            hitName = hit.transform.name;

            // Check for Video (Reference check OR Child check OR name-based fallback)
            if (videoPanel != null && (hit.transform == videoPanel || hit.transform.IsChildOf(videoPanel) || hitName.ToLower().Contains("video")))
                gaze = "Video";

            // Check for Instructions (Reference check OR Child check OR name-based fallback)
            else if (instructionPanel != null && (hit.transform == instructionPanel || hit.transform.IsChildOf(instructionPanel) || hitName.ToLower().Contains("inst")))
                gaze = "Instructions";
        }

        // --- Create Data Row ---
        string row = string.Format("{0:F2},{1},{2},{3},{4:F3},{5:F3},{6:F3},{7:F1},{8:F1},{9:F3},{10:F3},{11:F3},{12:F3},{13:F3},{14:F3},{15},{16:F1},{17:F1},{18},{19},{20}\n",
            t, participantID, activeScenario, sequenceOrder,
            head.position.x, head.position.y, head.position.z, head.eulerAngles.y, head.eulerAngles.x, // Head
            videoPanel.position.x, videoPanel.position.y, videoPanel.position.z, // Video
            instructionPanel.position.x, instructionPanel.position.y, instructionPanel.position.z, // Instructions
            gaze, angV, angI, vOcc, iOcc, hitName);

        buffer.Append(row);
    }

    // Runs automatically when you press STOP
    void OnDisable()
    {
        if (!isRecording) return;
        isRecording = false;

        File.WriteAllText(currentFilePath, buffer.ToString());
        Debug.Log($"<color=green><b>LOGGING SAVED:</b></color> {currentFilePath}");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}