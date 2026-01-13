using UnityEngine;
using System.IO;
using System.Text;
using System;

public class SimpleParticipantLogger : MonoBehaviour
{
    [Header("Configuration")]
    public string conditionName = "Adaptive_Dual";

    [Header("References (Drag these in!)")]
    public Transform head;           // CenterEyeAnchor
    public Transform videoPanel;     // Your Video Panel
    public Transform instructionPanel; // Your Instruction Panel

    // --- Internal Logging State ---
    private StringBuilder csvLog;
    private string participantID;
    private float startTime;
    private float logTimer = 0f;
    private string saveFolder;

    void Start()
    {
        if (!head) head = Camera.main.transform;

        // 1. Setup Save Folder
        if (Application.isEditor)
            saveFolder = Path.Combine(Application.dataPath, "ExperimentData");
        else
            saveFolder = Path.Combine(Application.persistentDataPath, "ExperimentData");

        if (!Directory.Exists(saveFolder)) Directory.CreateDirectory(saveFolder);

        // 2. Generate ID (Participant_01, 02, etc.)
        participantID = GetNextParticipantID();

        // 3. Start Logging
        csvLog = new StringBuilder();
        // CSV Header as requested
        csvLog.AppendLine("Time,ParticipantID,Condition,HeadX,HeadY,HeadZ,HeadYaw,HeadPitch,GazeTarget");
        startTime = Time.time;

        Debug.Log($"[Logger] Recording started for: {participantID}");
    }

    void Update()
    {
        // Log data every 0.1 seconds (10Hz)
        logTimer += Time.deltaTime;
        if (logTimer >= 0.1f)
        {
            CaptureFrame();
            logTimer = 0f;
        }
    }

    void CaptureFrame()
    {
        if (!head) return;

        // Check what the user is looking at
        string target = "None";
        RaycastHit hit;
        // Raycast 20 meters forward
        if (Physics.Raycast(head.position, head.forward, out hit, 20f))
        {
            // Check if we hit one of our panels (or their children)
            if (videoPanel && (hit.transform == videoPanel || hit.transform.IsChildOf(videoPanel)))
                target = "Video";
            else if (instructionPanel && (hit.transform == instructionPanel || hit.transform.IsChildOf(instructionPanel)))
                target = "Instructions";
        }

        // Format the data row
        string row = string.Format("{0:F2},{1},{2},{3:F3},{4:F3},{5:F3},{6:F1},{7:F1},{8}",
            Time.time - startTime,
            participantID,
            conditionName,
            head.position.x, head.position.y, head.position.z,
            head.eulerAngles.y, head.eulerAngles.x,
            target
        );

        csvLog.AppendLine(row);
    }

    string GetNextParticipantID()
    {
        // Counts existing files to auto-number the next one
        var info = new DirectoryInfo(saveFolder);
        var files = info.GetFiles("Participant_*.csv");
        int nextID = 1;

        foreach (var file in files)
        {
            string[] parts = file.Name.Split('_');
            if (parts.Length > 1 && int.TryParse(parts[1], out int id))
            {
                if (id >= nextID) nextID = id + 1;
            }
        }
        return $"Participant_{nextID:00}";
    }

    void OnApplicationQuit()
    {
        // Save to file when you stop the game
        if (csvLog != null && csvLog.Length > 0)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            string path = Path.Combine(saveFolder, $"{participantID}_{conditionName}_{timestamp}.csv");
            File.WriteAllText(path, csvLog.ToString());
            Debug.Log($"[Logger] Saved CSV to: {path}");
        }
    }
}