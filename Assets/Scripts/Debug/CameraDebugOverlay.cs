using UnityEngine;

public class CameraDebugOverlay : MonoBehaviour
{
    public Camera targetCamera;
    public TopDownCameraController cameraController;

    [Header("Display Options")]
    public int fontSize = 32;
    public Color textColor = Color.white;
    public Vector2 offset = new Vector2(10, 10);

    private GUIStyle guiStyle;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (cameraController == null)
            cameraController = FindObjectOfType<TopDownCameraController>();

        guiStyle = new GUIStyle
        {
            fontSize = fontSize,
            normal = new GUIStyleState { textColor = textColor }
        };
    }

    void OnGUI()
    {
        if (targetCamera == null) return;

        Vector3 pos = targetCamera.transform.position;
        Vector3 rot = targetCamera.transform.eulerAngles;
        float size = targetCamera.orthographic ? targetCamera.orthographicSize : -1;

        string debugText =
            $"[Camera]\n" +
            $"Pos: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})\n" +
            $"Rot: ({rot.x:F1}, {rot.y:F1}, {rot.z:F1})\n" +
            $"OrthoSize: {size:F2}";

        if (cameraController != null)
        {
            debugText += $"\n[State] {cameraController.CurrentState}";
        }

        GUI.Label(new Rect(offset.x, offset.y, 1000, 300), debugText, guiStyle);
    }
}
