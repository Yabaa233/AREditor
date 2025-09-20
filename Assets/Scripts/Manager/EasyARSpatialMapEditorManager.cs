using System;
using System.Collections.Generic;
using UnityEngine;
using easyar;
using SpatialMap_SparseSpatialMap;
using TouchController = MyEasyAR.TouchController;

namespace Assets.Scripts.Manager
{
    /// <summary>
    /// EasyAR空间地图编辑器管理器
    /// 专门处理基于稀疏空间地图的AR编辑功能
    /// </summary>
    public class EasyARSpatialMapEditorManager : singleton<EasyARSpatialMapEditorManager>
    {
        [Header("EasyAR Components")]
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
        public ARPlacedObject currentSelectedObject;

        // 基于 EasyAR 样例的集中手势控制系统
        private TouchController touchController;
        private bool isDragging = false;

        private GameObject easyarObject;
        private ARSession arSession;  // 私有变量，与官方示例一致
        private Camera arCamera;      // 缓存AR相机引用
        [Header("EasyAR Session Object")]
        public GameObject EasyARSession;

        private void Start()
        {
            // 在应用启动时就锁定屏幕方向（这是最可靠的方法）
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;

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
            // 参考官方示例的检查方式
            if (!isEditMode || arSession == null || arSession.Assembly == null || !arSession.Assembly.Camera)
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
            // 参考官方示例的安全检查
            if (arSession == null || arSession.Assembly == null || !arSession.Assembly.Camera)
            {
                Debug.LogWarning("[EasyAR] ARSession或Camera未就绪，跳过射线检测");
                return;
            }

            var camera = arSession.Assembly.Camera;
            Ray ray = camera.ScreenPointToRay(screenPosition);

            Debug.Log($"[EasyAR] 射线检测 - 相机: {camera.name}, 屏幕位置: {screenPosition}");

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log($"[EasyAR] 射线命中: {hit.collider.name}");

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

            // 启用 TouchController - 确保相机已就绪
            if (touchController != null && arCamera != null && arCamera.isActiveAndEnabled)
            {
                // 延后到下一帧启动TouchController，确保相机完全就绪
                StartCoroutine(EnableTouchControllerNextFrame(obj));
            }
        }

        private System.Collections.IEnumerator EnableTouchControllerNextFrame(ARPlacedObject obj)
        {
            yield return null; // 等一帧

            // 再次检查相机状态
            if (touchController != null && arCamera != null && arCamera.isActiveAndEnabled && obj != null)
            {
                touchController.TurnOn(obj.transform, arCamera, true, true, true, true);
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
            touchController = FindObjectOfType<TouchController>();
            if (touchController == null)
            {
                var go = new GameObject("TouchController");
                touchController = go.AddComponent<TouchController>();
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
            // 先清理旧会话
            DestroySession();

            // 延迟创建新会话
            Invoke(nameof(DelayedStartBuilding), 0.1f);

            Debug.Log("[EasyAR Spatial Map Editor] 开始构建地图流程");
        }

        private void DelayedStartBuilding()
        {
            // 创建新会话用于构建（传入空列表或 null）
            CreateSession();
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
            Debug.Log("[EasyAR Spatial Map Editor] 地图构建会话创建完成");
        }

        /// <summary>
        /// 按照官方示例的方式重构加载流程
        /// </summary>
        public void LoadMap(MapMeta mapMeta)
        {
            // 1. 先销毁旧会话
            DestroySession();

            // 2. 等一帧再创建新会话（但用Invoke而不是协程）
            Invoke(nameof(DelayedCreateAndLoad), 0.1f);

            // 保存参数供延迟调用使用
            _pendingMapMeta = mapMeta;

            Debug.Log($"[EasyAR] 开始加载地图: {mapMeta.Map.Name}");
        }

        private MapMeta _pendingMapMeta;

        private void DelayedCreateAndLoad()
        {
            if (_pendingMapMeta == null) return;

            // 3. 创建新会话
            CreateSession(new List<MapMeta> { _pendingMapMeta });

            // 4. 直接调用 MapSession 的 LoadMapMeta（官方方式）
            currentMapSession.LoadMapMeta(mapControllerPrefab, showPointCloud);

            // 5. 等待本地化
            StartCoroutine(WaitForLocalization());

            Debug.Log($"[EasyAR] 地图会话创建完成: {_pendingMapMeta.Map.Name}");
            _pendingMapMeta = null;
        }

        /// <summary>
        /// 创建地图会话（完全按照官方示例）
        /// </summary>
        private void CreateSession(List<MapMeta> selectedMaps = null)
        {
            // 完全按照官方示例的方式，不添加任何额外设置
            easyarObject = Instantiate(EasyARSession);
            easyarObject.SetActive(true);
            arSession = easyarObject.GetComponent<ARSession>();
            mapWorker = easyarObject.GetComponentInChildren<SparseSpatialMapWorkerFrameFilter>();

            // ✅ 参考官方示例：延迟获取相机，等待Assembly初始化
            StartCoroutine(InitializeARCamera());

            // 立刻锁一次屏幕方向
            ForceLandscapeLock();

            // 显式重置WorldRoot/AR相机父节点旋转
            ResetWorldRootTransform();

            currentMapSession = new MapSession(arSession, mapWorker, selectedMaps);

            isMapBuilding = false;
            isMapLocalized = false;
            isEditMode = false;

            // 下一帧再锁一次，避免竞态
            StartCoroutine(ReapplyOrientationNextFrame());

            Debug.Log($"[EasyAR] 创建地图会话，地图数量: {selectedMaps?.Count ?? 0}，屏幕方向: {Screen.orientation}");
        }

        private void ResetWorldRootTransform()
        {
            // 简化实现：直接重置AR相机的父节点或easyarObject本身
            Transform root = null;

            if (arCamera != null && arCamera.transform.parent != null)
            {
                root = arCamera.transform.parent;
            }
            else if (easyarObject != null)
            {
                root = easyarObject.transform;
            }

            if (root != null)
            {
                root.localPosition = Vector3.zero;
                root.localRotation = Quaternion.identity;
                root.localScale = Vector3.one;
                Debug.Log("[EasyAR] 重置AR根节点变换");
            }
        }

        private void ForceLandscapeLock()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
        }

        private System.Collections.IEnumerator ReapplyOrientationNextFrame()
        {
            yield return null; // 等一帧
            ForceLandscapeLock();
        }        // 删除这个方法，直接在 LoadMap 中调用 currentMapSession.LoadMapMeta
        // private void LoadMapMeta() 方法已移除，按官方示例直接调用

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
            Debug.Log("[EasyAR] RestoreObjectsFromMapMeta 开始");

            if (currentMapSession == null)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 无法恢复对象：currentMapSession is null");
                return;
            }

            if (currentMapSession.Maps == null)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 无法恢复对象：currentMapSession.Maps is null");
                return;
            }

            if (currentMapSession.Maps.Count == 0)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 无法恢复对象：currentMapSession.Maps.Count == 0");
                return;
            }

            var mapData = currentMapSession.Maps[0];
            if (mapData == null)
            {
                Debug.LogError("[EasyAR Spatial Map Editor] 无法恢复对象：mapData is null");
                return;
            }

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

        /// <summary>
        /// 根据 MapMeta 删除对应的地图和对象数据
        /// </summary>
        public bool DeleteMap(MapMeta mapMeta)
        {
            if (mapMeta?.Map == null)
            {
                Debug.LogError("[EasyAR] 删除失败：地图元数据无效");
                return false;
            }

            string mapID = mapMeta.Map.ID;
            string mapName = mapMeta.Map.Name;

            Debug.Log($"[EasyAR] 开始删除地图: {mapName} (ID: {mapID})");

            try
            {
                // 1. 如果当前正在使用这个地图，先清除
                if (currentMapSession != null && currentMapSession.Maps.Count > 0)
                {
                    var currentMapMeta = currentMapSession.Maps[0].Meta;
                    if (currentMapMeta?.Map?.ID == mapID)
                    {
                        Debug.Log("[EasyAR] 正在删除当前使用的地图，先清除会话");
                        ClearCurrentMap();
                    }
                }

                // 2. 从内存列表中移除
                availableMaps.RemoveAll(m => m?.Map?.ID == mapID);

                // 3. 删除所有相关文件（稀疏地图 + 对象数据）
                bool filesDeleted = DeleteMapFiles(mapID);

                if (filesDeleted)
                {
                    Debug.Log($"[EasyAR] 地图删除成功: {mapName}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[EasyAR] 地图删除部分成功: {mapName}");
                    return true; // 内存已清除就算成功
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EasyAR] 删除地图失败: {mapName}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除地图相关的所有文件（稀疏地图 + 对象数据）
        /// </summary>
        private bool DeleteMapFiles(string mapID)
        {
            string mapFolder = System.IO.Path.Combine(Application.persistentDataPath, "SparseSpatialMap");

            if (!System.IO.Directory.Exists(mapFolder))
            {
                Debug.LogWarning("[EasyAR] 地图文件夹不存在");
                return true;
            }

            bool allDeleted = true;

            try
            {
                // 删除所有以 mapID 开头的文件（包括 .meta, .map, 以及可能的对象数据文件）
                var allFiles = System.IO.Directory.GetFiles(mapFolder, mapID + "*");

                foreach (var file in allFiles)
                {
                    try
                    {
                        System.IO.File.Delete(file);
                        Debug.Log($"[EasyAR] 删除文件: {System.IO.Path.GetFileName(file)}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[EasyAR] 删除文件失败: {file}, 错误: {ex.Message}");
                        allDeleted = false;
                    }
                }

                Debug.Log($"[EasyAR] 删除了 {allFiles.Length} 个文件");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EasyAR] 删除文件时发生错误: {ex.Message}");
                allDeleted = false;
            }

            return allDeleted;
        }

        //TODO 跨状态时记得销毁，即编辑，创建等
        public void DestroySession()
        {
            // 停止所有协程，防止协程冲突
            if (gameObject.activeInHierarchy)
            {
                StopAllCoroutines();
            }

            // 安全清理静态变量
            if (currentSelectedObject != null)
            {
                currentSelectedObject = null;
            }

            if (currentMapSession != null)
            {
                currentMapSession.Dispose();
                currentMapSession = null;
            }

            // 完全按照官方示例：不设置easyarObject = null
            if (easyarObject)
            {
                Destroy(easyarObject);
            }

            // 清理AR相机引用
            arCamera = null;

            // 清理相关状态
            isMapLocalized = false;
            isMapBuilding = false;
            isEditMode = false;

            Debug.Log("[EasyAR] Session 已销毁，状态已重置");
        }

        /// <summary>
        /// 获取AR相机（供UI组件使用）
        /// </summary>
        /// <summary>
        /// 初始化AR相机（参考官方示例的方式）
        /// </summary>
        private System.Collections.IEnumerator InitializeARCamera()
        {
            // 参考官方示例：等待ARSession.Assembly.Camera完全就绪
            while (arSession == null || arSession.Assembly == null || !arSession.Assembly.Camera)
            {
                yield return new WaitForSeconds(0.1f); // 每100ms检查一次

                if (arSession != null)
                {
                    Debug.Log($"[EasyAR] 等待Assembly初始化... Assembly: {arSession.Assembly != null}, Camera: {arSession.Assembly?.Camera != null}");
                }
            }

            // Assembly和Camera都就绪后才获取
            arCamera = arSession.Assembly.Camera;
            Debug.Log($"[EasyAR] AR相机初始化成功: {arCamera.name}");
        }

        public Camera GetARCamera()
        {
            return arCamera;
        }

        /// <summary>
        /// 使用稀疏点云进行射线检测（供拖拽放置使用）
        /// </summary>
        /// <param name="normalizedScreenPosition">归一化的屏幕坐标 (0-1)</param>
        /// <returns>射线击中的世界坐标位置</returns>
        public easyar.Optional<Vector3> HitTestSparsePointCloud(Vector2 normalizedScreenPosition)
        {
            if (currentMapSession == null)
            {
                Debug.LogWarning("[EasyAR] 当前没有活动的地图会话");
                return new easyar.Optional<Vector3>();
            }

            return currentMapSession.HitTestOne(normalizedScreenPosition);
        }

        /// <summary>
        /// 注册已放置的对象到指定位置
        /// </summary>
        /// <param name="obj">要注册的游戏对象</param>
        /// <param name="position">世界坐标位置</param>
        public void RegisterPlacedObjectAtPosition(GameObject obj, Vector3 position)
        {
            if (obj == null) return;

            // 设置对象位置
            obj.transform.position = position;

            // 确保对象有 ARPlacedObject 组件
            var arPlacedObject = obj.GetComponent<ARPlacedObject>();
            if (arPlacedObject == null)
            {
                arPlacedObject = obj.AddComponent<ARPlacedObject>();
            }

            // 设置对象的唯一ID（如果ARPlacedObject有这个字段）
            // arPlacedObject.objectID = System.Guid.NewGuid().ToString();

            // 将对象挂载到地图控制器下
            if (currentMapSession?.Maps?.Count > 0)
            {
                var mapController = currentMapSession.Maps[0].Controller;
                if (mapController != null)
                {
                    obj.transform.SetParent(mapController.transform);
                }
            }

            // 触发对象放置事件
            OnObjectPlaced?.Invoke(obj);

            Debug.Log($"[EasyAR] 注册放置对象: {obj.name} 在位置: {position}");

            // // 如果开启了自动保存，保存地图
            // if (autoSaveOnEdit)
            // {
            //     SaveCurrentMap();
            //     SaveObjectsInfo();
            // }
            RegisterObject(obj);
        }
    }
}