using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace UI.AR
{
    /// <summary>
    /// AR版本的触发事件UI组件
    /// 复用底层TriggerActionEventData数据，但提供AR特有的交互体验
    /// </summary>
    public class ARTriggerActionEventUI : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Dropdown triggerDropdown;
        public TMP_Dropdown resultDropdown;
        public GameObject targetContainer;
        public Button targetButton;
        public Button deleteButton;
        public Text targetLabel;

        [Header("AR Specific")]
        public Button previewButton;        // 预览事件效果
        public Toggle enabledToggle;        // 启用/禁用事件
        public Image statusIndicator;       // 状态指示器
        public Button visualizeButton;      // 可视化连接线

        private TriggerActionEventData data;
        private System.Action onDeleteCallback;
        private ARPlacedObject ownerObject;
        private ARPlacedObject targetObject;

        public void Init(TriggerActionEventData eventData, ARPlacedObject owner, System.Action onDelete)
        {
            this.data = eventData;
            this.ownerObject = owner;
            this.onDeleteCallback = onDelete;

            SetupUI();
            RefreshUI();
        }

        private void SetupUI()
        {
            // 设置触发类型下拉菜单
            triggerDropdown.ClearOptions();
            triggerDropdown.AddOptions(System.Enum.GetNames(typeof(TriggerType)).ToList());
            triggerDropdown.value = (int)data.triggerType;
            triggerDropdown.onValueChanged.AddListener(OnTriggerTypeChanged);

            // 设置动作类型下拉菜单
            resultDropdown.ClearOptions();
            resultDropdown.AddOptions(System.Enum.GetNames(typeof(ActionType)).ToList());
            resultDropdown.value = (int)data.actionType;
            resultDropdown.onValueChanged.AddListener(OnActionTypeChanged);

            // 设置按钮事件
            targetButton.onClick.AddListener(OnSelectTarget);
            deleteButton.onClick.AddListener(OnDelete);
            previewButton.onClick.AddListener(OnPreview);
            visualizeButton.onClick.AddListener(OnToggleVisualization);

            // 设置启用开关
            enabledToggle.isOn = data.enabled;
            enabledToggle.onValueChanged.AddListener(OnEnabledChanged);
        }

        private void OnTriggerTypeChanged(int value)
        {
            data.triggerType = (TriggerType)value;
            RefreshUI();
            SaveData();
        }

        private void OnActionTypeChanged(int value)
        {
            data.actionType = (ActionType)value;
            RefreshTargetVisibility();
            SaveData();
        }

        private void OnEnabledChanged(bool enabled)
        {
            data.enabled = enabled;
            RefreshStatusIndicator();
            SaveData();

            // 刷新AR空间中的连接线显示
            AREventConnectionManager.Instance?.RefreshConnectionForEvent(ownerObject, data);
        }

        private void OnSelectTarget()
        {
            // AR模式的目标选择
            AREventTargetSelector.Instance.StartSelection(OnTargetSelected);
        }

        private void OnTargetSelected(ARPlacedObject target)
        {
            targetObject = target;
            data.targetObjectID = target.GetComponent<PlacedObject>().runtimeData.ID;
            RefreshTargetLabel();
            SaveData();

            // 更新AR空间中的连接线
            AREventConnectionManager.Instance?.RefreshConnectionForEvent(ownerObject, data);
        }

        private void OnPreview()
        {
            // 预览事件效果
            if (targetObject != null)
            {
                AREventPreviewManager.Instance.PreviewEvent(data, ownerObject, targetObject);
            }
        }

        private void OnToggleVisualization()
        {
            // 切换这个事件的连接线可视化
            AREventConnectionManager.Instance?.ToggleConnectionForEvent(ownerObject, data);
        }

        private void OnDelete()
        {
            // 删除AR空间中的连接线
            AREventConnectionManager.Instance?.RemoveConnectionForEvent(ownerObject, data);
            onDeleteCallback?.Invoke();
        }

        private void RefreshUI()
        {
            RefreshTargetVisibility();
            RefreshTargetLabel();
            RefreshStatusIndicator();
        }

        private void RefreshTargetVisibility()
        {
            bool needsTarget = data.actionType == ActionType.Enable ||
                              data.actionType == ActionType.Disable ||
                              data.actionType == ActionType.MoveTo;
            targetContainer.SetActive(needsTarget);
        }

        private void RefreshTargetLabel()
        {
            if (string.IsNullOrEmpty(data.targetObjectID))
            {
                targetLabel.text = "选择目标";
                targetLabel.color = Color.red;
            }
            else
            {
                // 通过ID查找目标对象
                targetObject = AREventManager.Instance.FindObjectByID(data.targetObjectID);
                if (targetObject != null)
                {
                    targetLabel.text = targetObject.name;
                    targetLabel.color = Color.green;
                }
                else
                {
                    targetLabel.text = "目标丢失";
                    targetLabel.color = Color.red;
                }
            }
        }

        private void RefreshStatusIndicator()
        {
            if (statusIndicator != null)
            {
                statusIndicator.color = data.enabled ? Color.green : Color.gray;
            }
        }

        private void SaveData()
        {
            // 触发保存
            if (EasyARSpatialMapEditorManager.Instance.autoSaveOnEdit)
            {
                EasyARSpatialMapEditorManager.Instance.SaveObjectsInfo();
            }
        }
    }
}