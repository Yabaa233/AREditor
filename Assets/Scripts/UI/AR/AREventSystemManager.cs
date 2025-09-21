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
        public float defaultLineWidth = 0.03f;  // 默认线宽

        [Header("连接线颜色")]
        public Color enableColor = Color.green;   // 启用事件颜色
        public Color disableColor = Color.red;    // 禁用事件颜色
        public Color winColor = Color.yellow;     // 胜利事件颜色
        public Color loseColor = Color.magenta;   // 失败事件颜色

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
        }

        private AREventConnection CreateConnectionObject(ARPlacedObject source, ARPlacedObject target, ActionType actionType)
        {
            GameObject connectionObj = new GameObject($"Connection_{source.name}_to_{target.name}");
            connectionObj.transform.SetParent(connectionContainer);

            AREventConnection connection = connectionObj.AddComponent<AREventConnection>();
            connection.Initialize(source.transform, target.transform, GetConnectionColor(actionType), lineMaterial, defaultLineWidth);

            return connection;
        }

        private Color GetConnectionColor(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Enable: return enableColor;
                case ActionType.Disable: return disableColor;
                case ActionType.Win: return winColor;
                case ActionType.Lose: return loseColor;
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
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    ARPlacedObject arObject = hit.collider.GetComponent<ARPlacedObject>();
                    if (arObject != null)
                    {
                        // 选择了AR对象作为目标
                        SelectTarget(arObject);
                        return; // 重要：选择目标后直接返回，不执行取消逻辑
                    }
                }

                // 只有点击空白区域时才取消选择
                CancelTargetSelection();
            }
        }

        private void SelectTarget(ARPlacedObject target)
        {
            if (target == null)
                return;

            // 调用回调
            currentCallback?.Invoke(target);

            // 结束选择模式
            isSelecting = false;

            // 隐藏选择UI面板
            if (selectionUIPanel != null)
            {
                selectionUIPanel.SetActive(false);
            }

            Debug.Log($"[AR Event System] 已选择目标: {target.name}");
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

        public void Initialize(Transform source, Transform target, Color color, Material material, float width)
        {
            sourceTransform = source;
            targetTransform = target;

            // 创建LineRenderer
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = material;
            lineRenderer.material.color = color;  // 修复：使用material.color而不是color属性
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            // 初始更新位置
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
                lineRenderer.SetPosition(0, sourceTransform.position);
                lineRenderer.SetPosition(1, targetTransform.position);
            }
        }
    }
}