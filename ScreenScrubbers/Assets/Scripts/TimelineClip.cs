using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.EventSystems;

public class TimelineClip : MonoBehaviour
{
    // IPointerDownHandler,    // mouse click
    // IPointerEnterHandler,    // mouse hover
    // IPointerExitHandler {    // mouse exit

    public float startTime;
    public float endTime;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable xrInteractable;
    private Renderer rend;
    private Color originalColor;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor currentInteractor;
    private TimelineManager timelineManager;

    void Awake() {
        timelineManager = FindObjectOfType<TimelineManager>();
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;
        xrInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        xrInteractable.selectEntered.AddListener(OnVRSelected);
        xrInteractable.selectExited.AddListener(OnVRSelectExit);
        xrInteractable.hoverEntered.AddListener(OnVRHovered);
        xrInteractable.hoverExited.AddListener(OnVRHoverExit);
    }

    // Logic for VR
    void OnVRSelected(SelectEnterEventArgs args) {
        currentInteractor = args.interactorObject;
        HandleSelect();
    }

    void OnVRSelectExit(SelectExitEventArgs args) {
        currentInteractor = null;
    }

    void OnVRHovered(HoverEnterEventArgs args) {
        rend.material.color = Color.cyan;
    }

    void OnVRHoverExit(HoverExitEventArgs args) {
        rend.material.color = originalColor;
    }

    // Logic for Mouse
    // public void OnPointerDown(PointerEventData data) {
    //     Object.FindFirstObjectByType<MouseKeyboardManager>().SetSelectedClip(this);
    //     HandleSelect();
    // }

    // public void OnPointerEnter(PointerEventData data) {
    //     rend.material.color = Color.cyan;
    // }

    // public void OnPointerExit(PointerEventData data) {
    //     rend.material.color = originalColor;
    // }

    // Logic for both
    void HandleSelect() {
        rend.material.color = Color.yellow;
        FindObjectOfType<FeedbackManager>().PlaySelectFeedback(gameObject);
        FindObjectOfType<ExperimentLogger>().OnClipSelected(this);
    }

    void Update() {
    if (currentInteractor == null) return;
    Debug.Log($"Interactor: {currentInteractor}, Manager: {timelineManager}");
    float rayX = currentInteractor.GetAttachTransform(xrInteractable).position.x;
    timelineManager.PlaceClip(this, rayX);
    }
}