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
    public bool autoStartSessionOnPlay = true;
    public bool runBothModalitiesSequentially = true;

    [Header("Quest Options")]
    public float positionToleranceNorm = 0.08f;
    public float scaleTolerance = 0.15f;
    public float holdOnTargetSeconds = 0.6f;
    
    [Header("UI Text Settings")]
    public bool showWorldStatusText = true;
    public Vector3 worldTextOffset = new Vector3(0f, 0.6f, 1.5f);
    public int worldTextFontSize = 260;
    public float worldTextCharacterSize = 0.06f;
    public float worldTextScale = 0.03f;

    [Header("Runtime (Read-Only)")]
    [SerializeField] private int activeRound = -1;
    [SerializeField] private bool sessionActive;
    [SerializeField] private bool sessionCompleted;
    [SerializeField] private InteractionModality activeModalityPhase = InteractionModality.Controllers;

    private Vector3 clipAStartPos, clipBStartPos, clipAStartScale, clipBStartScale;
    private float onTargetTimer;
    private TextMesh worldStatusText;
    private Transform statusAnchor;

    private readonly string[] roundInstructions = {
        "Round 1: Move clip A to ~2/3 of the timeline\nand resize clip B to ~0.5x.",
        "Round 2: Resize clip A to ~2.0x\nand move clip B to the front.",
        "Round 3: Move clip A to ~1/3, resize to ~1.5x.\nMove clip B to end, resize to ~0.5x."
    };

    private void Start()
    {
        if (logger == null) logger = FindObjectOfType<ExperimentLogger>();
        
        CacheInitialClipState();
        SetupWorldStatusText();

        if (autoStartSessionOnPlay) BeginSubjectSession();
    }

    private void Update()
    {
        UpdateWorldStatusText();
        if (sessionActive && activeRound >= 0) UpdateAutoCompleteCheck();
    }

    public void BeginSubjectSession()
    {
        logger.BeginNewRun();
        sessionActive = true;
        sessionCompleted = false;
        activeModalityPhase = InteractionModality.Controllers;
        StartRound(0);
    }

    public void StartRound(int roundIndex)
    {
        if (!sessionActive || roundIndex > 2) return;
        activeRound = roundIndex;
        onTargetTimer = 0f;
        logger.currentModality = activeModalityPhase;
        
        if (resetClipsAtRoundStart) ResetClipsToInitialState();
        logger.StartRound(roundIndex);
    }

    public void CompleteRoundAndAdvance()
    {
        if (activeRound < 0) return;
        
        logger.CompleteCurrentRound();
        onTargetTimer = 0f;

        if (!autoAdvanceToNextRound) return;

        if (activeRound + 1 <= 2) 
        {
            StartRound(activeRound + 1);
        }
        else if (runBothModalitiesSequentially && activeModalityPhase == InteractionModality.Controllers)
        {
            activeModalityPhase = InteractionModality.HandTracking;
            StartRound(0);
        }
        else
        {
            activeRound = -1;
            sessionActive = false;
            sessionCompleted = true;
        }
    }

    private void ResetClipsToInitialState()
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

    private void SetupWorldStatusText()
    {
        if (!showWorldStatusText || Camera.main == null) return;
        
        statusAnchor = Camera.main.transform;
        GameObject textObj = new GameObject("ExperimentStatusText");
        textObj.transform.SetParent(statusAnchor, false);
        
        worldStatusText = textObj.AddComponent<TextMesh>();
        worldStatusText.anchor = TextAnchor.UpperCenter;
        worldStatusText.alignment = TextAlignment.Center;
        worldStatusText.color = new Color(1f, 0.95f, 0.6f, 1f);
    }

    private void UpdateWorldStatusText()
    {
        if (!showWorldStatusText || worldStatusText == null) return;

        if (sessionCompleted) 
        {
            worldStatusText.text = "EXPERIMENT COMPLETE\nCSV file saved.";
        }
        else if (activeRound >= 0) 
        {
            float clipAPos = GetNormalizedX(clipA, logger.clipATimelineManager);
            float clipBPos = GetNormalizedX(clipB, logger.clipBTimelineManager);
            float clipAScale = clipAStartScale.x == 0f ? 0f : clipA.transform.localScale.x / clipAStartScale.x;
            float clipBScale = clipBStartScale.x == 0f ? 0f : clipB.transform.localScale.x / clipBStartScale.x;

            string clipALine = BuildClipStatusLine("A", clipAPos, clipAScale);
            string clipBLine = BuildClipStatusLine("B", clipBPos, clipBScale);

            worldStatusText.text = $"[{activeModalityPhase}] Round {activeRound + 1}\n\n{roundInstructions[activeRound]}\n\n{clipALine}\n{clipBLine}";
        }
        else 
        {
            worldStatusText.text = "Waiting to start...";
        }

        if (statusAnchor != null)
        {
            worldStatusText.transform.localPosition = worldTextOffset;
            worldStatusText.transform.localRotation = Quaternion.identity;
            worldStatusText.transform.localScale = Vector3.one * worldTextScale;
            worldStatusText.characterSize = worldTextCharacterSize;
            worldStatusText.fontSize = worldTextFontSize;
        }
    }

    private void UpdateAutoCompleteCheck()
    {
        if (IsCurrentRoundSatisfied())
        {
            onTargetTimer += Time.deltaTime;
            if (onTargetTimer >= holdOnTargetSeconds) CompleteRoundAndAdvance();
        }
        else 
        {
            onTargetTimer = Mathf.Max(0f, onTargetTimer - Time.deltaTime * 0.75f);
        }
    }

    private bool IsCurrentRoundSatisfied()
    {
        float clipAPos = GetNormalizedX(clipA, logger.clipATimelineManager);
        float clipBPos = GetNormalizedX(clipB, logger.clipBTimelineManager);
        float clipAScale = clipAStartScale.x == 0f ? 0f : clipA.transform.localScale.x / clipAStartScale.x;
        float clipBScale = clipBStartScale.x == 0f ? 0f : clipB.transform.localScale.x / clipBStartScale.x;

        if (activeRound == 0) return IsWithin(clipAPos, 2f/3f, positionToleranceNorm) && IsWithin(clipBScale, 0.5f, scaleTolerance);
        if (activeRound == 1) return IsWithin(clipAScale, 2.0f, scaleTolerance) && IsWithin(clipBPos, 0f, positionToleranceNorm);
        if (activeRound == 2) return IsWithin(clipAPos, 1f/3f, positionToleranceNorm) && IsWithin(clipAScale, 1.5f, scaleTolerance) && IsWithin(clipBPos, 1f, positionToleranceNorm) && IsWithin(clipBScale, 0.5f, scaleTolerance);
        return false;
    }

    private float GetNormalizedX(TimelineClip clip, TimelineManager manager)
    {
        if (clip == null || manager == null) return 0f;
        return Mathf.InverseLerp(manager.timelineStartX, manager.timelineEndX, clip.transform.position.x);
    }

    private bool IsWithin(float val, float target, float tol) => Mathf.Abs(val - target) <= tol;
    
    private string BoolMark(bool value) => value ? "Y" : "N";

    private string BuildClipStatusLine(string clipName, float posNorm, float scaleRatio)
    {
        bool posOk = true;
        bool scaleOk = true;

        if (activeRound == 0)
        {
            if (clipName == "A") posOk = IsWithin(posNorm, 2f / 3f, positionToleranceNorm);
            if (clipName == "B") scaleOk = IsWithin(scaleRatio, 0.5f, scaleTolerance);
        }
        else if (activeRound == 1)
        {
            if (clipName == "A") scaleOk = IsWithin(scaleRatio, 2.0f, scaleTolerance);
            if (clipName == "B") posOk = IsWithin(posNorm, 0f, positionToleranceNorm);
        }
        else if (activeRound == 2)
        {
            if (clipName == "A")
            {
                posOk = IsWithin(posNorm, 1f / 3f, positionToleranceNorm);
                scaleOk = IsWithin(scaleRatio, 1.5f, scaleTolerance);
            }
            else if (clipName == "B")
            {
                posOk = IsWithin(posNorm, 1f, positionToleranceNorm);
                scaleOk = IsWithin(scaleRatio, 0.5f, scaleTolerance);
            }
        }

        return $"Clip {clipName}: pos({posNorm:F2})[{BoolMark(posOk)}] size({scaleRatio:F2})[{BoolMark(scaleOk)}]";
    }
}