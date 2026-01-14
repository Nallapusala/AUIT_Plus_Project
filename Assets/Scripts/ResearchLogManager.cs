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
    public Transform head;                 // CenterEyeAnchor / Main Camera transform
    public Transform videoPanel;           // Assign the collider parent (e.g., Video Quad)
    public Transform instructionPanel;     // Assign collider parent (e.g., StepsRoot) - can be null for non-multi
    [Tooltip("Select the layer for walls/cabinets/counters that can occlude panels")]
    public LayerMask environmentLayer;

    [Header("3. Sampling")]
    [Tooltip("Samples per second. 10 Hz is typical for HCI.")]
    public float sampleHz = 10f;

    [Header("4. Comfort thresholds")]
    public float comfortConeDeg = 25f;
    public float minDistanceMeters = 0.8f;
    public float maxDistanceMeters = 1.6f;

    [Header("5. Gaze Detection")]
    [Tooltip("Layer mask containing ONLY the UI panel colliders (e.g., GazeUI).")]
    public LayerMask gazeUiLayer;
    public float gazeMaxDistance = 20f;
    public float gazeSphereRadius = 0.02f; // 2 cm

    [Header("6. Status")]
    public bool isRecording = false;

    private string currentFilePath;
    private float trialStartTime;
    private float nextSampleTime;
    private StringBuilder buffer = new StringBuilder();

    // Speed tracking (video + instruction)
    private Vector3 prevVideoPos;
    private float prevVideoT;
    private bool hasPrevVideo;

    private Vector3 prevInstPos;
    private float prevInstT;
    private bool hasPrevInst;

    void OnEnable()
    {
        if (!head)
        {
            if (Camera.main != null) head = Camera.main.transform;
        }

        string folder = Path.Combine(Application.dataPath, "ExperimentData");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string fileName = $"{participantID}_{activeScenario}_{sequenceOrder}_{DateTime.Now:HHmm}.csv";
        currentFilePath = Path.Combine(folder, fileName);

        buffer.Clear();
        buffer.AppendLine(
            "Time,ParticipantID,Scenario,Sequence," +
            "HeadX,HeadY,HeadZ,HeadYaw,HeadPitch," +
            "VideoX,VideoY,VideoZ,InstX,InstY,InstZ," +
            "GazeTarget,HitObjectName," +
            "AngleToVideo,AngleToInst," +
            "VideoDist,InstDist," +
            "VideoOcc,InstOcc,VideoOccHitName,InstOccHitName," +
            "VideoInCone,InstInCone,VideoVisible,InstVisible," +
            "VideoSpeed,InstSpeed," +
            "PanelSeparation,ScreenOverlapPct,BothVisible"
        );

        trialStartTime = Time.time;
        nextSampleTime = trialStartTime;

        // reset speed tracking
        hasPrevVideo = false;
        hasPrevInst = false;

        isRecording = true;
        Debug.Log($"<color=cyan><b>LOGGING STARTED:</b></color> Scenario: {activeScenario}");
    }

    void Update()
    {
        if (!isRecording) return;

        // Time-based sampling (more stable than frameCount%N)
        if (Time.time >= nextSampleTime)
        {
            RecordMetrics();
            nextSampleTime += 1f / Mathf.Max(1f, sampleHz);
        }
    }

    private Vector3 GetTargetPoint(Transform t)
    {
        if (!t) return Vector3.zero;

        var col = t.GetComponentInChildren<Collider>();
        if (col) return col.bounds.center;

        var rend = t.GetComponentInChildren<Renderer>();
        if (rend) return rend.bounds.center;

        return t.position;
    }

    private Rect BoundsToScreenRect(Camera cam, Bounds b)
    {
        Vector3 c = b.center, e = b.extents;
        Vector3[] p =
        {
            cam.WorldToScreenPoint(c + new Vector3(+e.x,+e.y,+e.z)),
            cam.WorldToScreenPoint(c + new Vector3(+e.x,+e.y,-e.z)),
            cam.WorldToScreenPoint(c + new Vector3(+e.x,-e.y,+e.z)),
            cam.WorldToScreenPoint(c + new Vector3(+e.x,-e.y,-e.z)),
            cam.WorldToScreenPoint(c + new Vector3(-e.x,+e.y,+e.z)),
            cam.WorldToScreenPoint(c + new Vector3(-e.x,+e.y,-e.z)),
            cam.WorldToScreenPoint(c + new Vector3(-e.x,-e.y,+e.z)),
            cam.WorldToScreenPoint(c + new Vector3(-e.x,-e.y,-e.z)),
        };

        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;

        for (int i = 0; i < p.Length; i++)
        {
            minX = Mathf.Min(minX, p[i].x);
            minY = Mathf.Min(minY, p[i].y);
            maxX = Mathf.Max(maxX, p[i].x);
            maxY = Mathf.Max(maxY, p[i].y);
        }
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private float RectOverlapPct(Rect a, Rect b)
    {
        float x1 = Mathf.Max(a.xMin, b.xMin);
        float y1 = Mathf.Max(a.yMin, b.yMin);
        float x2 = Mathf.Min(a.xMax, b.xMax);
        float y2 = Mathf.Min(a.yMax, b.yMax);
        if (x2 <= x1 || y2 <= y1) return 0f;

        float inter = (x2 - x1) * (y2 - y1);
        float minArea = Mathf.Max(1f, Mathf.Min(a.width * a.height, b.width * b.height));
        return Mathf.Clamp01(inter / minArea);
    }

    private float ComputeSpeed(ref bool hasPrev, ref Vector3 prevPos, ref float prevT, Vector3 currentPos, float now)
    {
        if (!hasPrev)
        {
            hasPrev = true;
            prevPos = currentPos;
            prevT = now;
            return 0f;
        }

        float dt = Mathf.Max(1e-4f, now - prevT);
        float speed = Vector3.Distance(currentPos, prevPos) / dt;

        prevPos = currentPos;
        prevT = now;

        return speed;
    }

    void RecordMetrics()
    {
        float t = Time.time - trialStartTime;

        if (!head || !videoPanel)
            return;

        Camera cam = Camera.main;

        // start slightly forward to avoid edge cases (starting inside colliders)
        Vector3 headPos = head.position;
        Vector3 rayStart = headPos + head.forward * 0.02f;

        // target points (more accurate than pivot)
        Vector3 videoTarget = GetTargetPoint(videoPanel);
        bool instActive = (instructionPanel != null && instructionPanel.gameObject.activeInHierarchy);
        Vector3 instTarget = instActive ? GetTargetPoint(instructionPanel) : Vector3.zero;

        // angles
        float angV = Vector3.Angle(head.forward, videoTarget - headPos);
        float angI = instActive ? Vector3.Angle(head.forward, instTarget - headPos) : float.NaN;

        // distances
        float videoDist = Vector3.Distance(headPos, videoTarget);
        float instDist = instActive ? Vector3.Distance(headPos, instTarget) : float.NaN;

        // occlusion (environment only)
        // occlusion (environment only)
        RaycastHit vOccHit;
        RaycastHit iOccHit = default; // <-- initialize to avoid CS0165

        bool vOcc = Physics.Linecast(rayStart, videoTarget, out vOccHit, environmentLayer, QueryTriggerInteraction.Ignore);

        bool iOcc = false;
        if (instActive)
        {
            iOcc = Physics.Linecast(rayStart, instTarget, out iOccHit, environmentLayer, QueryTriggerInteraction.Ignore);
        }

        string vOccHitName = vOcc ? vOccHit.collider.name : "None";
        string iOccHitName = (instActive && iOcc) ? iOccHit.collider.name : "None";


        // comfort + visibility flags
        bool videoInCone = angV <= comfortConeDeg;
        bool instInCone = instActive ? (angI <= comfortConeDeg) : false;

        bool videoInDist = (videoDist >= minDistanceMeters && videoDist <= maxDistanceMeters);
        bool instInDist = instActive && (instDist >= minDistanceMeters && instDist <= maxDistanceMeters);

        bool videoVisible = videoInCone && videoInDist && !vOcc;
        bool instVisible = instActive && instInCone && instInDist && !iOcc;

        // gaze detection (UI-first)
        string gaze = "World";
        string hitName = "None";
        RaycastHit gazeHit;

        bool uiHit = Physics.SphereCast(
            rayStart,
            gazeSphereRadius,
            head.forward,
            out gazeHit,
            gazeMaxDistance,
            gazeUiLayer,
            QueryTriggerInteraction.Collide
        );

        if (uiHit)
        {
            hitName = gazeHit.transform.name;

            if (videoPanel != null && (gazeHit.transform == videoPanel || gazeHit.transform.IsChildOf(videoPanel)))
                gaze = "Video";
            else if (instructionPanel != null && (gazeHit.transform == instructionPanel || gazeHit.transform.IsChildOf(instructionPanel)))
                gaze = "Instructions";
            else
                gaze = "UI";
        }
        else
        {
            // optional world hit name (debug/info only)
            int worldMask = ~(1 << 2);
            if (Physics.Raycast(rayStart, head.forward, out gazeHit, gazeMaxDistance, worldMask, QueryTriggerInteraction.Ignore))
                hitName = gazeHit.transform.name;
        }

        // speeds (based on transform positions - OK for stability proxy)
        float now = Time.time;
        float videoSpeed = ComputeSpeed(ref hasPrevVideo, ref prevVideoPos, ref prevVideoT, videoPanel.position, now);

        float instSpeed = float.NaN;
        if (instActive)
            instSpeed = ComputeSpeed(ref hasPrevInst, ref prevInstPos, ref prevInstT, instructionPanel.position, now);
        else
            hasPrevInst = false;

        // multi-only layout metrics
        float panelSep = float.NaN;
        float overlapPct = float.NaN;
        bool bothVisible = false;

        if (instActive)
        {
            panelSep = Vector3.Distance(videoTarget, instTarget);
            bothVisible = videoVisible && instVisible;

            // screen overlap pct (requires colliders or renderers)
            if (cam != null)
            {
                Collider vCol = videoPanel.GetComponentInChildren<Collider>();
                Collider iCol = instructionPanel.GetComponentInChildren<Collider>();

                if (vCol != null && iCol != null)
                {
                    Rect rv = BoundsToScreenRect(cam, vCol.bounds);
                    Rect ri = BoundsToScreenRect(cam, iCol.bounds);
                    overlapPct = RectOverlapPct(rv, ri);
                }
                else
                {
                    overlapPct = 0f; // fallback if no colliders; recommend adding BoxColliders
                }
            }
        }

        // positions for CSV (use NaN for inactive instructions)
        Vector3 videoPos = videoPanel.position;
        Vector3 instPos = instActive ? instructionPanel.position : new Vector3(float.NaN, float.NaN, float.NaN);

        string row = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0:F2},{1},{2},{3}," +
            "{4:F3},{5:F3},{6:F3},{7:F1},{8:F1}," +
            "{9:F3},{10:F3},{11:F3},{12:F3},{13:F3},{14:F3}," +
            "{15},{16}," +
            "{17:F1},{18:F1}," +
            "{19:F3},{20:F3}," +
            "{21},{22},{23},{24}," +
            "{25},{26},{27},{28}," +
            "{29:F4},{30:F4}," +
            "{31:F3},{32:F3},{33}\n",

            t, participantID, activeScenario, sequenceOrder,
            headPos.x, headPos.y, headPos.z, head.eulerAngles.y, head.eulerAngles.x,

            videoPos.x, videoPos.y, videoPos.z,
            instPos.x, instPos.y, instPos.z,

            gaze, hitName,

            angV, angI,

            videoDist, instDist,

            vOcc ? 1 : 0, iOcc ? 1 : 0, vOccHitName, iOccHitName,

            videoInCone ? 1 : 0, instInCone ? 1 : 0, videoVisible ? 1 : 0, instVisible ? 1 : 0,

            videoSpeed, instSpeed,

            panelSep, overlapPct, bothVisible ? 1 : 0
        );

        buffer.Append(row);
    }

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
