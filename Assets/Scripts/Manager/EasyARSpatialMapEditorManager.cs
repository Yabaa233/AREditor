using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using easyar;
using SpatialMap_SparseSpatialMap;
using TouchController = MyEasyAR.TouchController;
using UI.AR;
using Assets.Scripts.DebugTools;

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

        [Header("Debug Settings")]
        public bool showColliderDebug = false; // 显示碰撞体调试信息

        [Header("Mesh Alignment")]
        public GameObject denseMeshPrefab; // 精细mesh预制体（在Inspector中指定）
        public bool showMeshInEditMode = false; // 编辑模式下是否显示mesh（物理碰撞始终存在）
        private bool isMeshAlignmentMode = false; // 是否处于mesh对齐模式
        private GameObject currentAlignedMeshInstance; // 当前实例化的mesh对象
        private SavedMeshTransform savedMeshTransform; // 保存的mesh变换信息

        // 当前地图会话 - 现在可以直接使用 EasyAR 示例中的类型
        private MapSession currentMapSession;
        private List<MapMeta> availableMaps = new List<MapMeta>();

        // 编辑器状态
        public bool isMapLocalized = false;
        private bool isMapBuilding = false;
        private bool isEditMode = false;
        private bool isPlayMode = false;

        // 播放模式相关变量
        private GameObject playerCollider; // AR相机上的玩家碰撞体
        private List<EventActionHandler> activeEventHandlers = new List<EventActionHandler>();

        // 事件
        public event Action OnMapLocalized;
        public event Action OnMapBuildingStarted;
        public event Action OnMapBuildingCompleted;
        public event Action<GameObject> OnObjectPlaced;
        public event Action<GameObject> OnObjectRemoved;

        public bool IsMapLocalized => isMapLocalized;
        public bool IsMapBuilding => isMapBuilding;
        public bool IsEditMode => isEditMode;
        public bool IsPlayMode => isPlayMode;
        public bool IsMeshAlignmentMode => isMeshAlignmentMode;
        public MapSession CurrentMapSession => currentMapSession;

        // 游戏结果事件
        public event Action OnGameWinEvent;
        public event Action OnGameLoseEvent;

        // 新增：用于跟踪当前选中的对象，避免多个对象同时响应手势
        public ARPlacedObject currentSelectedObject;

        // 选中框可视化
        private GameObject selectionBox;

        // 基于 EasyAR 样例的集中手势控制系统
        private TouchController touchController;
        private bool isDragging = false;

        // 长按检测相关变量
        private bool isPointerDown = false;
        private float pointerDownTime = 0f;
        private Vector2 pointerDownPosition;
        private const float LONG_PRESS_DURATION = 0.8f; // 长按时间阈值
        private const float MOVE_THRESHOLD = 50f; // 移动阈值，防止手指移动时误触发长按
        private bool hasTriggeredLongPress = false;

        private GameObject easyarObject;
        private ARSession arSession;  // 私有变量，与官方示例一致
        private Camera arCamera;      // 缓存AR相机引用
        [Header("EasyAR Session Object")]
        public GameObject EasyARSession;

        /// <summary>
        /// 保存的mesh变换信息
        /// </summary>
        private class SavedMeshTransform
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;

            public SavedMeshTransform(Transform transform)
            {
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }
        }

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

            // 处理长按检测
            HandleLongPressDetection();

            // 在播放模式下更新碰撞体调试可视化
            if (isPlayMode && showColliderDebug)
            {
                UpdateColliderDebugVisualization();
            }

            // 确保mesh可见性符合当前状态（使用Renderer控制视觉，保持物理碰撞）
            if (currentAlignedMeshInstance != null)
            {
                // 对齐模式下一定可见，编辑模式下根据showMeshInEditMode控制
                bool shouldBeVisuallyVisible = isMeshAlignmentMode || (isEditMode && showMeshInEditMode);

                // 控制所有Renderer的可见性
                Renderer[] renderers = currentAlignedMeshInstance.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.enabled != shouldBeVisuallyVisible)
                    {
                        renderer.enabled = shouldBeVisuallyVisible;
                    }
                }

                // GameObject始终保持激活状态以保证碰撞体工作
                if (!currentAlignedMeshInstance.activeSelf)
                {
                    currentAlignedMeshInstance.SetActive(true);
                }
            }
        }

        /// <summary>
        /// 处理对象选择逻辑（基于 EasyAR 样例）
        /// </summary>
        private void HandleObjectSelection()
        {
            // 参考官方示例的检查方式
            if (!isEditMode || arSession == null || arSession.Assembly == null || !arSession.Assembly.Camera)
                return;

            // 原有的选择逻辑现在由HandleLongPressDetection处理
            // 这里保留用于其他需要的选择处理
        }

        /// <summary>
        /// 处理长按检测逻辑
        /// </summary>
        private void HandleLongPressDetection()
        {
            // 检查基本条件
            if (!isEditMode || arSession == null || arSession.Assembly == null || !arSession.Assembly.Camera)
                return;

            // 检查是否在设置目标模式
            var eventSystem = AREventSystemManager.Instance;
            if (eventSystem != null && eventSystem.IsSelectingTarget())
            {
                // 在设置目标模式下，只处理目标选择
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    Vector2 inputPosition = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;

                    // 检查是否点击了UI
                    if (UnityEngine.EventSystems.EventSystem.current != null &&
                        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    {
                        return;
                    }

                    // 处理目标选择
                    eventSystem.HandleTargetSelectionInput();
                }
                return;
            }

            // 获取输入位置
            Vector2 currentInputPosition = Vector2.zero;
            bool hasInput = false;

            if (Input.touchCount > 0)
            {
                currentInputPosition = Input.GetTouch(0).position;
                hasInput = true;
            }
            else if (Application.isEditor)
            {
                currentInputPosition = Input.mousePosition;
                hasInput = true;
            }

            if (!hasInput) return;

            // 处理输入开始
            if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) ||
                (Application.isEditor && Input.GetMouseButtonDown(0)))
            {
                // 检查是否点击了UI
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                isPointerDown = true;
                pointerDownTime = Time.time;
                pointerDownPosition = currentInputPosition;
                hasTriggeredLongPress = false;
            }
            // 处理输入结束
            else if ((Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled)) ||
                     (Application.isEditor && Input.GetMouseButtonUp(0)))
            {
                if (isPointerDown && !hasTriggeredLongPress)
                {
                    // 短按 - 处理选中物体
                    ProcessShortPress(pointerDownPosition);
                }

                isPointerDown = false;
                hasTriggeredLongPress = false;
            }
            // 处理长按检测
            else if (isPointerDown && !hasTriggeredLongPress)
            {
                // 检查是否移动太多（防误触）
                float moveDistance = Vector2.Distance(currentInputPosition, pointerDownPosition);
                if (moveDistance > MOVE_THRESHOLD)
                {
                    isPointerDown = false;
                    return;
                }

                // 检查是否达到长按时间
                if (Time.time - pointerDownTime >= LONG_PRESS_DURATION)
                {
                    hasTriggeredLongPress = true;
                    ProcessLongPress(pointerDownPosition);
                }
            }
        }

        /// <summary>
        /// 处理短按（选中物体）
        /// </summary>
        private void ProcessShortPress(Vector2 screenPosition)
        {
            Debug.Log($"[EasyAR] ProcessShortPress 被调用，屏幕位置: {screenPosition}");

            var camera = arSession.Assembly.Camera;
            Ray ray = camera.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            Debug.Log($"[EasyAR] 射线检测开始，相机: {camera.name}");

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log($"[EasyAR] 射线命中: {hit.collider.name}");

                var placedObject = hit.collider.GetComponent<ARPlacedObject>();
                if (placedObject != null)
                {
                    // 短按 - 选中物体（原有逻辑）
                    SelectObject(placedObject);
                    Debug.Log($"[EasyAR] 选中物体: {placedObject.name}");
                    return;
                }
                else
                {
                    Debug.Log($"[EasyAR] 命中的对象没有ARPlacedObject组件");
                }
            }
            else
            {
                Debug.Log("[EasyAR] 射线没有命中任何对象");
            }

            // 点击空白区域 - 取消选中
            DeselectAllObjects();
            Debug.Log("[EasyAR] 取消选中所有对象");
        }

        /// <summary>
        /// 处理长按（打开事件Inspector）
        /// </summary>
        private void ProcessLongPress(Vector2 screenPosition)
        {
            var camera = arSession.Assembly.Camera;
            Ray ray = camera.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                var placedObject = hit.collider.GetComponent<ARPlacedObject>();
                if (placedObject != null)
                {
                    // 长按 - 打开事件Inspector
                    OpenARObjectInspector(placedObject);
                    Debug.Log($"[EasyAR] 长按打开Inspector: {placedObject.name}");
                    return;
                }
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
                    // 打开AR对象Inspector
                    OpenARObjectInspector(placedObject);

                    SelectObject(placedObject);
                    return;
                }
            }

            // 没有点击到对象，取消选择
            DeselectAllObjects();
        }

        /// <summary>
        /// 打开AR对象Inspector
        /// </summary>
        private void OpenARObjectInspector(ARPlacedObject placedObject)
        {
            // 设置当前选中的对象
            currentSelectedObject = placedObject;

            // 查找UI控制器并调用其OpenARObjectInspector方法
            //var uiController = FindObjectOfType<EasyARUIManager>();
            if (EasyARUIManager.Instance != null)
            {
                EasyARUIManager.Instance.OpenARObjectInspector();
                Debug.Log($"[EasyAR] 已通过UI控制器打开AR对象Inspector: {placedObject.name}");
            }
            else
            {
                Debug.LogWarning("[EasyAR] 未找到EasyARUIController组件");
            }
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
                // 只有配置了mesh才启用向下投影功能
                bool enableProjection = (currentAlignedMeshInstance != null && savedMeshTransform != null);

                touchController.TurnOn(
                    obj.transform,
                    arCamera,
                    true,  // 单指拖动
                    true,  // 双指移动
                    true,  // 双指缩放
                    true,  // 双指旋转
                    false,  // 只有mesh配置后才启用投影
                    enableProjection ? GroundProjectionRaycast : null // 只有启用投影时才传回调
                );
                Debug.Log($"[EasyAR] 选中对象: {obj.name} (投影: {(enableProjection ? "启用" : "禁用")})");
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
        /// 应用选择视觉反馈 - 使用边框显示
        /// </summary>
        private void ApplySelectionVisual(ARPlacedObject obj, bool selected)
        {
            if (selected)
            {
                // 创建选中框
                CreateSelectionBox(obj.gameObject);
            }
            else
            {
                // 移除选中框
                DestroySelectionBox();
            }
        }

        /// <summary>
        /// 创建选中框 - 基于 BoxCollider
        /// </summary>
        private void CreateSelectionBox(GameObject target)
        {
            // 先销毁旧的选中框
            DestroySelectionBox();

            // 检查是否有 ARPlacedObject
            ARPlacedObject arPlaced = target.GetComponent<ARPlacedObject>();
            if (arPlaced == null)
            {
                Debug.LogWarning($"[EasyAR] 目标对象 {target.name} 没有 ARPlacedObject 组件");
            }

            // 获取 BoxCollider（与 ARPlacedObject 在同一个 GameObject 上）
            BoxCollider boxCollider = target.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                Debug.LogWarning($"[EasyAR] 目标对象 {target.name} 没有 BoxCollider，无法创建选中框");
                return;
            }

            // 从 BoxCollider 获取尺寸和中心（本地坐标）
            Vector3 size = boxCollider.size;
            Vector3 center = boxCollider.center;

            Debug.Log($"[EasyAR] 目标对象: {target.name}");
            Debug.Log($"[EasyAR] BoxCollider 本地尺寸: {size}");
            Debug.Log($"[EasyAR] BoxCollider 本地中心: {center}");
            Debug.Log($"[EasyAR] GameObject 缩放: {target.transform.localScale}");

            // 创建选中框容器
            selectionBox = new GameObject("SelectionBox");
            selectionBox.transform.SetParent(target.transform, false);
            selectionBox.transform.localPosition = center;
            selectionBox.transform.localRotation = Quaternion.identity;
            selectionBox.transform.localScale = Vector3.one; // 确保选中框不受父物体缩放影响

            // 创建线框的12条边（尺寸已经是本地坐标系的尺寸）
            CreateWireframeCube(selectionBox, size, Color.yellow, 0.01f);

            Debug.Log($"[EasyAR] 选中框创建完成");
        }

        /// <summary>
        /// 销毁选中框
        /// </summary>
        private void DestroySelectionBox()
        {
            if (selectionBox != null)
            {
                Destroy(selectionBox);
                selectionBox = null;
            }
        }

        /// <summary>
        /// 创建线框立方体（12条边）
        /// </summary>
        private void CreateWireframeCube(GameObject parent, Vector3 size, Color color, float lineWidth)
        {
            Vector3 halfSize = size * 0.5f;

            // 定义立方体的8个顶点
            Vector3[] vertices = new Vector3[8]
            {
                new Vector3(-halfSize.x, -halfSize.y, -halfSize.z), // 0: 左下后
                new Vector3(halfSize.x, -halfSize.y, -halfSize.z),  // 1: 右下后
                new Vector3(halfSize.x, halfSize.y, -halfSize.z),   // 2: 右上后
                new Vector3(-halfSize.x, halfSize.y, -halfSize.z),  // 3: 左上后
                new Vector3(-halfSize.x, -halfSize.y, halfSize.z),  // 4: 左下前
                new Vector3(halfSize.x, -halfSize.y, halfSize.z),   // 5: 右下前
                new Vector3(halfSize.x, halfSize.y, halfSize.z),    // 6: 右上前
                new Vector3(-halfSize.x, halfSize.y, halfSize.z)    // 7: 左上前
            };

            // 定义立方体的12条边（每条边由两个顶点索引定义）
            int[,] edges = new int[12, 2]
            {
                {0, 1}, {1, 2}, {2, 3}, {3, 0}, // 后面4条边
                {4, 5}, {5, 6}, {6, 7}, {7, 4}, // 前面4条边
                {0, 4}, {1, 5}, {2, 6}, {3, 7}  // 连接前后的4条边
            };

            // 为每条边创建LineRenderer
            for (int i = 0; i < 12; i++)
            {
                GameObject lineObj = new GameObject($"Edge_{i}");
                lineObj.transform.SetParent(parent.transform, false);
                lineObj.transform.localPosition = Vector3.zero;

                LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
                lineRenderer.positionCount = 2;
                lineRenderer.useWorldSpace = false;

                // 设置材质和颜色
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;

                // 设置边的两个端点
                int v1 = edges[i, 0];
                int v2 = edges[i, 1];
                lineRenderer.SetPosition(0, vertices[v1]);
                lineRenderer.SetPosition(1, vertices[v2]);
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
        /// 向下投影射线检测 - 优先检测mesh，fallback到点云
        /// fromPosition: TouchController传入的目标位置（世界坐标）
        /// </summary>
        private easyar.Optional<Vector3> GroundProjectionRaycast(Vector3 fromPosition)
        {
            if (currentMapSession == null || !isMapLocalized)
            {
                return new easyar.Optional<Vector3>();
            }

            // 优先检测mesh（如果mesh存在且已配置）
            if (currentAlignedMeshInstance != null && savedMeshTransform != null)
            {
                // 从fromPosition上方2米向下射线，保持XZ坐标
                Vector3 rayOrigin = new Vector3(fromPosition.x, fromPosition.y + 2f, fromPosition.z);
                Vector3 rayDirection = Vector3.down;
                float maxDistance = 10f; // 向下检测10米

                RaycastHit meshHit;
                if (Physics.Raycast(rayOrigin, rayDirection, out meshHit, maxDistance))
                {
                    // 验证是否命中mesh
                    if (meshHit.collider.gameObject == currentAlignedMeshInstance ||
                        meshHit.collider.transform.IsChildOf(currentAlignedMeshInstance.transform))
                    {
                        // meshHit.point是世界坐标，直接返回（物体和mesh都在同一个MapController下）
                        Vector3 worldPoint = meshHit.point;
                        float yDiff = worldPoint.y - fromPosition.y;

                        if (Mathf.Abs(yDiff) > 0.01f)
                        {
                            string arrow = yDiff > 0 ? "↑" : "↓";
                            string color = yDiff > 0 ? "yellow" : "cyan";
                            Debug.Log($"<color={color}>[Mesh投影] {arrow} {Mathf.Abs(yDiff):F3}m: Y {fromPosition.y:F3} → {worldPoint.y:F3}</color>");
                        }

                        return easyar.Optional<Vector3>.CreateSome(worldPoint);
                    }
                }
            }

            // Fallback到点云检测
            if (arCamera != null)
            {
                Vector3 screenPos = arCamera.WorldToScreenPoint(fromPosition);
                Vector2 normalizedScreenPos = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
                var hitResult = currentMapSession.HitTestOne(normalizedScreenPos);

                if (hitResult.OnSome)
                {
                    Debug.Log($"<color=magenta>[点云投影] Y {fromPosition.y:F3} → {hitResult.Value.y:F3}</color>");
                    return hitResult;
                }
            }

            // 所有投影都失败，返回空（TouchController会保持原位置）
            return new easyar.Optional<Vector3>();
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

                    // 检查是否是AR对象，如果是则保存扩展信息
                    var arPlacedObject = prop.GetComponent<ARPlacedObject>();
                    if (arPlacedObject != null)
                    {
                        // 转换事件类型：从 TriggerActionEventData 转换为 MapMeta.TriggerActionEventData
                        var convertedEvents = new List<MapMeta.TriggerActionEventData>();
                        if (arPlacedObject.runtimeData.events != null)
                        {
                            foreach (var eventData in arPlacedObject.runtimeData.events)
                            {
                                convertedEvents.Add(new MapMeta.TriggerActionEventData()
                                {
                                    triggerType = (MapMeta.TriggerType)eventData.triggerType,
                                    actionType = (MapMeta.ActionType)eventData.actionType,
                                    targetObjectID = eventData.targetObjectID
                                });
                            }
                        }
                        propInfos.Add(new MapMeta.PropInfo()
                        {
                            Name = prop.name,
                            Position = new float[3] { position.x, position.y, position.z },
                            Rotation = new float[4] { rotation.x, rotation.y, rotation.z, rotation.w },
                            Scale = new float[3] { scale.x, scale.y, scale.z },
                            Events = convertedEvents,
                            IfHiddenAtGameStart = arPlacedObject.runtimeData.ifHiddenAtGameStart,
                            ObjectID = arPlacedObject.runtimeData.ID
                        });
                    }
                    else
                    {
                        // 普通对象使用基础PropInfo
                        propInfos.Add(new MapMeta.PropInfo()
                        {
                            Name = prop.name,
                            Position = new float[3] { position.x, position.y, position.z },
                            Rotation = new float[4] { rotation.x, rotation.y, rotation.z, rotation.w },
                            Scale = new float[3] { scale.x, scale.y, scale.z },
                            Events = new List<MapMeta.TriggerActionEventData>(),
                            IfHiddenAtGameStart = false,
                            ObjectID = string.Empty
                        });
                    }
                }
            }

            mapData.Meta.Props = propInfos;

            // 保存mesh对齐信息
            if (currentAlignedMeshInstance != null && savedMeshTransform != null && denseMeshPrefab != null)
            {
                mapData.Meta.MeshAlignment = new MapMeta.MeshAlignmentInfo
                {
                    MeshPrefabName = denseMeshPrefab.name,
                    Position = new float[3] { savedMeshTransform.position.x, savedMeshTransform.position.y, savedMeshTransform.position.z },
                    Rotation = new float[4] { savedMeshTransform.rotation.x, savedMeshTransform.rotation.y, savedMeshTransform.rotation.z, savedMeshTransform.rotation.w },
                    Scale = new float[3] { savedMeshTransform.scale.x, savedMeshTransform.scale.y, savedMeshTransform.scale.z }
                };
                // Debug.Log($"[EasyAR] 保存mesh对齐信息到MapMeta - Prefab: {denseMeshPrefab.name}, Pos: [{savedMeshTransform.position.x}, {savedMeshTransform.position.y}, {savedMeshTransform.position.z}], Rot: [{savedMeshTransform.rotation.x}, {savedMeshTransform.rotation.y}, {savedMeshTransform.rotation.z}, {savedMeshTransform.rotation.w}], Scale: [{savedMeshTransform.scale.x}, {savedMeshTransform.scale.y}, {savedMeshTransform.scale.z}]");
            }
            else
            {
                mapData.Meta.MeshAlignment = null;
                Debug.Log($"[EasyAR] 未保存mesh对齐信息 - currentAlignedMeshInstance: {currentAlignedMeshInstance != null}, savedMeshTransform: {savedMeshTransform != null}, denseMeshPrefab: {denseMeshPrefab != null}");
            }

            MapMetaManager.Save(mapData.Meta);
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

            // 确保mesh在编辑模式下根据showMeshInEditMode控制可见性
            if (currentAlignedMeshInstance != null && !isMeshAlignmentMode)
            {
                SetMeshVisualVisibility(showMeshInEditMode);
            }

            // 通知AR事件系统更新连接线显示
            if (AREventSystemManager.Instance != null)
            {
                AREventSystemManager.Instance.OnModeChanged();

                // 延迟刷新连接线，确保编辑模式状态已完全更新
                StartCoroutine(DelayedRefreshConnections());
            }
        }

        /// <summary>
        /// 延迟刷新连接线
        /// </summary>
        private System.Collections.IEnumerator DelayedRefreshConnections()
        {
            // 等待几帧，确保编辑模式状态完全更新
            yield return null;
            yield return null;

            if (AREventSystemManager.Instance != null && isEditMode)
            {
                AREventSystemManager.Instance.RefreshAllConnections();
                Debug.Log("[EasyAR Spatial Map Editor] 进入编辑模式后刷新连接线");
            }
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

            // 通知AR事件系统更新连接线显示
            if (AREventSystemManager.Instance != null)
            {
                AREventSystemManager.Instance.OnModeChanged();
            }

            // 退出编辑模式时自动保存对象信息
            SaveObjectsInfo();
        }

        /// <summary>
        /// 进入播放模式
        /// </summary>
        public void EnterPlayMode()
        {
            if (!isMapLocalized)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 地图未本地化，无法进入播放模式");
                return;
            }

            // 先退出编辑模式
            if (isEditMode)
            {
                ExitEditMode();
            }

            isPlayMode = true;
            Debug.Log("[EasyAR Spatial Map Editor] 进入播放模式");

            // 确保mesh在播放模式下视觉隐藏
            if (currentAlignedMeshInstance != null)
            {
                SetMeshVisualVisibility(false);
            }

            // 1. 为所有AR对象生成事件逻辑
            GenerateEventLogic();

            // 2. 在AR相机位置创建玩家碰撞体
            CreatePlayerCollider();

            // 3. 隐藏连接线
            if (AREventSystemManager.Instance != null)
            {
                AREventSystemManager.Instance.OnModeChanged();
            }

            // 4. 处理游戏开始时隐藏的对象
            ProcessGameStartVisibility();

            // 5. 隐藏点云
            HidePointCloud();

            // 6. 设置碰撞体调试可视化
            SetupColliderDebugVisualization();

            Debug.Log("[EasyAR Spatial Map Editor] 播放模式已激活");
        }

        /// <summary>
        /// 退出播放模式
        /// </summary>
        public void ExitPlayMode()
        {
            if (!isPlayMode)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 当前不在播放模式");
                return;
            }

            isPlayMode = false;
            Debug.Log("[EasyAR Spatial Map Editor] 退出播放模式");

            // 1. 销毁玩家碰撞体
            DestroyPlayerCollider();

            // 2. 清理事件处理器
            ClearEventLogic();

            // 3. 恢复所有对象的可见性
            RestoreObjectVisibility();

            // 4. 恢复点云显示
            ShowPointCloud();

            // 4.5. 确保mesh在退出播放模式后仍然视觉隐藏
            if (currentAlignedMeshInstance != null)
            {
                SetMeshVisualVisibility(false);
            }

            // 5. 清理碰撞体调试可视化
            ClearColliderDebugVisualization();

            // 6. 通知AR事件系统更新连接线显示
            if (AREventSystemManager.Instance != null)
            {
                AREventSystemManager.Instance.OnModeChanged();
            }

            if (EasyARUIManager.Instance != null)
            {
                EasyARUIManager.Instance.CloseGamePlay();
            }

            Debug.Log("[EasyAR Spatial Map Editor] 已退出播放模式");
        }

        /// <summary>
        /// 基于当前编辑器关卡数据生成事件逻辑
        /// </summary>
        private void GenerateEventLogic()
        {
            Debug.Log("[EasyAR Spatial Map Editor] 开始生成事件逻辑");

            if (currentMapSession == null || currentMapSession.Maps.Count == 0)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 没有可用的地图数据");
                return;
            }

            var mapData = currentMapSession.Maps[0];
            int eventCount = 0;

            foreach (var obj in mapData.Props)
            {
                if (obj == null) continue;

                var arPlacedObject = obj.GetComponent<ARPlacedObject>();
                if (arPlacedObject?.runtimeData?.events == null || arPlacedObject.runtimeData.events.Count == 0)
                    continue;

                // 为每个对象创建一个事件处理器，包含该对象的所有事件
                CreateEventHandlerForObject(obj, arPlacedObject.runtimeData.events);
                eventCount += arPlacedObject.runtimeData.events.Count;
            }

            Debug.Log($"[EasyAR Spatial Map Editor] 事件逻辑生成完成，共 {eventCount} 个事件");
        }

        /// <summary>
        /// 为单个对象创建事件处理器
        /// </summary>
        private void CreateEventHandlerForObject(GameObject obj, List<TriggerActionEventData> events)
        {
            // 确保对象有碰撞体
            var collider = obj.GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                DestroyImmediate(collider);
                collider = null;
            }

            if (collider == null)
            {
                collider = obj.AddComponent<BoxCollider>();
            }
            collider.isTrigger = true;

            // 移除现有的事件处理器（如果有）
            var existingHandler = obj.GetComponent<EventActionHandler>();
            if (existingHandler != null)
            {
                DestroyImmediate(existingHandler);
            }

            // 创建新的事件处理器
            var handler = obj.AddComponent<EventActionHandler>();
            handler.eventList = new List<TriggerActionEventData>(events); // 复制所有事件
            activeEventHandlers.Add(handler);

            // 注册触发类型
            bool hasOnEnter = false;
            bool hasOnExit = false;
            foreach (var evt in events)
            {
                if (evt.triggerType == TriggerType.OnEnter) hasOnEnter = true;
                if (evt.triggerType == TriggerType.OnExit) hasOnExit = true;
            }

            if (hasOnEnter) handler.Register(true);
            if (hasOnExit) handler.Register(false);

            Debug.Log($"[EasyAR Spatial Map Editor] 为对象 {obj.name} 创建事件处理器，包含 {events.Count} 个事件");
        }

        /// <summary>
        /// 为对象添加触发器处理器
        /// </summary>
        private void AddTriggerHandler(GameObject source, TriggerActionEventData evt, bool onEnter)
        {
            // 确保对象有碰撞体
            var collider = source.GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                // 如果有非触发器碰撞体，先移除
                DestroyImmediate(collider);
                collider = null;
            }

            if (collider == null)
            {
                collider = source.AddComponent<BoxCollider>();
            }
            collider.isTrigger = true;

            // 添加或获取事件处理器
            var handler = source.GetComponent<EventActionHandler>();
            if (handler == null)
            {
                handler = source.AddComponent<EventActionHandler>();
                handler.eventList = new List<TriggerActionEventData>();
                activeEventHandlers.Add(handler);
            }

            // 重新创建事件列表（清空现有事件，确保数据一致性）
            if (handler.eventList == null)
            {
                handler.eventList = new List<TriggerActionEventData>();
            }

            // 添加当前事件
            if (!handler.eventList.Contains(evt))
            {
                handler.eventList.Add(evt);
            }

            // 注册触发类型
            handler.Register(onEnter);

            Debug.Log($"[EasyAR Spatial Map Editor] 为对象 {source.name} 添加 {(onEnter ? "OnEnter" : "OnExit")} 事件处理器");
        }

        /// <summary>
        /// 在AR相机位置创建玩家碰撞体
        /// </summary>
        private void CreatePlayerCollider()
        {
            if (arCamera == null)
            {
                Debug.LogError("[EasyAR Spatial Map Editor] AR相机未就绪，无法创建玩家碰撞体");
                return;
            }

            // 创建玩家碰撞体对象
            playerCollider = new GameObject("Player");
            playerCollider.tag = "Player";

            // 设置为AR相机的子对象
            playerCollider.transform.SetParent(arCamera.transform);
            playerCollider.transform.localPosition = Vector3.zero;
            playerCollider.transform.localRotation = Quaternion.identity;

            // 添加球形碰撞体（代表玩家的检测范围）
            var sphereCollider = playerCollider.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.2f; // 50cm的检测半径

            // 添加刚体以确保物理检测正常工作
            var rigidbody = playerCollider.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            Debug.Log("[EasyAR Spatial Map Editor] 玩家碰撞体已创建");
        }

        /// <summary>
        /// 销毁玩家碰撞体
        /// </summary>
        private void DestroyPlayerCollider()
        {
            if (playerCollider != null)
            {
                Destroy(playerCollider);
                playerCollider = null;
                Debug.Log("[EasyAR Spatial Map Editor] 玩家碰撞体已销毁");
            }
        }

        /// <summary>
        /// 清理事件逻辑
        /// </summary>
        private void ClearEventLogic()
        {
            Debug.Log("[EasyAR Spatial Map Editor] 开始清理事件逻辑");

            // 清理所有事件处理器
            foreach (var handler in activeEventHandlers)
            {
                if (handler != null)
                {
                    Destroy(handler);
                }
            }
            activeEventHandlers.Clear();

            // 恢复所有对象的碰撞体设置
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                var mapData = currentMapSession.Maps[0];
                foreach (var obj in mapData.Props)
                {
                    if (obj == null) continue;

                    // 移除触发器碰撞体，恢复为普通碰撞体（用于编辑模式的选择）
                    var collider = obj.GetComponent<Collider>();
                    if (collider != null && collider.isTrigger)
                    {
                        DestroyImmediate(collider);
                        // 重新添加普通碰撞体
                        var boxCollider = obj.AddComponent<BoxCollider>();
                        boxCollider.isTrigger = false;
                    }
                }
            }

            Debug.Log("[EasyAR Spatial Map Editor] 事件逻辑清理完成");
        }

        /// <summary>
        /// 处理游戏开始时的对象可见性
        /// </summary>
        private void ProcessGameStartVisibility()
        {
            if (currentMapSession == null || currentMapSession.Maps.Count == 0)
                return;

            var mapData = currentMapSession.Maps[0];
            int hiddenCount = 0;

            foreach (var obj in mapData.Props)
            {
                if (obj == null) continue;

                var arPlacedObject = obj.GetComponent<ARPlacedObject>();
                if (arPlacedObject?.runtimeData != null && arPlacedObject.runtimeData.ifHiddenAtGameStart)
                {
                    obj.SetActive(false);
                    hiddenCount++;
                    Debug.Log($"[EasyAR Spatial Map Editor] 隐藏对象: {obj.name}");
                }
            }

            Debug.Log($"[EasyAR Spatial Map Editor] 游戏开始时隐藏了 {hiddenCount} 个对象");
        }

        /// <summary>
        /// 恢复所有对象的可见性
        /// </summary>
        private void RestoreObjectVisibility()
        {
            if (currentMapSession == null || currentMapSession.Maps.Count == 0)
                return;

            var mapData = currentMapSession.Maps[0];
            int restoredCount = 0;

            foreach (var obj in mapData.Props)
            {
                if (obj == null) continue;

                if (!obj.activeInHierarchy)
                {
                    obj.SetActive(true);
                    restoredCount++;
                }
            }

            Debug.Log($"[EasyAR Spatial Map Editor] 恢复了 {restoredCount} 个对象的可见性");
        }

        /// <summary>
        /// 设置碰撞体调试可视化
        /// 只为Player和ARPlacedObject显示碰撞框
        /// </summary>
        private void SetupColliderDebugVisualization()
        {
            if (!showColliderDebug) return;

            Debug.Log("[EasyAR Spatial Map Editor] 启用碰撞体调试可视化");

            // 为玩家碰撞体添加可视化
            if (playerCollider != null)
            {
                AddColliderVisualizer(playerCollider, Color.green);
            }

            // 只为ARPlacedObject添加可视化
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                var mapData = currentMapSession.Maps[0];
                foreach (var obj in mapData.Props)
                {
                    if (obj == null) continue;

                    // 检查是否是ARPlacedObject
                    var arPlacedObject = obj.GetComponent<ARPlacedObject>();
                    if (arPlacedObject != null)
                    {
                        var collider = obj.GetComponent<Collider>();
                        if (collider != null)
                        {
                            // 根据是否有事件处理器决定颜色
                            var handler = obj.GetComponent<EventActionHandler>();
                            Color color = (handler != null && handler.eventList.Count > 0) ? Color.red : Color.cyan;
                            AddColliderVisualizer(obj, color);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 清理碰撞体调试可视化
        /// </summary>
        private void ClearColliderDebugVisualization()
        {
            Debug.Log("[EasyAR Spatial Map Editor] 清理碰撞体调试可视化");

            // 移除玩家碰撞体上的可视化组件
            if (playerCollider != null)
            {
                var visualizer = playerCollider.GetComponent<ColliderVisualizer>();
                if (visualizer != null)
                    Destroy(visualizer);
            }

            // 移除所有场景对象上的可视化组件
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                var mapData = currentMapSession.Maps[0];
                foreach (var obj in mapData.Props)
                {
                    if (obj == null) continue;

                    var visualizer = obj.GetComponent<ColliderVisualizer>();
                    if (visualizer != null)
                        Destroy(visualizer);
                }
            }
        }

        /// <summary>
        /// 为对象添加碰撞体可视化组件
        /// </summary>
        private void AddColliderVisualizer(GameObject obj, Color color)
        {
            if (obj == null) return;

            var collider = obj.GetComponent<Collider>();
            if (collider == null) return;

            // 检查是否已有可视化组件
            var existingVisualizer = obj.GetComponent<ColliderVisualizer>();
            if (existingVisualizer != null)
            {
                existingVisualizer.wireframeColor = color;
                return;
            }

            // 添加可视化组件
            var visualizer = obj.AddComponent<ColliderVisualizer>();
            visualizer.wireframeColor = color;
            visualizer.lineWidth = 0.01f; // 调细线宽

            Debug.Log($"[EasyAR Spatial Map Editor] 为 {obj.name} 添加了碰撞体可视化组件");
        }

        /// <summary>
        /// 切换碰撞体调试可视化（无参数版本）
        /// </summary>
        public void ToggleColliderDebugVisualization()
        {
            showColliderDebug = !showColliderDebug;

            if (isPlayMode)
            {
                if (showColliderDebug)
                {
                    SetupColliderDebugVisualization();
                }
                else
                {
                    ClearColliderDebugVisualization();
                }
            }

            Debug.Log($"[EasyAR Spatial Map Editor] 碰撞体调试可视化: {(showColliderDebug ? "开启" : "关闭")}");
        }

        /// <summary>
        /// 设置碰撞体调试可视化（用于Toggle的OnValueChanged）
        /// </summary>
        /// <param name="isOn">Toggle的开关状态</param>
        public void SetColliderDebugVisualization(bool isOn)
        {
            Debug.Log($"[EasyAR Spatial Map Editor] SetColliderDebugVisualization called with isOn={isOn}");

            showColliderDebug = isOn;

            if (isPlayMode)
            {
                if (showColliderDebug)
                {
                    SetupColliderDebugVisualization();
                }
                else
                {
                    ClearColliderDebugVisualization();
                }
            }

            Debug.Log($"[EasyAR Spatial Map Editor] 碰撞体调试可视化: {(showColliderDebug ? "开启" : "关闭")}");
        }

        /// <summary>
        /// 实时更新碰撞体调试可视化
        /// </summary>
        private void UpdateColliderDebugVisualization()
        {
            // 这个方法在 Update 中被调用，用于实时更新可视化
            // 目前可视化对象已经设置为父子关系，会自动跟随移动
            // 如果需要额外的实时更新逻辑，可以在这里添加
        }

        /// <summary>
        /// 隐藏点云
        /// </summary>
        private void HidePointCloud()
        {
            if (currentMapSession == null || currentMapSession.Maps.Count == 0)
                return;

            foreach (var mapData in currentMapSession.Maps)
            {
                if (mapData.Controller != null)
                {
                    mapData.Controller.ShowPointCloud = false;
                    Debug.Log($"[EasyAR Spatial Map Editor] 隐藏地图点云: {mapData.Meta.Map.Name}");
                }
            }
        }

        /// <summary>
        /// 显示点云
        /// </summary>
        private void ShowPointCloud()
        {
            if (currentMapSession == null || currentMapSession.Maps.Count == 0)
                return;

            foreach (var mapData in currentMapSession.Maps)
            {
                if (mapData.Controller != null)
                {
                    mapData.Controller.ShowPointCloud = showPointCloud;
                    Debug.Log($"[EasyAR Spatial Map Editor] 恢复地图点云显示: {mapData.Meta.Map.Name}");
                }
            }
        }

        /// <summary>
        /// 游戏胜利
        /// </summary>
        public void OnGameWin()
        {
            // if (!isPlayMode)
            // {
            //     Debug.LogWarning("[EasyAR Spatial Map Editor] 不在播放模式，无法触发游戏胜利");
            //     return;
            // }

            // Debug.Log("[EasyAR Spatial Map Editor] 游戏胜利！");

            // // 触发胜利事件
            // OnGameWinEvent?.Invoke();

            // 显示胜利提示并退出播放模式
            StartCoroutine(ShowGameResultAndExitPlayMode("Game Win!", Color.green));
        }

        /// <summary>
        /// 游戏失败
        /// </summary>
        public void OnGameLose()
        {
            // if (!isPlayMode)
            // {
            //     Debug.LogWarning("[EasyAR Spatial Map Editor] 不在播放模式，无法触发游戏失败");
            //     return;
            // }

            // Debug.Log("[EasyAR Spatial Map Editor] 游戏失败！");

            // // 触发失败事件
            // OnGameLoseEvent?.Invoke();

            // 显示失败提示并退出播放模式
            StartCoroutine(ShowGameResultAndExitPlayMode("Game Lose!", Color.red));
        }

        /// <summary>
        /// 显示游戏结果并退出播放模式
        /// </summary>
        private System.Collections.IEnumerator ShowGameResultAndExitPlayMode(string message, Color color)
        {
            // 创建UI显示游戏结果
            GameObject resultUI = CreateGameResultUI(message, color);

            // 显示2秒
            yield return new WaitForSeconds(2f);

            // 销毁UI
            if (resultUI != null)
            {
                Destroy(resultUI);
            }

            // 退出播放模式，返回编辑模式
            ExitPlayMode();

            // 如果之前在编辑模式，重新进入编辑模式
            if (isMapLocalized)
            {
                EnterEditMode();
            }

            Debug.Log("[EasyAR Spatial Map Editor] 已返回编辑状态");
        }

        /// <summary>
        /// 创建游戏结果UI
        /// </summary>
        private GameObject CreateGameResultUI(string message, Color color)
        {
            // 创建Canvas
            GameObject canvasGO = new GameObject("GameResultCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // 确保在最上层

            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 创建背景面板
            GameObject panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(canvasGO.transform, false);

            UnityEngine.UI.Image panelImage = panelGO.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f); // 半透明黑色背景

            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // 创建文字
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(panelGO.transform, false);

            UnityEngine.UI.Text text = textGO.AddComponent<UnityEngine.UI.Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 60;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            Debug.Log($"[EasyAR Spatial Map Editor] 创建游戏结果UI: {message}");

            return canvasGO;
        }

        /// <summary>
        /// 获取指定ID的游戏对象（供EventActionHandler使用）
        /// </summary>
        public GameObject GetGameObjectByID(string objectID)
        {
            if (currentMapSession == null || currentMapSession.Maps.Count == 0)
                return null;

            var mapData = currentMapSession.Maps[0];
            foreach (var obj in mapData.Props)
            {
                if (obj == null) continue;

                var arPlacedObject = obj.GetComponent<ARPlacedObject>();
                if (arPlacedObject?.runtimeData?.ID == objectID)
                {
                    return obj;
                }
            }

            Debug.LogWarning($"[EasyAR Spatial Map Editor] 未找到ID为 {objectID} 的对象");
            return null;
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

            // 确保销毁选中框
            DestroySelectionBox();
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

                    // 不在这里刷新连线，等待进入编辑模式时再刷新
                    if (AREventSystemManager.Instance != null)
                    {
                        AREventSystemManager.Instance.OnModeChanged();
                        Debug.Log("[EasyAR Spatial Map Editor] 通知AR事件系统地图已本地化");
                    }
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
                    var arPlacedObject = restoredObject.GetComponent<ARPlacedObject>();
                    if (arPlacedObject == null)
                    {
                        arPlacedObject = restoredObject.AddComponent<ARPlacedObject>();
                    }

                    if (restoredObject.GetComponent<Collider>() == null)
                    {
                        restoredObject.AddComponent<BoxCollider>();
                    }

                    // 转换事件类型：从 MapMeta.TriggerActionEventData 转换为 TriggerActionEventData
                    var convertedEvents = new List<TriggerActionEventData>();
                    if (propInfo.Events != null)
                    {
                        foreach (var eventData in propInfo.Events)
                        {
                            convertedEvents.Add(new TriggerActionEventData()
                            {
                                triggerType = (TriggerType)eventData.triggerType,
                                actionType = (ActionType)eventData.actionType,
                                targetObjectID = eventData.targetObjectID
                            });
                            Debug.Log($"[EasyAR Spatial Map Editor] 恢复事件: TriggerType={eventData.triggerType}, ActionType={eventData.actionType}, TargetID={eventData.targetObjectID}");
                        }
                    }

                    // 设置事件数据
                    arPlacedObject.runtimeData.events = convertedEvents;
                    arPlacedObject.runtimeData.ifHiddenAtGameStart = propInfo.IfHiddenAtGameStart;
                    arPlacedObject.runtimeData.ID = (propInfo.ObjectID == string.Empty) ? EditorManager.Instance.GenerateUniqueID() : propInfo.ObjectID;
                    arPlacedObject.initialized = true;


                    // 设置模板ID
                    arPlacedObject.runtimeData.templateID = template.templateID;

                    // 注册到地图数据
                    mapData.Props.Add(restoredObject);

                    Debug.Log($"[EasyAR Spatial Map Editor] 恢复对象: {propInfo.ObjectID} at {position}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[EasyAR Spatial Map Editor] 恢复对象 {propInfo.Name} 失败: {ex.Message}");
                }
            }

            Debug.Log("[EasyAR Spatial Map Editor] 对象恢复完成");

            // 恢复mesh对齐信息（如果有）
            if (mapData.Meta.MeshAlignment != null && denseMeshPrefab != null)
            {
                Debug.Log($"[EasyAR Spatial Map Editor] 发现mesh对齐信息 - Prefab: {mapData.Meta.MeshAlignment.MeshPrefabName}, Pos: [{mapData.Meta.MeshAlignment.Position[0]}, {mapData.Meta.MeshAlignment.Position[1]}, {mapData.Meta.MeshAlignment.Position[2]}]");

                // 验证prefab名称是否匹配
                if (denseMeshPrefab.name == mapData.Meta.MeshAlignment.MeshPrefabName)
                {
                    // 实例化mesh
                    currentAlignedMeshInstance = Instantiate(denseMeshPrefab);
                    currentAlignedMeshInstance.name = "AlignedMesh_" + denseMeshPrefab.name;

                    // 挂载到地图控制器下
                    currentAlignedMeshInstance.transform.SetParent(mapData.Controller.transform, false);

                    // 恢复变换
                    var meshInfo = mapData.Meta.MeshAlignment;
                    currentAlignedMeshInstance.transform.localPosition = new Vector3(meshInfo.Position[0], meshInfo.Position[1], meshInfo.Position[2]);
                    currentAlignedMeshInstance.transform.localRotation = new Quaternion(meshInfo.Rotation[0], meshInfo.Rotation[1], meshInfo.Rotation[2], meshInfo.Rotation[3]);
                    currentAlignedMeshInstance.transform.localScale = new Vector3(meshInfo.Scale[0], meshInfo.Scale[1], meshInfo.Scale[2]);

                    Debug.Log($"[EasyAR] 已恢复mesh变换 - localPos: {currentAlignedMeshInstance.transform.localPosition}, localRot: {currentAlignedMeshInstance.transform.localRotation.eulerAngles}, localScale: {currentAlignedMeshInstance.transform.localScale}");

                    // 保存到savedMeshTransform
                    savedMeshTransform = new SavedMeshTransform(currentAlignedMeshInstance.transform);
                    Debug.Log($"[EasyAR] 已保存到savedMeshTransform: pos={savedMeshTransform.position}, rot={savedMeshTransform.rotation.eulerAngles}, scale={savedMeshTransform.scale}");

                    // 确保mesh碰撞体存在（用于射线检测）
                    if (currentAlignedMeshInstance.GetComponent<Collider>() == null)
                    {
                        var meshCollider = currentAlignedMeshInstance.AddComponent<MeshCollider>();
                        meshCollider.convex = false;
                        Debug.Log("[EasyAR] 为恢复的mesh添加MeshCollider");
                    }

                    // 立即视觉隐藏mesh（仅在对齐模式下可见）
                    SetMeshVisualVisibility(false);

                    Debug.Log("[EasyAR Spatial Map Editor] Mesh恢复完成并隐藏");
                }
                else
                {
                    Debug.LogWarning($"[EasyAR] Mesh prefab名称不匹配：保存的是{mapData.Meta.MeshAlignment.MeshPrefabName}，当前是{denseMeshPrefab.name}");
                }
            }
            else
            {
                Debug.Log($"[EasyAR] 无mesh对齐信息 - MeshAlignment: {mapData.Meta.MeshAlignment != null}, denseMeshPrefab: {denseMeshPrefab != null}");
            }

            // 对象恢复完成后，通知AR事件系统但不立即刷新连线
            // 连线刷新将在进入编辑模式时进行
            if (AREventSystemManager.Instance != null)
            {
                AREventSystemManager.Instance.OnModeChanged();
                Debug.Log("[EasyAR Spatial Map Editor] 通知AR事件系统对象已恢复");
            }
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

            // 清理mesh实例
            if (currentAlignedMeshInstance != null)
            {
                Destroy(currentAlignedMeshInstance);
                currentAlignedMeshInstance = null;
                Debug.Log("[EasyAR] 销毁mesh实例");
            }
            savedMeshTransform = null;
            isMeshAlignmentMode = false;

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
            isPlayMode = false;

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

        #region Mesh Alignment Methods

        /// <summary>
        /// 公开方法：显示或隐藏mesh（供外部调用）
        /// </summary>
        public void ShowMesh(bool visible)
        {
            Debug.Log($"[EasyAR] ShowMesh({visible}) - currentAlignedMeshInstance: {(currentAlignedMeshInstance != null ? currentAlignedMeshInstance.name : "null")}");

            if (currentAlignedMeshInstance == null)
            {
                Debug.LogWarning("[EasyAR] ShowMesh失败：currentAlignedMeshInstance为null，mesh可能未配置或未恢复");
                return;
            }

            SetMeshVisualVisibility(visible);
        }

        /// <summary>
        /// 从2D编辑器的JSON文件加载物体到mesh下，然后转换到稀疏点云空间
        /// </summary>
        /// <param name="fileName">JSON文件名</param>
        /// <returns>是否加载成功</returns>
        public bool LoadObjectsFromJsonToMesh(string fileName)
        {
            // 前置条件检查
            if (!isMapLocalized)
            {
                Debug.LogWarning("[EasyAR] 地图未本地化，无法加载2D关卡数据");
                return false;
            }

            if (currentAlignedMeshInstance == null)
            {
                Debug.LogWarning("[EasyAR] Mesh未配置，无法加载2D关卡数据。请先完成Mesh对齐配置。");
                return false;
            }

            if (currentMapSession == null || currentMapSession.Maps.Count == 0)
            {
                Debug.LogWarning("[EasyAR] 当前没有有效的地图会话");
                return false;
            }

            // 获取templateDB
            var templateDB = EditorManager.Instance?.templateDB;
            if (templateDB == null)
            {
                Debug.LogError("[EasyAR] 模板数据库未找到，无法加载物体");
                return false;
            }

            // 读取JSON文件
            string fullPath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogError($"[EasyAR] 关卡文件不存在: {fullPath}");
                return false;
            }

            string json = System.IO.File.ReadAllText(fullPath);
            SceneSaveData sceneData = JsonUtility.FromJson<SceneSaveData>(json);

            if (sceneData == null || sceneData.objects == null || sceneData.objects.Count == 0)
            {
                Debug.LogWarning($"[EasyAR] 关卡文件为空或无效: {fileName}");
                return false;
            }

            // 清除已有AR物体
            ClearAllObjects();

            var mapData = currentMapSession.Maps[0];
            var mapController = mapData.Controller;
            int loadedCount = 0;

            // 查找带有LevelParent tag的子物体（这是2D编辑器中物体实际挂载的父节点）
            Transform levelParent = null;
            foreach (Transform child in currentAlignedMeshInstance.GetComponentsInChildren<Transform>(true))
            {
                if (child.CompareTag("LevelParent"))
                {
                    levelParent = child;
                    break;
                }
            }

            if (levelParent == null)
            {
                Debug.LogWarning("[EasyAR] 未找到LevelParent子物体，使用mesh根节点作为父节点");
                levelParent = currentAlignedMeshInstance.transform;
            }

            Debug.Log($"[EasyAR] 开始从JSON加载 {sceneData.objects.Count} 个物体到mesh下");
            Debug.Log($"[EasyAR] LevelParent: {levelParent.name}, 世界位置: {levelParent.position}, 世界旋转: {levelParent.eulerAngles}");

            foreach (var data in sceneData.objects)
            {
                try
                {
                    // 根据templateID查找模板
                    var template = templateDB.GetTemplateByID(data.templateID);
                    if (template == null || template.ARPrefab == null)
                    {
                        Debug.LogWarning($"[EasyAR] 找不到templateID为 {data.templateID} 的模板，跳过此物体");
                        continue;
                    }

                    // 实例化ARPrefab
                    GameObject obj = Instantiate(template.ARPrefab);
                    obj.name = template.ARPrefab.name;

                    Debug.Log($"[EasyAR] 加载物体 {obj.name} - JSON坐标: pos={data.position}, rot={data.rotation}, scale={data.scale}");

                    // Step 1: 先将物体设为LevelParent的子物体，应用存储的local坐标
                    obj.transform.SetParent(levelParent, worldPositionStays: false);
                    obj.transform.localPosition = data.position;
                    obj.transform.localRotation = Quaternion.Euler(data.rotation);
                    obj.transform.localScale = data.scale;

                    Debug.Log($"[EasyAR] Step1后（作为LevelParent子物体）- 本地位置: {obj.transform.localPosition}, 世界位置: {obj.transform.position}");

                    // Step 2: 转换到MapController下（保持世界坐标不变）
                    obj.transform.SetParent(mapController.transform, worldPositionStays: true);

                    Debug.Log($"[EasyAR] Step2后（转到MapController）- 本地位置: {obj.transform.localPosition}, 世界位置: {obj.transform.position}");

                    // Step 3: 重置scale为Vector3.one
                    obj.transform.localScale = Vector3.one;

                    // 确保有ARPlacedObject组件
                    var arPlacedObject = obj.GetComponent<ARPlacedObject>();
                    if (arPlacedObject == null)
                    {
                        arPlacedObject = obj.AddComponent<ARPlacedObject>();
                    }

                    // 确保有Collider组件
                    if (obj.GetComponent<Collider>() == null)
                    {
                        obj.AddComponent<BoxCollider>();
                    }

                    // 从JSON恢复完整的runtimeData
                    arPlacedObject.runtimeData = new PlacedObjectData
                    {
                        templateID = data.templateID,
                        ID = string.IsNullOrEmpty(data.ID) ? EditorManager.Instance.GenerateUniqueID() : data.ID,
                        ifHiddenAtGameStart = data.ifHiddenAtGameStart,
                        position = obj.transform.localPosition, // 使用转换后的位置
                        rotation = obj.transform.localEulerAngles,
                        scale = obj.transform.localScale,
                        events = data.events != null ? new List<TriggerActionEventData>(data.events) : new List<TriggerActionEventData>()
                    };
                    arPlacedObject.initialized = true;

                    // 注册到地图会话
                    mapData.Props.Add(obj);

                    // 编辑模式下始终显示物体
                    obj.SetActive(true);

                    loadedCount++;
                    Debug.Log($"[EasyAR] 加载物体: {obj.name}, ID: {arPlacedObject.runtimeData.ID}, 世界位置: {obj.transform.position}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[EasyAR] 加载物体失败: {ex.Message}");
                }
            }

            Debug.Log($"[EasyAR] 从JSON加载完成，共加载 {loadedCount}/{sceneData.objects.Count} 个物体");

            // 保存对象信息到MapMeta
            if (autoSaveOnEdit)
            {
                SaveObjectsInfo();
            }

            // 通知AR事件系统更新
            if (AREventSystemManager.Instance != null)
            {
                AREventSystemManager.Instance.OnModeChanged();
            }

            return loadedCount > 0;
        }

        /// <summary>
        /// 自动加载最新的JSON关卡文件到mesh，然后转换到稀疏点云空间
        /// </summary>
        public void LoadLatestJsonToMesh()
        {
            // 前置条件检查
            if (!isMapLocalized)
            {
                Debug.LogWarning("[EasyAR] 地图未本地化，无法加载2D关卡数据");
                return;
            }

            if (currentAlignedMeshInstance == null)
            {
                Debug.LogWarning("[EasyAR] Mesh未配置，无法加载2D关卡数据");
                return;
            }

            // 扫描JSON文件
            string savePath = Application.persistentDataPath;
            if (!System.IO.Directory.Exists(savePath))
            {
                Debug.LogWarning("[EasyAR] 保存路径不存在");
                return;
            }

            string[] jsonFiles = System.IO.Directory.GetFiles(savePath, "*.json");
            if (jsonFiles.Length == 0)
            {
                Debug.LogWarning("[EasyAR] 没有找到任何JSON关卡文件");
                return;
            }

            // 按修改时间排序，获取最新文件
            var latestFile = jsonFiles
                .Select(f => new System.IO.FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (latestFile == null)
            {
                Debug.LogWarning("[EasyAR] 无法获取最新的JSON文件");
                return;
            }

            string fileName = latestFile.Name;
            Debug.Log($"[EasyAR] 自动加载最新关卡: {fileName}, 修改时间: {latestFile.LastWriteTime}");

            // 调用现有的加载方法
            LoadObjectsFromJsonToMesh(fileName);
        }

        /// <summary>
        /// 设置mesh的视觉可见性（使用Renderer控制，保持物理碰撞）
        /// </summary>
        public void SetMeshVisualVisibility(bool visible)
        {
            if (currentAlignedMeshInstance == null) return;

            Debug.Log($"[EasyAR] SetMeshVisualVisibility({visible}) - mesh位置: {currentAlignedMeshInstance.transform.position}, 缩放: {currentAlignedMeshInstance.transform.lossyScale}, 激活状态: {currentAlignedMeshInstance.activeSelf}");

            // 首先确保mesh及其所有子物体都是激活的
            currentAlignedMeshInstance.SetActive(true);
            foreach (Transform child in currentAlignedMeshInstance.GetComponentsInChildren<Transform>(true))
            {
                if (!child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(true);
                    Debug.Log($"[EasyAR] 激活子物体: {child.name}");
                }
            }

            Renderer[] renderers = currentAlignedMeshInstance.GetComponentsInChildren<Renderer>(true); // 包含非激活的子物体
            Debug.Log($"[EasyAR] 找到 {renderers.Length} 个Renderer");

            foreach (var renderer in renderers)
            {
                Debug.Log($"[EasyAR] Renderer: {renderer.gameObject.name}, 当前enabled: {renderer.enabled}, GameObject激活: {renderer.gameObject.activeInHierarchy}");
                renderer.enabled = visible;
            }

            Debug.Log($"[EasyAR] SetMeshVisualVisibility完成，mesh应该{(visible ? "可见" : "隐藏")}");
        }

        /// <summary>
        /// 开始Mesh对齐模式
        /// </summary>
        public void StartMeshAlignment()
        {
            if (!isMapLocalized)
            {
                Debug.LogWarning("[EasyAR] 地图未本地化，无法开始mesh对齐");
                return;
            }

            if (denseMeshPrefab == null)
            {
                Debug.LogWarning("[EasyAR] denseMeshPrefab未指定，无法开始mesh对齐");
                return;
            }

            if (isMeshAlignmentMode)
            {
                Debug.LogWarning("[EasyAR] 已经处于mesh对齐模式");
                return;
            }

            Debug.Log($"[EasyAR] 开始Mesh对齐模式 - savedMeshTransform存在: {savedMeshTransform != null}");
            if (savedMeshTransform != null)
            {
                Debug.Log($"[EasyAR] savedMeshTransform值: pos={savedMeshTransform.position}, rot={savedMeshTransform.rotation.eulerAngles}, scale={savedMeshTransform.scale}");
            }

            // 如果已有实例，先销毁
            if (currentAlignedMeshInstance != null)
            {
                Debug.Log("[EasyAR] 销毁现有mesh实例");
                Destroy(currentAlignedMeshInstance);
            }

            // 实例化mesh
            currentAlignedMeshInstance = Instantiate(denseMeshPrefab);
            currentAlignedMeshInstance.name = "AlignedMesh_" + denseMeshPrefab.name;

            // 挂载到地图控制器下
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                var mapData = currentMapSession.Maps[0];
                currentAlignedMeshInstance.transform.SetParent(mapData.Controller.transform, false);
            }

            // 设置初始位置（如果有保存的变换则使用，否则放在屏幕中心）
            if (savedMeshTransform != null)
            {
                Debug.Log($"[EasyAR] 使用保存的mesh变换 - pos: {savedMeshTransform.position}, rot: {savedMeshTransform.rotation.eulerAngles}, scale: {savedMeshTransform.scale}");
                currentAlignedMeshInstance.transform.localPosition = savedMeshTransform.position;
                currentAlignedMeshInstance.transform.localRotation = savedMeshTransform.rotation;
                currentAlignedMeshInstance.transform.localScale = savedMeshTransform.scale;
                Debug.Log($"[EasyAR] Mesh变换已应用 - localPos: {currentAlignedMeshInstance.transform.localPosition}, localRot: {currentAlignedMeshInstance.transform.localRotation.eulerAngles}, localScale: {currentAlignedMeshInstance.transform.localScale}");
            }
            else
            {
                Debug.Log("[EasyAR] ⚠️ savedMeshTransform为null，使用默认位置");
                // 放在相机前方3米处
                if (arCamera != null)
                {
                    Vector3 spawnPos = arCamera.transform.position + arCamera.transform.forward * 3f;
                    currentAlignedMeshInstance.transform.position = spawnPos;
                    currentAlignedMeshInstance.transform.rotation = Quaternion.identity;
                    currentAlignedMeshInstance.transform.localScale = Vector3.one;
                    Debug.Log($"[EasyAR] 在相机前方生成mesh: {spawnPos}");
                }
            }

            // 确保mesh有碰撞体（用于射线检测）
            if (currentAlignedMeshInstance.GetComponent<Collider>() == null)
            {
                var meshCollider = currentAlignedMeshInstance.AddComponent<MeshCollider>();
                meshCollider.convex = false;
                Debug.Log("[EasyAR] 为mesh添加MeshCollider");
            }

            // 显示mesh
            currentAlignedMeshInstance.SetActive(true);

            // 进入对齐模式
            isMeshAlignmentMode = true;

            // 启用TouchController（将mesh作为可操作对象）
            if (touchController != null && arCamera != null)
            {
                StartCoroutine(EnableMeshTouchControllerNextFrame());
            }

            Debug.Log("[EasyAR] Mesh对齐模式已启动");
        }

        /// <summary>
        /// 延迟启用mesh的TouchController
        /// </summary>
        private System.Collections.IEnumerator EnableMeshTouchControllerNextFrame()
        {
            yield return null;

            if (touchController != null && arCamera != null && currentAlignedMeshInstance != null)
            {
                touchController.TurnOn(
                    currentAlignedMeshInstance.transform,
                    arCamera,
                    true,  // 单指拖动
                    true,  // 双指移动
                    true,  // 双指缩放
                    true,  // 双指旋转
                    false, // 不启用向下投影
                    null   // 无投影回调
                );
                Debug.Log("[EasyAR] 启用mesh TouchController");
            }
        }

        /// <summary>
        /// 完成Mesh对齐（保存）
        /// </summary>
        public void FinalizeMeshAlignment()
        {
            if (!isMeshAlignmentMode)
            {
                Debug.LogWarning("[EasyAR] 当前不在mesh对齐模式");
                return;
            }

            if (currentAlignedMeshInstance == null)
            {
                Debug.LogWarning("[EasyAR] mesh实例不存在");
                return;
            }

            Debug.Log("[EasyAR] 完成Mesh对齐");

            // 保存变换信息
            Debug.Log($"[EasyAR] 当前mesh transform - localPos: {currentAlignedMeshInstance.transform.localPosition}, localRot: {currentAlignedMeshInstance.transform.localRotation.eulerAngles}, localScale: {currentAlignedMeshInstance.transform.localScale}");
            savedMeshTransform = new SavedMeshTransform(currentAlignedMeshInstance.transform);
            Debug.Log($"[EasyAR] 已保存mesh变换到savedMeshTransform: pos={savedMeshTransform.position}, rot={savedMeshTransform.rotation.eulerAngles}, scale={savedMeshTransform.scale}");

            // 视觉隐藏mesh（保持物理碰撞）
            SetMeshVisualVisibility(false);

            // 关闭TouchController
            if (touchController != null)
            {
                touchController.TurnOff();
            }

            // 退出对齐模式
            isMeshAlignmentMode = false;

            // 自动保存到MapMeta
            SaveObjectsInfo();

            Debug.Log("[EasyAR] Mesh对齐已保存并隐藏");
        }

        /// <summary>
        /// 取消Mesh对齐（不保存）
        /// </summary>
        public void CancelMeshAlignment()
        {
            if (!isMeshAlignmentMode)
            {
                Debug.LogWarning("[EasyAR] 当前不在mesh对齐模式");
                return;
            }

            Debug.Log("[EasyAR] 取消Mesh对齐");

            // 关闭TouchController
            if (touchController != null)
            {
                touchController.TurnOff();
            }

            // 销毁mesh实例（不保存）
            if (currentAlignedMeshInstance != null)
            {
                Destroy(currentAlignedMeshInstance);
                currentAlignedMeshInstance = null;
            }

            // 如果有之前保存的变换，恢复实例但保持隐藏
            if (savedMeshTransform != null && denseMeshPrefab != null && currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                currentAlignedMeshInstance = Instantiate(denseMeshPrefab);
                currentAlignedMeshInstance.name = "AlignedMesh_" + denseMeshPrefab.name;
                currentAlignedMeshInstance.transform.SetParent(currentMapSession.Maps[0].Controller.transform, false);
                currentAlignedMeshInstance.transform.localPosition = savedMeshTransform.position;
                currentAlignedMeshInstance.transform.localRotation = savedMeshTransform.rotation;
                currentAlignedMeshInstance.transform.localScale = savedMeshTransform.scale;

                // 确保有碰撞体
                if (currentAlignedMeshInstance.GetComponent<Collider>() == null)
                {
                    var meshCollider = currentAlignedMeshInstance.AddComponent<MeshCollider>();
                    meshCollider.convex = false;
                }

                SetMeshVisualVisibility(false);
                Debug.Log("[EasyAR] 恢复之前保存的mesh变换（视觉隐藏，物理存在）");
            }

            // 退出对齐模式
            isMeshAlignmentMode = false;

            Debug.Log("[EasyAR] Mesh对齐已取消");
        }

        /// <summary>
        /// 退出Mesh对齐模式（cleanup）
        /// </summary>
        public void ExitMeshAlignmentMode()
        {
            if (!isMeshAlignmentMode)
            {
                return;
            }

            Debug.Log("[EasyAR] 退出Mesh对齐模式");

            // 视觉隐藏mesh（保持物理碰撞）
            if (currentAlignedMeshInstance != null)
            {
                SetMeshVisualVisibility(false);
            }

            // 关闭TouchController
            if (touchController != null)
            {
                touchController.TurnOff();
            }

            isMeshAlignmentMode = false;
        }

        #endregion
    }
}