using UnityEngine;
using Assets.Scripts.Manager;
using SpatialMap_SparseSpatialMap;

/// <summary>
/// 测试EasyAR空间地图编辑器的地图管理功能
/// 独立测试脚本，不修改任何现有管理器
/// </summary>
public class TestMapFunctions : MonoBehaviour
{
    [Header("Test Settings")]
    public bool enableKeyboardTesting = true;
    public bool enableAutoTesting = false;
    public float autoTestInterval = 5f;
    
    [Header("Debug Info")]
    public bool showDebugInfo = true;
    
    private EasyARSpatialMapEditorManager editorManager;
    private float lastAutoTestTime;
    private int testStep = 0;

    void Start()
    {
        Debug.Log("[TestMapFunctions] 开始测试地图管理功能");
        
        // 获取编辑器管理器
        editorManager = EasyARSpatialMapEditorManager.Instance;
        if (editorManager == null)
        {
            Debug.LogError("[TestMapFunctions] ❌ EasyARSpatialMapEditorManager 未找到");
            return;
        }
        
        Debug.Log("[TestMapFunctions] ✅ EasyARSpatialMapEditorManager 找到");
        
        // 订阅事件
        SubscribeToEvents();
        
        // 显示初始状态
        ShowEditorStatus();
    }

    void Update()
    {
        if (editorManager == null) return;
        
        // 键盘测试
        if (enableKeyboardTesting)
        {
            HandleKeyboardInput();
        }
        
        // 自动测试
        if (enableAutoTesting)
        {
            HandleAutoTesting();
        }
        
        // 显示调试信息
        if (showDebugInfo)
        {
            ShowDebugInfo();
        }
    }

    /// <summary>
    /// 处理键盘输入测试
    /// </summary>
    private void HandleKeyboardInput()
    {
        // 按C键创建地图
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[TestMapFunctions] 按键C - 开始创建地图");
            editorManager.StartMapBuilding();
        }
        
        // 按S键保存地图
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("[TestMapFunctions] 按键S - 保存地图");
            editorManager.SaveCurrentMap();
        }
        
        // 按L键加载地图
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("[TestMapFunctions] 按键L - 加载地图");
            LoadFirstAvailableMap();
        }
        
        // 按X键清除地图
        if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("[TestMapFunctions] 按键X - 清除地图");
            editorManager.ClearCurrentMap();
        }
        
        // 按E键进入编辑模式
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[TestMapFunctions] 按键E - 进入编辑模式");
            editorManager.EnterEditMode();
        }
        
        // 按Q键退出编辑模式
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("[TestMapFunctions] 按键Q - 退出编辑模式");
            editorManager.ExitEditMode();
        }
        
        // 按P键切换点云显示
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("[TestMapFunctions] 按键P - 切换点云显示");
            bool currentState = editorManager.showPointCloud;
            editorManager.SetPointCloudVisibility(!currentState);
        }
        
        // 按I键显示详细信息
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("[TestMapFunctions] 按键I - 显示详细信息");
            ShowDetailedInfo();
        }
    }

    /// <summary>
    /// 处理自动测试
    /// </summary>
    private void HandleAutoTesting()
    {
        if (Time.time - lastAutoTestTime < autoTestInterval) return;
        
        lastAutoTestTime = Time.time;
        
        switch (testStep)
        {
            case 0:
                Debug.Log("[TestMapFunctions] 自动测试步骤 0: 开始创建地图");
                editorManager.StartMapBuilding();
                testStep++;
                break;
                
            case 1:
                Debug.Log("[TestMapFunctions] 自动测试步骤 1: 等待地图构建完成");
                if (editorManager.IsMapBuilding)
                {
                    Debug.Log("[TestMapFunctions] 地图构建中...");
                }
                else
                {
                    testStep++;
                }
                break;
                
            case 2:
                Debug.Log("[TestMapFunctions] 自动测试步骤 2: 保存地图");
                editorManager.SaveCurrentMap();
                testStep++;
                break;
                
            case 3:
                Debug.Log("[TestMapFunctions] 自动测试步骤 3: 加载地图");
                LoadFirstAvailableMap();
                testStep++;
                break;
                
            case 4:
                Debug.Log("[TestMapFunctions] 自动测试步骤 4: 进入编辑模式");
                if (editorManager.IsMapLocalized)
                {
                    editorManager.EnterEditMode();
                    testStep++;
                }
                break;
                
            case 5:
                Debug.Log("[TestMapFunctions] 自动测试步骤 5: 测试完成");
                testStep = 0; // 重置测试步骤
                break;
        }
    }

    /// <summary>
    /// 加载第一个可用地图
    /// </summary>
    private void LoadFirstAvailableMap()
    {
        var availableMaps = editorManager.GetAvailableMaps();
        if (availableMaps.Count > 0)
        {
            Debug.Log($"[TestMapFunctions] 加载地图: {availableMaps[0].Map.Name}");
            editorManager.LoadMap(availableMaps[0]);
        }
        else
        {
            Debug.LogWarning("[TestMapFunctions] 没有可用的地图");
        }
    }

    /// <summary>
    /// 订阅编辑器事件
    /// </summary>
    private void SubscribeToEvents()
    {
        if (editorManager != null)
        {
            editorManager.OnMapLocalized += OnMapLocalized;
            editorManager.OnMapBuildingStarted += OnMapBuildingStarted;
            editorManager.OnMapBuildingCompleted += OnMapBuildingCompleted;
            editorManager.OnObjectPlaced += OnObjectPlaced;
            editorManager.OnObjectRemoved += OnObjectRemoved;
        }
    }

    /// <summary>
    /// 地图本地化事件
    /// </summary>
    private void OnMapLocalized()
    {
        Debug.Log("[TestMapFunctions] ✅ 地图已本地化");
        ShowEditorStatus();
    }

    /// <summary>
    /// 地图构建开始事件
    /// </summary>
    private void OnMapBuildingStarted()
    {
        Debug.Log("[TestMapFunctions] ✅ 地图构建开始");
        ShowEditorStatus();
    }

    /// <summary>
    /// 地图构建完成事件
    /// </summary>
    private void OnMapBuildingCompleted()
    {
        Debug.Log("[TestMapFunctions] ✅ 地图构建完成");
        ShowEditorStatus();
    }

    /// <summary>
    /// 对象放置事件
    /// </summary>
    private void OnObjectPlaced(GameObject obj)
    {
        Debug.Log($"[TestMapFunctions] ✅ 对象已放置: {obj.name}");
    }

    /// <summary>
    /// 对象移除事件
    /// </summary>
    private void OnObjectRemoved(GameObject obj)
    {
        Debug.Log($"[TestMapFunctions] ✅ 对象已移除: {obj.name}");
    }

    /// <summary>
    /// 显示编辑器状态
    /// </summary>
    private void ShowEditorStatus()
    {
        if (editorManager == null) return;
        
        string status = editorManager.GetEditorStatus();
        Debug.Log($"[TestMapFunctions] 编辑器状态:\n{status}");
    }

    /// <summary>
    /// 显示详细信息
    /// </summary>
    private void ShowDetailedInfo()
    {
        if (editorManager == null) return;
        
        Debug.Log("=== 详细信息 ===");
        Debug.Log($"地图构建状态: {editorManager.IsMapBuilding}");
        Debug.Log($"地图本地化状态: {editorManager.IsMapLocalized}");
        Debug.Log($"编辑模式状态: {editorManager.IsEditMode}");
        Debug.Log($"对象数量: {editorManager.GetAllPlacedObjects().Count}");
        
        var availableMaps = editorManager.GetAvailableMaps();
        Debug.Log($"可用地图数量: {availableMaps.Count}");
        foreach (var map in availableMaps)
        {
            Debug.Log($"  - {map.Map.Name} (ID: {map.Map.ID})");
        }
        
        var placedObjects = editorManager.GetAllPlacedObjects();
        Debug.Log($"已放置对象:");
        foreach (var obj in placedObjects)
        {
            if (obj != null)
            {
                Debug.Log($"  - {obj.name} at {obj.transform.position}");
            }
        }
        Debug.Log("=== 详细信息结束 ===");
    }

    /// <summary>
    /// 显示调试信息
    /// </summary>
    private void ShowDebugInfo()
    {
        if (editorManager == null) return;
        
        // 这里可以添加实时调试信息显示
        // 比如在屏幕上显示当前状态
    }

    /// <summary>
    /// 测试地图碰撞检测
    /// </summary>
    public void TestHitDetection(Vector2 screenPosition)
    {
        if (editorManager == null) return;
        
        var hitResult = editorManager.GetMapHitPoint(screenPosition);
        if (hitResult.OnSome)
        {
            Debug.Log($"[TestMapFunctions] ✅ 碰撞检测成功: {hitResult.Value}");
        }
        else
        {
            Debug.LogWarning("[TestMapFunctions] ❌ 碰撞检测失败");
        }
    }

    /// <summary>
    /// 测试对象放置
    /// </summary>
    public void TestObjectPlacement(GameObject prefab, Vector2 screenPosition)
    {
        if (editorManager == null || prefab == null) return;
        
        GameObject newObject = Instantiate(prefab);
        bool success = editorManager.PlaceGameObjectOnMap(newObject, screenPosition);
        
        if (success)
        {
            Debug.Log($"[TestMapFunctions] ✅ 对象放置成功: {newObject.name}");
        }
        else
        {
            Debug.LogWarning("[TestMapFunctions] ❌ 对象放置失败");
            Destroy(newObject);
        }
    }

    void OnDestroy()
    {
        // 取消事件订阅
        if (editorManager != null)
        {
            editorManager.OnMapLocalized -= OnMapLocalized;
            editorManager.OnMapBuildingStarted -= OnMapBuildingStarted;
            editorManager.OnMapBuildingCompleted -= OnMapBuildingCompleted;
            editorManager.OnObjectPlaced -= OnObjectPlaced;
            editorManager.OnObjectRemoved -= OnObjectRemoved;
        }
    }

    /// <summary>
    /// 在Inspector中显示测试说明
    /// </summary>
    void OnValidate()
    {
        // 这里可以添加Inspector中的验证逻辑
    }
} 