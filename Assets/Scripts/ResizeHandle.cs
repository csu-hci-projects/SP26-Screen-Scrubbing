using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ResizeHandle : MonoBehaviour {
    public enum HandleSide { Left, Right }
    public HandleSide side;
    public bool IsBeingResized => currentInteractor != null;

    private XRGrabInteractable grabInteractable;
    private Transform clipTransform;
    private IXRSelectInteractor currentInteractor;
    private float minClipWidth = 0.1f;
    private float grabOffset;

    void Awake() {
        grabInteractable = GetComponent<XRGrabInteractable>();
        clipTransform = transform.parent;
        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = false;
        grabInteractable.selectEntered.AddListener(args => {
            currentInteractor = args.interactorObject;
            // record difference between hand position and handle position at grab moment
            grabOffset = transform.position.x - currentInteractor.GetAttachTransform(grabInteractable).position.x;
        });
        grabInteractable.selectExited.AddListener(args => {
            currentInteractor = null;
            grabOffset = 0f;
    });
}

    void LateUpdate() {
        if (currentInteractor == null) return;
        float interactorX = currentInteractor.GetAttachTransform(grabInteractable).position.x;
        float centerX = clipTransform.position.x;
        float halfWidth = clipTransform.localScale.x / 2f;
        if (side == HandleSide.Right) {
            float leftEdge = centerX - halfWidth;
            float newWidth = Mathf.Max(minClipWidth, interactorX - leftEdge + grabOffset);
            clipTransform.localScale = new Vector3(newWidth, clipTransform.localScale.y, clipTransform.localScale.z);
            clipTransform.position = new Vector3(leftEdge + newWidth / 2f, clipTransform.position.y, clipTransform.position.z);
        } else {
            float rightEdge = centerX + halfWidth;
            float newWidth = Mathf.Max(minClipWidth, rightEdge - interactorX - grabOffset);
            clipTransform.localScale = new Vector3(newWidth, clipTransform.localScale.y, clipTransform.localScale.z);
            clipTransform.position = new Vector3(rightEdge - newWidth / 2f, clipTransform.position.y, clipTransform.position.z);
        }

        transform.localPosition = new Vector3(
            interactorX,
            clipTransform.position.y,
            clipTransform.position.z - .2f);
    }
}