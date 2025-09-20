using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UI.AR
{
    /// <summary>
    /// AR事件连接管理器
    /// 管理AR空间中事件关系的3D可视化连接线
    /// </summary>
    public class AREventConnectionManager : MonoBehaviour
    {
        public static AREventConnectionManager Instance { get; private set; }

        [Header("Connection Visualization")]
        public Material connectionLineMaterial;
        public float lineWidth = 0.03f;
        public bool showAllConnections = true;
        public bool enableAnimatedLines = true;

        [Header("Arrow Settings")]
        public GameObject arrowPrefab;
        public float arrowScale = 0.1f;

        [Header("Line Colors")]
        public Color enableColor = Color.green;
        public Color disableColor = Color.red;
        public Color moveColor = Color.blue;
        public Color soundColor = Color.yellow;
        public Color defaultColor = Color.white;
        public Color disabledEventColor = Color.gray;

        private Dictionary<string, AREventConnection> eventConnections = new Dictionary<string, AREventConnection>();
        private Transform connectionContainer;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeConnectionContainer();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // 延迟刷新，确保所有AR对象都已初始化
            StartCoroutine(DelayedRefresh());
        }

        private void InitializeConnectionContainer()
        {
            GameObject container = new GameObject("AR_Event_Connections");
            container.transform.SetParent(transform);
            connectionContainer = container.transform;
        }

        private IEnumerator DelayedRefresh()
        {
            yield return new WaitForSeconds(1f);
            RefreshAllConnections();
        }

        /// <summary>
        /// 刷新所有事件连接
        /// </summary>
        public void RefreshAllConnections()
        {
            if (!showAllConnections) return;

            Debug.Log("[AR Event Connection] 刷新所有连接");

            // 清理旧连接
            ClearAllConnections();

            // 获取所有AR对象并创建连接
            var allARObjects = FindObjectsOfType<ARPlacedObject>();

            foreach (var arObj in allARObjects)
            {
                var placedObject = arObj.GetComponent<PlacedObject>();
                if (placedObject?.data?.events != null)
                {
                    foreach (var eventData in placedObject.data.events)
                    {
                        if (!string.IsNullOrEmpty(eventData.targetObjectID))
                        {
                            CreateEventConnection(arObj, eventData);
                        }
                    }
                }
            }

            Debug.Log($"[AR Event Connection] 创建了 {eventConnections.Count} 个连接");
        }

        /// <summary>
        /// 为特定事件刷新连接
        /// </summary>
        public void RefreshConnectionForEvent(ARPlacedObject sourceObj, TriggerActionEventData eventData)
        {
            string connectionKey = GetConnectionKey(sourceObj, eventData);

            // 移除旧连接
            RemoveConnection(connectionKey);

            // 创建新连接
            if (!string.IsNullOrEmpty(eventData.targetObjectID))
            {
                CreateEventConnection(sourceObj, eventData);
            }
        }

        /// <summary>
        /// 切换特定事件的连接可视化
        /// </summary>
        public void ToggleConnectionForEvent(ARPlacedObject sourceObj, TriggerActionEventData eventData)
        {
            string connectionKey = GetConnectionKey(sourceObj, eventData);

            if (eventConnections.ContainsKey(connectionKey))
            {
                var connection = eventConnections[connectionKey];
                connection.SetVisible(!connection.IsVisible);
            }
        }

        /// <summary>
        /// 移除特定事件的连接
        /// </summary>
        public void RemoveConnectionForEvent(ARPlacedObject sourceObj, TriggerActionEventData eventData)
        {
            string connectionKey = GetConnectionKey(sourceObj, eventData);
            RemoveConnection(connectionKey);
        }

        private void CreateEventConnection(ARPlacedObject sourceObj, TriggerActionEventData eventData)
        {
            // 查找目标对象
            var targetObj = AREventManager.Instance?.FindObjectByID(eventData.targetObjectID);
            if (targetObj == null)
            {
                Debug.LogWarning($"[AR Event Connection] 找不到目标对象: {eventData.targetObjectID}");
                return;
            }

            string connectionKey = GetConnectionKey(sourceObj, eventData);

            // 创建连接线GameObject
            GameObject connectionGO = new GameObject($"Connection_{sourceObj.name}_to_{targetObj.name}");
            connectionGO.transform.SetParent(connectionContainer);

            // 添加AREventConnection组件
            var connection = connectionGO.AddComponent<AREventConnection>();
            connection.Initialize(sourceObj, targetObj, eventData, GetEventColor(eventData), lineWidth, enableAnimatedLines);

            // 添加箭头
            if (arrowPrefab != null)
            {
                connection.AddArrow(arrowPrefab, arrowScale);
            }

            eventConnections[connectionKey] = connection;

            Debug.Log($"[AR Event Connection] 创建连接: {sourceObj.name} -> {targetObj.name} ({eventData.actionType})");
        }

        private void RemoveConnection(string connectionKey)
        {
            if (eventConnections.ContainsKey(connectionKey))
            {
                var connection = eventConnections[connectionKey];
                if (connection != null)
                {
                    Destroy(connection.gameObject);
                }
                eventConnections.Remove(connectionKey);
            }
        }

        private void ClearAllConnections()
        {
            foreach (var connection in eventConnections.Values)
            {
                if (connection != null)
                {
                    Destroy(connection.gameObject);
                }
            }
            eventConnections.Clear();
        }

        private string GetConnectionKey(ARPlacedObject sourceObj, TriggerActionEventData eventData)
        {
            var placedObject = sourceObj.GetComponent<PlacedObject>();
            string sourceID = placedObject?.runtimeData?.ID ?? sourceObj.GetInstanceID().ToString();
            return $"{sourceID}_{eventData.targetObjectID}_{(int)eventData.triggerType}_{(int)eventData.actionType}";
        }

        private Color GetEventColor(TriggerActionEventData eventData)
        {
            if (!eventData.enabled)
                return disabledEventColor;

            switch (eventData.actionType)
            {
                case ActionType.Enable: return enableColor;
                case ActionType.Disable: return disableColor;
                case ActionType.MoveTo: return moveColor;
                case ActionType.PlaySound: return soundColor;
                default: return defaultColor;
            }
        }

        /// <summary>
        /// 设置是否显示所有连接
        /// </summary>
        public void SetShowAllConnections(bool show)
        {
            showAllConnections = show;

            if (show)
            {
                RefreshAllConnections();
            }
            else
            {
                ClearAllConnections();
            }
        }

        /// <summary>
        /// 设置连接线动画
        /// </summary>
        public void SetAnimatedLines(bool animated)
        {
            enableAnimatedLines = animated;

            foreach (var connection in eventConnections.Values)
            {
                if (connection != null)
                {
                    connection.SetAnimated(animated);
                }
            }
        }

        private void OnDestroy()
        {
            ClearAllConnections();
        }
    }

    /// <summary>
    /// AR事件连接线组件
    /// </summary>
    public class AREventConnection : MonoBehaviour
    {
        private LineRenderer lineRenderer;
        private ARPlacedObject sourceObject;
        private ARPlacedObject targetObject;
        private TriggerActionEventData eventData;
        private GameObject arrowObject;
        private bool isAnimated = false;
        private float animationOffset = 0f;

        public bool IsVisible { get; private set; } = true;

        public void Initialize(ARPlacedObject source, ARPlacedObject target, TriggerActionEventData data, Color color, float width, bool animated)
        {
            sourceObject = source;
            targetObject = target;
            eventData = data;
            isAnimated = animated;

            SetupLineRenderer(color, width);
            UpdateLinePositions();
        }

        private void SetupLineRenderer(Color color, float width)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();

            // 使用默认材质或指定材质
            if (AREventConnectionManager.Instance.connectionLineMaterial != null)
            {
                lineRenderer.material = AREventConnectionManager.Instance.connectionLineMaterial;
            }
            else
            {
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            lineRenderer.color = color;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            // 设置渲染层级，确保在AR场景中正确显示
            lineRenderer.sortingOrder = 1;
        }

        public void AddArrow(GameObject arrowPrefab, float scale)
        {
            if (arrowPrefab != null)
            {
                arrowObject = Instantiate(arrowPrefab, transform);
                arrowObject.transform.localScale = Vector3.one * scale;
                UpdateArrowPosition();
            }
        }

        void Update()
        {
            if (sourceObject != null && targetObject != null)
            {
                UpdateLinePositions();

                if (arrowObject != null)
                {
                    UpdateArrowPosition();
                }

                if (isAnimated)
                {
                    UpdateAnimation();
                }
            }
        }

        private void UpdateLinePositions()
        {
            if (lineRenderer != null && sourceObject != null && targetObject != null)
            {
                Vector3 startPos = sourceObject.transform.position + Vector3.up * 0.2f;
                Vector3 endPos = targetObject.transform.position + Vector3.up * 0.2f;

                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, endPos);
            }
        }

        private void UpdateArrowPosition()
        {
            if (arrowObject != null && sourceObject != null && targetObject != null)
            {
                Vector3 startPos = sourceObject.transform.position + Vector3.up * 0.2f;
                Vector3 endPos = targetObject.transform.position + Vector3.up * 0.2f;
                Vector3 direction = (endPos - startPos).normalized;

                // 箭头位置在连线的中点偏向目标一些
                Vector3 arrowPos = Vector3.Lerp(startPos, endPos, 0.7f);
                arrowObject.transform.position = arrowPos;
                arrowObject.transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void UpdateAnimation()
        {
            if (lineRenderer != null)
            {
                animationOffset += Time.deltaTime * 2f;

                // 简单的流动效果
                Material mat = lineRenderer.material;
                if (mat.HasProperty("_MainTex"))
                {
                    mat.mainTextureOffset = new Vector2(animationOffset, 0);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;

            if (lineRenderer != null)
            {
                lineRenderer.enabled = visible;
            }

            if (arrowObject != null)
            {
                arrowObject.SetActive(visible);
            }
        }

        public void SetAnimated(bool animated)
        {
            isAnimated = animated;
        }
    }
}