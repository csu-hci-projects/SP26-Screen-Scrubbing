using UnityEngine;

public class FeedbackManager : MonoBehaviour {
    public AudioSource audioSource;
    public AudioClip selectSound;
    public AudioClip placeSound;

    public bool audioEnabled = true;
    public bool visualEnabled = true;

    public void PlaySelectFeedback(GameObject target) {
        if (audioEnabled) audioSource.PlayOneShot(selectSound);
        if (visualEnabled) FlashColor(target, Color.yellow);
    }

    public void PlayPlaceFeedback(GameObject target) {
        if (audioEnabled) audioSource.PlayOneShot(placeSound);
        if (visualEnabled) FlashColor(target, Color.green);
    }

    void FlashColor(GameObject obj, Color color) {
        obj.GetComponent<Renderer>().material.color = color;
    }
}