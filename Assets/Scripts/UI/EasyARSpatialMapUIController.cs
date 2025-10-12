// using UnityEngine;
// using UnityEngine.UI;
// using Assets.Scripts.Manager;
// using System.Collections.Generic;
// using SpatialMap_SparseSpatialMap;
// using LocalMapMeta = SpatialMap_SparseSpatialMap.MapMeta;

// public class EasyARSpatialMapUIController : MonoBehaviour
// {
//     [Header("Map Control UI")]
//     public Button btnCreateMap, btnLoadMap, btnSaveMap, btnSaveMapWithObjects, btnClearMap, btnTogglePointCloud, btnEnterEditMode, btnExitEditMode;

//     [Header("Map Selection UI")]
//     public GameObject mapSelectionPanel;
//     public Transform mapListContent;
//     public GameObject mapItemPrefab;

//     [Header("Object Placement UI")]
//     public GameObject objectPalettePanel;
//     public Transform objectPaletteContent;
//     public GameObject objectItemPrefab;
//     public Button btnToggleObjectPalette;

//     [Header("Status UI")]
//     public GameObject debugArea;
//     public Text statusText, mapInfoText, editorStatusText;

//     [Header("Settings")]
//     public bool showPointCloud = true;

//     private EasyARSpatialMapEditorManager spatialMapManager;
//     private List<GameObject> objectItemInstances = new();
//     private bool isObjectPaletteOpen = false;

//     private void Start()
//     {
//         spatialMapManager = EasyARSpatialMapEditorManager.Instance;
//         if (spatialMapManager == null)
//         {
//             Debug.LogError("EasyARSpatialMapEditorManager not found!");
//             return;
//         }
//         InitializeUI();
//         SubscribeToEvents();
//         UpdateUIState();
//     }

//     private void InitializeUI()
//     {
//         btnCreateMap?.onClick.AddListener(OnCreateMapClicked);
//         btnLoadMap?.onClick.AddListener(OnLoadMapClicked);
//         btnSaveMap?.onClick.AddListener(OnSaveMapClicked);
//         btnSaveMapWithObjects?.onClick.AddListener(OnSaveMapWithObjectsClicked);
//         btnClearMap?.onClick.AddListener(OnClearMapClicked);
//         btnTogglePointCloud?.onClick.AddListener(OnTogglePointCloudClicked);
//         btnEnterEditMode?.onClick.AddListener(OnEnterEditModeClicked);
//         btnExitEditMode?.onClick.AddListener(OnExitEditModeClicked);
//         btnToggleObjectPalette?.onClick.AddListener(OnToggleObjectPaletteClicked);

//         mapSelectionPanel?.SetActive(false);
//         mapListContent?.gameObject.SetActive(false);
//         objectPalettePanel?.SetActive(false);
//     }

//     private void SubscribeToEvents()
//     {
//         if (spatialMapManager != null)
//         {
//             spatialMapManager.OnMapLocalized += OnMapLocalized;
//             spatialMapManager.OnMapBuildingStarted += OnMapBuildingStarted;
//             spatialMapManager.OnObjectPlaced += OnObjectPlaced;
//             spatialMapManager.OnObjectRemoved += OnObjectRemoved;
//         }
//     }

//     private void Update()
//     {
//         if (Time.frameCount % 30 == 0)
//         {
//             UpdateStatusText();
//             UpdateMapInfoText();
//             UpdateEditorStatusText();
//         }
//     }

//     private void OnCreateMapClicked()
//     {
//         spatialMapManager.StartMapBuilding();
//         UpdateUIState();
//     }

//     private void OnLoadMapClicked() => ShowMapSelectionPanel();

//     private void OnSaveMapClicked()
//     {
//         if (!spatialMapManager.IsMapBuilding)
//         {
//             ShowStatusMessage("没有正在构建的地图可以保存", 3f);
//             return;
//         }
//         ShowStatusMessage("正在保存地图，请稍候...", 3f);
//         spatialMapManager.SaveCurrentMap();
//         StartCoroutine(CheckMapStatusAfterSave());
//     }

//     private System.Collections.IEnumerator CheckMapStatusAfterSave()
//     {
//         yield return new WaitForSeconds(3f);
//         int initialMapCount = spatialMapManager.GetAvailableMaps().Count;
//         bool mapSaved = false;
//         int attempts = 0;
//         const int maxAttempts = 10;
//         while (!mapSaved && attempts < maxAttempts)
//         {
//             yield return new WaitForSeconds(1f);
//             attempts++;
//             var currentMaps = spatialMapManager.GetAvailableMaps();
//             if (currentMaps.Count > initialMapCount)
//             {
//                 mapSaved = true;
//                 var latestMap = currentMaps[^1];
//                 ShowStatusMessage("地图保存成功！正在加载...", 3f);
//                 spatialMapManager.ClearCurrentMap();
//                 yield return new WaitForSeconds(0.5f);
//                 spatialMapManager.LoadMap(latestMap);
//                 yield return StartCoroutine(WaitForMapLocalization());
//             }
//         }
//         if (!mapSaved)
//             ShowStatusMessage("地图保存可能失败，请检查", 4f);
//         UpdateUIState();
//     }

//     private System.Collections.IEnumerator WaitForMapLocalization()
//     {
//         float timeout = 15f, elapsed = 0f;
//         while (!spatialMapManager.IsMapLocalized && elapsed < timeout)
//         {
//             yield return new WaitForSeconds(0.5f);
//             elapsed += 0.5f;
//         }
//         if (spatialMapManager.IsMapLocalized)
//         {
//             ShowStatusMessage("地图已本地化，可以开始编辑", 2f);
//             yield return new WaitForSeconds(1f);
//             if (!spatialMapManager.IsEditMode)
//             {
//                 spatialMapManager.EnterEditMode();
//                 ShowStatusMessage("已自动进入编辑模式，可以放置对象了！", 3f);
//             }
//         }
//         else
//         {
//             ShowStatusMessage("地图本地化失败，请手动重新加载", 4f);
//         }
//         UpdateUIState();
//     }

//     private void OnClearMapClicked()
//     {
//         spatialMapManager.ClearCurrentMap();
//         UpdateUIState();
//     }

//     private void OnTogglePointCloudClicked()
//     {
//         showPointCloud = !showPointCloud;
//         spatialMapManager.SetPointCloudVisibility(showPointCloud);
//         if (btnTogglePointCloud)
//             btnTogglePointCloud.GetComponentInChildren<Text>().text = showPointCloud ? "隐藏点云" : "显示点云";
//     }

//     private void OnEnterEditModeClicked()
//     {
//         spatialMapManager.EnterEditMode();
//         UpdateUIState();
//         if (!isObjectPaletteOpen && btnToggleObjectPalette && btnToggleObjectPalette.interactable)
//             OnToggleObjectPaletteClicked();
//     }

//     private void OnExitEditModeClicked()
//     {
//         spatialMapManager.ExitEditMode();
//         if (isObjectPaletteOpen)
//         {
//             isObjectPaletteOpen = false;
//             if (objectPalettePanel) objectPalettePanel.SetActive(false);
//             if (btnToggleObjectPalette)
//             {
//                 var textComp = btnToggleObjectPalette.GetComponentInChildren<Text>();
//                 if (textComp != null)
//                     textComp.text = "打开对象面板";
//             }
//         }
//         UpdateUIState();
//     }

//     private void OnToggleObjectPaletteClicked()
//     {
//         isObjectPaletteOpen = !isObjectPaletteOpen;
//         objectPalettePanel?.SetActive(isObjectPaletteOpen);
//         if (isObjectPaletteOpen) PopulateObjectPalette();
//         if (btnToggleObjectPalette)
//             btnToggleObjectPalette.GetComponentInChildren<Text>().text = isObjectPaletteOpen ? "关闭对象面板" : "打开对象面板";
//     }

//     private void PopulateObjectPalette()
//     {
//         ClearObjectItems();
//         if (objectPaletteContent == null) return;
//         var templateDB = EditorManager.Instance?.templateDB;
//         if (templateDB == null) return;
//         foreach (var template in templateDB.templates)
//         {
//             if (template == null || template.ARPrefab == null) continue;
//             var objectItem = CreateObjectItemUI(template);
//             if (objectItem != null) objectItemInstances.Add(objectItem);
//         }
//         if (objectPaletteContent is RectTransform rt)
//             UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
//     }

//     private GameObject CreateObjectItemUI(ObjectTemplateData template)
//     {
//         if (template == null) return null;
//         GameObject objectItem = objectItemPrefab != null
//             ? Instantiate(objectItemPrefab, objectPaletteContent, false)
//             : CreateDynamicObjectItem();
//         if (objectItem == null) return null;
//         SetupObjectItemUI(objectItem, template);
//         return objectItem;
//     }

//     private GameObject CreateDynamicObjectItem()
//     {
//         var item = new GameObject("ObjectItem");
//         item.transform.SetParent(objectPaletteContent, false);
//         item.AddComponent<Button>();
//         var bgImage = item.AddComponent<Image>();
//         bgImage.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
//         var iconObj = new GameObject("Icon");
//         iconObj.transform.SetParent(item.transform, false);
//         iconObj.AddComponent<Image>().preserveAspect = true;
//         var textObj = new GameObject("Text");
//         textObj.transform.SetParent(item.transform, false);
//         var text = textObj.AddComponent<Text>();
//         text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
//         text.fontSize = 14;
//         text.color = Color.black;
//         text.alignment = TextAnchor.MiddleCenter;
//         item.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 120);
//         iconObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.1f, 0.3f);
//         iconObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.9f, 0.9f);
//         textObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.1f, 0.1f);
//         textObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.9f, 0.3f);
//         return item;
//     }

//     private void SetupObjectItemUI(GameObject objectItem, ObjectTemplateData template)
//     {
//         if (objectItem == null || template == null) return;
//         var nameText = objectItem.GetComponentInChildren<Text>(true);
//         if (nameText) nameText.text = template.templateName;
//         var images = objectItem.GetComponentsInChildren<Image>(true);
//         Image iconImage = null;
//         foreach (var img in images)
//         {
//             if (img && img.gameObject.name == "Icon") { iconImage = img; break; }
//         }
//         if (iconImage == null && images.Length > 0) iconImage = images[^1];
//         if (iconImage == null)
//         {
//             var iconObj = new GameObject("Icon");
//             iconObj.transform.SetParent(objectItem.transform, false);
//             iconImage = iconObj.AddComponent<Image>();
//             iconImage.preserveAspect = true;
//             iconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(64, 64);
//         }
//         if (iconImage != null)
//         {
//             iconImage.sprite = template.icon;
//             iconImage.color = template.icon != null ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.8f);
//             iconImage.raycastTarget = true;
//         }
//         var button = objectItem.GetComponent<Button>() ?? objectItem.GetComponentInChildren<Button>(true);
//         if (button == null)
//         {
//             var bg = objectItem.GetComponent<Image>() ?? objectItem.AddComponent<Image>();
//             bg.color = new Color(1f, 1f, 1f, 0.01f);
//             button = objectItem.AddComponent<Button>();
//         }
//         if (button != null)
//         {
//             string capturedID = template.templateID;
//             button.onClick.RemoveAllListeners();
//             button.onClick.AddListener(() => OnObjectItemClicked(capturedID));
//         }
//     }

//     private void OnObjectItemClicked(string templateID)
//     {
//         if (!spatialMapManager.IsEditMode) return;
//         var templateDB = EditorManager.Instance?.templateDB;
//         if (templateDB == null) return;
//         var template = templateDB.GetTemplateByID(templateID);
//         if (template == null || template.ARPrefab == null) return;
//         if (EasyARSpatialMapEditorManager.Instance == null) return;
//         var newObject = Instantiate(template.ARPrefab);
//         if (newObject == null) return;
//         if (newObject.GetComponent<ARPlacedObject>() == null)
//             newObject.AddComponent<ARPlacedObject>();
//         if (newObject.GetComponent<Collider>() == null)
//             newObject.AddComponent<BoxCollider>();
//         Vector2 pixelCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
//         Vector2 viewPoint = new(pixelCenter.x / Screen.width, pixelCenter.y / Screen.height);
//         bool success = EasyARSpatialMapEditorManager.Instance.PlaceGameObjectOnMap(newObject, viewPoint);
//         if (!success)
//         {
//             Vector2 pixelFallback = new(Screen.width * 0.5f, Screen.height * 0.35f);
//             Vector2 viewFallback = new(pixelFallback.x / Screen.width, pixelFallback.y / Screen.height);
//             success = EasyARSpatialMapEditorManager.Instance.PlaceGameObjectOnMap(newObject, viewFallback);
//         }
//         if (success)
//         {
//             ShowStatusMessage("对象放置成功！", 2f);
//             if (isObjectPaletteOpen) OnToggleObjectPaletteClicked();
//         }
//         else
//         {
//             ShowStatusMessage($"对象放置失败: {GetPlacementFailureReason()}", 3f);
//             if (newObject != null) Destroy(newObject);
//         }
//     }

//     private void ClearObjectItems()
//     {
//         foreach (var item in objectItemInstances)
//             if (item != null) Destroy(item);
//         objectItemInstances.Clear();
//     }

//     private void OnMapLocalized() { UpdateStatusText(); UpdateUIState(); }
//     private void OnMapBuildingStarted() { UpdateStatusText(); UpdateUIState(); }
//     private void OnObjectPlaced(GameObject obj) => UpdateEditorStatusText();
//     private void OnObjectRemoved(GameObject obj) => UpdateEditorStatusText();

//     private void UpdateStatusText()
//     {
//         if (statusText == null) return;
//         statusText.text = spatialMapManager.IsMapBuilding ? "正在构建地图..." :
//             spatialMapManager.IsMapLocalized ? "地图已本地化，可以编辑" : "等待地图本地化...";
//     }

//     private void UpdateMapInfoText()
//     {
//         if (mapInfoText == null) return;
//         try
//         {
//             var currentSession = spatialMapManager?.CurrentMapSession;
//             if (currentSession != null && currentSession.MapWorker != null)
//             {
//                 var localizedMap = currentSession.MapWorker.LocalizedMap;
//                 if (localizedMap != null && localizedMap.MapInfo != null)
//                 {
//                     string mapName = localizedMap.MapInfo.Name ?? "未命名地图";
//                     int pointCount = localizedMap.PointCloud?.Count ?? 0;
//                     mapInfoText.text = $"地图: {mapName}\n点云数量: {pointCount}";
//                 }
//                 else mapInfoText.text = "地图未本地化";
//             }
//             else mapInfoText.text = "无活动地图";
//         }
//         catch
//         {
//             mapInfoText.text = "地图信息获取失败";
//         }
//     }

//     private void UpdateEditorStatusText()
//     {
//         if (editorStatusText == null) return;
//         try
//         {
//             editorStatusText.text = spatialMapManager?.GetEditorStatus() ?? "状态未知";
//         }
//         catch
//         {
//             editorStatusText.text = "状态获取失败";
//         }
//     }

//     private void UpdateUIState()
//     {
//         bool isMapBuilding = spatialMapManager.IsMapBuilding;
//         bool isMapLocalized = spatialMapManager.IsMapLocalized;
//         bool isEditMode = spatialMapManager.IsEditMode;
//         if (btnCreateMap) btnCreateMap.interactable = !isMapBuilding;
//         if (btnLoadMap) btnLoadMap.interactable = true; //!isMapBuilding && !isMapLocalized;
//         if (btnSaveMap) btnSaveMap.interactable = isMapBuilding;
//         if (btnSaveMapWithObjects) btnSaveMapWithObjects.interactable = isEditMode;
//         if (btnClearMap) btnClearMap.interactable = isMapBuilding || isMapLocalized;
//         if (btnEnterEditMode) btnEnterEditMode.interactable = isMapLocalized && !isEditMode;
//         if (btnExitEditMode) btnExitEditMode.interactable = isEditMode;
//         if (btnToggleObjectPalette)
//         {
//             btnToggleObjectPalette.interactable = isEditMode;
//             btnToggleObjectPalette.gameObject.SetActive(isEditMode);
//         }
//     }

//     private void OnDestroy()
//     {
//         ClearObjectItems();
//         if (spatialMapManager != null)
//         {
//             spatialMapManager.OnMapLocalized -= OnMapLocalized;
//             spatialMapManager.OnMapBuildingStarted -= OnMapBuildingStarted;
//             spatialMapManager.OnObjectPlaced -= OnObjectPlaced;
//             spatialMapManager.OnObjectRemoved -= OnObjectRemoved;
//         }
//     }

//     private void ShowStatusMessage(string message, float duration)
//     {
//         if (statusText != null)
//         {
//             string originalText = statusText.text;
//             statusText.text = message;
//             StartCoroutine(RestoreStatusTextAfterDelay(originalText, duration));
//         }
//     }

//     private System.Collections.IEnumerator RestoreStatusTextAfterDelay(string originalText, float delay)
//     {
//         yield return new WaitForSeconds(delay);
//         if (statusText != null) statusText.text = originalText;
//     }

//     private string GetPlacementFailureReason()
//     {
//         if (!spatialMapManager.IsMapLocalized) return "地图未本地化";
//         if (!spatialMapManager.IsEditMode) return "未进入编辑模式";
//         if (spatialMapManager.CurrentMapSession == null) return "地图会话无效";
//         return "未找到有效的放置点";
//     }

//     private void ShowMapSelectionPanel()
//     {
//         if (mapSelectionPanel == null)
//         {
//             LoadFirstAvailableMap();
//             return;
//         }
//         if (mapListContent != null && !mapListContent.gameObject.activeSelf)
//             mapListContent.gameObject.SetActive(true);
//         PopulateMapList();
//         mapSelectionPanel.SetActive(true);
//     }

//     private void PopulateMapList()
//     {
//         if (mapListContent == null) return;
//         if (!mapListContent.gameObject.activeSelf)
//             mapListContent.gameObject.SetActive(true);
//         spatialMapManager.RefreshAvailableMaps();
//         foreach (Transform child in mapListContent)
//             Destroy(child.gameObject);
//         var availableMaps = spatialMapManager.GetAvailableMaps();
//         if (availableMaps.Count == 0)
//         {
//             ShowStatusMessage("没有可用的地图，请先创建并保存地图", 3f);
//             mapSelectionPanel.SetActive(false);
//             return;
//         }
//         foreach (var mapMeta in availableMaps)
//             CreateMapListItem(mapMeta);
//         if (mapListContent is RectTransform rt)
//             UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
//     }

//     private void CreateMapListItem(LocalMapMeta mapMeta)
//     {
//         GameObject mapItem = mapItemPrefab != null
//             ? Instantiate(mapItemPrefab, mapListContent, false)
//             : CreateDynamicMapItem();
//         var nameText = mapItem.GetComponentInChildren<Text>();
//         if (nameText != null)
//             nameText.text = $"{mapMeta.Map.Name}\nID: {mapMeta.Map.ID}";
//         var button = mapItem.GetComponent<Button>();
//         if (button != null)
//             button.onClick.AddListener(() => OnMapSelected(mapMeta));
//     }

//     private GameObject CreateDynamicMapItem()
//     {
//         var item = new GameObject("MapItem");
//         item.transform.SetParent(mapListContent, false);
//         var rect = item.AddComponent<RectTransform>();
//         rect.anchorMin = new Vector2(0f, 1f);
//         rect.anchorMax = new Vector2(1f, 1f);
//         rect.pivot = new Vector2(0.5f, 1f);
//         var layoutElem = item.AddComponent<UnityEngine.UI.LayoutElement>();
//         layoutElem.preferredHeight = 60f;
//         layoutElem.flexibleWidth = 1f;
//         item.AddComponent<Button>();
//         var bgImage = item.AddComponent<Image>();
//         bgImage.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
//         var textObj = new GameObject("Text");
//         textObj.transform.SetParent(item.transform, false);
//         var text = textObj.AddComponent<Text>();
//         text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
//         text.fontSize = 16;
//         text.color = Color.black;
//         text.alignment = TextAnchor.MiddleLeft;
//         item.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 60);
//         textObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.05f, 0.1f);
//         textObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.95f, 0.9f);
//         return item;
//     }

//     private void OnMapSelected(LocalMapMeta mapMeta)
//     {
//         mapSelectionPanel?.SetActive(false);
//         spatialMapManager.LoadMap(mapMeta);
//         ShowStatusMessage($"正在加载地图: {mapMeta.Map.Name}", 2f);
//         UpdateUIState();
//     }

//     private void LoadFirstAvailableMap()
//     {
//         var availableMaps = spatialMapManager.GetAvailableMaps();
//         if (availableMaps.Count > 0)
//         {
//             spatialMapManager.LoadMap(availableMaps[0]);
//             UpdateUIState();
//         }
//         else
//         {
//             ShowStatusMessage("没有可用的地图，请先创建并保存地图", 3f);
//         }
//     }

//     private void OnSaveMapWithObjectsClicked()
//     {
//         if (EasyARSpatialMapEditorManager.Instance == null)
//         {
//             ShowStatusMessage("保存失败：编辑管理器不存在", 3f);
//             return;
//         }
//         EasyARSpatialMapEditorManager.Instance.SaveObjectsInfo();
//         OnSaveMapClicked();
//     }

//     public void ChangeDebugInfo()
//     {
//         debugArea.SetActive(!debugArea.activeSelf);
//     }

// }