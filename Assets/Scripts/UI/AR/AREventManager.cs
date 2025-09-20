using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UI.AR
{
    /// <summary>
    /// AR事件管理器
    /// 统一管理AR空间中的事件系统，包括事件处理、对象查找和组件协调
    /// </summary>
    public class AREventManager : MonoBehaviour
    {
        public static AREventManager Instance { get; private set; }

        [Header("Event System Components")]
        public AREventConnectionManager connectionManager;
        public AREventPreviewManager previewManager;
        public ARTriggerActionEventUI eventUI;
        public AREventTargetSelector targetSelector;

        [Header("Event Processing")]
        public bool enableEventProcessing = true;
        public bool debugEventTriggers = true;

        [Header("Object Management")]
        public float objectSearchRadius = 50f;

        // 事件相关缓存
        private Dictionary<string, ARPlacedObject> objectCache = new Dictionary<string, ARPlacedObject>();
        private List<ARPlacedObject> allARObjects = new List<ARPlacedObject>();

        // 事件处理委托
        public delegate void EventTriggeredDelegate(ARPlacedObject source, TriggerActionEventData eventData);
        public static event EventTriggeredDelegate OnEventTriggered;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeEventSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // 延迟初始化，确保所有AR对象都已创建
            Invoke(nameof(RefreshObjectCache), 1f);
        }

        private void InitializeEventSystem()
        {
            // 自动查找或创建事件系统组件
            if (connectionManager == null)
                connectionManager = FindObjectOfType<AREventConnectionManager>();

            if (previewManager == null)
                previewManager = FindObjectOfType<AREventPreviewManager>();

            if (eventUI == null)
                eventUI = FindObjectOfType<ARTriggerActionEventUI>();

            if (targetSelector == null)
                targetSelector = FindObjectOfType<AREventTargetSelector>();

            Debug.Log("[AR Event Manager] 事件系统初始化完成");
        }

        /// <summary>
        /// 刷新对象缓存
        /// </summary>
        public void RefreshObjectCache()
        {
            objectCache.Clear();
            allARObjects.Clear();

            // 查找所有AR放置的对象
            var arObjects = FindObjectsOfType<ARPlacedObject>();

            foreach (var arObj in arObjects)
            {
                allARObjects.Add(arObj);

                var placedObject = arObj.GetComponent<PlacedObject>();
                if (placedObject?.runtimeData?.ID != null)
                {
                    objectCache[placedObject.runtimeData.ID] = arObj;
                }
            }

            Debug.Log($"[AR Event Manager] 缓存了 {allARObjects.Count} 个AR对象");

            // 刷新连接显示
            if (connectionManager != null)
            {
                connectionManager.RefreshAllConnections();
            }
        }

        /// <summary>
        /// 根据ID查找AR对象
        /// </summary>
        public ARPlacedObject FindObjectByID(string objectID)
        {
            if (string.IsNullOrEmpty(objectID))
                return null;

            if (objectCache.ContainsKey(objectID))
            {
                var obj = objectCache[objectID];
                if (obj != null)
                    return obj;
                else
                    objectCache.Remove(objectID); // 清理无效缓存
            }

            // 在所有对象中搜索
            foreach (var arObj in allARObjects)
            {
                if (arObj == null) continue;

                var placedObject = arObj.GetComponent<PlacedObject>();
                if (placedObject?.runtimeData?.ID == objectID)
                {
                    objectCache[objectID] = arObj; // 更新缓存
                    return arObj;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取指定位置附近的所有AR对象
        /// </summary>
        public List<ARPlacedObject> GetObjectsNearPosition(Vector3 position, float radius = -1f)
        {
            if (radius < 0) radius = objectSearchRadius;

            var nearbyObjects = new List<ARPlacedObject>();

            foreach (var arObj in allARObjects)
            {
                if (arObj != null && Vector3.Distance(arObj.transform.position, position) <= radius)
                {
                    nearbyObjects.Add(arObj);
                }
            }

            return nearbyObjects;
        }

        /// <summary>
        /// 获取所有AR对象
        /// </summary>
        public List<ARPlacedObject> GetAllARObjects()
        {
            // 清理空引用
            allARObjects = allARObjects.Where(obj => obj != null).ToList();
            return new List<ARPlacedObject>(allARObjects);
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public void TriggerEvent(ARPlacedObject sourceObj, TriggerActionEventData eventData)
        {
            if (!enableEventProcessing || !eventData.enabled)
                return;

            if (debugEventTriggers)
            {
                Debug.Log($"[AR Event Manager] 触发事件: {sourceObj.name} -> {eventData.targetObjectID} ({eventData.actionType})");
            }

            var targetObj = FindObjectByID(eventData.targetObjectID);
            if (targetObj == null)
            {
                Debug.LogWarning($"[AR Event Manager] 找不到目标对象: {eventData.targetObjectID}");
                return;
            }

            // 执行事件动作
            ExecuteEventAction(targetObj, eventData);

            // 触发事件回调
            OnEventTriggered?.Invoke(sourceObj, eventData);
        }

        private void ExecuteEventAction(ARPlacedObject targetObj, TriggerActionEventData eventData)
        {
            switch (eventData.actionType)
            {
                case ActionType.Enable:
                    targetObj.gameObject.SetActive(true);
                    if (debugEventTriggers) Debug.Log($"[AR Event] 启用对象: {targetObj.name}");
                    break;

                case ActionType.Disable:
                    targetObj.gameObject.SetActive(false);
                    if (debugEventTriggers) Debug.Log($"[AR Event] 禁用对象: {targetObj.name}");
                    break;

                case ActionType.MoveTo:
                    ExecuteMoveAction(targetObj, eventData);
                    break;

                case ActionType.PlaySound:
                    ExecuteSoundAction(targetObj, eventData);
                    break;
            }
        }

        private void ExecuteMoveAction(ARPlacedObject targetObj, TriggerActionEventData eventData)
        {
            if (eventData.moveData == null) return;

            Vector3 targetPosition = targetObj.transform.position;

            switch (eventData.moveData.moveType)
            {
                case MoveType.ToPosition:
                    targetPosition = eventData.moveData.targetPosition;
                    break;

                case MoveType.ByOffset:
                    targetPosition = targetObj.transform.position + eventData.moveData.offset;
                    break;

                case MoveType.ToObject:
                    var moveTargetObj = FindObjectByID(eventData.moveData.targetObjectID);
                    if (moveTargetObj != null)
                    {
                        targetPosition = moveTargetObj.transform.position + eventData.moveData.offset;
                    }
                    break;
            }

            // 执行移动（可以是立即移动或动画移动）
            if (eventData.moveData.isInstant)
            {
                targetObj.transform.position = targetPosition;
            }
            else
            {
                StartCoroutine(AnimateMovement(targetObj, targetPosition, eventData.moveData.duration));
            }

            if (debugEventTriggers)
                Debug.Log($"[AR Event] 移动对象: {targetObj.name} 到 {targetPosition}");
        }

        private void ExecuteSoundAction(ARPlacedObject targetObj, TriggerActionEventData eventData)
        {
            if (eventData.soundData == null) return;

            var audioSource = targetObj.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = targetObj.gameObject.AddComponent<AudioSource>();
            }

            if (eventData.soundData.audioClip != null)
            {
                audioSource.clip = eventData.soundData.audioClip;
                audioSource.volume = eventData.soundData.volume;
                audioSource.pitch = eventData.soundData.pitch;
                audioSource.loop = eventData.soundData.loop;
                audioSource.Play();

                if (debugEventTriggers)
                    Debug.Log($"[AR Event] 播放声音: {targetObj.name} - {eventData.soundData.audioClip.name}");
            }
        }

        private System.Collections.IEnumerator AnimateMovement(ARPlacedObject targetObj, Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = targetObj.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                targetObj.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            targetObj.transform.position = targetPosition;
        }

        /// <summary>
        /// 注册新的AR对象
        /// </summary>
        public void RegisterARObject(ARPlacedObject arObject)
        {
            if (arObject == null) return;

            if (!allARObjects.Contains(arObject))
            {
                allARObjects.Add(arObject);

                var placedObject = arObject.GetComponent<PlacedObject>();
                if (placedObject?.runtimeData?.ID != null)
                {
                    objectCache[placedObject.runtimeData.ID] = arObject;
                }

                Debug.Log($"[AR Event Manager] 注册AR对象: {arObject.name}");
            }
        }

        /// <summary>
        /// 注销AR对象
        /// </summary>
        public void UnregisterARObject(ARPlacedObject arObject)
        {
            if (arObject == null) return;

            allARObjects.Remove(arObject);

            var placedObject = arObject.GetComponent<PlacedObject>();
            if (placedObject?.runtimeData?.ID != null)
            {
                objectCache.Remove(placedObject.runtimeData.ID);
            }

            Debug.Log($"[AR Event Manager] 注销AR对象: {arObject.name}");
        }

        /// <summary>
        /// 预览事件效果
        /// </summary>
        public void PreviewEventEffect(ARPlacedObject sourceObj, TriggerActionEventData eventData)
        {
            if (previewManager != null)
            {
                previewManager.PreviewEvent(sourceObj, eventData);
            }
        }

        /// <summary>
        /// 停止所有预览
        /// </summary>
        public void StopAllPreviews()
        {
            if (previewManager != null)
            {
                previewManager.StopAllPreviews();
            }
        }

        /// <summary>
        /// 显示事件编辑UI
        /// </summary>
        public void ShowEventEditUI(ARPlacedObject arObject)
        {
            if (eventUI != null)
            {
                eventUI.ShowEventEditor(arObject);
            }
        }

        /// <summary>
        /// 开始目标选择模式
        /// </summary>
        public void StartTargetSelection(System.Action<ARPlacedObject> onTargetSelected)
        {
            if (targetSelector != null)
            {
                targetSelector.StartTargetSelection(onTargetSelected);
            }
        }

        /// <summary>
        /// 设置连接可视化
        /// </summary>
        public void SetConnectionVisualization(bool enabled)
        {
            if (connectionManager != null)
            {
                connectionManager.SetShowAllConnections(enabled);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 编辑器辅助方法
#if UNITY_EDITOR
        [UnityEditor.MenuItem("AR Editor/Refresh Event System")]
        public static void RefreshEventSystemFromMenu()
        {
            if (Instance != null)
            {
                Instance.RefreshObjectCache();
                Debug.Log("AR事件系统已刷新");
            }
            else
            {
                Debug.LogWarning("AR事件管理器未找到");
            }
        }
#endif
    }
}