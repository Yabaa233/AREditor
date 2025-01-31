using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ZoomView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // 属性变量
    private Vector2 translation; // 移动
    private float scale = 1; // 伸缩比例
    private float rotation; // 旋转角度

    // 临时变量
    private Vector2 actionPosition;
    private float initialSpacing;
    private float initialRotation;
    private int moveType; // 0=未选择，1=拖动，2=缩放与旋转

    // 双击检测
    private float lastTapTime = 0f; // 上次触控的时间
    public float doubleTapThreshold = 0.8f; // 双击时间间隔阈值（秒）
    public float tapMoveThreshold = 200f; // 双击移动容忍距离阈值（像素）
    private Vector2 lastTapPosition; // 上次触控的位置

    // 宽容时间检测
    private float lastMultiTouchEndTime = 0f; // 上次双指操作结束的时间
    public float singleTouchDelayThreshold = 0.8f; // 单指操作的延迟时间（秒）

    public RectTransform content; // 需要操作的内容

    private bool isPointerInside = false;

    // 缩放限制
    public float minScale = 0.3f;
    public float maxScale = 10.0f; // 最大放大倍数

    void Start()
    {
        // 设置初始值
        translation = Vector2.zero;
        scale = 1f;
        rotation = 0f;
    }

    void Update()
    {
        HandleTouchInput();
    }

    private void HandleTouchInput()
    {
        if (!isPointerInside) return;

        switch (Input.touchCount)
        {
            case 1:
                if (Time.time - lastMultiTouchEndTime < singleTouchDelayThreshold)
                {
                    // 如果单指操作紧接双指操作，忽略当前单指操作
                    return;
                }

                // 单指移动
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    // 检测双击
                    if (Time.time - lastTapTime < doubleTapThreshold &&
                        Vector2.Distance(touch.position, lastTapPosition) < tapMoveThreshold)
                    {
                        ResetView(); // 双击触发复位
                    }

                    // 更新上次触控时间和位置
                    lastTapTime = Time.time;
                    lastTapPosition = touch.position;

                    moveType = 1;
                    actionPosition = touch.position;
                }
                else if (touch.phase == TouchPhase.Moved && moveType == 1)
                {
                    Vector2 delta = touch.position - actionPosition;
                    translation += delta;
                    content.anchoredPosition += delta;
                    actionPosition = touch.position;
                }
                break;

            case 2:
                // 双指缩放与旋转
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
                {
                    moveType = 2;
                    initialSpacing = GetSpacing(touch0, touch1);
                    initialRotation = GetAngle(touch0, touch1);
                }
                else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
                {
                    if (moveType == 2)
                    {
                        // 缩放
                        float currentSpacing = GetSpacing(touch0, touch1);
                        scale *= currentSpacing / initialSpacing;
                        content.localScale = Vector3.one * Mathf.Clamp(scale, minScale, maxScale);
                        initialSpacing = currentSpacing;

                        // 旋转
                        float currentRotation = GetAngle(touch0, touch1);
                        float deltaRotation = currentRotation - initialRotation;
                        rotation += deltaRotation;
                        content.localRotation = Quaternion.Euler(0, 0, rotation);
                        initialRotation = currentRotation;
                    }
                }
                else if (touch0.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Ended)
                {
                    // 记录双指操作结束的时间
                    lastMultiTouchEndTime = Time.time;
                }
                break;

            default:
                moveType = 0;
                break;
        }
    }

    // 获取两点之间的距离
    private float GetSpacing(Touch touch0, Touch touch1)
    {
        float dx = touch0.position.x - touch1.position.x;
        float dy = touch0.position.y - touch1.position.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    // 获取两点之间的角度
    private float GetAngle(Touch touch0, Touch touch1)
    {
        float dx = touch1.position.x - touch0.position.x;
        float dy = touch1.position.y - touch0.position.y;
        return Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
    }

    public void ResetView()
    {
        translation = Vector2.zero;
        scale = 1f;
        rotation = 0f;
        content.anchoredPosition = Vector2.zero;
        content.localScale = Vector3.one;
        content.localRotation = Quaternion.identity;
    }
}
