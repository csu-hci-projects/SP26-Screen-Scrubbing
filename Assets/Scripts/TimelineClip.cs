using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Hands;
using UnityEngine.EventSystems;

public class TimelineClip : MonoBehaviour
{
    public float startTime;
    public float endTime;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable xrInteractable;
    private Renderer rend;
    private Color originalColor;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor currentInteractor;
    private TimelineManager timelineManager;
    void Awake() {
        timelineManager = FindObjectOfType<TimelineManager>();
        rend = GetComponent<Renderer>();
        GetComponent<Rigidbody>().isKinematic = true;
        originalColor = rend.material.color;
        xrInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        xrInteractable.trackPosition = false;
        xrInteractable.trackRotation = false;
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

    void HandleSelect() {
        rend.material.color = Color.yellow;
        FindObjectOfType<FeedbackManager>().PlaySelectFeedback(gameObject);
        FindObjectOfType<ExperimentLogger>().OnClipSelected(this);
    }

    void LateUpdate() {
        if (currentInteractor == null) return;
        foreach (var handle in GetComponentsInChildren<ResizeHandle>())
            if (handle.IsBeingResized) return;
        Vector3 attachPos = currentInteractor.GetAttachTransform(xrInteractable).position;
        timelineManager.PlaceClip(this, attachPos.x);
    }
}