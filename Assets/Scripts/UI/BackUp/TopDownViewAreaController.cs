using UnityEngine;
using UnityEngine.EventSystems;

public class TopDownCameraController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Camera topDownCamera;

    // 拖动参数
    public float panSpeed = 0.01f;

    // 缩放限制
    public float zoomMin = 2f;
    public float zoomMax = 20f;

    // 旋转参数
    public float rotationThreshold = 5f;      // 最小触发角度（单位：度）
    public float rotationSensitivity = 1f;    // 设置为 -1 可反转旋转方向

    // 状态控制
    private CameraControlState currentState = CameraControlState.Idle;
    public CameraControlState CurrentState => currentState;  // 提供只读访问

    // 状态变量
    private Vector2 actionPosition;
    private float initialSpacing;
    private float initialRotation_forRotation;

    private bool isPointerInside = false;

    // 双击复位用
    private float lastTapTime = 0f;
    private Vector2 lastTapPosition;
    public float doubleTapThreshold = 0.8f;
    public float tapMoveThreshold = 200f;

    private float lastMultiTouchEndTime = 0f;
    public float singleTouchDelayThreshold = 0.8f;

    // 记录初始状态
    private Vector3 initialPosition;
    private Quaternion initialRotation_forReset;
    private float initialOrthoSize;

    void Start()
    {
        if (topDownCamera != null)
        {
            initialPosition = topDownCamera.transform.position;
            initialRotation_forReset = topDownCamera.transform.rotation;
            initialOrthoSize = topDownCamera.orthographicSize;
        }
    }

    void Update()
    {
        if (!isPointerInside) return;

        switch (Input.touchCount)
        {
            case 0:
                currentState = CameraControlState.Idle;
                break;

            case 1:
                HandlePanTouch();
                break;

            case 2:
                HandleZoomRotateTouch();
                break;

            default:
                currentState = CameraControlState.Idle;
                break;
        }
    }

    private void HandlePanTouch()
    {
        if (Time.time - lastMultiTouchEndTime < singleTouchDelayThreshold) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            if (Time.time - lastTapTime < doubleTapThreshold &&
                Vector2.Distance(touch.position, lastTapPosition) < tapMoveThreshold)
            {
                ResetView();
            }

            lastTapTime = Time.time;
            lastTapPosition = touch.position;

            actionPosition = touch.position;
            currentState = CameraControlState.Pan;
        }
        else if (touch.phase == TouchPhase.Moved && currentState == CameraControlState.Pan)
        {
            Vector2 delta = touch.position - actionPosition;

            Vector3 right = topDownCamera.transform.right;
            Vector3 up = topDownCamera.transform.up;

            Vector3 move = (-right * delta.x - up * delta.y) * panSpeed;
            topDownCamera.transform.position += move;

            actionPosition = touch.position;
        }

    }

    private void HandleZoomRotateTouch()
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
        {
            currentState = CameraControlState.ZoomRotate;
            initialSpacing = Vector2.Distance(t0.position, t1.position);
            initialRotation_forRotation = GetAngle(t0, t1);
        }
        else if ((t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved) && currentState == CameraControlState.ZoomRotate)
        {
            float currentSpacing = Vector2.Distance(t0.position, t1.position);
            float zoomFactor = currentSpacing / initialSpacing;
            topDownCamera.orthographicSize = Mathf.Clamp(topDownCamera.orthographicSize / zoomFactor, zoomMin, zoomMax);
            initialSpacing = currentSpacing;

            float currentRotation = GetAngle(t0, t1);
            float deltaRot = currentRotation - initialRotation_forRotation;
            deltaRot = Mathf.Repeat(deltaRot + 180f, 360f) - 180f;

            if (Mathf.Abs(deltaRot) > rotationThreshold)
            {
                topDownCamera.transform.Rotate(Vector3.up, deltaRot * rotationSensitivity, Space.World);
                initialRotation_forRotation = currentRotation;
            }
        }
        else if (t0.phase == TouchPhase.Ended || t1.phase == TouchPhase.Ended)
        {
            currentState = CameraControlState.Idle;
            lastMultiTouchEndTime = Time.time;
        }
    }

    private float GetAngle(Touch t0, Touch t1)
    {
        Vector2 diff = t1.position - t0.position;
        return Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
    }

    public void ResetView()
    {
        if (topDownCamera != null)
        {
            topDownCamera.transform.position = initialPosition;
            topDownCamera.transform.rotation = initialRotation_forReset;
            topDownCamera.orthographicSize = initialOrthoSize;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => isPointerInside = true;
    public void OnPointerExit(PointerEventData eventData) => isPointerInside = false;

}
