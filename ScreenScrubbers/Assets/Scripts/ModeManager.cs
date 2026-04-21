using UnityEngine;

public enum InputMode { Traditional, VR }

public class ModeManager : MonoBehaviour {
    public GameObject vrModeGroup;
    public GameObject traditionalModeGroup;
    public InputMode startingMode = InputMode.Traditional;

    void Start() {
        SetMode(startingMode);
    }

    public void SetMode(InputMode mode) {
        bool isVR = mode == InputMode.VR;
        vrModeGroup.SetActive(isVR);
        traditionalModeGroup.SetActive(!isVR);
    }

    void Update() {
        if (switchToTraditionalAction != null && switchToTraditionalAction.action.WasPressedThisFrame()) 
        {
            SetMode(InputMode.Traditional);
        }
        if (Input.GetKeyDown(KeyCode.F1)) SetMode(InputMode.Traditional);
        if (Input.GetKeyDown(KeyCode.F2)) SetMode(InputMode.VR);
    }
}