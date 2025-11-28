using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Assets.Scripts.Manager;

public class ARDragItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private Canvas canvas;
    private RectTransform dragIcon;
    private Image iconImage;
    private Vector2 originalPos;

    [Header("拖拽配置")]
    public GameObject arObjectPrefab; // 直接引用带ARPlacedObject的预制体

    [Header("移动设备配置")]
    public float dragThreshold = 10f; // 拖拽阈值（像素）
    public bool useTouchFallback = true; // 使用触摸备用方案

    [Header("调试信息")]
    public bool enableDebugLogs = true;

    private EasyARSpatialMapEditorManager arManager;
    private bool isDragging = false;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        arManager = EasyARSpatialMapEditorManager.Instance;

        // 设置拖拽阈值
        var eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.pixelDragThreshold = (int)dragThreshold;
            if (enableDebugLogs)
                Debug.Log($"[ARDrag] 设置拖拽阈值: {dragThreshold}");
        }

        // 确保UI元素可以接收触摸事件
        var image = GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }

        // 确保有Button组件用于移动设备交互
        if (GetComponent<Button>() == null && useTouchFallback)
        {
            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            // 添加点击事件作为备用方案
            //button.onClick.AddListener(OnButtonClick);
        }

        if (enableDebugLogs)
            Debug.Log($"[ARDrag] {gameObject.name} 初始化完成");
    }

    ///// <summary>
    ///// 备用方案：按钮点击直接放置对象（适用于移动设备）
    ///// </summary>
    //public void OnButtonClick()
    //{
    //    if (enableDebugLogs)
    //        Debug.Log("[ARDrag] 按钮点击 - 尝试放置对象");

    //    if (!CanPlaceObject())
    //    {
    //        Debug.LogWarning("[ARDrag] 无法放置对象：地图未本地化或不在编辑模式");
    //        return;
    //    }

    //    // 在屏幕中心放置对象
    //    Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    //    TryPlaceObjectInARSpace(screenCenter);
    //}

    void Update()
    {
        // 备用的触摸检测（针对移动设备）
        if (enableDebugLogs && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Debug.Log($"[ARDrag] 检测到触摸开始: {touch.position}");
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (enableDebugLogs)
            Debug.Log($"[ARDrag] OnPointerDown - 触摸开始: {eventData.position}");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (enableDebugLogs)
            Debug.Log($"[ARDrag] OnPointerUp - 触摸结束: {eventData.position}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (enableDebugLogs)
            Debug.Log($"[ARDrag] OnBeginDrag - 开始拖拽: {eventData.position}");

        isDragging = true;

        // 取消之前选中的对象，避免拖拽新物体时旧物体跟着移动
        if (arManager != null)
        {
            arManager.DeselectAllObjects();
            if (enableDebugLogs)
                Debug.Log("[ARDrag] 已取消之前选中的对象");
        }

        // 创建拖拽图标
        CreateDragIcon(eventData);

        // 检查是否可以放置对象
        if (!CanPlaceObject())
        {
            Debug.LogWarning("[ARDrag] 无法放置对象：地图未本地化或不在编辑模式");
            return;
        }

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (enableDebugLogs)
            Debug.Log($"[ARDrag] OnDrag - 拖拽中: {eventData.position}");

        if (dragIcon)
            dragIcon.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (enableDebugLogs)
            Debug.Log($"[ARDrag] OnEndDrag - 结束拖拽: {eventData.position}");

        isDragging = false;

        if (dragIcon)
            Destroy(dragIcon.gameObject);

        // 检查是否可以放置对象
        if (!CanPlaceObject()) return;

        // 使用 EasyAR 空间地图的放置方法
        TryPlaceObjectInARSpace(eventData.position);
    }

    /// <summary>
    /// 检查是否可以放置对象
    /// </summary>
    private bool CanPlaceObject()
    {
        return arManager != null &&
               arManager.IsMapLocalized &&
               arManager.IsEditMode &&
               arObjectPrefab != null &&
               arObjectPrefab.GetComponent<ARPlacedObject>() != null;
    }

    /// <summary>
    /// 创建拖拽图标
    /// </summary>
    private void CreateDragIcon(PointerEventData eventData)
    {
        // 创建拖拽图标GameObject
        dragIcon = new GameObject("DragIcon").AddComponent<RectTransform>();
        dragIcon.SetParent(canvas.transform, false);
        dragIcon.sizeDelta = ((RectTransform)transform).sizeDelta;

        // 添加Image组件并设置图标
        iconImage = dragIcon.gameObject.AddComponent<Image>();
        var sourceImage = GetComponent<Image>();
        if (sourceImage != null)
        {
            iconImage.sprite = sourceImage.sprite;
        }
        iconImage.raycastTarget = false;
        iconImage.color = new Color(1, 1, 1, 0.8f); // 半透明

        // 设置初始位置
        originalPos = eventData.position;
        dragIcon.position = originalPos;

        // 确保拖拽图标在最上层
        dragIcon.SetAsLastSibling();

        if (enableDebugLogs)
            Debug.Log($"[ARDrag] 创建拖拽图标: {dragIcon.position}");
    }

    /// <summary>
    /// 使用 EasyAR 空间地图放置对象 - 复用ARPlacedObject的放置功能
    /// </summary>
    private void TryPlaceObjectInARSpace(Vector2 screenPosition)
    {
        if (arObjectPrefab == null)
        {
            Debug.LogError("[ARDrag] arObjectPrefab 未设置");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[ARDrag] 尝试在位置放置对象: {screenPosition}");

        // 将屏幕坐标转换为归一化视口坐标
        Vector2 normalizedPosition = new Vector2(
            screenPosition.x / Screen.width,
            screenPosition.y / Screen.height
        );

        if (enableDebugLogs)
            Debug.Log($"[ARDrag] 归一化坐标: {normalizedPosition}");

        // 使用 EasyAR 的空间地图碰撞检测
        var hitResult = arManager.HitTestSparsePointCloud(normalizedPosition);

        if (hitResult.OnSome)
        {
            Vector3 hitPosition = hitResult.Value;

            if (enableDebugLogs)
                Debug.Log($"[ARDrag] 射线击中位置: {hitPosition}");

            // 实例化AR对象预制体
            GameObject newObject = Instantiate(arObjectPrefab, hitPosition, Quaternion.identity);

            // 获取或添加ARPlacedObject组件
            var arPlacedObject = newObject.GetComponent<ARPlacedObject>();
            if (arPlacedObject == null)
            {
                arPlacedObject = newObject.AddComponent<ARPlacedObject>();
                if (enableDebugLogs)
                    Debug.Log("[ARDrag] 添加了ARPlacedObject组件");
            }

            // 手动设置位置和状态
            newObject.transform.position = hitPosition;

            // 注册到管理器
            arManager.RegisterPlacedObjectAtPosition(newObject, hitPosition);

            Debug.Log($"[ARDrag] 成功放置对象: {arObjectPrefab.name} 在位置: {hitPosition}");
        }
        else
        {
            Debug.LogWarning("[ARDrag] 射线未击中稀疏点云，无法放置对象");
        }
    }
}
