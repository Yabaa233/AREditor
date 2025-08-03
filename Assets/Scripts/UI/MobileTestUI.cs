using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Manager;
using System.Collections.Generic;

/// <summary>
/// 手机可操作的测试UI界面
/// 替代键盘输入，提供触摸友好的按钮操作
/// </summary>
public class MobileTestUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject mapControlPanel;
    public GameObject editModePanel;
    public GameObject infoPanel;
    
    [Header("Main Control Buttons")]
    public Button btnShowMapControl;
    public Button btnShowEditMode;
    public Button btnShowInfo;
    public Button btnAutoTest;
    
    [Header("Map Control Buttons")]
    public Button btnCreateMap;
    public Button btnSaveMap;
    public Button btnLoadMap;
    public Button btnClearMap;
    public Button btnTogglePointCloud;
    
    [Header("Edit Mode Buttons")]
    public Button btnEnterEditMode;
    public Button btnExitEditMode;
    public Button btnPlaceObject;
    public Button btnRemoveObject;
    
    [Header("Info Display")]
    public Text statusText;
    public Text mapInfoText;
    public Text objectInfoText;
    public ScrollRect infoScrollRect;
    
    [Header("Object Placement")]
    public GameObject objectPalettePanel;
    public Transform objectPaletteContent;
    public GameObject objectItemPrefab;
    public Button btnToggleObjectPalette;
    
    [Header("Settings")]
    public bool showStatusOnScreen = true;
    public bool autoUpdateInfo = true;
    public float infoUpdateInterval = 2f;
    
    private EasyARSpatialMapEditorManager editorManager;
    private TestMapFunctions testFunctions;
    private List<GameObject> objectItemInstances = new List<GameObject>();
    private bool isObjectPaletteOpen = false;
    private float lastInfoUpdateTime;
    
    // 预设对象列表
    private string[] objectPrefabNames = {
        "Cube", "Sphere", "Cylinder", "Capsule"
    };

    void Start()
    {
        Debug.Log("[MobileTestUI] 初始化手机测试UI");
        
        // 获取管理器
        editorManager = EasyARSpatialMapEditorManager.Instance;
        testFunctions = FindObjectOfType<TestMapFunctions>();
        
        if (editorManager == null)
        {
            Debug.LogError("[MobileTestUI] ❌ EasyARSpatialMapEditorManager 未找到");
            return;
        }
        
        Debug.Log("[MobileTestUI] ✅ EasyARSpatialMapEditorManager 找到");
        
        // 初始化UI
        InitializeUI();
        SubscribeToEvents();
        UpdateAllInfo();
        
        // 默认显示主面板
        ShowMainPanel();
    }

    void Update()
    {
        if (editorManager == null) return;
        
        // 自动更新信息
        if (autoUpdateInfo && Time.time - lastInfoUpdateTime > infoUpdateInterval)
        {
            UpdateAllInfo();
            lastInfoUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 设置按钮事件
        SetupButtonEvents();
        
        // 初始化对象面板
        InitializeObjectPalette();
        
        // 设置默认状态
        SetAllPanelsInactive();
    }

    /// <summary>
    /// 设置按钮事件
    /// </summary>
    private void SetupButtonEvents()
    {
        // 主控制按钮
        if (btnShowMapControl != null)
            btnShowMapControl.onClick.AddListener(ShowMapControlPanel);
        if (btnShowEditMode != null)
            btnShowEditMode.onClick.AddListener(ShowEditModePanel);
        if (btnShowInfo != null)
            btnShowInfo.onClick.AddListener(ShowInfoPanel);
        if (btnAutoTest != null)
            btnAutoTest.onClick.AddListener(ToggleAutoTest);
        
        // 地图控制按钮
        if (btnCreateMap != null)
            btnCreateMap.onClick.AddListener(CreateMap);
        if (btnSaveMap != null)
            btnSaveMap.onClick.AddListener(SaveMap);
        if (btnLoadMap != null)
            btnLoadMap.onClick.AddListener(LoadMap);
        if (btnClearMap != null)
            btnClearMap.onClick.AddListener(ClearMap);
        if (btnTogglePointCloud != null)
            btnTogglePointCloud.onClick.AddListener(TogglePointCloud);
        
        // 编辑模式按钮
        if (btnEnterEditMode != null)
            btnEnterEditMode.onClick.AddListener(EnterEditMode);
        if (btnExitEditMode != null)
            btnExitEditMode.onClick.AddListener(ExitEditMode);
        if (btnPlaceObject != null)
            btnPlaceObject.onClick.AddListener(PlaceObject);
        if (btnRemoveObject != null)
            btnRemoveObject.onClick.AddListener(RemoveObject);
        
        // 对象面板按钮
        if (btnToggleObjectPalette != null)
            btnToggleObjectPalette.onClick.AddListener(ToggleObjectPalette);
    }

    /// <summary>
    /// 初始化对象面板
    /// </summary>
    private void InitializeObjectPalette()
    {
        if (objectPaletteContent == null || objectItemPrefab == null) return;
        
        // 清除现有对象
        foreach (var obj in objectItemInstances)
        {
            if (obj != null) Destroy(obj);
        }
        objectItemInstances.Clear();
        
        // 创建对象项
        foreach (var objectName in objectPrefabNames)
        {
            GameObject item = Instantiate(objectItemPrefab, objectPaletteContent);
            Button itemButton = item.GetComponent<Button>();
            Text itemText = item.GetComponentInChildren<Text>();
            
            if (itemText != null)
                itemText.text = objectName;
            
            if (itemButton != null)
            {
                string objName = objectName; // 捕获变量
                itemButton.onClick.AddListener(() => SelectObject(objName));
            }
            
            objectItemInstances.Add(item);
        }
    }

    /// <summary>
    /// 订阅事件
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

    #region Panel Management

    /// <summary>
    /// 显示主面板
    /// </summary>
    public void ShowMainPanel()
    {
        SetAllPanelsInactive();
        if (mainPanel != null) mainPanel.SetActive(true);
        UpdateAllInfo();
    }

    /// <summary>
    /// 显示地图控制面板
    /// </summary>
    public void ShowMapControlPanel()
    {
        SetAllPanelsInactive();
        if (mapControlPanel != null) mapControlPanel.SetActive(true);
        UpdateMapInfo();
    }

    /// <summary>
    /// 显示编辑模式面板
    /// </summary>
    public void ShowEditModePanel()
    {
        SetAllPanelsInactive();
        if (editModePanel != null) editModePanel.SetActive(true);
        UpdateObjectInfo();
    }

    /// <summary>
    /// 显示信息面板
    /// </summary>
    public void ShowInfoPanel()
    {
        SetAllPanelsInactive();
        if (infoPanel != null) infoPanel.SetActive(true);
        UpdateAllInfo();
    }

    /// <summary>
    /// 设置所有面板为非活动状态
    /// </summary>
    private void SetAllPanelsInactive()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (mapControlPanel != null) mapControlPanel.SetActive(false);
        if (editModePanel != null) editModePanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    #endregion

    #region Map Control Functions

    /// <summary>
    /// 创建地图
    /// </summary>
    public void CreateMap()
    {
        Debug.Log("[MobileTestUI] 🗺️ 开始创建地图");
        if (editorManager != null)
        {
            editorManager.StartMapBuilding();
            UpdateStatusText("正在创建地图...");
        }
    }

    /// <summary>
    /// 保存地图
    /// </summary>
    public void SaveMap()
    {
        Debug.Log("[MobileTestUI] 💾 保存地图");
        if (editorManager != null)
        {
            editorManager.SaveCurrentMap();
            editorManager.SaveObjectsInfo(); // 同时保存对象信息
            UpdateStatusText("地图和对象信息已保存");
        }
    }

    /// <summary>
    /// 加载地图
    /// </summary>
    public void LoadMap()
    {
        Debug.Log("[MobileTestUI] 📂 加载地图");
        if (editorManager != null)
        {
            var availableMaps = editorManager.GetAvailableMaps();
            if (availableMaps.Count > 0)
            {
                editorManager.LoadMap(availableMaps[0]);
                UpdateStatusText($"加载地图: {availableMaps[0].Map.Name}");
            }
            else
            {
                UpdateStatusText("没有可用的地图");
            }
        }
    }

    /// <summary>
    /// 清除地图
    /// </summary>
    public void ClearMap()
    {
        Debug.Log("[MobileTestUI] 🗑️ 清除地图");
        if (editorManager != null)
        {
            editorManager.ClearCurrentMap();
            UpdateStatusText("地图已清除");
        }
    }

    /// <summary>
    /// 切换点云显示
    /// </summary>
    public void TogglePointCloud()
    {
        Debug.Log("[MobileTestUI] ☁️ 切换点云显示");
        if (editorManager != null)
        {
            bool currentState = editorManager.showPointCloud;
            editorManager.SetPointCloudVisibility(!currentState);
            UpdateStatusText($"点云显示: {(!currentState ? "开启" : "关闭")}");
        }
    }

    #endregion

    #region Edit Mode Functions

    /// <summary>
    /// 进入编辑模式
    /// </summary>
    public void EnterEditMode()
    {
        Debug.Log("[MobileTestUI] ✏️ 进入编辑模式");
        if (editorManager != null)
        {
            editorManager.EnterEditMode();
            UpdateStatusText("已进入编辑模式");
        }
    }

    /// <summary>
    /// 退出编辑模式
    /// </summary>
    public void ExitEditMode()
    {
        Debug.Log("[MobileTestUI] 🚪 退出编辑模式");
        if (editorManager != null)
        {
            editorManager.ExitEditMode();
            UpdateStatusText("已退出编辑模式");
        }
    }

    /// <summary>
    /// 放置对象
    /// </summary>
    public void PlaceObject()
    {
        Debug.Log("[MobileTestUI] 📦 准备放置对象");
        ToggleObjectPalette();
        UpdateStatusText("请选择要放置的对象");
    }

    /// <summary>
    /// 移除对象
    /// </summary>
    public void RemoveObject()
    {
        Debug.Log("[MobileTestUI] 🗑️ 移除对象");
        if (editorManager != null)
        {
            var objects = editorManager.GetAllPlacedObjects();
            if (objects.Count > 0)
            {
                editorManager.UnregisterObject(objects[objects.Count - 1]);
                UpdateStatusText("已移除最后一个对象");
            }
            else
            {
                UpdateStatusText("没有可移除的对象");
            }
        }
    }

    #endregion

    #region Object Palette Functions

    /// <summary>
    /// 切换对象面板
    /// </summary>
    public void ToggleObjectPalette()
    {
        if (objectPalettePanel != null)
        {
            isObjectPaletteOpen = !isObjectPaletteOpen;
            objectPalettePanel.SetActive(isObjectPaletteOpen);
            
            if (isObjectPaletteOpen)
            {
                UpdateStatusText("对象面板已打开");
            }
            else
            {
                UpdateStatusText("对象面板已关闭");
            }
        }
    }

    /// <summary>
    /// 选择对象
    /// </summary>
    public void SelectObject(string objectName)
    {
        Debug.Log($"[MobileTestUI] 📦 选择对象: {objectName}");
        UpdateStatusText($"已选择对象: {objectName}");
        
        // 这里可以添加对象放置逻辑
        // 比如在屏幕中心放置对象
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        PlaceObjectAtScreenPosition(objectName, screenCenter);
        
        // 关闭对象面板
        ToggleObjectPalette();
    }

    /// <summary>
    /// 在屏幕位置放置对象
    /// </summary>
    public void PlaceObjectAtScreenPosition(string objectName, Vector2 screenPosition)
    {
        if (editorManager == null) return;
        
        // 创建简单的几何体对象
        GameObject newObject = CreateSimpleObject(objectName);
        if (newObject != null)
        {
            bool success = editorManager.PlaceGameObjectOnMap(newObject, screenPosition);
            if (success)
            {
                UpdateStatusText($"对象 {objectName} 放置成功");
            }
            else
            {
                UpdateStatusText($"对象 {objectName} 放置失败");
                Destroy(newObject);
            }
        }
    }

    /// <summary>
    /// 创建简单对象
    /// </summary>
    private GameObject CreateSimpleObject(string objectName)
    {
        GameObject obj = null;
        
        switch (objectName.ToLower())
        {
            case "cube":
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                break;
            case "sphere":
                obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                break;
            case "cylinder":
                obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                break;
            case "capsule":
                obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                break;
            default:
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                break;
        }
        
        if (obj != null)
        {
            obj.name = objectName;
            // 添加EasyARPlacedObject组件
            obj.AddComponent<EasyARPlacedObject>();
        }
        
        return obj;
    }

    #endregion

    #region Auto Test Functions

    /// <summary>
    /// 切换自动测试
    /// </summary>
    public void ToggleAutoTest()
    {
        if (testFunctions != null)
        {
            testFunctions.enableAutoTesting = !testFunctions.enableAutoTesting;
            UpdateStatusText($"自动测试: {(testFunctions.enableAutoTesting ? "开启" : "关闭")}");
        }
    }

    #endregion

    #region Info Update Functions

    /// <summary>
    /// 更新所有信息
    /// </summary>
    public void UpdateAllInfo()
    {
        UpdateStatusText();
        UpdateMapInfo();
        UpdateObjectInfo();
    }

    /// <summary>
    /// 更新状态文本
    /// </summary>
    public void UpdateStatusText(string customMessage = null)
    {
        if (statusText == null) return;
        
        if (!string.IsNullOrEmpty(customMessage))
        {
            statusText.text = customMessage;
            return;
        }
        
        if (editorManager == null)
        {
            statusText.text = "编辑器管理器未找到";
            return;
        }
        
        string status = $"地图构建: {(editorManager.IsMapBuilding ? "进行中" : "未开始")}\n";
        status += $"地图本地化: {(editorManager.IsMapLocalized ? "已定位" : "未定位")}\n";
        status += $"编辑模式: {(editorManager.IsEditMode ? "开启" : "关闭")}";
        
        statusText.text = status;
    }

    /// <summary>
    /// 更新地图信息
    /// </summary>
    public void UpdateMapInfo()
    {
        if (mapInfoText == null || editorManager == null) return;
        
        var availableMaps = editorManager.GetAvailableMaps();
        string info = $"可用地图数量: {availableMaps.Count}\n\n";
        
        for (int i = 0; i < availableMaps.Count && i < 5; i++)
        {
            info += $"{i + 1}. {availableMaps[i].Map.Name}\n";
            info += $"   ID: {availableMaps[i].Map.ID}\n\n";
        }
        
        mapInfoText.text = info;
    }

    /// <summary>
    /// 更新对象信息
    /// </summary>
    public void UpdateObjectInfo()
    {
        if (objectInfoText == null || editorManager == null) return;
        
        var objects = editorManager.GetAllPlacedObjects();
        string info = $"已放置对象数量: {objects.Count}\n\n";
        
        for (int i = 0; i < objects.Count && i < 10; i++)
        {
            if (objects[i] != null)
            {
                info += $"{i + 1}. {objects[i].name}\n";
                info += $"   位置: {objects[i].transform.position}\n\n";
            }
        }
        
        objectInfoText.text = info;
    }

    #endregion

    #region Event Handlers

    private void OnMapLocalized()
    {
        Debug.Log("[MobileTestUI] ✅ 地图已本地化");
        UpdateStatusText("地图已本地化");
    }

    private void OnMapBuildingStarted()
    {
        Debug.Log("[MobileTestUI] ✅ 地图构建开始");
        UpdateStatusText("地图构建开始");
    }

    private void OnMapBuildingCompleted()
    {
        Debug.Log("[MobileTestUI] ✅ 地图构建完成");
        UpdateStatusText("地图构建完成");
    }

    private void OnObjectPlaced(GameObject obj)
    {
        Debug.Log($"[MobileTestUI] ✅ 对象已放置: {obj.name}");
        UpdateObjectInfo();
    }

    private void OnObjectRemoved(GameObject obj)
    {
        Debug.Log($"[MobileTestUI] ✅ 对象已移除: {obj.name}");
        UpdateObjectInfo();
    }

    #endregion

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
} 