// using System.Collections.Generic;
// using Unity.VisualScripting;
// using UnityEngine;

// public class EventActionHandler : MonoBehaviour
// {
//     public List<TriggerActionEventData> eventList = new();

//     private bool onEnterRegistered = false;
//     private bool onExitRegistered = false;

//     public void Register(bool onEnter)
//     {
//         if (onEnter) onEnterRegistered = true;
//         else onExitRegistered = true;
//     }


//     private void OnTriggerEnter(Collider other)
//     {
//         Debug.Log("Enter");
//         if (!onEnterRegistered) return;
//         // Only responds to the Player
//         if (!other.CompareTag("Player")) return;
//         Debug.Log("is Player");
//         HandleEvent(TriggerType.OnEnter);
//     }

//     private void OnTriggerExit(Collider other)
//     {
//         if (!onExitRegistered) return;
//         // Only responds to the Player
//         if (!other.CompareTag("Player")) return;
//         HandleEvent(TriggerType.OnExit);
//     }

//     private void HandleEvent(TriggerType type)
//     {
//         foreach (var evt in eventList)
//         {
//             if (evt.triggerType != type) continue;

//             switch (evt.actionType)
//             {
//                 case ActionType.Win:
//                     Debug.Log($"[{name}] Triggered WIN");
//                     // TODO: Win logic
//                     break;
//                 case ActionType.Lose:
//                     Debug.Log($"[{name}] Triggered LOSE");
//                     // TODO: Lose logic
//                     break;
//                 case ActionType.Enable:
//                     if (evt.targetObjectID == null) continue;
//                     // TODO: Need more
//                     EditorManager.Instance.GetGameObjectByID(evt.targetObjectID).SetActive(true);
//                     break;
//                 case ActionType.Disable:
//                     if (evt.targetObjectID == null) continue;
//                     EditorManager.Instance.GetGameObjectByID(evt.targetObjectID).SetActive(true);
//                     break;
//             }
//         }
//     }
// }


using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Manager
{
    /// <summary>
    /// AR环境中的事件动作处理器
    /// </summary>
    public class EventActionHandler : MonoBehaviour
    {
        public List<TriggerActionEventData> eventList = new List<TriggerActionEventData>();

        private bool onEnterRegistered = false;
        private bool onExitRegistered = false;

        public void Register(bool onEnter)
        {
            if (onEnter)
                onEnterRegistered = true;
            else
                onExitRegistered = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[EventActionHandler] {name} - OnTriggerEnter: {other.name}");

            if (!onEnterRegistered) return;

            // 只响应玩家
            if (!other.CompareTag("Player")) return;

            Debug.Log($"[EventActionHandler] {name} - 玩家进入触发区域");
            HandleEvent(TriggerType.OnEnter);
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log($"[EventActionHandler] {name} - OnTriggerExit: {other.name}");

            if (!onExitRegistered) return;

            // 只响应玩家
            if (!other.CompareTag("Player")) return;

            Debug.Log($"[EventActionHandler] {name} - 玩家离开触发区域");
            HandleEvent(TriggerType.OnExit);
        }

        private void HandleEvent(TriggerType type)
        {
            foreach (var evt in eventList)
            {
                if (evt.triggerType != type) continue;

                Debug.Log($"[EventActionHandler] {name} - 处理事件: {evt.triggerType} -> {evt.actionType}");

                switch (evt.actionType)
                {
                    case ActionType.Win:
                        Debug.Log($"[EventActionHandler] {name} - 触发胜利条件");
                        HandleWinCondition();
                        break;

                    case ActionType.Lose:
                        Debug.Log($"[EventActionHandler] {name} - 触发失败条件");
                        HandleLoseCondition();
                        break;

                    case ActionType.Enable:
                        if (string.IsNullOrEmpty(evt.targetObjectID))
                        {
                            Debug.LogWarning($"[EventActionHandler] {name} - Enable事件缺少目标对象ID");
                            continue;
                        }
                        HandleEnableObject(evt.targetObjectID);
                        break;

                    case ActionType.Disable:
                        if (string.IsNullOrEmpty(evt.targetObjectID))
                        {
                            Debug.LogWarning($"[EventActionHandler] {name} - Disable事件缺少目标对象ID");
                            continue;
                        }
                        HandleDisableObject(evt.targetObjectID);
                        break;
                }
            }
        }

        private void HandleWinCondition()
        {
            // TODO: 实现胜利逻辑
            Debug.Log("[EventActionHandler] 游戏胜利！");

            // 可以在这里添加胜利UI显示、音效播放等
            EasyARSpatialMapEditorManager.Instance.OnGameWin();
        }

        private void HandleLoseCondition()
        {
            // TODO: 实现失败逻辑
            Debug.Log("[EventActionHandler] 游戏失败！");

            // 可以在这里添加失败UI显示、音效播放等
            EasyARSpatialMapEditorManager.Instance.OnGameLose();
        }

        private void HandleEnableObject(string targetObjectID)
        {
            var targetObject = EasyARSpatialMapEditorManager.Instance.GetGameObjectByID(targetObjectID);
            if (targetObject != null)
            {
                targetObject.SetActive(true);
                Debug.Log($"[EventActionHandler] 启用对象: {targetObject.name}");
            }
            else
            {
                Debug.LogError($"[EventActionHandler] 未找到目标对象: {targetObjectID}");
            }
        }

        private void HandleDisableObject(string targetObjectID)
        {
            var targetObject = EasyARSpatialMapEditorManager.Instance.GetGameObjectByID(targetObjectID);
            if (targetObject != null)
            {
                targetObject.SetActive(false);
                Debug.Log($"[EventActionHandler] 禁用对象: {targetObject.name}");
            }
            else
            {
                Debug.LogError($"[EventActionHandler] 未找到目标对象: {targetObjectID}");
            }
        }
    }
}