using UnityEngine;
using System;

namespace UI.AR
{
    /// <summary>
    /// AR事件目标选择器
    /// 在AR空间中选择事件目标对象
    /// </summary>
    public class AREventTargetSelector : MonoBehaviour
    {
        public static AREventTargetSelector Instance { get; private set; }

        [Header("Visual Feedback")]
        public Material highlightMaterial;
        public Color selectionColor = Color.yellow;
        public GameObject selectionIndicatorPrefab;

        [Header("UI Feedback")]
        public GameObject selectionHintUI;
        public UnityEngine.UI.Text hintText;

        private Action<ARPlacedObject> onTargetSelected;
        private bool isSelecting = false;
        private ARPlacedObject[] availableTargets;
        private GameObject[] selectionIndicators;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Update()
        {
            if (isSelecting)
            {
                HandleSelection();
            }
        }

        public void StartSelection(Action<ARPlacedObject> callback)
        {
            onTargetSelected = callback;
            isSelecting = true;

            // 获取所有可选择的AR对象
            availableTargets = FindObjectsOfType<ARPlacedObject>();

            // 在AR空间中显示选择指示器
            ShowSelectionIndicators();

            // 显示选择提示UI
            ShowSelectionHint("点击AR空间中的对象来选择目标");

            Debug.Log($"[AR Event Selector] 开始选择目标，可选对象数量: {availableTargets.Length}");
        }

        private void HandleSelection()
        {
            Vector2 inputPosition = Vector2.zero;
            bool hasInput = false;

            // 检查触摸输入
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    inputPosition = touch.position;
                    hasInput = true;
                }
            }
            // 检查鼠标输入（编辑器调试）
            else if (Input.GetMouseButtonDown(0))
            {
                inputPosition = Input.mousePosition;
                hasInput = true;
            }

            if (hasInput)
            {
                ProcessTouch(inputPosition);
            }

            // ESC键取消选择
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSelection();
            }
        }

        private void ProcessTouch(Vector2 screenPosition)
        {
            var camera = EasyARSpatialMapEditorManager.Instance.GetARCamera();
            if (camera == null) return;

            Ray ray = camera.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                var arObject = hit.collider.GetComponent<ARPlacedObject>();
                if (arObject != null && Array.IndexOf(availableTargets, arObject) >= 0)
                {
                    SelectTarget(arObject);
                }
            }
        }

        private void SelectTarget(ARPlacedObject target)
        {
            Debug.Log($"[AR Event Selector] 选择目标: {target.name}");

            // 隐藏选择指示器
            HideSelectionIndicators();

            // 选择目标
            onTargetSelected?.Invoke(target);

            // 结束选择
            isSelecting = false;
            HideSelectionHint();
        }

        private void ShowSelectionIndicators()
        {
            selectionIndicators = new GameObject[availableTargets.Length];

            for (int i = 0; i < availableTargets.Length; i++)
            {
                var target = availableTargets[i];

                // 创建选择指示器
                GameObject indicator;
                if (selectionIndicatorPrefab != null)
                {
                    indicator = Instantiate(selectionIndicatorPrefab, target.transform.position + Vector3.up * 0.5f, Quaternion.identity);
                }
                else
                {
                    // 创建简单的指示器
                    indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    indicator.transform.localScale = Vector3.one * 0.2f;
                    indicator.transform.position = target.transform.position + Vector3.up * 0.5f;

                    var renderer = indicator.GetComponent<Renderer>();
                    renderer.material.color = selectionColor;
                    renderer.material.SetFloat("_Mode", 2); // 设置为透明模式
                    renderer.material.color = new Color(selectionColor.r, selectionColor.g, selectionColor.b, 0.7f);
                }

                // 添加闪烁效果
                var blinker = indicator.AddComponent<ARSelectionBlinker>();
                blinker.Init(selectionColor);

                selectionIndicators[i] = indicator;
            }
        }

        private void HideSelectionIndicators()
        {
            if (selectionIndicators != null)
            {
                foreach (var indicator in selectionIndicators)
                {
                    if (indicator != null)
                    {
                        Destroy(indicator);
                    }
                }
                selectionIndicators = null;
            }
        }

        private void ShowSelectionHint(string hint)
        {
            if (selectionHintUI != null)
            {
                selectionHintUI.SetActive(true);
                if (hintText != null)
                {
                    hintText.text = hint;
                }
            }
        }

        private void HideSelectionHint()
        {
            if (selectionHintUI != null)
            {
                selectionHintUI.SetActive(false);
            }
        }

        public void CancelSelection()
        {
            if (isSelecting)
            {
                Debug.Log("[AR Event Selector] 取消选择");

                HideSelectionIndicators();
                isSelecting = false;
                HideSelectionHint();
            }
        }

        private void OnDestroy()
        {
            CancelSelection();
        }
    }

    /// <summary>
    /// AR选择指示器闪烁效果
    /// </summary>
    public class ARSelectionBlinker : MonoBehaviour
    {
        private Renderer targetRenderer;
        private Color originalColor;
        private float blinkSpeed = 2f;

        public void Init(Color color)
        {
            targetRenderer = GetComponent<Renderer>();
            originalColor = color;
        }

        void Update()
        {
            if (targetRenderer != null)
            {
                float alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
                Color blinkColor = new Color(originalColor.r, originalColor.g, originalColor.b, alpha * 0.7f);
                targetRenderer.material.color = blinkColor;
            }
        }
    }
}