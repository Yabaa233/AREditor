using UnityEngine;
using UnityEngine.EventSystems;

public enum CameraControlState { Idle, Pan, ZoomRotate }

public class TopDownCameraControllerUpdate : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Camera topDownCamera;
    public float panSpeed = 0.01f;
    public float zoomMin = 2f;
    public float zoomMax = 20f;
    public float rotationThreshold = 5f;
    public float rotationSensitivity = 1f;

    private CameraControlState currentState = CameraControlState.Idle;
    public CameraControlState CurrentState => currentState;

    private Vector2 actionPosition;
    private float initialSpacing;
    private float initialRotation_forRotation;

    private bool isPointerInside = false;

    private float lastTapTime = 0f;
    private Vector2 lastTapPosition;
    public float doubleTapThreshold = 0.3f;
    public float tapMoveThreshold = 20f;

    private Vector3 initialPosition;
    private Quaternion initialRotation_forReset;
    private float initialOrthoSize;

    void Start()
    {
        topDownCamera = UIManager.Instance.raycastCamera;
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

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
            HandleTouchInput();
#endif
    }

    // =================== Windows (鼠标操作) =================
#if UNITY_EDITOR || UNITY_STANDALONE
    void HandleMouseInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            topDownCamera.orthographicSize = Mathf.Clamp(topDownCamera.orthographicSize - scroll * 5f, zoomMin, zoomMax);
        }

        if (Input.GetMouseButtonDown(1))
        {
            actionPosition = Input.mousePosition;
            currentState = CameraControlState.Pan;
        }
        else if (Input.GetMouseButton(1) && currentState == CameraControlState.Pan)
        {
            Vector2 delta = (Vector2)Input.mousePosition - actionPosition;

            Vector3 right = topDownCamera.transform.right;
            Vector3 up = topDownCamera.transform.up;

            Vector3 move = (-right * delta.x - up * delta.y) * panSpeed;
            topDownCamera.transform.position += move;

            actionPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            currentState = CameraControlState.Idle;
        }

        if (Input.GetMouseButtonDown(2))
        {
            actionPosition = Input.mousePosition;
            currentState = CameraControlState.ZoomRotate;
        }
        else if (Input.GetMouseButton(2) && currentState == CameraControlState.ZoomRotate)
        {
            float deltaX = Input.mousePosition.x - actionPosition.x;
            if (Mathf.Abs(deltaX) > rotationThreshold)
            {
                topDownCamera.transform.Rotate(Vector3.up, deltaX * rotationSensitivity, Space.World);
                actionPosition = Input.mousePosition;
            }
        }
        else if (Input.GetMouseButtonUp(2))
        {
            currentState = CameraControlState.Idle;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            topDownCamera.transform.Rotate(Vector3.up, -1f * rotationSensitivity, Space.World);
        }
        if (Input.GetKey(KeyCode.E))
        {
            topDownCamera.transform.Rotate(Vector3.up, 1f * rotationSensitivity, Space.World);
        }

        if (Input.GetMouseButtonDown(0))
        {
            float timeSinceLast = Time.time - lastTapTime;
            float dist = Vector2.Distance(Input.mousePosition, lastTapPosition);
            if (timeSinceLast < doubleTapThreshold && dist < tapMoveThreshold)
            {
                ResetView();
            }
            lastTapTime = Time.time;
            lastTapPosition = Input.mousePosition;
        }
    }
#else
        // =================== 移动设备 (触摸操作) ===================
        void HandleTouchInput()
        {
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
        }

        private float GetAngle(Touch t0, Touch t1)
        {
            Vector2 diff = t1.position - t0.position;
            return Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        }
#endif

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
