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
    
    public TimelineManager clipATimelineManager;
    public TimelineManager clipBTimelineManager;
    public TimelineClip clipA;
    public TimelineClip clipB;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private int currentRoundIndex = -1;
    [SerializeField] private bool roundActive;
    [SerializeField] private float roundStartTime;

    private string runCsvPath;
    
    private const string CsvHeader = "Modality,Round,CompletionTimeSec,TimestampUtc";

    private readonly string[] roundNames = { "Round1", "Round2", "Round3" };

    private void Start()
    {
        ResolveTimelineManagers();
        StartNewRunCsv();
    }

    public void StartRound(int roundIndex)
    {
        if (roundIndex < 0 || roundIndex >= roundNames.Length) return;

        currentRoundIndex = roundIndex;
        roundStartTime = Time.time;
        roundActive = true;
    }

    public void CompleteCurrentRound()
    {
        if (!roundActive || currentRoundIndex < 0) return;

        roundActive = false;
        
        float completionTime = Time.time - roundStartTime;
        string roundName = roundNames[currentRoundIndex];
        string timestamp = System.DateTime.UtcNow.ToString("o");

        StringBuilder row = new StringBuilder();
        row.Append(currentModality).Append(",");
        row.Append(roundName).Append(",");
        row.Append(completionTime.ToString("F3")).Append(",");
        row.Append(timestamp);

        File.AppendAllText(runCsvPath, row + "\n");
    }

    public void BeginNewRun()
    {
        StartNewRunCsv();
        currentRoundIndex = -1;
        roundActive = false;
    }

    public string GetCurrentRunCsvPath()
    {
        return runCsvPath;
    }

    public void OnClipSelected(TimelineClip clip) 
    { 
    }

    private void StartNewRunCsv()
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = "results_ScreenScrubbers_" + timestamp + ".csv";
        runCsvPath = Path.Combine(Application.persistentDataPath, fileName);
        
        if (!File.Exists(runCsvPath))
        {
            File.WriteAllText(runCsvPath, CsvHeader + "\n");
        }
    }

    private void ResolveTimelineManagers()
    {
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
}