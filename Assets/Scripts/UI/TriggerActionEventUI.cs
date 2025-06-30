using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class TriggerActionEventUI : MonoBehaviour
{
    public Dropdown triggerDropdown;
    public Dropdown resultDropdown;
    public GameObject targetContainer;

    public Button targetButton;
    public Text targetLabel;
    public Button deleteButton;

    private TriggerActionEventData data;

    public void Init(TriggerActionEventData data, System.Action onDelete)
    {
        this.data = data;

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
            RefreshTargetVisibility(); // ← 更新目标UI可见性
        });

        RefreshTargetVisibility();
        RefreshTargetLabel();

        targetButton.onClick.AddListener(() =>
        {
            UIManager.Instance.StartPick(obj =>
            {
                data.targetObjectID = obj.GetComponent<PlacedObject>().runtimeData.ID;
                RefreshTargetLabel();
            });
        });

        deleteButton.onClick.AddListener(() => onDelete?.Invoke());
    }

    private void RefreshTargetVisibility()
    {
        bool needsTarget = data.actionType == ActionType.Enable || data.actionType == ActionType.Disable;
        targetContainer.SetActive(needsTarget);
    }

    void RefreshTargetLabel()
    {
        //TODO 按理来说被删了之后不需要额外处理
        // 打开的时候调用这个函数
        // if (data.targetObject is null)
        // {
        //     targetLabel.text = "Set";
        // }
        // else
        // {
        //     targetLabel.text = "Done";
        // }
    }
}
