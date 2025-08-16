using System;
using System.Collections.Generic;
using UnityEngine;
using easyar;
using SpatialMap_SparseSpatialMap;

namespace Assets.Scripts.Manager
{
    /// <summary>
    /// EasyAR空间地图编辑器管理器
    /// 专门处理基于稀疏空间地图的AR编辑功能
    /// </summary>
    public class EasyARSpatialMapEditorManager : singleton<EasyARSpatialMapEditorManager>
    {
        [Header("EasyAR Components")]
        public ARSession arSession;
        public SparseSpatialMapWorkerFrameFilter mapWorker;
        public SparseSpatialMapController mapControllerPrefab;

        [Header("Editor Settings")]
        public bool showPointCloud = true;
        public bool autoSaveOnEdit = false;

        // 当前地图会话 - 现在可以直接使用 EasyAR 示例中的类型
        private MapSession currentMapSession;
        private List<MapMeta> availableMaps = new List<MapMeta>();

        // 编辑器状态
        private bool isMapLocalized = false;
        private bool isMapBuilding = false;
        private bool isEditMode = false;

        // 事件
        public event Action OnMapLocalized;
        public event Action OnMapBuildingStarted;
        public event Action OnMapBuildingCompleted;
        public event Action<GameObject> OnObjectPlaced;
        public event Action<GameObject> OnObjectRemoved;

        public bool IsMapLocalized => isMapLocalized;
        public bool IsMapBuilding => isMapBuilding;
        public bool IsEditMode => isEditMode;
        public MapSession CurrentMapSession => currentMapSession;

        // 新增：用于跟踪当前选中的对象，避免多个对象同时响应手势
        private static ARPlacedObject currentSelectedObject;

        // 基于 EasyAR 样例的集中手势控制系统
        private Common.TouchController touchController;
        private bool isDragging = false;

        private void Start()
        {
            InitializeEditor();
            LoadAvailableMaps();
        }

        private void Update()
        {
            // 在建图过程中，确保点云可视化正确显示累积的地图数据
            if (isMapBuilding && currentMapSession != null && currentMapSession.MapWorker.LocalizedMap != null)
            {
                var localizedMap = currentMapSession.MapWorker.LocalizedMap;

                // 确保每个地图控制器的点云显示状态正确
                foreach (var mapData in currentMapSession.Maps)
                {
                    if (mapData.Controller != null)
                    {
                        // 同步显示状态（EasyAR会自动从LocalizedMap获取点云数据）
                        if (mapData.Controller.ShowPointCloud != showPointCloud)
                        {
                            mapData.Controller.ShowPointCloud = showPointCloud;
                        }
                    }
                }
            }

            // 处理对象选择（基于 EasyAR 样例的 Dragger 模式）
            HandleObjectSelection();
        }

        /// <summary>
        /// 处理对象选择逻辑（基于 EasyAR 样例）
        /// </summary>
        private void HandleObjectSelection()
        {
            if (!isEditMode || Camera.main == null)
                return;

            // 处理触摸输入
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    ProcessSelectionInput(touch.position);
                }
            }
            // 编辑器中处理鼠标输入
            else if (Application.isEditor && Input.GetMouseButtonDown(0))
            {
                ProcessSelectionInput(Input.mousePosition);
            }
        }

        /// <summary>
        /// 处理选择输入（射线检测和对象选择）
        /// </summary>
        private void ProcessSelectionInput(Vector2 screenPosition)
        {
            // Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            Camera arCamera = currentMapSession?.ARSession?.Assembly?.Camera;
            if (arCamera == null)
            {
                Debug.LogWarning("[EasyAR] 当前ARSession未找到有效相机，无法进行射线检测");
                DeselectAllObjects();
                return;
            }
            Ray ray = arCamera.ScreenPointToRay(screenPosition);

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // 检查是否点击到 ARPlacedObject
                var placedObject = hit.collider.GetComponent<ARPlacedObject>();
                if (placedObject != null)
                {
                    SelectObject(placedObject);
                    return;
                }
            }

            // 没有点击到对象，取消选择
            DeselectAllObjects();
        }

        /// <summary>
        /// 选择对象并启用手势控制
        /// </summary>
        private void SelectObject(ARPlacedObject obj)
        {
            if (currentSelectedObject == obj)
                return; // 已经选中

            // 取消之前的选择
            if (currentSelectedObject != null)
            {
                DeselectObject(currentSelectedObject);
            }

            // 选择新对象
            currentSelectedObject = obj;

            // 应用视觉反馈
            ApplySelectionVisual(obj, true);

            // 启用 TouchController
            if (touchController != null && Camera.main != null)
            {
                touchController.TurnOn(obj.transform, Camera.main, true, true, true, true);
                Debug.Log($"[EasyAR] 选中对象: {obj.name}");
            }
        }

        /// <summary>
        /// 取消选择对象
        /// </summary>
        private void DeselectObject(ARPlacedObject obj)
        {
            if (obj == null) return;

            // 移除视觉反馈
            ApplySelectionVisual(obj, false);

            // 关闭 TouchController
            if (touchController != null)
            {
                touchController.TurnOff();
                Debug.Log($"[EasyAR] 取消选中: {obj.name}");
            }
        }

        /// <summary>
        /// 应用选择视觉反馈
        /// </summary>
        private void ApplySelectionVisual(ARPlacedObject obj, bool selected)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    // 简单的颜色变化作为选择反馈
                    if (selected)
                    {
                        renderer.material.color = Color.yellow;
                    }
                    else
                    {
                        renderer.material.color = Color.white; // 或原始颜色
                    }
                }
            }
        }

        private void InitializeEditor()
        {
            Debug.Log("[EasyAR Spatial Map Editor] 初始化编辑器");

            // 初始化 TouchController（基于 EasyAR 样例）
            InitializeTouchController();
        }

        /// <summary>
        /// 初始化 TouchController 系统
        /// </summary>
        private void InitializeTouchController()
        {
            // 查找现有的 TouchController 或创建新的
            touchController = FindObjectOfType<Common.TouchController>();
            if (touchController == null)
            {
                var go = new GameObject("TouchController");
                touchController = go.AddComponent<Common.TouchController>();
                Debug.Log("[EasyAR] 创建 TouchController");
            }
            else
            {
                Debug.Log("[EasyAR] 找到现有的 TouchController");
            }
        }

        /// <summary>
        /// 开始构建新地图
        /// </summary>
        public void StartMapBuilding()
        {
            if (currentMapSession != null)
            {
                currentMapSession.Dispose();
            }

            currentMapSession = new MapSession(arSession, mapWorker, null);
            currentMapSession.SetupMapBuilder(mapControllerPrefab);

            // 设置初始的点云显示状态
            if (currentMapSession.Maps.Count > 0 && currentMapSession.Maps[0].Controller != null)
            {
                currentMapSession.Maps[0].Controller.ShowPointCloud = showPointCloud;
            }

            isMapBuilding = true;
            isMapLocalized = false;
            isEditMode = false;

            OnMapBuildingStarted?.Invoke();
            Debug.Log("[EasyAR Spatial Map Editor] 开始构建地图");
        }

        /// <summary>
        /// 加载现有地图
        /// </summary>
        public void LoadMap(MapMeta mapMeta)
        {
            if (currentMapSession != null)
            {
                currentMapSession.Dispose();
            }

            currentMapSession = new MapSession(arSession, mapWorker, new List<MapMeta> { mapMeta });
            currentMapSession.LoadMapMeta(mapControllerPrefab, showPointCloud);

            isMapBuilding = false;
            isMapLocalized = false;
            isEditMode = false;

            // 监听本地化状态
            StartCoroutine(WaitForLocalization());

            Debug.Log($"[EasyAR Spatial Map Editor] 加载地图: {mapMeta.Map.Name}");
        }

        /// <summary>
        /// 保存当前地图
        /// </summary>
        public void SaveCurrentMap()
        {
            if (currentMapSession == null || !isMapBuilding)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 没有可保存的地图");
                return;
            }

            string mapName = $"GameMap_{DateTime.Now:yyyyMMdd_HHmmss}";
            currentMapSession.Save(mapName, null);

            // 保存完成后刷新可用地图列表
            Invoke(nameof(RefreshAvailableMapsAfterSave), 2f);

            Debug.Log($"[EasyAR Spatial Map Editor] 保存地图: {mapName}");
        }

        /// <summary>
        /// 延迟刷新地图列表（保存后调用）
        /// </summary>
        private void RefreshAvailableMapsAfterSave()
        {
            RefreshAvailableMaps();
            Debug.Log("[EasyAR Spatial Map Editor] 保存后自动刷新地图列表");
        }

        /// <summary>
        /// 保存对象信息（与EasyAR示例保持一致）
        /// </summary>
        public void SaveObjectsInfo()
        {
            if (currentMapSession == null || currentMapSession.Maps.Count == 0)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 没有可保存的对象信息");
                return;
            }

            var mapData = currentMapSession.Maps[0];
            var propInfos = new List<MapMeta.PropInfo>();

            foreach (var prop in mapData.Props)
            {
                if (prop != null)
                {
                    var position = prop.transform.localPosition;
                    var rotation = prop.transform.localRotation;
                    var scale = prop.transform.localScale;

                    propInfos.Add(new MapMeta.PropInfo()
                    {
                        Name = prop.name,
                        Position = new float[3] { position.x, position.y, position.z },
                        Rotation = new float[4] { rotation.x, rotation.y, rotation.z, rotation.w },
                        Scale = new float[3] { scale.x, scale.y, scale.z }
                    });
                }
            }

            mapData.Meta.Props = propInfos;
            MapMetaManager.Save(mapData.Meta);

            Debug.Log($"[EasyAR Spatial Map Editor] 保存对象信息: {propInfos.Count} 个对象");
        }

        /// <summary>
        /// 清除当前地图
        /// </summary>
        public void ClearCurrentMap()
        {
            ClearAllObjects();

            if (currentMapSession != null)
            {
                currentMapSession.Dispose();
                currentMapSession = null;
            }

            isMapLocalized = false;
            isMapBuilding = false;
            isEditMode = false;

            Debug.Log("[EasyAR Spatial Map Editor] 清除地图");
        }

        /// <summary>
        /// 进入编辑模式
        /// </summary>
        public void EnterEditMode()
        {
            if (!isMapLocalized)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 地图未本地化，无法进入编辑模式");
                return;
            }

            isEditMode = true;
            Debug.Log("[EasyAR Spatial Map Editor] 进入编辑模式");
        }

        /// <summary>
        /// 退出编辑模式
        /// </summary>
        public void ExitEditMode()
        {
            isEditMode = false;
            Debug.Log("[EasyAR Spatial Map Editor] 退出编辑模式");

            // 退出时取消所有对象的选中状态
            DeselectAllObjects();

            // 退出编辑模式时自动保存对象信息
            SaveObjectsInfo();
        }

        /// <summary>
        /// 在空间地图上放置游戏对象
        /// </summary>
        public bool PlaceGameObjectOnMap(GameObject gameObject, Vector2 screenPosition)
        {
            if (!isMapLocalized || currentMapSession == null || currentMapSession.Maps.Count == 0)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 地图未本地化，无法放置对象");
                return false;
            }

            // 使用EasyAR的碰撞检测
            var hitResult = currentMapSession.HitTestOne(screenPosition);
            if (hitResult.OnSome)
            {
                // 按照EasyAR示例的方式：将对象挂在MapController下
                var mapData = currentMapSession.Maps[0];
                gameObject.transform.parent = mapData.Controller.transform;
                gameObject.transform.localPosition = mapData.Controller.transform.InverseTransformPoint(hitResult.Value);

                // 添加到MapData的Props列表（与示例保持一致）
                mapData.Props.Add(gameObject);

                OnObjectPlaced?.Invoke(gameObject);

                //// 新增：如果对象包含 ARPlacedObject，设置为当前选中对象并启用手势控制
                //try
                //{
                //    var placedComp = gameObject.GetComponent<ARPlacedObject>();
                //    if (placedComp != null)
                //    {
                //        // 首先取消所有其他对象的选择
                //        DeselectAllObjects();

                //        // 设置为当前选中对象
                //        SetCurrentSelectedObject(placedComp);

                //        Debug.Log("[EasyAR] 放置后自动进入编辑模式并设为当前选中对象");
                //    }
                //}
                //catch (System.Exception ex)
                //{
                //    Debug.LogWarning($"[EasyAR] 放置后尝试设置选中状态失败: {ex.Message}");
                //}

                if (autoSaveOnEdit)
                {
                    SaveObjectsInfo();
                }

                Debug.Log($"[EasyAR Spatial Map Editor] 对象已放置: {gameObject.name} at {hitResult.Value}");
                return true;
            }

            Debug.LogWarning("[EasyAR Spatial Map Editor] 未找到有效的放置点");
            return false;
        }

        /// <summary>
        /// 外部调用：设置当前选中的对象
        /// </summary>
        public static void SetCurrentSelectedObject(ARPlacedObject obj)
        {
            var instance = Instance;
            if (instance != null)
            {
                instance.SelectObject(obj);
            }
        }

        /// <summary>
        /// 取消所有对象的选中状态
        /// </summary>
        public void DeselectAllObjects()
        {
            if (currentSelectedObject != null)
            {
                DeselectObject(currentSelectedObject);
                currentSelectedObject = null;
            }
        }

        /// <summary>
        /// 获取地图上的碰撞点
        /// </summary>
        public Optional<Vector3> GetMapHitPoint(Vector2 screenPoint)
        {
            if (!isMapLocalized || currentMapSession == null)
            {
                return Optional<Vector3>.CreateNone();
            }

            return currentMapSession.HitTestOne(screenPoint);
        }

        /// <summary>
        /// 注册对象（与EasyAR示例保持一致）
        /// </summary>
        public void RegisterObject(GameObject obj)
        {
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                var mapData = currentMapSession.Maps[0];
                if (!mapData.Props.Contains(obj))
                {
                    mapData.Props.Add(obj);
                    Debug.Log($"[EasyAR Spatial Map Editor] 注册对象: {obj.name}");
                    if (autoSaveOnEdit)
                    {
                        SaveObjectsInfo();
                    }
                }
            }
        }

        /// <summary>
        /// 注销对象（与EasyAR示例保持一致）
        /// </summary>
        public void UnregisterObject(GameObject obj)
        {
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                var mapData = currentMapSession.Maps[0];
                if (mapData.Props.Contains(obj))
                {
                    mapData.Props.Remove(obj);
                    OnObjectRemoved?.Invoke(obj);
                    Debug.Log($"[EasyAR Spatial Map Editor] 注销对象: {obj.name}");
                    if (autoSaveOnEdit)
                    {
                        SaveObjectsInfo();
                    }
                }
            }
        }

        /// <summary>
        /// 清除所有对象（与EasyAR示例保持一致）
        /// </summary>
        public void ClearAllObjects()
        {
            if (currentMapSession == null)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 无法清除对象：没有地图会话");
                return;
            }

            if (currentMapSession.Maps == null || currentMapSession.Maps.Count == 0)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 无法清除对象：当前会话没有地图数据");
                return;
            }

            var mapData = currentMapSession.Maps[0];
            if (mapData.Props != null && mapData.Props.Count > 0)
            {
                foreach (var obj in new List<GameObject>(mapData.Props)) // 复制一份避免修改时枚举错误
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                mapData.Props.Clear();
                Debug.Log("[EasyAR Spatial Map Editor] 清除所有对象");

                if (autoSaveOnEdit)
                {
                    SaveObjectsInfo();
                }
            }
            else
            {
                Debug.Log("[EasyAR Spatial Map Editor] 没有要清除的对象");
            }
        }

        /// <summary>
        /// 获取所有放置的对象（与EasyAR示例保持一致）
        /// </summary>
        public List<GameObject> GetAllPlacedObjects()
        {
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                return new List<GameObject>(currentMapSession.Maps[0].Props);
            }
            return new List<GameObject>();
        }

        /// <summary>
        /// 根据ID查找对象（与EasyAR示例保持一致）
        /// </summary>
        public GameObject GetObjectByID(string id)
        {
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                var mapData = currentMapSession.Maps[0];
                foreach (var obj in mapData.Props)
                {
                    if (obj != null && obj.name == id)
                    {
                        return obj;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 等待地图本地化
        /// </summary>
        private System.Collections.IEnumerator WaitForLocalization()
        {
            while (!isMapLocalized && currentMapSession != null)
            {
                if (mapWorker.LocalizedMap != null)
                {
                    isMapLocalized = true;
                    OnMapLocalized?.Invoke();
                    OnMapBuildingCompleted?.Invoke();

                    Debug.Log($"[EasyAR Spatial Map Editor] 地图已本地化: {mapWorker.LocalizedMap.MapInfo.Name}");

                    // 地图本地化后，恢复保存的对象
                    RestoreObjectsFromMapMeta();
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>
        /// 从地图元数据恢复对象
        /// </summary>
        private void RestoreObjectsFromMapMeta()
        {
            if (currentMapSession == null || currentMapSession.Maps.Count == 0)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 无法恢复对象：没有地图会话");
                return;
            }

            var mapData = currentMapSession.Maps[0];
            if (mapData.Meta?.Props == null || mapData.Meta.Props.Count == 0)
            {
                Debug.Log("[EasyAR Spatial Map Editor] 没有保存的对象需要恢复");
                return;
            }

            Debug.Log($"[EasyAR Spatial Map Editor] 开始恢复 {mapData.Meta.Props.Count} 个对象");

            var templateDB = EditorManager.Instance?.templateDB;
            if (templateDB == null)
            {
                Debug.LogError("[EasyAR Spatial Map Editor] 模板数据库未找到，无法恢复对象");
                return;
            }

            foreach (var propInfo in mapData.Meta.Props)
            {
                try
                {
                    // 根据名称或其他标识符找到对应的模板
                    // 这里假设使用对象名称来匹配模板
                    var template = FindTemplateByObjectName(templateDB, propInfo.Name);
                    if (template?.ARPrefab == null)
                    {
                        Debug.LogWarning($"[EasyAR Spatial Map Editor] 无法找到对象 {propInfo.Name} 的模板");
                        continue;
                    }

                    // 实例化对象
                    GameObject restoredObject = Instantiate(template.ARPrefab);
                    restoredObject.name = propInfo.Name;

                    // 设置变换
                    var position = new Vector3(propInfo.Position[0], propInfo.Position[1], propInfo.Position[2]);
                    var rotation = new Quaternion(propInfo.Rotation[0], propInfo.Rotation[1], propInfo.Rotation[2], propInfo.Rotation[3]);
                    var scale = new Vector3(propInfo.Scale[0], propInfo.Scale[1], propInfo.Scale[2]);

                    // 将对象挂在MapController下（与放置时保持一致）
                    restoredObject.transform.parent = mapData.Controller.transform;
                    restoredObject.transform.localPosition = position;
                    restoredObject.transform.localRotation = rotation;
                    restoredObject.transform.localScale = scale;

                    // 添加必要的组件
                    if (restoredObject.GetComponent<ARPlacedObject>() == null)
                    {
                        restoredObject.AddComponent<ARPlacedObject>();
                    }

                    if (restoredObject.GetComponent<Collider>() == null)
                    {
                        restoredObject.AddComponent<BoxCollider>();
                    }

                    // 注册到地图数据
                    mapData.Props.Add(restoredObject);

                    Debug.Log($"[EasyAR Spatial Map Editor] 恢复对象: {propInfo.Name} at {position}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[EasyAR Spatial Map Editor] 恢复对象 {propInfo.Name} 失败: {ex.Message}");
                }
            }

            Debug.Log("[EasyAR Spatial Map Editor] 对象恢复完成");
        }

        /// <summary>
        /// 根据对象名称查找模板（简单实现）
        /// </summary>
        private ObjectTemplateData FindTemplateByObjectName(PlacedObjectTemplateDatabase templateDB, string objectName)
        {
            // 简单实现：匹配模板名称或AR预制体名称
            foreach (var template in templateDB.templates)
            {
                if (template != null && template.ARPrefab != null)
                {
                    if (template.templateName == objectName ||
                        template.ARPrefab.name == objectName ||
                        objectName.Contains(template.templateName) ||
                        objectName.Contains(template.ARPrefab.name))
                    {
                        return template;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 加载可用地图列表
        /// </summary>
        private void LoadAvailableMaps()
        {
            availableMaps = MapMetaManager.LoadAll();
            Debug.Log($"[EasyAR Spatial Map Editor] 加载了 {availableMaps.Count} 个地图");
        }

        /// <summary>
        /// 获取可用地图列表
        /// </summary>
        public List<MapMeta> GetAvailableMaps()
        {
            return new List<MapMeta>(availableMaps);
        }

        /// <summary>
        /// 刷新可用地图列表（重新从磁盘加载）
        /// </summary>
        public void RefreshAvailableMaps()
        {
            Debug.Log("[EasyAR Spatial Map Editor] 刷新可用地图列表");
            LoadAvailableMaps();
        }

        /// <summary>
        /// 设置点云显示
        /// </summary>
        public void SetPointCloudVisibility(bool visible)
        {
            showPointCloud = visible;
            if (currentMapSession != null && currentMapSession.MapWorker != null)
            {
                // 更新点云显示状态
                Debug.Log($"[EasyAR Spatial Map Editor] 点云显示: {(visible ? "开启" : "关闭")}");

                // 将状态应用到当前所有地图控制器
                foreach (var mapData in currentMapSession.Maps)
                {
                    if (mapData.Controller != null)
                    {
                        mapData.Controller.ShowPointCloud = visible;
                    }
                }
            }
        }

        /// <summary>
        /// 获取编辑器状态信息
        /// </summary>
        public string GetEditorStatus()
        {
            string status = "";
            status += $"地图构建: {(isMapBuilding ? "进行中" : "未开始")}\n";
            status += $"地图本地化: {(isMapLocalized ? "已完成" : "未完成")}\n";
            status += $"编辑模式: {(isEditMode ? "开启" : "关闭")}\n";

            if (currentMapSession != null && currentMapSession.MapWorker != null)
            {
                var localizedMap = currentMapSession.MapWorker.LocalizedMap;
                if (localizedMap != null)
                {
                    status += $"当前地图: {localizedMap.MapInfo.Name}\n";
                    status += $"点云数量: {localizedMap.PointCloud.Count}";
                }
            }

            return status;
        }

        protected override void OnDestroy()
        {
            if (currentMapSession != null)
            {
                currentMapSession.Dispose();
            }

            ClearAllObjects();

            base.OnDestroy();
        }
    }
}