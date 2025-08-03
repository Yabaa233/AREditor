using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.AR;
using Assets.Scripts.Manager;

/// <summary>
/// 专门用于EasyAR空间地图的PlacedObject
/// 继承自ARPlacedObject，集成EasyAR的空间地图功能
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(ARSelectionInteractable))]
[RequireComponent(typeof(ARTranslationInteractable))]
[RequireComponent(typeof(ARScaleInteractable))]
[RequireComponent(typeof(ARRotationInteractable))]
public class EasyARPlacedObject : ARPlacedObject
{
    [Header("EasyAR Integration")]
    public bool useSpatialMapPlacement = true;
    
    private ARSelectionInteractable selection;
    private ARTranslationInteractable translation;
    private ARScaleInteractable scale;
    private ARRotationInteractable rotation;
    
    // EasyAR空间地图相关
    private bool isPlacedOnMap = false;
    private Vector3 mapPlacementPosition;

    protected override void Start()
    {
        // 不调用 base.Start()，因为基类有 PlaneViewer 的引用
        if (!initialized)
        {
            InitializeFromTemplateARSafe();
        }

        EnableARInteraction(true);
        
        // 如果启用了空间地图放置，禁用默认的AR放置
        if (useSpatialMapPlacement)
        {
            DisableDefaultARPlacement();
        }
    }

    void Awake()
    {
        // 确保 AR 组件存在并被正确启用
        selection = GetComponent<ARSelectionInteractable>() ?? gameObject.AddComponent<ARSelectionInteractable>();
        translation = GetComponent<ARTranslationInteractable>() ?? gameObject.AddComponent<ARTranslationInteractable>();
        scale = GetComponent<ARScaleInteractable>() ?? gameObject.AddComponent<ARScaleInteractable>();
        rotation = GetComponent<ARRotationInteractable>() ?? gameObject.AddComponent<ARRotationInteractable>();
    }

    private void EnableARInteraction(bool enable)
    {
        if (selection) selection.enabled = enable;
        if (translation) translation.enabled = enable;
        if (scale) scale.enabled = enable;
        if (rotation) rotation.enabled = enable;
    }

    /// <summary>
    /// 禁用默认的AR放置功能
    /// </summary>
    private void DisableDefaultARPlacement()
    {
        // 禁用AR Foundation的平面检测放置
        var arPlacement = FindObjectOfType<ARPlacementInteractable>();
        if (arPlacement != null)
        {
            arPlacement.enabled = false;
        }
    }

    /// <summary>
    /// 在空间地图上放置对象
    /// </summary>
    public bool PlaceOnSpatialMap(Vector2 screenPosition)
    {
        if (!useSpatialMapPlacement)
        {
            return false;
        }

        var spatialMapManager = EasyARSpatialMapEditorManager.Instance;
        if (spatialMapManager == null || !spatialMapManager.IsMapLocalized)
        {
            Debug.LogWarning("空间地图未本地化，无法放置对象");
            return false;
        }

        var hitResult = spatialMapManager.GetMapHitPoint(screenPosition);
        if (hitResult.OnSome)
        {
            transform.position = hitResult.Value;
            isPlacedOnMap = true;
            mapPlacementPosition = hitResult.Value;
            
            // 注册到空间地图编辑器管理器
            spatialMapManager.RegisterObject(gameObject);
            
            Debug.Log($"对象已放置在空间地图上: {transform.position}");
            return true;
        }
        
        Debug.LogWarning("未找到有效的放置点");
        return false;
    }

    /// <summary>
    /// 在空间地图上移动对象
    /// </summary>
    public bool MoveOnSpatialMap(Vector2 screenPosition)
    {
        if (!useSpatialMapPlacement || !isPlacedOnMap)
        {
            return false;
        }

        var spatialMapManager = EasyARSpatialMapEditorManager.Instance;
        if (spatialMapManager == null || !spatialMapManager.IsMapLocalized)
        {
            return false;
        }

        var hitResult = spatialMapManager.GetMapHitPoint(screenPosition);
        if (hitResult.OnSome)
        {
            transform.position = hitResult.Value;
            mapPlacementPosition = hitResult.Value;
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 检查对象是否放置在空间地图上
    /// </summary>
    public bool IsPlacedOnMap()
    {
        return isPlacedOnMap;
    }

    /// <summary>
    /// 获取地图放置位置
    /// </summary>
    public Vector3 GetMapPlacementPosition()
    {
        return mapPlacementPosition;
    }

    /// <summary>
    /// 更新运行时数据中的位置信息
    /// </summary>
    public void UpdateRuntimeDataPosition()
    {
        if (runtimeData != null)
        {
            runtimeData.position = transform.position;
            runtimeData.rotation = transform.rotation.eulerAngles;
            runtimeData.scale = transform.localScale;
        }
    }

    /// <summary>
    /// 从运行时数据恢复位置信息
    /// </summary>
    public void RestoreFromRuntimeData()
    {
        if (runtimeData != null)
        {
            transform.position = runtimeData.position;
            transform.rotation = Quaternion.Euler(runtimeData.rotation);
            transform.localScale = runtimeData.scale;
            
            // 如果位置不为零，认为已经放置在地图上
            if (runtimeData.position != Vector3.zero)
            {
                isPlacedOnMap = true;
                mapPlacementPosition = runtimeData.position;
            }
        }
    }

    /// <summary>
    /// 处理触摸输入
    /// </summary>
    private void Update()
    {
        // 如果启用了空间地图放置，处理触摸输入
        if (useSpatialMapPlacement && Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                // 检查是否点击了当前对象
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
                {
                    // 选中对象
                    OnSelected();
                }
                else if (isPlacedOnMap)
                {
                    // 移动对象到新位置
                    MoveOnSpatialMap(touch.position);
                }
                else
                {
                    // 放置新对象
                    PlaceOnSpatialMap(touch.position);
                }
            }
        }
    }

    /// <summary>
    /// 当对象被选中时调用
    /// </summary>
    public void OnSelected()
    {
        Debug.Log($"对象被选中: {gameObject.name}");
    }

    /// <summary>
    /// 当对象被取消选中时调用
    /// </summary>
    public void OnDeselected()
    {
        Debug.Log($"对象被取消选中: {gameObject.name}");
    }

    private void OnDestroy()
    {
        // 从空间地图编辑器管理器中注销
        var spatialMapManager = EasyARSpatialMapEditorManager.Instance;
        if (spatialMapManager != null)
        {
            spatialMapManager.UnregisterObject(gameObject);
        }
    }
} 