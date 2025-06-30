using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EventActionHandler : MonoBehaviour
{
    public List<TriggerActionEventData> eventList = new();

    private bool onEnterRegistered = false;
    private bool onExitRegistered = false;

    public void Register(bool onEnter)
    {
        if (onEnter) onEnterRegistered = true;
        else onExitRegistered = true;
    }


    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Enter");
        if (!onEnterRegistered) return;
        // 只响应 Player
        if (!other.CompareTag("Player")) return;
        Debug.Log("is Player");
        HandleEvent(TriggerType.OnEnter);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!onExitRegistered) return;
        // 只响应 Player
        if (!other.CompareTag("Player")) return;
        HandleEvent(TriggerType.OnExit);
    }

    private void HandleEvent(TriggerType type)
    {
        foreach (var evt in eventList)
        {
            if (evt.triggerType != type) continue;

            switch (evt.actionType)
            {
                case ActionType.Win:
                    Debug.Log($"[{name}] Triggered WIN");
                    // 你的胜利逻辑
                    break;
                case ActionType.Lose:
                    Debug.Log($"[{name}] Triggered LOSE");
                    // 你的失败逻辑
                    break;
                case ActionType.Enable:
                    if (evt.targetObjectID == null) continue;
                    // 更多处理
                    EditorManager.Instance.GetGameObjectByID(evt.targetObjectID).SetActive(true);
                    break;
                case ActionType.Disable:
                    if (evt.targetObjectID == null) continue;
                    EditorManager.Instance.GetGameObjectByID(evt.targetObjectID).SetActive(true);
                    break;
            }
        }
    }
}
