using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Linq; // Needed for sorting files

public class AutoLoggingManager : MonoBehaviour
{
    [Header("Experiment Configuration")]
    [Tooltip("If 'Static', panels won't move. If 'Adaptive', they follow.")]
    public string conditionName = "Adaptive_Dual";

    [Header("References")]
    public Transform head;
    public Transform panel1; // Video
    public Transform panel2; // Instructions

    [Header("Layout Settings")]
    public float distance = 1.5f;
    public float spacing = 0.1f;
    public float panel1Width = 1.0f;
    public float panel2Width = 0.5f;
    public float smoothTime = 0.2f;

    // Internal State
    private StringBuilder csvLog;
    private float startTime;
    private string saveFolder;
    private float logTimer = 0f;
    private string currentParticipantID;

    // Movement State
    private Vector3 currentCenterPos;
    private Vector3 centerVelocity;
    private Quaternion currentRot;

    void Start()
    {
        if (!head) head = Camera.main.transform;

        // 1. Setup Path
        if (Application.isEditor)
            saveFolder = Path.Combine(Application.dataPath, "ExperimentData");
        else
            saveFolder = Path.Combine(Application.persistentDataPath, "ExperimentData");

        if (!Directory.Exists(saveFolder)) Directory.CreateDirectory(saveFolder);

        // 2. AUTO-CALCULATE ID (The Magic Part)
        currentParticipantID = GetNextParticipantID();
        Debug.Log($"AUTO-ASSIGNED ID: {currentParticipantID}");

        // 3. Initialize Positions
        currentCenterPos = head.position + (head.forward * distance);
        currentRot = Quaternion.LookRotation(head.forward);

        // 4. Start Logging Immediately
        StartLogging();
    }

    void LateUpdate()
    {
        if (!head) return;

        // Adaptive Placement Logic
        HandleAdaptivePlacement();

        // Logging Logic (10Hz)
        logTimer += Time.deltaTime;
        if (logTimer >= 0.1f)
        {
            LogFrame();
            logTimer = 0f;
        }
    }

    string GetNextParticipantID()
    {
        // Check folder for existing files like "Participant_01..."
        var info = new DirectoryInfo(saveFolder);
        var files = info.GetFiles("Participant_*.csv");

        int nextID = 1;

        // If files exist, find the highest number
        if (files.Length > 0)
        {
            foreach (var file in files)
            {
                // Parse filename to find numbers
                string name = file.Name; // e.g., Participant_03_...
                string[] parts = name.Split('_');
                if (parts.Length > 1 && int.TryParse(parts[1], out int id))
                {
                    if (id >= nextID) nextID = id + 1;
                }
            }
        }

        // Return formatted ID "P01", "P02", etc.
        return $"Participant_{nextID:00}";
    }

    void HandleAdaptivePlacement()
    {
        // --- Same adaptive logic as before ---
        Vector3 flatForward = head.forward;
        flatForward.y = 0;
        Vector3 targetPos = head.position + (flatForward.normalized * distance);
        currentCenterPos = Vector3.SmoothDamp(currentCenterPos, targetPos, ref centerVelocity, smoothTime);

        Vector3 dirToHead = (head.position - currentCenterPos).normalized;
        Quaternion targetRot = Quaternion.LookRotation(-dirToHead, Vector3.up);
        currentRot = Quaternion.Slerp(currentRot, targetRot, Time.deltaTime * 5f);

        float totalWidth = panel1Width + panel2Width + spacing;
        Vector3 right = currentRot * Vector3.right;

        if (panel1 && panel1.gameObject.activeInHierarchy)
        {
            panel1.position = currentCenterPos - (right * totalWidth * 0.5f) + (right * panel1Width * 0.5f);
            panel1.rotation = currentRot;
        }

        if (panel2 && panel2.gameObject.activeInHierarchy)
        {
            panel2.position = currentCenterPos + (right * totalWidth * 0.5f) - (right * panel2Width * 0.5f);
            panel2.rotation = currentRot;
        }
    }

    void StartLogging()
    {
        csvLog = new StringBuilder();
        // Header
        csvLog.AppendLine("Time,ParticipantID,Condition,HeadX,HeadY,HeadZ,HeadYaw,HeadPitch,GazeTarget");

        startTime = Time.time;
    }

    void LogFrame()
    {
        string gazeTarget = "None";
        RaycastHit hit;
        if (Physics.Raycast(head.position, head.forward, out hit, 20f))
        {
            if (hit.transform == panel1) gazeTarget = "Video";
            else if (hit.transform == panel2) gazeTarget = "Instructions";
        }

        float timeStamp = Time.time - startTime;
        string row = string.Format("{0:F2},{1},{2},{3:F3},{4:F3},{5:F3},{6:F1},{7:F1},{8}",
            timeStamp,
            currentParticipantID,
            conditionName,
            head.position.x, head.position.y, head.position.z,
            head.eulerAngles.y, head.eulerAngles.x,
            gazeTarget
        );
        csvLog.AppendLine(row);
    }

    private void OnApplicationQuit()
    {
        // Save file: Participant_01_Adaptive_Dual_20260113.csv
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        string fileName = $"{currentParticipantID}_{conditionName}_{timestamp}.csv";
        string fullPath = Path.Combine(saveFolder, fileName);

        File.WriteAllText(fullPath, csvLog.ToString());
        Debug.Log($"SAVED CSV: {fullPath}");
    }
}