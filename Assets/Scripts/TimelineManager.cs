using UnityEngine;

public class TimelineManager : MonoBehaviour {
    public float timelineStartX;
    public float timelineEndX;
    public float totalDuration;
    public float snapInterval = 0.1f;

    public float WorldXToTime(float worldX) {
        float t = Mathf.InverseLerp(
            timelineStartX, 
            timelineEndX, 
            worldX);
        return t * totalDuration;
    }

    public float SnapTime(float time) {
        return Mathf.Round(time / snapInterval) * snapInterval;
    }

    public void PlaceClip(TimelineClip clip, float worldX) {
        float snappedTime = SnapTime(WorldXToTime(worldX));
        float snappedX = Mathf.Lerp(
            timelineStartX, 
            timelineEndX, 
            snappedTime / totalDuration);
        
        clip.transform.position = new Vector3(
            snappedX, 
            clip.transform.position.y,
            clip.transform.position.z); 
            
        clip.startTime = snappedTime;
    }
}