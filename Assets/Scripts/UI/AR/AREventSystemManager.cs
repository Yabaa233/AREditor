using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace UI.AR
{
    /// <summary>
    /// AR事件系统统一管理器
    /// 整合事件管理、连接线可视化、目标选择等所有AR事件系统功能
    /// </summary>
    public class AREventSystemManager : MonoBehaviour
    {
        public static AREventSystemManager Instance { get; private set; }

        [Header("事件系统设置")]
        public bool enableEventProcessing = true;
        public bool debugEventTriggers = true;
        public float objectSearchRadius = 50f;

        [Header("3D连接线设置")]
        public Material lineMaterial;           // 连接线材质
        public GameObject arrowHeadPrefab;      // 箭头预制体
        public float defaultLineWidth = 0.03f;  // 默认线宽
        [Range(0f, 1f)]
        public float connectionOffset = 0.1f;   // 连接线回缩比例 (0-1之间，1表示完全回缩到起点)
        [Range(0f, 1f)]
        public float arrowPosition = 0.1f;      // 箭头在原始连线上的位置比例 (0=目标对象, 1=源对象，不受连接线回缩影响)

        [Header("连接线颜色")]
        public Color enableColor = Color.green;   // 启用事件颜色
        public Color disableColor = Color.red;    // 禁用事件颜色

        [Header("目标选择UI")]
        public GameObject selectionUIPanel; // 包含指导文字的选择UI面板

        // 事件管理
        private Dictionary<string, ARPlacedObject> objectCache = new Dictionary<string, ARPlacedObject>();
        private List<ARPlacedObject> allARObjects = new List<ARPlacedObject>();

        // 连接线管理
        private Dictionary<string, AREventConnection> eventConnections = new Dictionary<string, AREventConnection>();
        private Transform connectionContainer;

        // 目标选择
        private bool isSelecting = false;
        private System.Action<ARPlacedObject> currentCallback;
        private ARPlacedObject[] availableTargets;

        // 事件委托
        public delegate void EventTriggeredDelegate(ARPlacedObject source, ARPlacedObject target, TriggerActionEventData eventData);
        public static event EventTriggeredDelegate OnEventTriggered;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            RefreshAllObjects();
            // 延迟刷新连接线以确保所有对象都已加载
            Invoke("RefreshAllConnections", 0.5f);
        }

        void Update()
        {
            if (isSelecting)
            {
                HandleTargetSelectionInput();
            }
        }

        #region 系统初始化

        private void InitializeSystem()
        {
            InitializeConnectionContainer();
            InitializeTargetSelection();
        }

        private void InitializeConnectionContainer()
        {
            GameObject container = new GameObject("AR_Event_Connections");
            container.transform.SetParent(transform);
            connectionContainer = container.transform;

            if (lineMaterial == null)
            {
                lineMaterial = CreateDefaultLineMaterial();
            }
        }

        private void InitializeTargetSelection()
        {
            // 初始时隐藏选择面板
            if (selectionUIPanel != null)
                selectionUIPanel.SetActive(false);
        }

        private Material CreateDefaultLineMaterial()
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = enableColor;
            material.SetFloat("_Metallic", 0.2f);
            material.SetFloat("_Smoothness", 0.8f);
            return material;
        }

        #endregion

        #region 事件管理

        /// <summary>
        /// 显示事件编辑UI
        /// </summary>
        public void ShowEventEditUI(ARPlacedObject arObject)
        {
            // 查找并使用ARPlacedObjectInspector显示对象检查器
            // 使用GameObject.Find方式避免编译顺序问题
            var inspectorGO = GameObject.Find("ARPlacedObjectInspector");
            if (inspectorGO != null)
            {
                var inspector = inspectorGO.GetComponent<MonoBehaviour>();
                if (inspector != null)
                {
                    // 使用反射调用SetData方法
                    var setDataMethod = inspector.GetType().GetMethod("SetData");
                    if (setDataMethod != null)
                    {
                        setDataMethod.Invoke(inspector, new object[] { arObject });
                    }
                }
            }
            else
            {
                Debug.LogError("[AR Event System] 场景中没有找到ARPlacedObjectInspector");
            }
        }

        /// <summary>
        /// 刷新所有AR对象
        /// </summary>
        public void RefreshAllObjects()
        {
            allARObjects.Clear();
            objectCache.Clear();

            ARPlacedObject[] objects = FindObjectsOfType<ARPlacedObject>();
            foreach (var obj in objects)
            {
                if (obj != null && obj.runtimeData != null)
                {
                    allARObjects.Add(obj);
                    string id = obj.runtimeData.ID;
                    if (!string.IsNullOrEmpty(id))
                    {
                        objectCache[id] = obj;
                    }
                }
            }

            if (debugEventTriggers)
            {
                Debug.Log($"[AR Event System] 已刷新 {allARObjects.Count} 个AR对象");
            }
        }

        /// <summary>
        /// 根据ID查找AR对象
        /// </summary>
        public ARPlacedObject FindObjectById(string objectId)
        {
            if (string.IsNullOrEmpty(objectId))
                return null;

            if (objectCache.ContainsKey(objectId))
                return objectCache[objectId];

            // 如果缓存中没有，重新搜索
            foreach (var obj in allARObjects)
            {
                if (obj != null && obj.runtimeData != null && obj.runtimeData.ID == objectId)
                {
                    objectCache[objectId] = obj;
                    return obj;
                }
            }

            return null;
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public void TriggerEvent(ARPlacedObject source, TriggerActionEventData eventData)
        {
            if (!enableEventProcessing || source == null || eventData == null)
                return;

            ARPlacedObject target = FindObjectById(eventData.targetObjectID);
            if (target == null)
            {
                if (debugEventTriggers)
                {
                    Debug.LogWarning($"[AR Event System] 找不到目标对象: {eventData.targetObjectID}");
                }
                return;
            }

            // 执行动作
            ExecuteAction(target, eventData.actionType);

            // 触发事件
            OnEventTriggered?.Invoke(source, target, eventData);

            if (debugEventTriggers)
            {
                Debug.Log($"[AR Event System] 事件触发: {source.name} -> {target.name} ({eventData.actionType})");
            }
        }

        private void ExecuteAction(ARPlacedObject target, ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Enable:
                    target.gameObject.SetActive(true);
                    break;
                case ActionType.Disable:
                    target.gameObject.SetActive(false);
                    break;
                case ActionType.Win:
                    // 触发胜利逻辑
                    Debug.Log($"[AR Event System] 胜利触发: {target.name}");
                    break;
                case ActionType.Lose:
                    // 触发失败逻辑
                    Debug.Log($"[AR Event System] 失败触发: {target.name}");
                    break;
            }
        }

        #endregion

        #region 连接线管理

        /// <summary>
        /// 刷新所有连接线
        /// </summary>
        public void RefreshAllConnections()
        {
            ClearAllConnections();
            RefreshAllObjects();

            foreach (var sourceObj in allARObjects)
            {
                if (sourceObj?.runtimeData?.events != null)
                {
                    foreach (var eventData in sourceObj.runtimeData.events)
                    {
                        CreateConnection(sourceObj, eventData);
                    }
                }
            }

            if (debugEventTriggers)
            {
                Debug.Log($"[AR Event System] 已刷新 {eventConnections.Count} 条连接线");
            }
        }

        /// <summary>
        /// 创建单个连接线
        /// </summary>
        public void CreateConnection(ARPlacedObject source, TriggerActionEventData eventData)
        {
            // Win和Lose事件不需要目标对象，跳过连接线创建
            if (eventData.actionType == ActionType.Win || eventData.actionType == ActionType.Lose)
            {
                if (debugEventTriggers)
                {
                    Debug.Log($"[AR Event System] {eventData.actionType}事件无需连接线，跳过创建");
                }
                return;
            }

            if (source == null || eventData == null || string.IsNullOrEmpty(eventData.targetObjectID))
                return;

            ARPlacedObject target = FindObjectById(eventData.targetObjectID);
            if (target == null)
                return;

            string connectionId = $"{source.runtimeData.ID}_{eventData.targetObjectID}_{eventData.GetHashCode()}";

            if (eventConnections.ContainsKey(connectionId))
            {
                DestroyConnection(connectionId);
            }

            AREventConnection connection = CreateConnectionObject(source, target, eventData.actionType);
            eventConnections[connectionId] = connection;

            // 详细的连接诊断信息
            Vector3 sourcePos = source.transform.position;
            Vector3 targetPos = target.transform.position;
            Vector3 direction = targetPos - sourcePos;
            float distance = direction.magnitude;

            Debug.Log($"[AR Event System] ===== 连接线创建诊断 =====");
            Debug.Log($"[AR Event System] 源对象: {source.name} ({source.runtimeData.ID})");
            Debug.Log($"[AR Event System] 目标对象: {target.name} ({target.runtimeData.ID})");
            Debug.Log($"[AR Event System] 源位置: {sourcePos}");
            Debug.Log($"[AR Event System] 目标位置: {targetPos}");
            Debug.Log($"[AR Event System] 3D距离: {distance}");
            Debug.Log($"[AR Event System] 方向向量: {direction}");
            Debug.Log($"[AR Event System] 连接回缩比例: {connectionOffset}");
            Debug.Log($"[AR Event System] 事件类型: {eventData.actionType}");
            Debug.Log($"[AR Event System] ==============================");
        }

        private AREventConnection CreateConnectionObject(ARPlacedObject source, ARPlacedObject target, ActionType actionType)
        {
            GameObject connectionObj = new GameObject($"Connection_{source.name}_to_{target.name}");
            connectionObj.transform.SetParent(connectionContainer);

            Debug.Log($"[AR Event System] 创建连接线对象: {connectionObj.name}");
            Debug.Log($"[AR Event System] 源位置: {source.transform.position}, 目标位置: {target.transform.position}");
            Debug.Log($"[AR Event System] 连接线父级: {connectionContainer.name}");

            AREventConnection connection = connectionObj.AddComponent<AREventConnection>();
            connection.Initialize(source.transform, target.transform, GetConnectionColor(actionType), lineMaterial, defaultLineWidth, arrowHeadPrefab, connectionOffset, arrowPosition, actionType);

            return connection;
        }

        private Color GetConnectionColor(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Enable: return enableColor;
                case ActionType.Disable: return disableColor;
                default: return enableColor;
            }
        }

        /// <summary>
        /// 清除所有连接线
        /// </summary>
        public void ClearAllConnections()
        {
            foreach (var connection in eventConnections.Values)
            {
                if (connection != null)
                {
                    DestroyImmediate(connection.gameObject);
                }
            }
            eventConnections.Clear();
        }

        /// <summary>
        /// 更新所有连接线的颜色（当颜色设置改变时调用）
        /// </summary>
        public void UpdateAllConnectionColors()
        {
            foreach (var connection in eventConnections.Values)
            {
                if (connection != null)
                {
                    connection.UpdateColor(GetConnectionColorForConnection(connection));
                }
            }
        }

        private Color GetConnectionColorForConnection(AREventConnection connection)
        {
            // 根据连接线存储的ActionType返回对应颜色
            return GetConnectionColor(connection.ActionType);
        }

        /// <summary>
        /// 删除指定连接线
        /// </summary>
        public void DestroyConnection(string connectionId)
        {
            if (eventConnections.ContainsKey(connectionId))
            {
                if (eventConnections[connectionId] != null)
                {
                    DestroyImmediate(eventConnections[connectionId].gameObject);
                }
                eventConnections.Remove(connectionId);
            }
        }

        #endregion

        #region 目标选择

        /// <summary>
        /// 开始目标选择
        /// </summary>
        public void StartTargetSelection(System.Action<ARPlacedObject> callback)
        {
            if (isSelecting)
            {
                CancelTargetSelection();
            }

            currentCallback = callback;
            isSelecting = true;

            // 显示选择UI面板
            if (selectionUIPanel != null)
            {
                selectionUIPanel.SetActive(true);
            }

            // 刷新可用目标
            RefreshAvailableTargets();

            Debug.Log("[AR Event System] 开始目标选择模式");
        }

        /// <summary>
        /// 取消目标选择
        /// </summary>
        public void CancelTargetSelection()
        {
            isSelecting = false;
            currentCallback = null;

            // 隐藏选择UI面板
            if (selectionUIPanel != null)
            {
                selectionUIPanel.SetActive(false);
            }

            Debug.Log("[AR Event System] 已取消目标选择");
        }

        private void RefreshAvailableTargets()
        {
            RefreshAllObjects();
            availableTargets = allARObjects.ToArray();
        }

        private void HandleTargetSelectionInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("[AR Event System] 检测到鼠标点击，进行射线检测...");

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log($"[AR Event System] 射线击中对象: {hit.collider.name}");

                    ARPlacedObject arObject = hit.collider.GetComponent<ARPlacedObject>();
                    if (arObject != null)
                    {
                        Debug.Log($"[AR Event System] 找到 ARPlacedObject: {arObject.name}");
                        // 选择了AR对象作为目标
                        SelectTarget(arObject);
                        return; // 重要：选择目标后直接返回，不执行取消逻辑
                    }
                    else
                    {
                        Debug.Log("[AR Event System] 击中的对象没有 ARPlacedObject 组件");
                    }
                }
                else
                {
                    Debug.Log("[AR Event System] 射线没有击中任何对象");
                }

                // 只有点击空白区域时才取消选择
                Debug.Log("[AR Event System] 点击空白区域，取消目标选择");
                CancelTargetSelection();
            }
        }

        private void SelectTarget(ARPlacedObject target)
        {
            if (target == null)
                return;

            Debug.Log($"[AR Event System] SelectTarget 被调用，目标: {target.name}");

            // 调用回调
            if (currentCallback != null)
            {
                Debug.Log("[AR Event System] 正在调用目标选择回调...");
                currentCallback.Invoke(target);
            }
            else
            {
                Debug.LogWarning("[AR Event System] currentCallback 为 null");
            }

            // 结束选择模式
            isSelecting = false;

            // 隐藏选择UI面板
            if (selectionUIPanel != null)
            {
                selectionUIPanel.SetActive(false);
            }

            Debug.Log($"[AR Event System] 已选择目标: {target.name}");

            // 备份：确保连接线被刷新（延迟一帧以确保数据已更新）
            Invoke("RefreshAllConnections", 0.1f);
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 获取所有AR对象
        /// </summary>
        public List<ARPlacedObject> GetAllARObjects()
        {
            return new List<ARPlacedObject>(allARObjects);
        }

        /// <summary>
        /// 是否正在选择目标
        /// </summary>
        public bool IsSelectingTarget()
        {
            return isSelecting;
        }

        /// <summary>
        /// 重新初始化系统
        /// </summary>
        public void ReinitializeSystem()
        {
            ClearAllConnections();
            RefreshAllObjects();
            RefreshAllConnections();
        }

        #endregion
    }

    /// <summary>
    /// AR事件连接线组件
    /// </summary>
    public class AREventConnection : MonoBehaviour
    {
        private Transform sourceTransform;
        private Transform targetTransform;
        private LineRenderer lineRenderer;
        private Transform arrowHead;
        private float connectionOffset;  // 连接回缩距离
        private float arrowPosition;     // 箭头在连接线上的位置比例
        private ActionType actionType;  // 存储事件类型用于颜色更新

        public void Initialize(Transform source, Transform target, Color color, Material material, float width, GameObject arrowPrefab, float offset, float arrowPos, ActionType actionType = ActionType.Enable)
        {
            sourceTransform = source;
            targetTransform = target;
            this.connectionOffset = offset;
            this.arrowPosition = arrowPos;
            this.actionType = actionType;  // 存储事件类型

            // 创建LineRenderer
            lineRenderer = gameObject.AddComponent<LineRenderer>();

            Debug.Log($"[AR Event Connection] 初始化连接线: {gameObject.name}");
            Debug.Log($"[AR Event Connection] 颜色: {color}, 宽度: {width}");

            // 创建材质实例并设置颜色 - 避免修改共享材质
            if (material != null)
            {
                lineRenderer.material = new Material(material);
                lineRenderer.material.color = color;
                Debug.Log($"[AR Event Connection] 使用提供的材质: {material.name}");
            }
            else
            {
                // 如果没有提供材质，创建默认材质
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.material.color = color;
                Debug.Log("[AR Event Connection] 使用默认Sprites/Default材质");
            }

            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            // 创建箭头
            if (arrowPrefab != null)
            {
                GameObject arrow = Instantiate(arrowPrefab, transform);
                arrowHead = arrow.transform;

                // 延迟设置箭头颜色，确保组件完全初始化
                StartCoroutine(DelayedSetArrowColor(color));
            }            // 初始更新位置
            UpdateLinePosition();
        }

        void Update()
        {
            UpdateLinePosition();
        }

        private void UpdateLinePosition()
        {
            if (sourceTransform != null && targetTransform != null && lineRenderer != null)
            {
                // 检查是否为自连接
                bool isSelfConnection = sourceTransform == targetTransform;

                if (isSelfConnection)
                {
                    // 自连接：创建一个环形路径
                    CreateSelfConnectionLoop();
                }
                else
                {
                    // 正常连接：直线连接
                    CreateNormalConnection();
                }
            }
        }

        private void CreateSelfConnectionLoop()
        {
            // 为自连接创建一个环形可视化
            Vector3 centerPos = sourceTransform.position; // 不需要Y偏移，而是使用回缩
            float loopRadius = 0.5f; // 环形半径

            // 设置多个点创建环形
            lineRenderer.positionCount = 16; // 使用更多点创建平滑的环

            for (int i = 0; i < lineRenderer.positionCount; i++)
            {
                float angle = (float)i / (lineRenderer.positionCount - 1) * Mathf.PI * 2;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * loopRadius, connectionOffset, Mathf.Sin(angle) * loopRadius);
                lineRenderer.SetPosition(i, centerPos + offset);
            }

            // 更新箭头位置（放在环的顶部）
            if (arrowHead != null)
            {
                arrowHead.position = centerPos + Vector3.forward * loopRadius + Vector3.up * connectionOffset;
                arrowHead.rotation = Quaternion.LookRotation(Vector3.right); // 箭头朝向切线方向
                arrowHead.localScale = Vector3.one * 0.2f;
                arrowHead.gameObject.SetActive(true);

                // 设置箭头颜色与连接线一致
                SetArrowColor(GetCurrentLineColor());
            }
        }

        private void CreateNormalConnection()
        {
            // 正常的直线连接，使用回缩逻辑
            Vector3 fromPos = sourceTransform.position;
            Vector3 toPos = targetTransform.position;

            // 保存原始位置用于箭头计算
            Vector3 originalFromPos = fromPos;
            Vector3 originalToPos = toPos;

            // 计算从目标到源的方向向量
            Vector3 direction = fromPos - toPos;
            float magnitude = direction.magnitude;

            // 使用相对比例回缩（connectionOffset是0-1之间的比例）
            if (magnitude > 0.001f) // 确保有足够的距离
            {
                Vector3 normalizedDirection = direction.normalized;
                float offsetDistance = magnitude * connectionOffset; // 按比例计算回缩距离
                toPos = toPos + normalizedDirection * offsetDistance; // 终点向起点回缩
            }

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, fromPos);
            lineRenderer.SetPosition(1, toPos);

            // 更新箭头位置和旋转
            if (arrowHead != null)
            {
                // 使用原始起点和终点计算箭头位置，不受connectionOffset影响
                Vector3 originalDirection = originalToPos - originalFromPos;
                float originalLength = originalDirection.magnitude;

                // 检查方向向量是否为零（防止Look rotation viewing vector is zero错误）
                if (originalLength > 0.001f)
                {
                    // 基于原始连线计算箭头位置
                    arrowHead.position = Vector3.Lerp(originalToPos, originalFromPos, arrowPosition);
                    // 箭头朝向原始目标方向
                    arrowHead.rotation = Quaternion.LookRotation(originalDirection);
                    // 设置箭头大小
                    arrowHead.localScale = Vector3.one * 0.2f;

                    // 确保箭头是激活的
                    if (!arrowHead.gameObject.activeSelf)
                    {
                        arrowHead.gameObject.SetActive(true);
                    }

                    // 设置箭头颜色与连接线一致
                    SetArrowColor(GetCurrentLineColor());
                }
                else
                {
                    // 如果两个对象位置相同，隐藏箭头
                    arrowHead.gameObject.SetActive(false);
                }
            }
        }        /// <summary>
                 /// 更新连接线颜色
                 /// </summary>
        public void UpdateColor(Color newColor)
        {
            if (lineRenderer != null && lineRenderer.material != null)
            {
                lineRenderer.material.color = newColor;

                // 同时更新箭头颜色
                SetArrowColor(newColor);
            }
        }

        /// <summary>
        /// 获取当前连接线颜色
        /// </summary>
        private Color GetCurrentLineColor()
        {
            if (lineRenderer != null && lineRenderer.material != null)
            {
                return lineRenderer.material.color;
            }
            return Color.white;
        }

        /// <summary>
        /// 延迟设置箭头颜色，确保组件完全初始化
        /// </summary>
        private System.Collections.IEnumerator DelayedSetArrowColor(Color color)
        {
            yield return null; // 等待一帧
            SetArrowColor(color);
        }

        /// <summary>
        /// 设置箭头颜色
        /// </summary>
        private void SetArrowColor(Color color)
        {
            if (arrowHead != null)
            {
                // 尝试获取箭头的所有Renderer组件（包括子物体）
                Renderer[] renderers = arrowHead.GetComponentsInChildren<Renderer>();

                if (renderers.Length == 0)
                {
                    Debug.LogWarning($"[AR Event Connection] 箭头 {arrowHead.name} 没有找到Renderer组件");
                    return;
                }

                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null && renderer.material != null)
                    {
                        // 创建材质实例以避免修改共享材质
                        if (renderer.material.name.Contains("(Instance)"))
                        {
                            renderer.material.color = color;
                        }
                        else
                        {
                            renderer.material = new Material(renderer.material);
                            renderer.material.color = color;
                        }

                        Debug.Log($"[AR Event Connection] 已设置箭头材质 {renderer.material.name} 颜色为 {color}");
                    }
                }
            }
        }

        /// <summary>
        /// 获取事件类型
        /// </summary>
        public ActionType ActionType => actionType;
    }
}