using UnityEngine;
using UnityEngine.InputSystem;

public enum InputMode { Traditional, VR }

public class ModeManager : MonoBehaviour {
    public GameObject vrModeGroup;
    public GameObject traditionalModeGroup;
    public InputMode startingMode = InputMode.Traditional;
    public InputActionReference switchModeAction;

    void Start() {
        SetMode(startingMode);
        switchModeAction.action.performed += _ => SetMode(InputMode.Traditional);
    }

    public void SetMode(InputMode mode) {
        bool isVR = mode == InputMode.VR;
        vrModeGroup.SetActive(isVR);
        traditionalModeGroup.SetActive(!isVR);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.F1)) SetMode(InputMode.Traditional);
        if (Input.GetKeyDown(KeyCode.F2)) SetMode(InputMode.VR);
    }

    void OnEnable() { switchModeAction.action.Enable(); }
    void OnDisable() { switchModeAction.action.Disable(); }
}