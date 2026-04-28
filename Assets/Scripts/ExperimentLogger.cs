using UnityEngine;
using System.IO;

public enum InteractionModality {
    Traditional,
    VR_Unimodal_Visual,
    VR_Unimodal_Audio,
    VR_Multimodal
}

public class ExperimentLogger : MonoBehaviour {
    public InteractionModality currentModality;
    public float targetStart;
    public float targetEnd;
    public float targetInsertPoint;

    private float taskStartTime;
    private string logPath;

    void Start() {
        logPath = Application.persistentDataPath + "/results.csv";
        if (!File.Exists(logPath)) {
            File.WriteAllText(logPath, "Modality,CompletionTime,StartError,EndError,InsertError\n");
        }
    }

    public void StartTask(float tStart, float tEnd, float tInsert) {
        targetStart = tStart;
        targetEnd = tEnd;
        targetInsertPoint = tInsert;
        taskStartTime = Time.time;
    }

    public void OnClipSelected(TimelineClip clip) {
        // Log intermediate events if needed
    }

    public void EndTask(float userStart, float userEnd, float userInsert) {
        float completionTime = Time.time - taskStartTime;
        float startError  = Mathf.Abs(userStart  - targetStart);
        float endError    = Mathf.Abs(userEnd    - targetEnd);
        float insertError = Mathf.Abs(userInsert - targetInsertPoint);

        string row = $"{currentModality},{completionTime:F3}," + $"{startError:F3},{endError:F3},{insertError:F3}";
        File.AppendAllText(logPath, row + "\n");
        Debug.Log("Logged: " + row);
    }
}