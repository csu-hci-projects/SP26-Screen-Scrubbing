using System.IO;
using System.Text;
using UnityEngine;

public enum InteractionModality
{
    Traditional,
    VR_Unimodal_Visual,
    VR_Unimodal_Audio,
    VR_Multimodal,
    HandTracking,
    Controllers
}

public class ExperimentLogger : MonoBehaviour
{
    [Header("Session Setup")]
    public InteractionModality currentModality;
    public string participantId = "P001";
    public TimelineManager clipATimelineManager;
    public TimelineManager clipBTimelineManager;
    public TimelineClip clipA;
    public TimelineClip clipB;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private int currentRoundIndex = -1;
    [SerializeField] private bool roundActive;
    [SerializeField] private float roundStartTime;

    private float clipABaselineWidth;
    private float clipBBaselineWidth;
    private float lastClipAX;
    private float lastClipBX;
    private float lastClipAWidth;
    private float lastClipBWidth;
    private int clipAMoveCount;
    private int clipBMoveCount;
    private int clipAResizeCount;
    private int clipBResizeCount;

    private string runCsvPath;
    private const float MoveThreshold = 0.01f;
    private const float ResizeThreshold = 0.01f;
    private const string CsvHeader =
        "RunId,ParticipantId,Modality,Round,CompletionTimeSec," +
        "ClipATargetPosNorm,ClipAActualPosNorm,ClipAPositionErrorNorm,ClipATargetScale,ClipAActualScale,ClipAScaleError," +
        "ClipBTargetPosNorm,ClipBActualPosNorm,ClipBPositionErrorNorm,ClipBTargetScale,ClipBActualScale,ClipBScaleError," +
        "ClipAMoveCount,ClipBMoveCount,ClipAResizeCount,ClipBResizeCount,TimestampUtc";

    private struct RoundDefinition
    {
        public string Name;
        public float ClipATargetPosNorm;
        public float ClipATargetScale;
        public float ClipBTargetPosNorm;
        public float ClipBTargetScale;
    }

    private readonly RoundDefinition[] rounds =
    {
        new RoundDefinition
        {
            Name = "Round1",
            ClipATargetPosNorm = 2f / 3f,
            ClipATargetScale = 1.0f,
            ClipBTargetPosNorm = -1f,
            ClipBTargetScale = 0.5f
        },
        new RoundDefinition
        {
            Name = "Round2",
            ClipATargetPosNorm = -1f,
            ClipATargetScale = 2.0f,
            ClipBTargetPosNorm = 0f,
            ClipBTargetScale = 1.0f
        },
        new RoundDefinition
        {
            Name = "Round3",
            ClipATargetPosNorm = 1f / 3f,
            ClipATargetScale = 1.5f,
            ClipBTargetPosNorm = 1f,
            ClipBTargetScale = 0.5f
        }
    };

    private void Start()
    {
        ResolveTimelineManagers();

        if (clipA == null || clipB == null || clipATimelineManager == null || clipBTimelineManager == null)
        {
            Debug.LogError("ExperimentLogger missing references. Assign clipA, clipB, clipATimelineManager, and clipBTimelineManager.");
            enabled = false;
            return;
        }

        clipABaselineWidth = clipA.transform.localScale.x;
        clipBBaselineWidth = clipB.transform.localScale.x;

        CacheClipState();
        StartNewRunCsv();
        Debug.Log("Experiment logger initialized. Call StartRound(0..2) to begin.");
    }

    private void Update()
    {
        if (!roundActive) return;
        TrackMoveAndResizeCounts();
    }

    public void StartRound(int roundIndex)
    {
        if (roundIndex < 0 || roundIndex >= rounds.Length)
        {
            Debug.LogError("Invalid round index. Use 0, 1, or 2.");
            return;
        }

        currentRoundIndex = roundIndex;
        roundStartTime = Time.time;
        roundActive = true;
        ResetInteractionCounters();
        CacheClipState();

        Debug.Log("Started " + rounds[roundIndex].Name);
    }

    public void StartNextRound()
    {
        int next = currentRoundIndex + 1;
        if (next >= rounds.Length)
        {
            Debug.Log("All rounds complete.");
            return;
        }

        StartRound(next);
    }

    public void CompleteCurrentRound()
    {
        if (!roundActive || currentRoundIndex < 0)
        {
            Debug.LogWarning("No active round to complete.");
            return;
        }

        roundActive = false;
        float completionTime = Time.time - roundStartTime;
        RoundDefinition target = rounds[currentRoundIndex];

        float clipAActualPosNorm = WorldXToNormalized(clipA.transform.position.x, clipATimelineManager);
        float clipBActualPosNorm = WorldXToNormalized(clipB.transform.position.x, clipBTimelineManager);
        float clipAActualScale = clipA.transform.localScale.x / clipABaselineWidth;
        float clipBActualScale = clipB.transform.localScale.x / clipBBaselineWidth;

        float clipAPosError = ComputeOptionalError(target.ClipATargetPosNorm, clipAActualPosNorm);
        float clipBPosError = ComputeOptionalError(target.ClipBTargetPosNorm, clipBActualPosNorm);
        float clipAScaleError = Mathf.Abs(clipAActualScale - target.ClipATargetScale);
        float clipBScaleError = Mathf.Abs(clipBActualScale - target.ClipBTargetScale);

        string runId = Path.GetFileNameWithoutExtension(runCsvPath);
        string timestamp = System.DateTime.UtcNow.ToString("o");

        StringBuilder row = new StringBuilder();
        row.Append(runId).Append(",");
        row.Append(Sanitize(participantId)).Append(",");
        row.Append(currentModality).Append(",");
        row.Append(target.Name).Append(",");
        row.Append(completionTime.ToString("F3")).Append(",");
        row.Append(OptionalFloat(target.ClipATargetPosNorm)).Append(",");
        row.Append(clipAActualPosNorm.ToString("F3")).Append(",");
        row.Append(OptionalFloat(clipAPosError)).Append(",");
        row.Append(target.ClipATargetScale.ToString("F3")).Append(",");
        row.Append(clipAActualScale.ToString("F3")).Append(",");
        row.Append(clipAScaleError.ToString("F3")).Append(",");
        row.Append(OptionalFloat(target.ClipBTargetPosNorm)).Append(",");
        row.Append(clipBActualPosNorm.ToString("F3")).Append(",");
        row.Append(OptionalFloat(clipBPosError)).Append(",");
        row.Append(target.ClipBTargetScale.ToString("F3")).Append(",");
        row.Append(clipBActualScale.ToString("F3")).Append(",");
        row.Append(clipBScaleError.ToString("F3")).Append(",");
        row.Append(clipAMoveCount).Append(",");
        row.Append(clipBMoveCount).Append(",");
        row.Append(clipAResizeCount).Append(",");
        row.Append(clipBResizeCount).Append(",");
        row.Append(timestamp);

        File.AppendAllText(runCsvPath, row + "\n");
        Debug.Log("Logged " + target.Name + ": " + row);
    }

    public void BeginNewRun()
    {
        StartNewRunCsv();
        currentRoundIndex = -1;
        roundActive = false;
        ResetInteractionCounters();
        CacheClipState();
        Debug.Log("Started new run log at " + runCsvPath);
    }

    public void OnClipSelected(TimelineClip clip)
    {
        if (!roundActive || clip == null) return;

        if (clip == clipA) clipAMoveCount++;
        if (clip == clipB) clipBMoveCount++;
    }

    public string GetCurrentRunCsvPath()
    {
        return runCsvPath;
    }

    private void StartNewRunCsv()
    {
        string safeParticipant = string.IsNullOrWhiteSpace(participantId) ? "Unknown" : participantId.Trim();
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = "results_" + safeParticipant + "_" + currentModality + "_" + timestamp + ".csv";
        runCsvPath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(runCsvPath, CsvHeader + "\n");
    }

    private void ResetInteractionCounters()
    {
        clipAMoveCount = 0;
        clipBMoveCount = 0;
        clipAResizeCount = 0;
        clipBResizeCount = 0;
    }

    private void CacheClipState()
    {
        lastClipAX = clipA.transform.position.x;
        lastClipBX = clipB.transform.position.x;
        lastClipAWidth = clipA.transform.localScale.x;
        lastClipBWidth = clipB.transform.localScale.x;
    }

    private void TrackMoveAndResizeCounts()
    {
        float clipAX = clipA.transform.position.x;
        float clipBX = clipB.transform.position.x;
        float clipAWidth = clipA.transform.localScale.x;
        float clipBWidth = clipB.transform.localScale.x;

        if (Mathf.Abs(clipAX - lastClipAX) > MoveThreshold)
        {
            clipAMoveCount++;
            lastClipAX = clipAX;
        }

        if (Mathf.Abs(clipBX - lastClipBX) > MoveThreshold)
        {
            clipBMoveCount++;
            lastClipBX = clipBX;
        }

        if (Mathf.Abs(clipAWidth - lastClipAWidth) > ResizeThreshold)
        {
            clipAResizeCount++;
            lastClipAWidth = clipAWidth;
        }

        if (Mathf.Abs(clipBWidth - lastClipBWidth) > ResizeThreshold)
        {
            clipBResizeCount++;
            lastClipBWidth = clipBWidth;
        }
    }

    private void ResolveTimelineManagers()
    {
        if (clipATimelineManager == null && clipA != null)
        {
            clipATimelineManager = clipA.timelineManager != null
                ? clipA.timelineManager
                : clipA.GetComponentInParent<TimelineManager>();
        }

        if (clipBTimelineManager == null && clipB != null)
        {
            clipBTimelineManager = clipB.timelineManager != null
                ? clipB.timelineManager
                : clipB.GetComponentInParent<TimelineManager>();
        }

        if (clipATimelineManager == null || clipBTimelineManager == null)
        {
            TimelineManager[] managers = FindObjectsOfType<TimelineManager>();
            if (managers.Length == 1)
            {
                if (clipATimelineManager == null) clipATimelineManager = managers[0];
                if (clipBTimelineManager == null) clipBTimelineManager = managers[0];
            }
        }
    }

    private float WorldXToNormalized(float x, TimelineManager manager)
    {
        float denom = manager.timelineEndX - manager.timelineStartX;
        if (Mathf.Abs(denom) < Mathf.Epsilon) return 0f;
        return Mathf.InverseLerp(manager.timelineStartX, manager.timelineEndX, x);
    }

    private float ComputeOptionalError(float target, float actual)
    {
        if (target < 0f) return -1f;
        return Mathf.Abs(actual - target);
    }

    private string OptionalFloat(float value)
    {
        return value < 0f ? "" : value.ToString("F3");
    }

    private string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Unknown";
        return value.Replace(",", "_").Trim();
    }
}