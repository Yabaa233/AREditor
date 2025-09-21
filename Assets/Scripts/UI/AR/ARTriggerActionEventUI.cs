using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Reflection;

namespace UI.AR
{
    /// <summary>
    /// AR版本的触发事件UI组件
    /// 完全复制原版TriggerActionEventUI的字段和逻辑，只是适配AR环境的目标选择
    /// </summary>
    public class ARTriggerActionEventUI : MonoBehaviour
    {
        [Header("基础UI组件 - 与原版完全相同")]
        public Dropdown triggerDropdown;
        public Dropdown resultDropdown;
        public GameObject targetContainer;
        public Button targetButton;
        public Text targetLabel;
        public Button deleteButton;

        private TriggerActionEventData data;
        private System.Action onDeleteCallback;

        /// <summary>
        /// 初始化事件UI - 完全复制原版TriggerActionEventUI的逻辑
        /// </summary>
        public void Init(TriggerActionEventData data, System.Action onDelete)
        {
            this.data = data;

            // 完全复制原版的下拉菜单设置
            triggerDropdown.ClearOptions();
            triggerDropdown.AddOptions(System.Enum.GetNames(typeof(TriggerType)).ToList());
            triggerDropdown.value = (int)data.triggerType;
            triggerDropdown.onValueChanged.AddListener(i => data.triggerType = (TriggerType)i);

            resultDropdown.ClearOptions();
            resultDropdown.AddOptions(System.Enum.GetNames(typeof(ActionType)).ToList());
            resultDropdown.value = (int)data.actionType;
            resultDropdown.onValueChanged.AddListener(i =>
            {
                data.actionType = (ActionType)i;
                RefreshTargetVisibility(); // 与原版相同：更新目标UI可见性
            });

            RefreshTargetVisibility();
            RefreshTargetLabel();

            // AR版本的目标选择：使用AR事件系统而不是UIManager.Instance.StartPick
            targetButton.onClick.AddListener(() =>
            {
                StartARTargetSelection();
            });

            deleteButton.onClick.AddListener(() => onDelete?.Invoke());
        }

        private void RefreshTargetVisibility()
        {
            // 完全复制原版逻辑：Enable和Disable需要目标
            bool needsTarget = data.actionType == ActionType.Enable || data.actionType == ActionType.Disable;
            targetContainer.SetActive(needsTarget);
        }

        void RefreshTargetLabel()
        {
            // 简化版本：显示目标对象名称或"Set"
            if (targetLabel != null)
            {
                if (string.IsNullOrEmpty(data.targetObjectID))
                {
                    targetLabel.text = "Set";
                }
                else
                {
                    // 尝试找到目标对象显示名称
                    var targetObj = FindARObjectById(data.targetObjectID);
                    targetLabel.text = targetObj != null ? targetObj.name : "Set";
                }
            }
        }

        private void StartARTargetSelection()
        {
            // 直接使用AREventSystemManager的Instance
            if (AREventSystemManager.Instance != null)
            {
                AREventSystemManager.Instance.StartTargetSelection(OnARTargetSelected);
            }
            else
            {
                Debug.LogError("[AR Event UI] AREventSystemManager.Instance 未找到");
            }
        }

        private void OnARTargetSelected(ARPlacedObject target)
        {
            if (target != null)
            {
                var placedObject = target.GetComponent<PlacedObject>();
                if (placedObject != null && placedObject.runtimeData != null)
                {
                    data.targetObjectID = placedObject.runtimeData.ID;
                    RefreshTargetLabel();
                }
            }
        }

        private ARPlacedObject FindARObjectById(string objectId)
        {
            // 简化的对象查找
            var allObjects = FindObjectsOfType<ARPlacedObject>();
            foreach (var obj in allObjects)
            {
                var placedObj = obj.GetComponent<PlacedObject>();
                if (placedObj != null && placedObj.runtimeData != null &&
                    placedObj.runtimeData.ID == objectId)
                {
                    return obj;
                }
            }
            return null;
        }
    }
}