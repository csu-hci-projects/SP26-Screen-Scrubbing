using UnityEngine;

public class MouseKeyboardManager : MonoBehaviour {
    private TimelineClip selectedClip;
    private TimelineManager timeline;

    void Start() {
        timeline = Object.FindFirstObjectByType<TimelineManager>();
    }

    void Update() {
        if (Input.GetMouseButton(0) && selectedClip != null) {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
            timeline.PlaceClip(selectedClip, mouseWorld.x);
        }

        if (Input.GetMouseButtonUp(0)) {
            selectedClip = null;
        }
    }
    public void SetSelectedClip(TimelineClip clip) {
        selectedClip = clip;
    }
}