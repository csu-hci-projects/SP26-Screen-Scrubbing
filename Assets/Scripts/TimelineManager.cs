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
        // to keep on timeline
        // float lockedY = transform.position.y;
        // float lockedZ = transform.position.z;

        float snappedTime = SnapTime(WorldXToTime(worldX));
        float snappedX = Mathf.Lerp(
            timelineStartX, 
            timelineEndX, 
            snappedTime / totalDuration);
        
        clip.transform.position = new Vector3(
            snappedX, 
            transform.position.y, 
            transform.position.z); 
        // clip.transform.position = new Vector3(
        //     transform.position.x,
        //     lockedY,
        //     lockedZ
        // );
        clip.startTime = snappedTime;
    }
}