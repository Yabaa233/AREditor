using UnityEngine;
using Assets.Scripts.Manager;
using Common;

/// <summary>
/// 基于EasyAR空间地图的AR放置对象
/// 简化版本：只负责数据管理，不管理选择和手势控制
/// </summary>
[RequireComponent(typeof(Collider))]
public class ARPlacedObject : PlacedObject
{
    [Header("EasyAR Integration")]
    public bool useSpatialMapPlacement = true;

    // EasyAR空间地图相关
    private bool isPlacedOnMap = false;
    private Vector3 mapPlacementPosition;

    // 视觉反馈
    private Renderer[] renderers;
    private Color[] originalColors;

    // transform change detection for auto-save
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale; protected override void Start()
    {
        // 不调用 base.Start()，因为基类有 PlaneViewer 的引用
        if (!initialized)
        {
            InitializeFromTemplateARSafe();
        }

        // 如果启用了空间地图放置，禁用默认的AR放置
        if (useSpatialMapPlacement)
        {
            DisableDefaultARPlacement();
        }

        // cache last transform for change detection
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastScale = transform.localScale;

        // EasyARSpatialMapEditorManager.Instance.RegisterObject(gameObject);
    }

    void Awake()
    {
        // 确保有碰撞器用于射线检测
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }

        // 初始化渲染器用于视觉反馈
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    /// <summary>
    /// 启用或禁用交互功能
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void EnableInteraction(bool enable)
    {
        // 简化：只保留基本的启用/禁用功能
        Debug.Log($"对象 {name} 交互功能: {(enable ? "启用" : "禁用")}");
    }

    /// <summary>
    /// 禁用默认的AR放置功能
    /// </summary>
    private void DisableDefaultARPlacement()
    {
        // EasyAR不需要禁用AR Foundation的组件
        // 这里可以禁用其他可能冲突的组件
        Debug.Log("使用EasyAR空间地图放置模式");
    }

    protected void InitializeFromTemplateARSafe()
    {
        if (templateDatabase == null)
        {
            Debug.LogError("Template Database is not assigned.");
            return;
        }

        var template = templateDatabase.GetTemplateByID(selectedTemplateID);
        if (template == null)
        {
            Debug.LogError($"Template ID '{selectedTemplateID}' not found in database ");
            return;
        }

        runtimeData = new PlacedObjectData
        {
            ID = EditorManager.Instance.GenerateUniqueID(),
            templateID = selectedTemplateID,
            position = transform.position,
            rotation = transform.rotation.eulerAngles,
            scale = transform.localScale,
            events = new(template.defaultEvents)
        };

        initialized = true;
    }

    public override void InitializeFromJson()
    {
        ObjectTemplateData template = templateDatabase.GetTemplateByID(selectedTemplateID);
        if (template == null)
        {
            Debug.LogError($"Template ID '{selectedTemplateID}' not found in database ");
            return;
        }

        initialized = true;
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

            // 通知Manager保存对象信息（如果启用了自动保存）
            var manager = EasyARSpatialMapEditorManager.Instance;
            if (manager != null && manager.autoSaveOnEdit)
            {
                manager.SaveObjectsInfo();
            }
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
    /// 简化的 Update - 只负责数据同步和自动保存
    /// </summary>
    private void Update()
    {
        // 监测 transform 变化以便自动保存运行时数据
        if (transform.position != lastPosition || transform.rotation != lastRotation || transform.localScale != lastScale)
        {
            UpdateRuntimeDataPosition();
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastScale = transform.localScale;
        }
    }

    /// <summary>
    /// 删除对象
    /// </summary>
    public void DeleteObject()
    {
        var spatialMapManager = EasyARSpatialMapEditorManager.Instance;
        if (spatialMapManager != null)
        {
            spatialMapManager.UnregisterObject(gameObject);
        }

        // 通知EditorManager
        if (EditorManager.Instance != null)
        {
            EditorManager.Instance.UnregisterObject(gameObject);
        }

        Debug.Log($"删除对象: {gameObject.name}");
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
