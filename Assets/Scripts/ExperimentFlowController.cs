using UnityEngine;

public class ExperimentFlowController : MonoBehaviour
{
    [Header("References")]
    public ExperimentLogger logger;
    public TimelineClip clipA;
    public TimelineClip clipB;

    [Header("Flow Options")]
    public bool resetClipsAtRoundStart = true;
    public bool autoAdvanceToNextRound = true;
    public bool enableDebugHotkeys = true;
    public bool autoStartSessionOnPlay = true;
    public bool autoStartRound1OnPlay = true;
    public bool showDebugOverlay = true;

    [Header("Quest / No-Keyboard Options")]
    public bool autoCompleteWhenTargetsMet = true;
    public float positionToleranceNorm = 0.05f;
    public float scaleTolerance = 0.10f;
    public float holdOnTargetSeconds = 0.75f;
    public bool showWorldStatusText = true;
    public Vector3 worldTextOffset = new Vector3(0f, 0f, 1.5f);
    public int worldTextFontSize = 48;

    [Header("Runtime (Read-Only)")]
    [SerializeField] private int activeRound = -1;
    [SerializeField] private bool sessionActive;

    private Vector3 clipAStartPos;
    private Vector3 clipBStartPos;
    private Vector3 clipAStartScale;
    private Vector3 clipBStartScale;
    private float onTargetTimer;
    private TextMesh worldStatusText;
    private Transform statusAnchor;

    private readonly string[] roundInstructions =
    {
        "Round 1: Move clip A to ~2/3 of the timeline and resize clip B to ~0.5x.",
        "Round 2: Resize clip A to ~2.0x and move clip B to the front of the timeline.",
        "Round 3: Move clip A to ~1/3 and resize to ~1.5x. Move clip B to the end and resize to ~0.5x."
    };

    private void Start()
    {
        if (logger == null) logger = FindObjectOfType<ExperimentLogger>();

        if (logger == null || clipA == null || clipB == null)
        {
            Debug.LogError("ExperimentFlowController missing references. Assign logger, clipA, and clipB.");
            enabled = false;
            return;
        }

        CacheInitialClipState();
        SetupWorldStatusText();

        Debug.Log("ExperimentFlowController ready.");
        Debug.Log("Hotkeys: F1 Begin session, F2 Start Round 1, F3 Complete round/advance, F4 Reset clips.");

        if (autoStartSessionOnPlay)
        {
            BeginSubjectSession();
            if (autoStartRound1OnPlay)
            {
                StartRound(0);
            }
        }
    }

    private void Update()
    {
        UpdateWorldStatusText();

        if (autoCompleteWhenTargetsMet && sessionActive && activeRound >= 0)
        {
            UpdateAutoCompleteCheck();
        }

        if (!enableDebugHotkeys) return;

        if (Input.GetKeyDown(KeyCode.F1)) BeginSubjectSession();
        if (Input.GetKeyDown(KeyCode.F2)) StartRound(0);
        if (Input.GetKeyDown(KeyCode.F3)) CompleteRoundAndAdvance();
        if (Input.GetKeyDown(KeyCode.F4)) ResetClipsToInitialState();
    }

    public void BeginSubjectSession()
    {
        logger.BeginNewRun();
        sessionActive = true;
        activeRound = -1;

        if (resetClipsAtRoundStart)
        {
            ResetClipsToInitialState();
        }

        Debug.Log("New subject session started. CSV: " + logger.GetCurrentRunCsvPath());
        Debug.Log("Call StartRound(0) to begin Round 1.");
    }

    public void StartRound(int roundIndex)
    {
        if (!sessionActive)
        {
            Debug.LogWarning("No active session. Call BeginSubjectSession() first.");
            return;
        }

        if (roundIndex < 0 || roundIndex > 2)
        {
            Debug.LogError("Round index must be 0, 1, or 2.");
            return;
        }

        activeRound = roundIndex;
        onTargetTimer = 0f;
        if (resetClipsAtRoundStart)
        {
            ResetClipsToInitialState();
        }

        logger.StartRound(roundIndex);
        Debug.Log(GetCurrentInstruction());
    }

    public void StartRound1()
    {
        StartRound(0);
    }

    public void StartRound2()
    {
        StartRound(1);
    }

    public void StartRound3()
    {
        StartRound(2);
    }

    public void CompleteRoundAndAdvance()
    {
        if (activeRound < 0)
        {
            Debug.LogWarning("No active round to complete.");
            return;
        }

        logger.CompleteCurrentRound();
        onTargetTimer = 0f;

        if (!autoAdvanceToNextRound)
        {
            return;
        }

        int nextRound = activeRound + 1;
        if (nextRound <= 2)
        {
            StartRound(nextRound);
        }
        else
        {
            activeRound = -1;
            sessionActive = false;
            Debug.Log("Session complete. All 3 trials logged in one CSV: " + logger.GetCurrentRunCsvPath());
        }
    }

    public void CompleteRoundOnly()
    {
        if (activeRound < 0)
        {
            Debug.LogWarning("No active round to complete.");
            return;
        }

        logger.CompleteCurrentRound();
        onTargetTimer = 0f;
    }

    public void EndSession()
    {
        sessionActive = false;
        activeRound = -1;
        Debug.Log("Session marked as ended.");
    }

    public string GetCurrentInstruction()
    {
        if (activeRound < 0 || activeRound > 2)
        {
            return "No active round.";
        }

        return roundInstructions[activeRound];
    }

    public void ResetClipsToInitialState()
    {
        clipA.transform.position = clipAStartPos;
        clipB.transform.position = clipBStartPos;
        clipA.transform.localScale = clipAStartScale;
        clipB.transform.localScale = clipBStartScale;
    }

    private void CacheInitialClipState()
    {
        clipAStartPos = clipA.transform.position;
        clipBStartPos = clipB.transform.position;
        clipAStartScale = clipA.transform.localScale;
        clipBStartScale = clipB.transform.localScale;
    }

    private void OnGUI()
    {
        if (!showDebugOverlay) return;

        string roundText = activeRound < 0 ? "None" : (activeRound + 1).ToString();
        string csvPath = logger != null ? logger.GetCurrentRunCsvPath() : "Logger not assigned";
        float clipAPos = GetNormalizedX(clipA, logger.clipATimelineManager);
        float clipBPos = GetNormalizedX(clipB, logger.clipBTimelineManager);
        float clipAScale = clipAStartScale.x == 0f ? 0f : clipA.transform.localScale.x / clipAStartScale.x;
        float clipBScale = clipBStartScale.x == 0f ? 0f : clipB.transform.localScale.x / clipBStartScale.x;

        string overlay =
            "Experiment Debug\n" +
            "Session Active: " + sessionActive + "\n" +
            "Active Round: " + roundText + "\n" +
            "Instruction: " + GetCurrentInstruction() + "\n" +
            "ClipA pos/scale: " + clipAPos.ToString("F2") + " / " + clipAScale.ToString("F2") + "\n" +
            "ClipB pos/scale: " + clipBPos.ToString("F2") + " / " + clipBScale.ToString("F2") + "\n" +
            "CSV: " + csvPath + "\n\n" +
            "Hotkeys\n" +
            "F1 = Begin session\n" +
            "F2 = Start round 1\n" +
            "F3 = Complete round / advance\n" +
            "F4 = Reset clips";

        GUI.color = Color.white;
        GUI.Box(new Rect(10f, 10f, 780f, 220f), overlay);
    }

    private void SetupWorldStatusText()
    {
        if (!showWorldStatusText) return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("ExperimentFlowController: No main camera found for world status text.");
            return;
        }

        statusAnchor = cam.transform;
        GameObject textObj = new GameObject("ExperimentStatusText");
        textObj.transform.SetParent(statusAnchor, false);
        textObj.transform.localPosition = worldTextOffset;
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one * 0.01f;

        worldStatusText = textObj.AddComponent<TextMesh>();
        worldStatusText.fontSize = worldTextFontSize;
        worldStatusText.characterSize = 0.02f;
        worldStatusText.anchor = TextAnchor.MiddleCenter;
        worldStatusText.alignment = TextAlignment.Center;
        worldStatusText.color = Color.white;
    }

    private void UpdateWorldStatusText()
    {
        if (!showWorldStatusText || worldStatusText == null || logger == null) return;

        string roundText = activeRound < 0 ? "None" : "Round " + (activeRound + 1);
        float clipAPos = GetNormalizedX(clipA, logger.clipATimelineManager);
        float clipBPos = GetNormalizedX(clipB, logger.clipBTimelineManager);
        float clipAScale = clipAStartScale.x == 0f ? 0f : clipA.transform.localScale.x / clipAStartScale.x;
        float clipBScale = clipBStartScale.x == 0f ? 0f : clipB.transform.localScale.x / clipBStartScale.x;

        worldStatusText.text =
            "Experiment Running\n" +
            "Session: " + (sessionActive ? "Active" : "Inactive") + "\n" +
            "Round: " + roundText + "\n" +
            GetCurrentInstruction() + "\n" +
            "A pos/scale: " + clipAPos.ToString("F2") + " / " + clipAScale.ToString("F2") + "\n" +
            "B pos/scale: " + clipBPos.ToString("F2") + " / " + clipBScale.ToString("F2");

        if (statusAnchor != null)
        {
            worldStatusText.transform.localPosition = worldTextOffset;
            worldStatusText.transform.localRotation = Quaternion.identity;
        }
    }

    private void UpdateAutoCompleteCheck()
    {
        if (IsCurrentRoundSatisfied())
        {
            onTargetTimer += Time.deltaTime;
            if (onTargetTimer >= holdOnTargetSeconds)
            {
                Debug.Log("Round goals satisfied. Auto-completing round.");
                CompleteRoundAndAdvance();
            }
        }
        else
        {
            onTargetTimer = 0f;
        }
    }

    private bool IsCurrentRoundSatisfied()
    {
        float clipAPos = GetNormalizedX(clipA, logger.clipATimelineManager);
        float clipBPos = GetNormalizedX(clipB, logger.clipBTimelineManager);
        float clipAScale = clipAStartScale.x == 0f ? 0f : clipA.transform.localScale.x / clipAStartScale.x;
        float clipBScale = clipBStartScale.x == 0f ? 0f : clipB.transform.localScale.x / clipBStartScale.x;

        if (activeRound == 0)
        {
            return IsWithin(clipAPos, 2f / 3f, positionToleranceNorm) &&
                   IsWithin(clipBScale, 0.5f, scaleTolerance);
        }

        if (activeRound == 1)
        {
            return IsWithin(clipAScale, 2.0f, scaleTolerance) &&
                   IsWithin(clipBPos, 0f, positionToleranceNorm);
        }

        if (activeRound == 2)
        {
            return IsWithin(clipAPos, 1f / 3f, positionToleranceNorm) &&
                   IsWithin(clipAScale, 1.5f, scaleTolerance) &&
                   IsWithin(clipBPos, 1f, positionToleranceNorm) &&
                   IsWithin(clipBScale, 0.5f, scaleTolerance);
        }

        return false;
    }

    private float GetNormalizedX(TimelineClip clip, TimelineManager manager)
    {
        if (clip == null || manager == null) return 0f;
        return Mathf.InverseLerp(manager.timelineStartX, manager.timelineEndX, clip.transform.position.x);
    }

    private bool IsWithin(float value, float target, float tolerance)
    {
        return Mathf.Abs(value - target) <= tolerance;
    }
}
