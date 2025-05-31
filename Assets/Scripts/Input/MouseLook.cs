using UnityEngine;

public class MouseLook : MonoBehaviour {
    [Header("Settings")]
    public float mouseSensitivity = 1.0f;
    public float controllerSensitivity = 1.0f;
    public Transform rotationTarget;
    public Transform followTarget;

    [Header("Debug")]
    public float lookSense;

    private float pitch = 0f;
    private float yaw = 0f;

    void Update()
    {
        Vector2 lookInput = InputProvider.Instance.LookInput;
        lookSense = InputProvider.Instance.CurrentControlScheme == ControlScheme.KeyboardMouse ? mouseSensitivity : controllerSensitivity;

        // Apply sensitivity
        lookInput *= lookSense * Time.deltaTime;

        // Accumulate rotation
        yaw += lookInput.x;
        pitch -= lookInput.y;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        // Apply horizontal rotation to both the camera and the rotation target
        rotationTarget.localEulerAngles = new Vector3(0f, yaw, 0f);
        transform.localEulerAngles = new Vector3(pitch, yaw, 0f);
    }

    void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position;
        }
    }
}