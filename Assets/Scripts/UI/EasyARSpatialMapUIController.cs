using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Manager;
using System.Collections.Generic;

/// <summary>
/// EasyAR空间地图UI控制器
/// 管理空间地图相关的UI交互
/// </summary>
public class EasyARSpatialMapUIController : MonoBehaviour
{
    [Header("Map Control UI")]
    public Button btnCreateMap;
    public Button btnLoadMap;
    public Button btnSaveMap;
    public Button btnClearMap;
    public Button btnTogglePointCloud;
    public Button btnEnterEditMode;
    public Button btnExitEditMode;
    
    [Header("Object Placement UI")]
    public GameObject objectPalettePanel;
    public Transform objectPaletteContent;
    public GameObject objectItemPrefab;
    public Button btnToggleObjectPalette;
    
    [Header("Status UI")]
    public Text statusText;
    public Text mapInfoText;
    public Text editorStatusText;
    
    [Header("Settings")]
    public bool showPointCloud = true;
    
    private EasyARSpatialMapEditorManager spatialMapManager;
    private List<GameObject> objectItemInstances = new List<GameObject>();
    private bool isObjectPaletteOpen = false;

    private void Start()
    {
        spatialMapManager = EasyARSpatialMapEditorManager.Instance;
        if (spatialMapManager == null)
        {
            Debug.LogError("EasyARSpatialMapEditorManager not found!");
            return;
        }

        InitializeUI();
        SubscribeToEvents();
        UpdateUIState();
    }

    private void InitializeUI()
    {
        // 地图控制按钮
        if (btnCreateMap) btnCreateMap.onClick.AddListener(OnCreateMapClicked);
        if (btnLoadMap) btnLoadMap.onClick.AddListener(OnLoadMapClicked);
        if (btnSaveMap) btnSaveMap.onClick.AddListener(OnSaveMapClicked);
        if (btnClearMap) btnClearMap.onClick.AddListener(OnClearMapClicked);
        if (btnTogglePointCloud) btnTogglePointCloud.onClick.AddListener(OnTogglePointCloudClicked);
        if (btnEnterEditMode) btnEnterEditMode.onClick.AddListener(OnEnterEditModeClicked);
        if (btnExitEditMode) btnExitEditMode.onClick.AddListener(OnExitEditModeClicked);
        
        // 对象放置UI
        if (btnToggleObjectPalette) btnToggleObjectPalette.onClick.AddListener(OnToggleObjectPaletteClicked);
        
        // 初始化面板状态
        if (objectPalettePanel) objectPalettePanel.SetActive(false);
    }

    private void SubscribeToEvents()
    {
        if (spatialMapManager != null)
        {
            spatialMapManager.OnMapLocalized += OnMapLocalized;
            spatialMapManager.OnMapBuildingStarted += OnMapBuildingStarted;
            spatialMapManager.OnObjectPlaced += OnObjectPlaced;
            spatialMapManager.OnObjectRemoved += OnObjectRemoved;
        }
    }

    private void Update()
    {
        UpdateStatusText();
        UpdateMapInfoText();
        UpdateEditorStatusText();
    }

    /// <summary>
    /// 创建地图按钮点击事件
    /// </summary>
    private void OnCreateMapClicked()
    {
        spatialMapManager.StartMapBuilding();
        UpdateUIState();
    }

    /// <summary>
    /// 加载地图按钮点击事件
    /// </summary>
    private void OnLoadMapClicked()
    {
        // 简单示例，加载第一个可用地图
        var availableMaps = spatialMapManager.GetAvailableMaps();
        if (availableMaps.Count > 0)
        {
            spatialMapManager.LoadMap(availableMaps[0]);
        }
        else
        {
            Debug.Log("没有可用的地图");
        }
    }

    /// <summary>
    /// 保存地图按钮点击事件
    /// </summary>
    private void OnSaveMapClicked()
    {
        spatialMapManager.SaveCurrentMap();
    }

    /// <summary>
    /// 清除地图按钮点击事件
    /// </summary>
    private void OnClearMapClicked()
    {
        spatialMapManager.ClearCurrentMap();
        UpdateUIState();
    }

    /// <summary>
    /// 切换点云显示按钮点击事件
    /// </summary>
    private void OnTogglePointCloudClicked()
    {
        showPointCloud = !showPointCloud;
        spatialMapManager.SetPointCloudVisibility(showPointCloud);
        
        if (btnTogglePointCloud)
        {
            btnTogglePointCloud.GetComponentInChildren<Text>().text = showPointCloud ? "隐藏点云" : "显示点云";
        }
    }

    /// <summary>
    /// 进入编辑模式按钮点击事件
    /// </summary>
    private void OnEnterEditModeClicked()
    {
        spatialMapManager.EnterEditMode();
        UpdateUIState();
    }

    /// <summary>
    /// 退出编辑模式按钮点击事件
    /// </summary>
    private void OnExitEditModeClicked()
    {
        spatialMapManager.ExitEditMode();
        UpdateUIState();
    }

    /// <summary>
    /// 切换对象面板
    /// </summary>
    private void OnToggleObjectPaletteClicked()
    {
        isObjectPaletteOpen = !isObjectPaletteOpen;
        if (objectPalettePanel) objectPalettePanel.SetActive(isObjectPaletteOpen);
        
        if (isObjectPaletteOpen)
        {
            PopulateObjectPalette();
        }
        
        if (btnToggleObjectPalette)
        {
            btnToggleObjectPalette.GetComponentInChildren<Text>().text = isObjectPaletteOpen ? "关闭对象面板" : "打开对象面板";
        }
    }

    /// <summary>
    /// 填充对象面板
    /// </summary>
    private void PopulateObjectPalette()
    {
        // 清除现有项目
        ClearObjectItems();
        
        if (objectPaletteContent == null || objectItemPrefab == null) return;
        
        var templateDB = EditorManager.Instance.templateDB;
        if (templateDB == null) return;
        
        foreach (var template in templateDB.templates)
        {
            if (template.ARPrefab == null) continue; // 跳过没有AR预制体的模板
            
            var objectItem = Instantiate(objectItemPrefab, objectPaletteContent);
            objectItemInstances.Add(objectItem);
            
            // 设置对象信息
            var nameText = objectItem.GetComponentInChildren<Text>();
            if (nameText)
            {
                nameText.text = template.templateName;
            }
            
            // 设置图标
            var image = objectItem.GetComponentInChildren<Image>();
            if (image && template.icon)
            {
                image.sprite = template.icon;
            }
            
            // 添加点击事件
            var button = objectItem.GetComponent<Button>();
            if (button)
            {
                button.onClick.AddListener(() => OnObjectItemClicked(template.templateID));
            }
        }
    }

    /// <summary>
    /// 对象项目点击事件
    /// </summary>
    private void OnObjectItemClicked(string templateID)
    {
        // 通过模板ID找到对应的对象模板并实例化其AR预制体
        Debug.Log($"选择对象模板: {templateID}");

        var templateDB = EditorManager.Instance.templateDB;
        if (templateDB == null)
        {
            Debug.LogWarning("Template database not found");
            return;
        }

        var template = templateDB.GetTemplateByID(templateID);
        if (template == null || template.ARPrefab == null)
        {
            Debug.LogWarning($"Template or ARPrefab not found for ID: {templateID}");
            return;
        }

        // 实例化AR预制体
        GameObject newObject = Instantiate(template.ARPrefab);

        // 为实例添加 EasyARPlacedObject 组件（若不存在）
        if (newObject.GetComponent<EasyARPlacedObject>() == null)
        {
            newObject.AddComponent<EasyARPlacedObject>();
        }

        // 在屏幕中心放置对象
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        bool success = EasyARSpatialMapEditorManager.Instance.PlaceGameObjectOnMap(newObject, screenCenter);

        if (success)
        {
            // 放置成功后关闭对象面板
            if (isObjectPaletteOpen)
            {
                OnToggleObjectPaletteClicked();
            }
        }
        else
        {
            // 放置失败销毁对象并提示
            Destroy(newObject);
            Debug.LogWarning("对象放置失败，请尝试其他位置");
        }
    }

    /// <summary>
    /// 清除对象项目
    /// </summary>
    private void ClearObjectItems()
    {
        foreach (var item in objectItemInstances)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        objectItemInstances.Clear();
    }

    /// <summary>
    /// 地图本地化事件
    /// </summary>
    private void OnMapLocalized()
    {
        UpdateStatusText();
        UpdateUIState();
    }

    /// <summary>
    /// 地图构建开始事件
    /// </summary>
    private void OnMapBuildingStarted()
    {
        UpdateStatusText();
        UpdateUIState();
    }

    /// <summary>
    /// 对象放置事件
    /// </summary>
    private void OnObjectPlaced(GameObject obj)
    {
        UpdateEditorStatusText();
    }

    /// <summary>
    /// 对象移除事件
    /// </summary>
    private void OnObjectRemoved(GameObject obj)
    {
        UpdateEditorStatusText();
    }

    /// <summary>
    /// 更新状态文本
    /// </summary>
    private void UpdateStatusText()
    {
        if (statusText == null) return;
        
        string status = "";
        if (spatialMapManager.IsMapBuilding)
        {
            status = "正在构建地图...";
        }
        else if (spatialMapManager.IsMapLocalized)
        {
            status = "地图已本地化，可以编辑";
        }
        else
        {
            status = "等待地图本地化...";
        }
        
        statusText.text = status;
    }

    /// <summary>
    /// 更新地图信息文本
    /// </summary>
    private void UpdateMapInfoText()
    {
        if (mapInfoText == null) return;
        
        var currentSession = spatialMapManager.CurrentMapSession;
        if (currentSession != null && currentSession.MapWorker != null)
        {
            var localizedMap = currentSession.MapWorker.LocalizedMap;
            if (localizedMap != null)
            {
                mapInfoText.text = $"地图: {localizedMap.MapInfo.Name}\n点云数量: {localizedMap.PointCloud.Count}";
            }
            else
            {
                mapInfoText.text = "地图未本地化";
            }
        }
        else
        {
            mapInfoText.text = "无活动地图";
        }
    }

    /// <summary>
    /// 更新编辑器状态文本
    /// </summary>
    private void UpdateEditorStatusText()
    {
        if (editorStatusText == null) return;
        
        editorStatusText.text = spatialMapManager.GetEditorStatus();
    }

    /// <summary>
    /// 更新UI状态
    /// </summary>
    private void UpdateUIState()
    {
        if (btnCreateMap) btnCreateMap.interactable = !spatialMapManager.IsMapBuilding;
        if (btnLoadMap) btnLoadMap.interactable = !spatialMapManager.IsMapBuilding && !spatialMapManager.IsMapLocalized;
        if (btnSaveMap) btnSaveMap.interactable = spatialMapManager.IsMapBuilding;
        if (btnClearMap) btnClearMap.interactable = spatialMapManager.IsMapBuilding || spatialMapManager.IsMapLocalized;
        if (btnEnterEditMode) btnEnterEditMode.interactable = spatialMapManager.IsMapLocalized && !spatialMapManager.IsEditMode;
        if (btnExitEditMode) btnExitEditMode.interactable = spatialMapManager.IsEditMode;
        if (btnToggleObjectPalette) btnToggleObjectPalette.interactable = spatialMapManager.IsMapLocalized;
    }

    private void OnDestroy()
    {
        ClearObjectItems();
        
        // 取消事件订阅
        if (spatialMapManager != null)
        {
            spatialMapManager.OnMapLocalized -= OnMapLocalized;
            spatialMapManager.OnMapBuildingStarted -= OnMapBuildingStarted;
            spatialMapManager.OnObjectPlaced -= OnObjectPlaced;
            spatialMapManager.OnObjectRemoved -= OnObjectRemoved;
        }
    }
} 