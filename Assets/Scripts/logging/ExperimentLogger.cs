using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class ExperimentLogger : MonoBehaviour
{
    [Header("IDs (shown in CSV)")]
    public string participantId = "P01";
    public string condition = "AUIT_SinglePanel";   // e.g., "Static", "AUIT_SinglePanel"
    public string trialId = "T01";

    [Header("References")]
    public Transform headTransform;   // XR camera transform
    public Transform panelTransform;  // moving panel root
    public LayerMask occluderMask;    // Environment layer(s) only

    [Header("Sampling")]
    [Range(1f, 60f)] public float sampleRateHz = 10f;  // 10–20 recommended
    public float visibleAngleDeg = 25f;               // “comfortable FOV cone”
    public bool logInEditorConsole = false;

    [Header("Optional: detect big panel jumps as events")]
    public float jumpSpeedThresholdMps = 1.5f; // tune for your AUIT; event if exceeded

    private StreamWriter _frameWriter;
    private StreamWriter _eventWriter;

    private float _trialStartTime;
    private float _nextSampleTime;
    private Vector3 _prevPanelPos;
    private bool _prevOccluded;
    private bool _isTrialRunning;

    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    void Start()
    {
        if (headTransform == null || panelTransform == null)
        {
            Debug.LogError("[ExperimentLogger] Assign headTransform and panelTransform in Inspector.");
            enabled = false;
            return;
        }

        // Auto-start a trial for simplicity (you can control this manually too)
        StartTrial();
    }

    public void StartTrial()
    {
        _trialStartTime = Time.unscaledTime;
        _nextSampleTime = _trialStartTime;
        _prevPanelPos = panelTransform.position;
        _prevOccluded = false;
        _isTrialRunning = true;

        // Create file names (persistentDataPath works in Editor + Quest builds)
        string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string baseName = $"{participantId}_{condition}_{trialId}_{ts}";

        string framePath = Path.Combine(Application.persistentDataPath, $"FrameLog_{baseName}.csv");
        string eventPath = Path.Combine(Application.persistentDataPath, $"EventLog_{baseName}.csv");

        _frameWriter = new StreamWriter(framePath, false, Encoding.UTF8);
        _eventWriter = new StreamWriter(eventPath, false, Encoding.UTF8);

        // Headers
        _frameWriter.WriteLine(
            "participant_id,condition,trial_id,timestamp_s," +
            "head_px,head_py,head_pz,head_fx,head_fy,head_fz," +
            "panel_px,panel_py,panel_pz," +
            "distance_m,angle_deg,visible_01,occluded_01,panel_speed_mps"
        );

        _eventWriter.WriteLine("participant_id,condition,trial_id,timestamp_s,event_type,event_value");

        LogEvent("trial_start", "auto");

        Debug.Log($"[ExperimentLogger] Logging started.\nFrame: {framePath}\nEvent: {eventPath}");
    }

    public void EndTrial(string reason = "manual")
    {
        if (!_isTrialRunning) return;

        LogEvent("trial_end", reason);
        _isTrialRunning = false;

        _frameWriter?.Flush();
        _eventWriter?.Flush();

        _frameWriter?.Close();
        _eventWriter?.Close();

        _frameWriter = null;
        _eventWriter = null;

        Debug.Log("[ExperimentLogger] Logging stopped.");
    }

    void Update()
    {
        if (!_isTrialRunning) return;

        // Sample at fixed rate
        if (Time.unscaledTime < _nextSampleTime) return;
        float now = Time.unscaledTime;
        float dt = Mathf.Max(1e-6f, now - (_nextSampleTime - (1f / sampleRateHz)));
        _nextSampleTime = now + (1f / sampleRateHz);

        Vector3 headPos = headTransform.position;
        Vector3 headFwd = headTransform.forward.normalized;

        Vector3 panelPos = panelTransform.position;

        Vector3 toPanel = panelPos - headPos;
        float distance = toPanel.magnitude;
        Vector3 toPanelDir = (distance > 1e-6f) ? (toPanel / distance) : headFwd;

        float angleDeg = Vector3.Angle(headFwd, toPanelDir);
        int visible01 = (angleDeg <= visibleAngleDeg) ? 1 : 0;

        // Occlusion: if ray hits Environment before reaching panel distance, it's occluded.
        int occluded01 = 0;
        if (distance > 1e-3f)
        {
            if (Physics.Raycast(headPos, toPanelDir, out RaycastHit hit, distance, occluderMask, QueryTriggerInteraction.Ignore))
            {
                occluded01 = 1;
            }
        }

        // Panel speed (stability)
        float panelSpeed = Vector3.Distance(panelPos, _prevPanelPos) / dt;
        _prevPanelPos = panelPos;

        // Log occlusion change as an event (optional but useful)
        bool occludedBool = occluded01 == 1;
        if (occludedBool != _prevOccluded)
        {
            LogEvent("occlusion_change", occludedBool ? "occluded" : "clear");
            _prevOccluded = occludedBool;
        }

        // Log big jumps as events (optional)
        if (panelSpeed >= jumpSpeedThresholdMps)
        {
            LogEvent("panel_jump", panelSpeed.ToString("F3", CI));
        }

        // Write CSV row
        string line =
            $"{participantId},{condition},{trialId},{(now - _trialStartTime).ToString("F3", CI)}," +
            $"{headPos.x.ToString("F4", CI)},{headPos.y.ToString("F4", CI)},{headPos.z.ToString("F4", CI)}," +
            $"{headFwd.x.ToString("F4", CI)},{headFwd.y.ToString("F4", CI)},{headFwd.z.ToString("F4", CI)}," +
            $"{panelPos.x.ToString("F4", CI)},{panelPos.y.ToString("F4", CI)},{panelPos.z.ToString("F4", CI)}," +
            $"{distance.ToString("F4", CI)},{angleDeg.ToString("F2", CI)},{visible01},{occluded01},{panelSpeed.ToString("F4", CI)}";

        _frameWriter.WriteLine(line);

        if (logInEditorConsole)
            Debug.Log(line);
    }

    public void LogEvent(string eventType, string eventValue)
    {
        if (!_isTrialRunning || _eventWriter == null) return;

        float t = Time.unscaledTime - _trialStartTime;

        // Basic CSV sanitization (avoid breaking columns)
        eventType = eventType.Replace(",", ";");
        eventValue = eventValue.Replace(",", ";");

        string line = $"{participantId},{condition},{trialId},{t.ToString("F3", CI)},{eventType},{eventValue}";
        _eventWriter.WriteLine(line);
    }

    void OnApplicationQuit()
    {
        // Ensure files close cleanly
        if (_isTrialRunning) EndTrial("app_quit");
    }

    void OnDestroy()
    {
        if (_isTrialRunning) EndTrial("destroy");
    }
}