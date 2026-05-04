using UnityEngine;
using UnityEngine.XR.Hands;
using System.Collections.Generic;

public class HandDebug : MonoBehaviour {
    void Update() {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count == 0)
            Debug.Log("NO HAND SUBSYSTEM RUNNING");
        else
            Debug.Log("Hand subsystem active: " + subsystems[0].running);
    }
}