using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Manager;
using System.Collections.Generic;
using SpatialMap_SparseSpatialMap;
using LocalMapMeta = SpatialMap_SparseSpatialMap.MapMeta;  // 使用本地项目的MapMeta类型

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
    public Button btnSaveMapWithObjects; // 新增：保存地图并包含当前放置的物体
    public Button btnClearMap;
    public Button btnTogglePointCloud;
    public Button btnEnterEditMode;
    public Button btnExitEditMode;

    [Header("Map Selection UI")]
    public GameObject mapSelectionPanel;     // 地图选择面板
    public Transform mapListContent;         // 地图列表容器（ScrollView的Content）
    public GameObject mapItemPrefab;         // 地图项目预制体模板

    [Header("Object Placement UI")]
    public GameObject objectPalettePanel;
    public Transform objectPaletteContent;
    public GameObject objectItemPrefab; // 新增：用于对象面板的UI预制体（优先使用）
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

        // 验证并打印 mapItemPrefab 信息，帮助调试列表项不显示的问题
        ValidateMapItemPrefab();

        // 打印持久化路径和 SparseSpatialMap 文件夹（方便在 Android 上定位）
        DebugPrintMapFolder();

        // 初始状态调试
        Debug.Log("[UI] UI初始化完成，调试物体面板状态:");
        DebugObjectPaletteState();
    }

    private void InitializeUI()
    {
        // 地图控制按钮
        if (btnCreateMap) btnCreateMap.onClick.AddListener(OnCreateMapClicked);
        if (btnLoadMap) btnLoadMap.onClick.AddListener(OnLoadMapClicked);
        if (btnSaveMap)
        {
            btnSaveMap.onClick.RemoveAllListeners();
            btnSaveMap.onClick.AddListener(OnSaveMapClicked);
        }

        // 绑定保存（包含物体）按钮
        if (btnSaveMapWithObjects)
        {
            btnSaveMapWithObjects.onClick.RemoveAllListeners();
            btnSaveMapWithObjects.onClick.AddListener(OnSaveMapWithObjectsClicked);
        }
        if (btnClearMap) btnClearMap.onClick.AddListener(OnClearMapClicked);
        if (btnTogglePointCloud) btnTogglePointCloud.onClick.AddListener(OnTogglePointCloudClicked);
        if (btnEnterEditMode) btnEnterEditMode.onClick.AddListener(OnEnterEditModeClicked);
        if (btnExitEditMode) btnExitEditMode.onClick.AddListener(OnExitEditModeClicked);

        // 地图选择UI - 初始化时隐藏地图选择面板
        if (mapSelectionPanel) mapSelectionPanel.SetActive(false);
        if (mapListContent) mapListContent.gameObject.SetActive(false);

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
        // 降低更新频率，避免过度调用
        if (Time.frameCount % 30 == 0) // 每30帧更新一次，约0.5秒
        {
            UpdateStatusText();
            UpdateMapInfoText();
            UpdateEditorStatusText();
        }
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
    /// 加载地图按钮点击事件 - 显示地图选择界面
    /// </summary>
    private void OnLoadMapClicked()
    {
        Debug.Log("[UI] 点击加载地图按钮");
        ShowMapSelectionPanel();
    }

    /// <summary>
    /// 保存地图按钮点击事件
    /// </summary>
    private void OnSaveMapClicked()
    {
        Debug.Log("[UI] 点击保存地图按钮");

        // 检查是否可以保存
        if (!spatialMapManager.IsMapBuilding)
        {
            ShowStatusMessage("没有正在构建的地图可以保存", 3f);
            return;
        }

        ShowStatusMessage("正在保存地图，请稍候...", 3f);

        // 执行保存地图
        spatialMapManager.SaveCurrentMap();
        Debug.Log("[UI] 保存命令已发送，开始监控保存状态");

        // 立即打印路径，方便确认文件是否写入（在设备上查看）
        DebugPrintMapFolder();

        // 保存后自动加载新地图（无需重启应用）
        StartCoroutine(CheckMapStatusAfterSave());
    }

    /// <summary>
    /// 保存后检查地图状态并自动加载
    /// </summary>
    private System.Collections.IEnumerator CheckMapStatusAfterSave()
    {
        yield return new UnityEngine.WaitForSeconds(3f); // 等待3秒让保存操作完全完成

        Debug.Log("[UI] 检查保存后的地图状态");

        // 获取当前可用地图数量
        int initialMapCount = spatialMapManager.GetAvailableMaps().Count;
        Debug.Log($"[UI] 保存前地图数量: {initialMapCount}");

        // 等待保存完成并检查新地图
        bool mapSaved = false;
        int attempts = 0;
        const int maxAttempts = 10; // 最多尝试10次，每次等待1秒

        while (!mapSaved && attempts < maxAttempts)
        {
            yield return new UnityEngine.WaitForSeconds(1f);
            attempts++;

            // 重新获取地图列表（这会触发内部的地图列表刷新）
            var currentMaps = spatialMapManager.GetAvailableMaps();
            Debug.Log($"[UI] 第{attempts}次检查，当前地图数量: {currentMaps.Count}");

            if (currentMaps.Count > initialMapCount)
            {
                mapSaved = true;
                Debug.Log("[UI] 检测到新保存的地图");

                // 自动加载最新保存的地图
                var latestMap = currentMaps[currentMaps.Count - 1];
                Debug.Log($"[UI] 自动加载最新保存的地图: {latestMap.Map.Name}");

                ShowStatusMessage("地图保存成功！正在加载...", 3f);

                // 先清除当前地图状态
                spatialMapManager.ClearCurrentMap();
                yield return new UnityEngine.WaitForSeconds(0.5f);

                // 加载地图
                spatialMapManager.LoadMap(latestMap);

                // 等待地图本地化
                yield return StartCoroutine(WaitForMapLocalization());
            }
        }

        // 如果循环结束时还没有保存成功，显示失败消息
        if (!mapSaved)
        {
            Debug.LogWarning("[UI] 地图保存超时或失败");
            ShowStatusMessage("地图保存可能失败，请检查", 4f);
        }

        UpdateUIState();
    }

    /// <summary>
    /// 等待地图本地化完成
    /// </summary>
    private System.Collections.IEnumerator WaitForMapLocalization()
    {
        float timeout = 15f; // 15秒超时
        float elapsed = 0f;

        Debug.Log("[UI] 开始等待地图本地化...");

        while (!spatialMapManager.IsMapLocalized && elapsed < timeout)
        {
            yield return new UnityEngine.WaitForSeconds(0.5f);
            elapsed += 0.5f;

            if (elapsed % 2f < 0.5f) // 每2秒输出一次日志
            {
                Debug.Log($"[UI] 等待地图本地化... ({elapsed:F1}s/{timeout}s)");
            }
        }

        if (spatialMapManager.IsMapLocalized)
        {
            Debug.Log("[UI] 地图本地化成功！");
            ShowStatusMessage("地图已本地化，可以开始编辑", 2f);

            // 可选：自动进入编辑模式
            yield return new UnityEngine.WaitForSeconds(1f);
            if (!spatialMapManager.IsEditMode)
            {
                Debug.Log("[UI] 自动进入编辑模式");
                spatialMapManager.EnterEditMode();
                ShowStatusMessage("已自动进入编辑模式，可以放置对象了！", 3f);
            }
        }
        else
        {
            Debug.LogWarning("[UI] 地图本地化超时");
            ShowStatusMessage("地图本地化失败，请手动重新加载", 4f);
        }

        UpdateUIState();
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

        // 调试进入编辑模式后的UI状态
        Debug.Log("[UI] 进入编辑模式后调试物体面板状态:");
        DebugObjectPaletteState();

        // 进入编辑模式后自动显示物体面板
        if (!isObjectPaletteOpen && btnToggleObjectPalette && btnToggleObjectPalette.interactable)
        {
            Debug.Log("[UI] 进入编辑模式，自动打开对象面板");
            OnToggleObjectPaletteClicked();
        }
        else
        {
            Debug.LogWarning($"[UI] 无法自动打开对象面板 - 已打开:{isObjectPaletteOpen}, 按钮存在:{btnToggleObjectPalette != null}, 可交互:{btnToggleObjectPalette?.interactable}");
        }
    }

    /// <summary>
    /// 退出编辑模式按钮点击事件
    /// </summary>
    private void OnExitEditModeClicked()
    {
        spatialMapManager.ExitEditMode();

        // 退出编辑模式时关闭物体面板
        if (isObjectPaletteOpen)
        {
            Debug.Log("[UI] 退出编辑模式，自动关闭对象面板");
            isObjectPaletteOpen = false;
            if (objectPalettePanel) objectPalettePanel.SetActive(false);
            if (btnToggleObjectPalette)
            {
                btnToggleObjectPalette.GetComponentInChildren<Text>().text = "打开对象面板";
            }
        }

        UpdateUIState();
    }

    /// <summary>
    /// 切换对象面板
    /// </summary>
    private void OnToggleObjectPaletteClicked()
    {
        Debug.Log($"[UI] 对象面板切换被调用 - 当前状态: {isObjectPaletteOpen}");

        isObjectPaletteOpen = !isObjectPaletteOpen;

        Debug.Log($"[UI] 对象面板新状态: {isObjectPaletteOpen}");

        if (objectPalettePanel)
        {
            objectPalettePanel.SetActive(isObjectPaletteOpen);
            Debug.Log($"[UI] 对象面板GameObject设置激活状态: {isObjectPaletteOpen}");
        }
        else
        {
            Debug.LogError("[UI] objectPalettePanel 为 null！");
        }

        if (isObjectPaletteOpen)
        {
            Debug.Log("[UI] 填充对象面板内容");
            PopulateObjectPalette();
        }

        if (btnToggleObjectPalette)
        {
            string newText = isObjectPaletteOpen ? "关闭对象面板" : "打开对象面板";
            btnToggleObjectPalette.GetComponentInChildren<Text>().text = newText;
            Debug.Log($"[UI] 对象面板按钮文本更新为: {newText}");
        }
        else
        {
            Debug.LogError("[UI] btnToggleObjectPalette 为 null！");
        }
    }

    /// <summary>
    /// 填充对象面板 - 直接从模板数据库创建UI
    /// </summary>
    private void PopulateObjectPalette()
    {
        try
        {
            // 清除现有项目
            ClearObjectItems();

            if (objectPaletteContent == null)
            {
                Debug.LogWarning("[UI] objectPaletteContent is null");
                return;
            }

            var templateDB = EditorManager.Instance?.templateDB;
            if (templateDB == null)
            {
                Debug.LogWarning("[UI] Template database is null");
                return;
            }

            Debug.Log($"[UI] 开始填充对象面板，模板数量: {templateDB.templates.Count}");

            foreach (var template in templateDB.templates)
            {
                if (template == null)
                {
                    Debug.LogWarning("[UI] 跳过null模板");
                    continue;
                }

                if (template.ARPrefab == null)
                {
                    Debug.LogWarning($"[UI] 跳过没有AR预制体的模板: {template.templateName}");
                    continue; // 跳过没有AR预制体的模板
                }

                // 创建对象项UI（优先使用预制体）
                GameObject objectItem = CreateObjectItemUI(template);
                if (objectItem != null)
                {
                    objectItemInstances.Add(objectItem);
                    Debug.Log($"[UI] 创建对象项: {template.templateName}");
                }
            }

            // 在填充完成后强制重建布局，确保ScrollView/布局组件生效
            try
            {
                var rt = objectPaletteContent as RectTransform;
                if (rt != null)
                {
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                    Debug.Log("[UI] 已强制重建对象面板布局");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UI] 重建对象面板布局失败: {ex.Message}");
            }

            Debug.Log($"[UI] 对象面板填充完成，创建了 {objectItemInstances.Count} 个对象项");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UI] PopulateObjectPalette发生错误: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 创建对象项UI元素
    /// </summary>
    private GameObject CreateObjectItemUI(ObjectTemplateData template)
    {
        try
        {
            if (template == null)
            {
                Debug.LogError("[UI] CreateObjectItemUI: template is null");
                return null;
            }

            GameObject objectItem = null;

            // 优先使用用户在Inspector指定的预制体
            if (objectItemPrefab != null)
            {
                objectItem = Instantiate(objectItemPrefab, objectPaletteContent, false);
                if (objectItem != null)
                {
                    objectItem.transform.localScale = Vector3.one;
                    Debug.Log($"[UI] 使用预制体创建对象项: {template.templateName}");
                }
            }

            // 回退到动态创建（仅在未设置预制体时）
            if (objectItem == null)
            {
                objectItem = CreateDynamicObjectItem();
                Debug.Log($"[UI] 动态创建UI: {template.templateName}");
            }

            if (objectItem == null)
            {
                Debug.LogError("[UI] 创建对象项UI失败");
                return null;
            }

            // 设置对象信息
            SetupObjectItemUI(objectItem, template);

            return objectItem;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UI] CreateObjectItemUI发生错误: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// 动态创建对象项UI（不依赖预制体）
    /// </summary>
    private GameObject CreateDynamicObjectItem()
    {
        // 创建主容器
        GameObject item = new GameObject("ObjectItem");
        item.transform.SetParent(objectPaletteContent, false);

        // 添加Button组件
        Button button = item.AddComponent<Button>();

        // 添加Image组件作为背景
        Image bgImage = item.AddComponent<Image>();
        bgImage.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);

        // 创建图标子对象
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(item.transform, false);
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.preserveAspect = true;

        // 创建文本子对象
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(item.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;

        // 设置布局
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(100, 120);

        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.3f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.1f);
        textRect.anchorMax = new Vector2(0.9f, 0.3f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return item;
    }

    /// <summary>
    /// 设置对象项UI的内容
    /// </summary>
    private void SetupObjectItemUI(GameObject objectItem, ObjectTemplateData template)
    {
        try
        {
            if (objectItem == null || template == null)
            {
                Debug.LogError("[UI] SetupObjectItemUI: objectItem or template is null");
                return;
            }

            // 设置名称（如果存在Text）
            Text nameText = objectItem.GetComponentInChildren<Text>(true);
            if (nameText)
            {
                nameText.text = template.templateName;
                Debug.Log($"[UI] 设置文本: {template.templateName}");
            }
            else
            {
                Debug.Log("[UI] 没有找到Text组件用于显示名称");
            }

            // 查找所有Image组件（包括子对象）
            Image[] images = objectItem.GetComponentsInChildren<Image>(true);
            Debug.Log($"[UI] 找到 {images.Length} 个Image组件");

            Image iconImage = null;

            // 优先按子对象名寻找名为 Icon 的 Image
            foreach (var img in images)
            {
                if (img == null) continue;
                if (string.Equals(img.gameObject.name, "Icon", System.StringComparison.OrdinalIgnoreCase))
                {
                    iconImage = img;
                    break;
                }
            }

            // 如果没有按名找到，但只存在一个Image，把它作为图标
            if (iconImage == null && images.Length == 1)
            {
                iconImage = images[0];
                Debug.Log("[UI] 仅找到1个Image，使用该Image作为图标");
            }

            // 如果没有找到且存在多个Image，选择最后一个作为图标（通常是图标而非背景）
            if (iconImage == null && images.Length > 1)
            {
                iconImage = images[images.Length - 1];
                Debug.Log("[UI] 未按名找到Icon，使用最后一个Image作为图标的回退方案");
            }

            // 如果仍然没有找到Image，则尝试在objectItem上添加一个子Image来显示图标
            if (iconImage == null)
            {
                Debug.LogWarning("[UI] 没有找到Image用于显示图标，将动态创建一个Icon子对象");
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(objectItem.transform, false);
                iconImage = iconObj.AddComponent<Image>();
                iconImage.preserveAspect = true;
                RectTransform r = iconObj.GetComponent<RectTransform>();
                r.sizeDelta = new Vector2(64, 64);
            }

            // 设置图标
            if (iconImage != null)
            {
                if (template.icon != null)
                {
                    iconImage.sprite = template.icon;
                    iconImage.color = Color.white; // 确保图标可见
                    Debug.Log($"[UI] 成功设置图标: {template.icon.name}");
                }
                else
                {
                    Debug.LogWarning($"[UI] 模板 {template.templateName} 没有图标");
                    iconImage.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
                }

                // 确保Image可以接收射线以响应Button点击
                iconImage.raycastTarget = true;
            }

            // 获取或添加Button（优先查找自身或子对象上的Button）
            Button button = objectItem.GetComponent<Button>() ?? objectItem.GetComponentInChildren<Button>(true);
            if (button == null)
            {
                // 如果没有Button，尝试给主容器添加一个（Image必需）
                Debug.LogWarning("[UI] 找不到Button组件，将在项上动态添加一个Button以支持点击");
                Image bg = objectItem.GetComponent<Image>();
                if (bg == null)
                {
                    bg = objectItem.AddComponent<Image>();
                    bg.color = new Color(1f, 1f, 1f, 0.01f); // 透明背景以便Button可交互
                }
                button = objectItem.AddComponent<Button>();
            }

            // 绑定点击事件（捕获局部变量以避免闭包问题）
            if (button != null)
            {
                string capturedID = template.templateID;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnObjectItemClicked(capturedID));
                Debug.Log($"[UI] 添加点击事件: {capturedID}");
            }
            else
            {
                Debug.LogError("[UI] 无法添加点击事件：Button仍然为null");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UI] SetupObjectItemUI发生错误: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 对象项目点击事件
    /// </summary>
    private void OnObjectItemClicked(string templateID)
    {
        try
        {
            // 通过模板ID找到对应的对象模板并实例化其AR预制体
            Debug.Log($"[UI] 选择对象模板: {templateID}");

            // 检查是否在编辑模式
            if (!spatialMapManager.IsEditMode)
            {
                Debug.LogWarning("[UI] 不在编辑模式，无法放置对象");
                return;
            }

            var templateDB = EditorManager.Instance?.templateDB;
            if (templateDB == null)
            {
                Debug.LogError("[UI] Template database not found");
                return;
            }

            var template = templateDB.GetTemplateByID(templateID);
            if (template == null)
            {
                Debug.LogError($"[UI] Template not found for ID: {templateID}");
                return;
            }

            if (template.ARPrefab == null)
            {
                Debug.LogError($"[UI] ARPrefab is null for template: {templateID}");
                return;
            }

            // 检查EasyARSpatialMapEditorManager实例
            if (EasyARSpatialMapEditorManager.Instance == null)
            {
                Debug.LogError("[UI] EasyARSpatialMapEditorManager instance is null");
                return;
            }

            // 实例化AR预制体
            GameObject newObject = Instantiate(template.ARPrefab);
            if (newObject == null)
            {
                Debug.LogError("[UI] Failed to instantiate AR prefab");
                return;
            }

            Debug.Log($"[UI] 成功实例化对象: {newObject.name}");

            // 为实例添加 ARPlacedObject 组件（若不存在）
            ARPlacedObject placedObjectComponent = newObject.GetComponent<ARPlacedObject>();
            if (placedObjectComponent == null)
            {
                placedObjectComponent = newObject.AddComponent<ARPlacedObject>();
                Debug.Log("[UI] 添加了ARPlacedObject组件");
            }

            // 确保对象有Collider组件（用于射线检测）
            Collider collider = newObject.GetComponent<Collider>();
            if (collider == null)
            {
                // 如果没有Collider，添加一个BoxCollider
                BoxCollider boxCollider = newObject.AddComponent<BoxCollider>();
                Debug.Log("[UI] 添加了BoxCollider组件");
            }

            // 在屏幕中心放置对象
            Vector2 pixelCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 viewPoint = new Vector2(pixelCenter.x / Screen.width, pixelCenter.y / Screen.height); // 规范化到 [0,1]
            Debug.Log($"[UI] 尝试放置对象 - pixels: {pixelCenter}, viewPoint(normalized): {viewPoint}");

            // 打印当前地图会话/点云信息，帮助定位放置失败原因
            try
            {
                var session = spatialMapManager?.CurrentMapSession;
                var lm = session?.MapWorker?.LocalizedMap;
                Debug.Log($"[UI] 放置前检查: IsMapLocalized={spatialMapManager.IsMapLocalized}, IsEditMode={spatialMapManager.IsEditMode}, CurrentMapSession={(session != null)}");
                Debug.Log($"[UI] LocalizedMap present: {(lm != null)}, pointCount={(lm?.PointCloud?.Count ?? 0)}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UI] 无法读取地图点云信息: {ex.Message}");
            }

            bool success = EasyARSpatialMapEditorManager.Instance.PlaceGameObjectOnMap(newObject, viewPoint);

            // 如果屏幕中心放置失败，尝试在屏幕中下方做一次回退检测（有时UI遮挡或中心点无有效Hit）
            if (!success)
            {
                Vector2 pixelFallback = new Vector2(Screen.width * 0.5f, Screen.height * 0.35f);
                Vector2 viewFallback = new Vector2(pixelFallback.x / Screen.width, pixelFallback.y / Screen.height);
                Debug.Log($"[UI] 居中放置失败，尝试备用屏幕点 - pixels: {pixelFallback}, viewPoint: {viewFallback}");
                success = EasyARSpatialMapEditorManager.Instance.PlaceGameObjectOnMap(newObject, viewFallback);
                Debug.Log($"[UI] 备用放置结果: {success}");
            }

            if (success)
            {
                Debug.Log("[UI] 对象放置成功");
                // 显示成功提示（可选）
                ShowStatusMessage("对象放置成功！", 2f);

                // 放置成功后关闭对象面板
                if (isObjectPaletteOpen)
                {
                    OnToggleObjectPaletteClicked();
                }
            }
            else
            {
                // 放置失败销毁对象并提示
                Debug.LogWarning("[UI] 对象放置失败，销毁对象");

                // 显示详细的失败原因
                string failureReason = GetPlacementFailureReason();
                ShowStatusMessage($"对象放置失败: {failureReason}", 3f);

                if (newObject != null)
                {
                    Destroy(newObject);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UI] OnObjectItemClicked发生错误: {ex.Message}\n{ex.StackTrace}");
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

        // 添加调试信息
        // Debug.Log($"[UI状态] IsMapBuilding: {spatialMapManager.IsMapBuilding}, IsMapLocalized: {spatialMapManager.IsMapLocalized}, IsEditMode: {spatialMapManager.IsEditMode}");

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

        try
        {
            var currentSession = spatialMapManager?.CurrentMapSession;
            if (currentSession != null && currentSession.MapWorker != null)
            {
                var localizedMap = currentSession.MapWorker.LocalizedMap;
                if (localizedMap != null && localizedMap.MapInfo != null)
                {
                    string mapName = localizedMap.MapInfo.Name ?? "未命名地图";
                    int pointCount = localizedMap.PointCloud?.Count ?? 0;
                    mapInfoText.text = $"地图: {mapName}\n点云数量: {pointCount}";
                    // Debug.Log($"[UI] 地图信息更新: {mapName}, 点云: {pointCount}");
                }
                else
                {
                    mapInfoText.text = "地图未本地化";
                    Debug.Log("[UI] 地图未本地化");
                }
            }
            else
            {
                mapInfoText.text = "无活动地图";
                Debug.Log("[UI] 无活动地图会话");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UI] UpdateMapInfoText发生错误: {ex.Message}\n{ex.StackTrace}");
            mapInfoText.text = "地图信息获取失败";
        }
    }

    /// <summary>
    /// 更新编辑器状态文本
    /// </summary>
    private void UpdateEditorStatusText()
    {
        if (editorStatusText == null) return;

        try
        {
            string status = spatialMapManager?.GetEditorStatus() ?? "状态未知";
            editorStatusText.text = status;
            // Debug.Log($"[UI] 编辑器状态更新: {status}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UI] UpdateEditorStatusText发生错误: {ex.Message}\n{ex.StackTrace}");
            editorStatusText.text = "状态获取失败";
        }
    }

    /// <summary>
    /// 更新UI状态
    /// </summary>
    private void UpdateUIState()
    {
        bool isMapBuilding = spatialMapManager.IsMapBuilding;
        bool isMapLocalized = spatialMapManager.IsMapLocalized;
        bool isEditMode = spatialMapManager.IsEditMode;

        Debug.Log($"[UI] 更新按钮状态 - Building: {isMapBuilding}, Localized: {isMapLocalized}, EditMode: {isEditMode}");

        if (btnCreateMap)
        {
            bool enabled = !isMapBuilding;
            btnCreateMap.interactable = enabled;
            Debug.Log($"[UI] 创建地图按钮: {enabled}");
        }

        if (btnLoadMap)
        {
            bool enabled = !isMapBuilding && !isMapLocalized;
            btnLoadMap.interactable = enabled;
            Debug.Log($"[UI] 加载地图按钮: {enabled}");
        }

        if (btnSaveMap)
        {
            bool enabled = isMapBuilding;
            btnSaveMap.interactable = enabled;
            Debug.Log($"[UI] 保存地图按钮: {enabled}");
        }

        // 保存（包含物体）按钮的可用性与普通保存按钮一致
        if (btnSaveMapWithObjects)
        {
            bool enabled = isMapBuilding;
            btnSaveMapWithObjects.interactable = enabled;
            Debug.Log($"[UI] 保存（包含物体）按钮: {enabled}");
        }

        if (btnClearMap)
        {
            bool enabled = isMapBuilding || isMapLocalized;
            btnClearMap.interactable = enabled;
            Debug.Log($"[UI] 清除地图按钮: {enabled}");
        }

        if (btnEnterEditMode)
        {
            bool enabled = isMapLocalized && !isEditMode;
            btnEnterEditMode.interactable = enabled;
            Debug.Log($"[UI] 进入编辑模式按钮: {enabled}");
        }

        if (btnExitEditMode)
        {
            bool enabled = isEditMode;
            btnExitEditMode.interactable = enabled;
            Debug.Log($"[UI] 退出编辑模式按钮: {enabled}");
        }

        if (btnToggleObjectPalette)
        {
            bool enabled = isEditMode;  // 只有在编辑模式下才能打开对象面板
            btnToggleObjectPalette.interactable = enabled;
            btnToggleObjectPalette.gameObject.SetActive(enabled);  // 编辑模式下显示，非编辑模式下隐藏
            Debug.Log($"[UI] 对象面板按钮: {enabled} (编辑模式: {isEditMode})");
        }
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

    /// <summary>
    /// 强制检查地图状态并更新UI（临时解决方案）
    /// </summary>
    public void ForceCheckMapStatus()
    {
        Debug.Log("[UI] 强制检查地图状态");

        // 检查是否有可用地图但状态没有更新
        var availableMaps = spatialMapManager.GetAvailableMaps();
        if (availableMaps.Count > 0 && !spatialMapManager.IsMapLocalized)
        {
            Debug.Log($"[UI] 发现 {availableMaps.Count} 个可用地图，尝试加载第一个");
            spatialMapManager.LoadMap(availableMaps[0]);
        }

        UpdateUIState();
    }

    /// <summary>
    /// 添加手动进入编辑模式的按钮（调试用）
    /// </summary>
    public void ForceEnterEditMode()
    {
        Debug.Log("[UI] 强制进入编辑模式");
        spatialMapManager.EnterEditMode();
        UpdateUIState();
    }

    /// <summary>
    /// 显示状态消息（用户友好的提示）
    /// </summary>
    private void ShowStatusMessage(string message, float duration)
    {
        Debug.Log($"[UI消息] {message}");

        // 如果有状态文本UI，可以临时显示消息
        if (statusText != null)
        {
            string originalText = statusText.text;
            statusText.text = message;

            // 延迟恢复原始文本
            StartCoroutine(RestoreStatusTextAfterDelay(originalText, duration));
        }
    }

    /// <summary>
    /// 延迟恢复状态文本
    /// </summary>
    private System.Collections.IEnumerator RestoreStatusTextAfterDelay(string originalText, float delay)
    {
        yield return new UnityEngine.WaitForSeconds(delay);
        if (statusText != null)
        {
            statusText.text = originalText;
        }
    }

    /// <summary>
    /// 获取对象放置失败的原因
    /// </summary>
    private string GetPlacementFailureReason()
    {
        if (!spatialMapManager.IsMapLocalized)
        {
            return "地图未本地化";
        }

        if (!spatialMapManager.IsEditMode)
        {
            return "未进入编辑模式";
        }

        if (spatialMapManager.CurrentMapSession == null)
        {
            return "地图会话无效";
        }

        return "未找到有效的放置点";
    }

    /// <summary>
    /// 显示地图选择面板
    /// </summary>
    private void ShowMapSelectionPanel()
    {
        if (mapSelectionPanel == null)
        {
            Debug.LogWarning("[UI] 地图选择面板未设置");
            // 回退到直接加载第一个地图
            LoadFirstAvailableMap();
            return;
        }

        // 确保列表容器处于激活状态（否则无法显示或布局）
        if (mapListContent != null && !mapListContent.gameObject.activeSelf)
        {
            Debug.Log("[UI] 激活 mapListContent 游戏对象以显示列表");
            mapListContent.gameObject.SetActive(true);
        }

        // 填充地图列表
        PopulateMapList();

        // 显示面板
        mapSelectionPanel.SetActive(true);
    }

    /// <summary>
    /// 填充地图列表
    /// </summary>
    private void PopulateMapList()
    {
        if (mapListContent == null) return;

        // 确保内容容器处于激活状态以便布局组件工作
        if (!mapListContent.gameObject.activeSelf)
            mapListContent.gameObject.SetActive(true);

        // 在填充前强制刷新Manager的地图缓存（确保获取最新保存的地图）
        spatialMapManager.RefreshAvailableMaps();

        // 清除现有项目
        foreach (Transform child in mapListContent)
        {
            Destroy(child.gameObject);
        }

        // 获取可用地图
        var availableMaps = spatialMapManager.GetAvailableMaps();
        Debug.Log($"[UI] 找到 {availableMaps.Count} 个可用地图");

        if (availableMaps.Count == 0)
        {
            ShowStatusMessage("没有可用的地图，请先创建并保存地图", 3f);
            mapSelectionPanel.SetActive(false);
            return;
        }

        // 为每个地图创建UI项目
        int idx = 0;
        foreach (var mapMeta in availableMaps)
        {
            Debug.Log($"[UI] 可用地图[{idx}] Name='{mapMeta?.Map?.Name}' ID='{mapMeta?.Map?.ID}'");
            CreateMapListItem(mapMeta);
            idx++;
        }

        // 强制刷新布局以确保ScrollView/布局组件正确计算
        try
        {
            var rt = mapListContent as RectTransform;
            if (rt != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                Debug.Log("[UI] 已强制重建地图列表布局");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UI] 强制重建布局失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建地图列表项目
    /// </summary>
    private void CreateMapListItem(LocalMapMeta mapMeta)
    {
        GameObject mapItem;

        if (mapItemPrefab != null)
        {
            // 使用带 parent 的重载并指定 worldPositionStays = false，以保持预制体的 RectTransform 布局
            mapItem = Instantiate(mapItemPrefab, mapListContent, false);
        }
        else
        {
            // 动态创建地图项目
            mapItem = CreateDynamicMapItem();
        }

        // 设置地图信息
        Text nameText = mapItem.GetComponentInChildren<Text>();
        if (nameText != null)
        {
            nameText.text = $"{mapMeta.Map.Name}\nID: {mapMeta.Map.ID}";
        }

        // 添加点击事件
        Button button = mapItem.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnMapSelected(mapMeta));
        }

        Debug.Log($"[UI] 已创建地图列表项: {mapMeta.Map.Name} (预制: {mapItemPrefab != null})");
    }

    /// <summary>
    /// 动态创建地图项目UI
    /// </summary>
    private GameObject CreateDynamicMapItem()
    {
        GameObject item = new GameObject("MapItem");
        // 确保在 UI 层级中以正确的方式设置父对象
        item.transform.SetParent(mapListContent, false);

        // 添加RectTransform并设置默认锚点
        var rect = item.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);

        // 添加LayoutElement以便VerticalLayoutGroup/Content区分大小
        var layoutElem = item.AddComponent<UnityEngine.UI.LayoutElement>();
        layoutElem.preferredHeight = 60f;
        layoutElem.flexibleWidth = 1f;

        // 添加Button组件
        Button button = item.AddComponent<Button>();

        // 添加Image组件作为背景
        Image bgImage = item.AddComponent<Image>();
        bgImage.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);

        // 创建文本子对象
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(item.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 16;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleLeft;

        // 设置布局
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(300, 60);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.1f);
        textRect.anchorMax = new Vector2(0.95f, 0.9f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return item;
    }

    /// <summary>
    /// 地图被选中时的处理
    /// </summary>
    private void OnMapSelected(LocalMapMeta mapMeta)
    {
        Debug.Log($"[UI] 选择地图: {mapMeta.Map.Name}");

        // 隐藏选择面板
        if (mapSelectionPanel) mapSelectionPanel.SetActive(false);

        // 加载选中的地图
        spatialMapManager.LoadMap(mapMeta);
        ShowStatusMessage($"正在加载地图: {mapMeta.Map.Name}", 2f);
        UpdateUIState();
    }

    /// <summary>
    /// 直接加载第一个可用地图（备用方法）
    /// </summary>
    private void LoadFirstAvailableMap()
    {
        var availableMaps = spatialMapManager.GetAvailableMaps();
        if (availableMaps.Count > 0)
        {
            Debug.Log($"[UI] 加载地图: {availableMaps[0].Map.Name}");
            spatialMapManager.LoadMap(availableMaps[0]);
            UpdateUIState();
        }
        else
        {
            Debug.Log("[UI] 没有可用的地图，请先创建并保存地图");
            ShowStatusMessage("没有可用的地图，请先创建并保存地图", 3f);
        }
    }

    /// <summary>
    /// 调试用：打印 Application.persistentDataPath 以及 SparseSpatialMap 文件夹内容（用于 Android 路径确认）
    /// </summary>
    private void DebugPrintMapFolder()
    {
        try
        {
            string persistent = Application.persistentDataPath ?? "(null)";
            string folder = System.IO.Path.Combine(persistent, "SparseSpatialMap");
            Debug.Log($"[UI] persistentDataPath = {persistent}");
            Debug.Log($"[UI] SparseSpatialMap folder = {folder}");

            if (System.IO.Directory.Exists(folder))
            {
                var files = System.IO.Directory.GetFiles(folder);
                Debug.Log($"[UI] SparseSpatialMap contains {files.Length} files:");
                foreach (var f in files)
                {
                    Debug.Log($"[UI]  - {f}");
                }
            }
            else
            {
                Debug.Log("[UI] SparseSpatialMap 文件夹不存在");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UI] DebugPrintMapFolder error: {ex.Message}");
        }
    }

    private void ValidateMapItemPrefab()
    {
        try
        {
            if (mapItemPrefab == null)
            {
                Debug.Log("[UI] mapItemPrefab 未设置，使用动态创建的列表项");
                return;
            }

            Debug.Log($"[UI] 验证 mapItemPrefab: {mapItemPrefab.name}");

            var rt = mapItemPrefab.GetComponent<RectTransform>();
            Debug.Log($"[UI] mapItemPrefab has RectTransform: {(rt != null)}");

            var btn = mapItemPrefab.GetComponent<Button>();
            Debug.Log($"[UI] mapItemPrefab has Button: {(btn != null)}");

            var txt = mapItemPrefab.GetComponentInChildren<Text>();
            Debug.Log($"[UI] mapItemPrefab has Text in children: {(txt != null)} => '{(txt != null ? txt.text : "(null)")}'");

            var layout = mapItemPrefab.GetComponent<LayoutElement>();
            Debug.Log($"[UI] mapItemPrefab has LayoutElement: {(layout != null)}");

            var fitter = mapItemPrefab.GetComponent<ContentSizeFitter>();
            Debug.Log($"[UI] mapItemPrefab has ContentSizeFitter: {(fitter != null)}");

            // 检查是否预制体根对象的锚点/sizeDelta 看起来合理
            if (rt != null)
            {
                Debug.Log($"[UI] prefab RectTransform sizeDelta: {rt.sizeDelta}, anchorMin: {rt.anchorMin}, anchorMax: {rt.anchorMax}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[UI] ValidateMapItemPrefab error: {ex.Message}");
        }
    }

    /// <summary>
    /// 调试用：检查完整的对象编辑流程
    /// </summary>
    public void DebugObjectEditingFlow()
    {
        Debug.Log("=== 调试对象编辑流程 ===");

        // 1. 检查管理器状态
        Debug.Log($"1. Manager状态: Building={spatialMapManager.IsMapBuilding}, Localized={spatialMapManager.IsMapLocalized}, EditMode={spatialMapManager.IsEditMode}");

        // 2. 检查模板数据库
        var templateDB = EditorManager.Instance?.templateDB;
        if (templateDB != null)
        {
            Debug.Log($"2. 模板数据库: {templateDB.templates.Count} 个模板");
            foreach (var template in templateDB.templates)
            {
                if (template != null)
                {
                    Debug.Log($"   - {template.templateName} (ID: {template.templateID}) 有AR预制体: {template.ARPrefab != null}");
                }
            }
        }
        else
        {
            Debug.LogWarning("2. 模板数据库为null");
        }

        // 3. 检查UI组件
        Debug.Log($"3. UI组件检查:");
        Debug.Log($"   - objectPalettePanel: {objectPalettePanel != null}");
        Debug.Log($"   - objectPaletteContent: {objectPaletteContent != null}");
        Debug.Log($"   - btnToggleObjectPalette: {btnToggleObjectPalette != null}");
        Debug.Log($"   - btnToggleObjectPalette.interactable: {(btnToggleObjectPalette?.interactable ?? false)}");

        // 4. 检查当前地图会话
        var session = spatialMapManager.CurrentMapSession;
        if (session != null)
        {
            Debug.Log($"4. 地图会话: 存在, Maps数量: {session.Maps?.Count ?? 0}");
            if (session.Maps != null && session.Maps.Count > 0)
            {
                var mapData = session.Maps[0];
                Debug.Log($"   - Props数量: {mapData.Props?.Count ?? 0}");
                if (mapData.Props != null)
                {
                    foreach (var prop in mapData.Props)
                    {
                        if (prop != null)
                        {
                            Debug.Log($"     * {prop.name} 位置: {prop.transform.position}");
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("4. 地图会话为null");
        }

        Debug.Log("=== 流程检查完成 ===");
    }

    /// <summary>
    /// 调试物体面板UI状态
    /// </summary>
    public void DebugObjectPaletteState()
    {
        Debug.Log("=== 物体面板UI状态调试 ===");

        Debug.Log($"1. 编辑模式状态: {spatialMapManager.IsEditMode}");
        Debug.Log($"2. 地图本地化状态: {spatialMapManager.IsMapLocalized}");
        Debug.Log($"3. 物体面板打开状态: {isObjectPaletteOpen}");

        if (btnToggleObjectPalette)
        {
            Debug.Log($"4. 物体面板按钮存在: true");
            Debug.Log($"   - 按钮可交互: {btnToggleObjectPalette.interactable}");
            Debug.Log($"   - 按钮激活: {btnToggleObjectPalette.gameObject.activeInHierarchy}");
            Debug.Log($"   - 按钮文本: {btnToggleObjectPalette.GetComponentInChildren<Text>()?.text}");
        }
        else
        {
            Debug.LogWarning("4. 物体面板按钮不存在");
        }

        if (objectPalettePanel)
        {
            Debug.Log($"5. 物体面板存在: true");
            Debug.Log($"   - 面板激活: {objectPalettePanel.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("5. 物体面板不存在");
        }

        if (objectPaletteContent)
        {
            Debug.Log($"6. 物体面板内容存在: true");
            Debug.Log($"   - 子对象数量: {objectPaletteContent.transform.childCount}");
        }
        else
        {
            Debug.LogWarning("6. 物体面板内容不存在");
        }

        Debug.Log("=== 物体面板UI状态调试完成 ===");
    }

    /// <summary>
    /// 强制打开对象面板（用于测试调试）
    /// </summary>
    public void ForceOpenObjectPalette()
    {
        Debug.Log("[UI] 强制打开对象面板 - 调试用");
        DebugObjectPaletteState();

        if (btnToggleObjectPalette && btnToggleObjectPalette.interactable)
        {
            OnToggleObjectPaletteClicked();
        }
        else
        {
            Debug.LogError("[UI] 无法强制打开对象面板 - 按钮不可用");
        }
    }

    /// <summary>
    /// 保存地图并包含当前放置物体按钮
    /// </summary>
    private void OnSaveMapWithObjectsClicked()
    {
        try
        {
            Debug.Log("[UI] 保存（包含物体）按钮点击 - 开始保存对象信息");

            if (EasyARSpatialMapEditorManager.Instance == null)
            {
                Debug.LogError("[UI] EasyARSpatialMapEditorManager instance is null - 无法保存对象信息");
                ShowStatusMessage("保存失败：编辑管理器不存在", 3f);
                return;
            }

            // 先保存当前放置的物体信息到 MapMeta/Props
            EasyARSpatialMapEditorManager.Instance.SaveObjectsInfo();
            Debug.Log("[UI] SaveObjectsInfo 已调用");

            // 继续调用已有的保存地图逻辑（复用 OnSaveMapClicked）
            OnSaveMapClicked();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UI] OnSaveMapWithObjectsClicked 发生错误: {ex.Message}\n{ex.StackTrace}");
            ShowStatusMessage("保存失败，请查看日志", 3f);
        }
    }
}