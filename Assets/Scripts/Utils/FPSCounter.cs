using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    private float deltaTime = 0.0f;

    private void OnEnable()
    {
        if (InputProvider.Instance != null)
            InputProvider.Instance.VSyncToggle.Started += ToggleVSync;
    }

    private void OnDisable()
    {
        if (InputProvider.Instance != null)
            InputProvider.Instance.VSyncToggle.Started -= ToggleVSync;
    }

    private void ToggleVSync()
    {
        QualitySettings.vSyncCount = (QualitySettings.vSyncCount == 0) ? 1 : 0;
        Debug.Log($"[FPSCounter] VSync Toggled: {(QualitySettings.vSyncCount == 0 ? "Off" : "On")}");
    }


    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        int width = Screen.width, height = Screen.height;
        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(10, 10, width, height * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = height * 2 / 50;
        style.normal.textColor = Color.white;

        float fps = 1.0f / deltaTime;
        string text = $"{fps:0.} FPS";
        GUI.Label(rect, text, style);
    }
}
