using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UI.AR
{
    /// <summary>
    /// AR事件预览管理器
    /// 提供实时的AR空间事件效果预览
    /// </summary>
    public class AREventPreviewManager : MonoBehaviour
    {
        public static AREventPreviewManager Instance { get; private set; }

        [Header("Preview Settings")]
        public bool enableRealTimePreview = true;
        public float previewDuration = 3f;
        public Color previewTintColor = new Color(1f, 1f, 0f, 0.3f);

        [Header("Movement Preview")]
        public GameObject movementTrailPrefab;
        public Color movementPathColor = Color.cyan;
        public float pathWidth = 0.05f;

        [Header("Sound Preview")]
        public GameObject soundVisualizationPrefab;
        public float soundVisualizationScale = 1f;

        [Header("Effect Indicators")]
        public GameObject enableIndicatorPrefab;
        public GameObject disableIndicatorPrefab;
        public Material ghostMaterial;

        private Dictionary<string, AREventPreview> activepreviews = new Dictionary<string, AREventPreview>();
        private Transform previewContainer;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePreviewContainer();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializePreviewContainer()
        {
            GameObject container = new GameObject("AR_Event_Previews");
            container.transform.SetParent(transform);
            previewContainer = container.transform;
        }

        /// <summary>
        /// 预览特定事件的效果
        /// </summary>
        public void PreviewEvent(ARPlacedObject sourceObj, TriggerActionEventData eventData)
        {
            if (!enableRealTimePreview) return;

            string previewKey = GetPreviewKey(sourceObj, eventData);

            // 停止当前预览
            StopPreview(previewKey);

            // 查找目标对象
            var targetObj = AREventManager.Instance?.FindObjectByID(eventData.targetObjectID);
            if (targetObj == null)
            {
                Debug.LogWarning($"[AR Event Preview] 找不到目标对象进行预览: {eventData.targetObjectID}");
                return;
            }

            // 创建预览
            var preview = CreateEventPreview(sourceObj, targetObj, eventData);
            if (preview != null)
            {
                activePreews[previewKey] = preview;
                StartCoroutine(AutoStopPreview(previewKey, previewDuration));
            }
        }

        /// <summary>
        /// 停止特定事件的预览
        /// </summary>
        public void StopPreview(string previewKey)
        {
            if (activePrews.ContainsKey(previewKey))
            {
                var preview = activePrews[previewKey];
                if (preview != null)
                {
                    preview.StopPreview();
                    Destroy(preview.gameObject);
                }
                activePrews.Remove(previewKey);
            }
        }

        /// <summary>
        /// 停止所有预览
        /// </summary>
        public void StopAllPreviews()
        {
            foreach (var preview in activePrews.Values)
            {
                if (preview != null)
                {
                    preview.StopPreview();
                    Destroy(preview.gameObject);
                }
            }
            activePrews.Clear();
        }

        private AREventPreview CreateEventPreview(ARPlacedObject sourceObj, ARPlacedObject targetObj, TriggerActionEventData eventData)
        {
            GameObject previewGO = new GameObject($"Preview_{sourceObj.name}_to_{targetObj.name}");
            previewGO.transform.SetParent(previewContainer);

            var preview = previewGO.AddComponent<AREventPreview>();
            preview.Initialize(sourceObj, targetObj, eventData, this);

            return preview;
        }

        private string GetPreviewKey(ARPlacedObject sourceObj, TriggerActionEventData eventData)
        {
            var placedObject = sourceObj.GetComponent<PlacedObject>();
            string sourceID = placedObject?.runtimeData?.ID ?? sourceObj.GetInstanceID().ToString();
            return $"{sourceID}_{eventData.targetObjectID}_{(int)eventData.actionType}";
        }

        private IEnumerator AutoStopPreview(string previewKey, float delay)
        {
            yield return new WaitForSeconds(delay);
            StopPreview(previewKey);
        }

        /// <summary>
        /// 创建移动路径预览
        /// </summary>
        public GameObject CreateMovementPathPreview(Vector3 startPos, Vector3 endPos)
        {
            GameObject pathGO = new GameObject("MovementPath_Preview");
            pathGO.transform.SetParent(previewContainer);

            LineRenderer lineRenderer = pathGO.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.color = movementPathColor;
            lineRenderer.startWidth = pathWidth;
            lineRenderer.endWidth = pathWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);

            return pathGO;
        }

        /// <summary>
        /// 创建物体状态预览
        /// </summary>
        public GameObject CreateObjectStatePreview(ARPlacedObject targetObj, ActionType actionType)
        {
            GameObject previewGO = new GameObject($"StatePreview_{targetObj.name}");
            previewGO.transform.SetParent(previewContainer);
            previewGO.transform.position = targetObj.transform.position;
            previewGO.transform.rotation = targetObj.transform.rotation;

            // 复制目标对象的视觉外观
            CopyObjectVisuals(targetObj.gameObject, previewGO);

            // 应用预览效果
            ApplyPreviewEffect(previewGO, actionType);

            return previewGO;
        }

        private void CopyObjectVisuals(GameObject source, GameObject destination)
        {
            // 复制MeshRenderer和MeshFilter
            var sourceMeshRenderer = source.GetComponent<MeshRenderer>();
            var sourceMeshFilter = source.GetComponent<MeshFilter>();

            if (sourceMeshRenderer != null && sourceMeshFilter != null)
            {
                var destMeshRenderer = destination.AddComponent<MeshRenderer>();
                var destMeshFilter = destination.AddComponent<MeshFilter>();

                destMeshFilter.mesh = sourceMeshFilter.mesh;

                // 使用半透明的幽灵材质
                if (ghostMaterial != null)
                {
                    destMeshRenderer.material = ghostMaterial;
                }
                else
                {
                    // 创建半透明版本的原始材质
                    Material ghostMat = new Material(sourceMeshRenderer.material);
                    ghostMat.color = new Color(ghostMat.color.r, ghostMat.color.g, ghostMat.color.b, 0.5f);
                    destMeshRenderer.material = ghostMat;
                }
            }

            // 递归处理子对象
            for (int i = 0; i < source.transform.childCount; i++)
            {
                var sourceChild = source.transform.GetChild(i);
                var destChild = new GameObject(sourceChild.name);
                destChild.transform.SetParent(destination.transform);
                destChild.transform.localPosition = sourceChild.localPosition;
                destChild.transform.localRotation = sourceChild.localRotation;
                destChild.transform.localScale = sourceChild.localScale;

                CopyObjectVisuals(sourceChild.gameObject, destChild);
            }
        }

        private void ApplyPreviewEffect(GameObject previewObj, ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Enable:
                    // 添加启用指示器
                    if (enableIndicatorPrefab != null)
                    {
                        var indicator = Instantiate(enableIndicatorPrefab, previewObj.transform);
                        indicator.transform.localPosition = Vector3.up * 0.5f;
                    }
                    break;

                case ActionType.Disable:
                    // 添加禁用指示器和暗化效果
                    if (disableIndicatorPrefab != null)
                    {
                        var indicator = Instantiate(disableIndicatorPrefab, previewObj.transform);
                        indicator.transform.localPosition = Vector3.up * 0.5f;
                    }

                    // 暗化预览对象
                    var renderers = previewObj.GetComponentsInChildren<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        var material = renderer.material;
                        material.color = material.color * 0.3f; // 暗化
                    }
                    break;

                case ActionType.MoveTo:
                    // 移动预览已在AREventPreview中处理
                    break;

                case ActionType.PlaySound:
                    // 添加声音可视化
                    if (soundVisualizationPrefab != null)
                    {
                        var soundViz = Instantiate(soundVisualizationPrefab, previewObj.transform);
                        soundViz.transform.localPosition = Vector3.up * 0.3f;
                        soundViz.transform.localScale = Vector3.one * soundVisualizationScale;
                    }
                    break;
            }
        }

        private void OnDestroy()
        {
            StopAllPreviews();
        }
    }

    /// <summary>
    /// AR事件预览组件
    /// </summary>
    public class AREventPreview : MonoBehaviour
    {
        private ARPlacedObject sourceObject;
        private ARPlacedObject targetObject;
        private TriggerActionEventData eventData;
        private AREventPreviewManager previewManager;

        private GameObject movementPathPreview;
        private GameObject statePreview;
        private Vector3 originalPosition;
        private bool isMoving = false;

        public void Initialize(ARPlacedObject source, ARPlacedObject target, TriggerActionEventData data, AREventPreviewManager manager)
        {
            sourceObject = source;
            targetObject = target;
            eventData = data;
            previewManager = manager;
            originalPosition = target.transform.position;

            StartPreview();
        }

        private void StartPreview()
        {
            switch (eventData.actionType)
            {
                case ActionType.MoveTo:
                    StartMovementPreview();
                    break;

                case ActionType.Enable:
                case ActionType.Disable:
                case ActionType.PlaySound:
                    StartStatePreview();
                    break;
            }
        }

        private void StartMovementPreview()
        {
            Vector3 targetPosition = GetTargetPosition();

            // 创建移动路径可视化
            movementPathPreview = previewManager.CreateMovementPathPreview(originalPosition, targetPosition);

            // 创建目标状态预览
            statePreview = previewManager.CreateObjectStatePreview(targetObject, eventData.actionType);

            // 开始移动动画
            StartCoroutine(AnimateMovement(targetPosition));
        }

        private void StartStatePreview()
        {
            statePreview = previewManager.CreateObjectStatePreview(targetObject, eventData.actionType);
        }

        private Vector3 GetTargetPosition()
        {
            // 根据moveData计算目标位置
            if (eventData.moveData != null)
            {
                switch (eventData.moveData.moveType)
                {
                    case MoveType.ToPosition:
                        return eventData.moveData.targetPosition;

                    case MoveType.ByOffset:
                        return originalPosition + eventData.moveData.offset;

                    case MoveType.ToObject:
                        var targetObj = AREventManager.Instance?.FindObjectByID(eventData.moveData.targetObjectID);
                        if (targetObj != null)
                        {
                            return targetObj.transform.position + eventData.moveData.offset;
                        }
                        break;
                }
            }

            return originalPosition;
        }

        private IEnumerator AnimateMovement(Vector3 targetPosition)
        {
            if (statePreview == null) yield break;

            isMoving = true;
            float duration = 2f; // 预览动画持续时间
            float elapsed = 0f;
            Vector3 startPos = originalPosition;

            while (elapsed < duration && statePreview != null)
            {
                float t = elapsed / duration;
                Vector3 currentPos = Vector3.Lerp(startPos, targetPosition, t);
                statePreview.transform.position = currentPos;

                elapsed += Time.deltaTime;
                yield return null;
            }

            isMoving = false;
        }

        public void StopPreview()
        {
            if (movementPathPreview != null)
            {
                Destroy(movementPathPreview);
            }

            if (statePreview != null)
            {
                Destroy(statePreview);
            }

            isMoving = false;
        }

        private void OnDestroy()
        {
            StopPreview();
        }
    }
}