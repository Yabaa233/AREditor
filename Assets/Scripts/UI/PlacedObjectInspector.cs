using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlacedObjectInspector : MonoBehaviour
{
    public Toggle hiddenAtStartToggle;
    public Button addEventButton;
    public Button deleteButton;
    public Transform eventListContainer;
    public GameObject eventItemPrefab;

    private PlacedObjectData currentData;

    public void SetData(PlacedObjectData data)
    {
        currentData = data;

        // 初始 UI 状态同步
        hiddenAtStartToggle.isOn = data.ifHiddenAtGameStart;

        // 清空旧事件列表
        foreach (Transform child in eventListContainer)
        {
            Destroy(child.gameObject);
        }

        // 显示事件列表
        foreach (var evt in data.events)
        {
            AddEventItemUI(evt);
        }

        // 注册监听
        hiddenAtStartToggle.onValueChanged.RemoveAllListeners();
        hiddenAtStartToggle.onValueChanged.AddListener(val =>
        {
            currentData.ifHiddenAtGameStart = val;
        });
    }

    public void AddEventItemUI(TriggerActionEventData evt)
    {
        GameObject item = Instantiate(eventItemPrefab, eventListContainer);
        var ui = item.GetComponent<TriggerActionEventUI>();
        ui.Init(evt, () =>
        {
            currentData.events.Remove(evt);
            Destroy(item);
        });
    }

    void Awake()
    {
        addEventButton.onClick.AddListener(() =>
        {
            var evt = new TriggerActionEventData();
            currentData.events.Add(evt);
            AddEventItemUI(evt);
        });
        deleteButton.onClick.AddListener(() =>
        {
            EditorManager.Instance.UnregisterObject(EditorManager.Instance.focusedObject);
            UIManager.Instance.CloseInspectorUI();
        });
    }
}
