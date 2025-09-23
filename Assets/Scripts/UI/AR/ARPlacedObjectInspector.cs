using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace UI.AR
{
    /// <summary>
    /// AR版本的放置对象检查器 - 面向玩家的关卡编辑器
    /// 完全复制PlacedObjectInspector的功能和字段，适配AR环境
    /// </summary>
    public class ARPlacedObjectInspector : MonoBehaviour
    {
        [Header("对象属性UI - 与原版PlacedObjectInspector完全一致")]
        public Toggle hiddenAtStartToggle;
        public Button addEventButton;
        public Button deleteButton;
        public Transform eventListContainer;
        public GameObject eventItemPrefab; // 使用ARTriggerActionEventUI预制体

        private PlacedObjectData currentData;
        private ARPlacedObject currentARObject;

        void Awake()
        {
            // 完全复制原版的按钮设置逻辑
            addEventButton.onClick.AddListener(() =>
            {
                var evt = new TriggerActionEventData();
                currentData.events.Add(evt);
                AddEventItemUI(evt);
            });

            deleteButton.onClick.AddListener(() =>
            {
                // AR版本：删除AR对象并关闭面板
                if (currentARObject != null)
                {
                    // 使用统一管理器刷新对象列表
                    var systemManager = AREventSystemManager.Instance;
                    if (systemManager != null)
                    {
                        systemManager.RefreshAllObjects();
                    }

                    Destroy(currentARObject.gameObject);
                    CloseInspector();
                }
            });
        }

        /// <summary>
        /// 设置要检查的AR对象数据 - 对应原版的SetData(PlacedObjectData data)
        /// </summary>
        public void SetData(ARPlacedObject arObject)
        {
            currentARObject = arObject;

            if (arObject == null)
            {
                gameObject.SetActive(false);
                return;
            }

            var placedObject = arObject.GetComponent<PlacedObject>();
            if (placedObject == null || placedObject.runtimeData == null)
            {
                Debug.LogError("[AR Inspector] ARPlacedObject没有有效的运行时数据");
                gameObject.SetActive(false);
                return;
            }

            currentData = placedObject.runtimeData;

            // 显示面板
            gameObject.SetActive(true);

            // 按照原版逻辑初始化UI状态
            hiddenAtStartToggle.isOn = currentData.ifHiddenAtGameStart;

            // 清除旧的事件列表
            foreach (Transform child in eventListContainer)
            {
                Destroy(child.gameObject);
            }

            // 显示事件列表
            foreach (var evt in currentData.events)
            {
                AddEventItemUI(evt);
            }

            // 注册toggle监听器（完全复制原版逻辑）
            hiddenAtStartToggle.onValueChanged.RemoveAllListeners();
            hiddenAtStartToggle.onValueChanged.AddListener(val =>
            {
                currentData.ifHiddenAtGameStart = val;
                // AR版本额外功能：实时应用可见性变化
                if (currentARObject != null)
                {
                    currentARObject.gameObject.SetActive(!val);
                }
            });
        }

        public void AddEventItemUI(TriggerActionEventData evt)
        {
            // 完全复制原版逻辑，只是使用AR版本的UI组件
            GameObject item = Instantiate(eventItemPrefab, eventListContainer);
            var ui = item.GetComponent<ARTriggerActionEventUI>();
            ui.Init(evt, () =>
            {
                currentData.events.Remove(evt);
                Destroy(item);
                // AR版本额外功能：刷新连接线
                RefreshARConnections();
            }, currentARObject);  // 传递源对象引用
        }

        private void CloseInspector()
        {
            currentData = null;
            currentARObject = null;
            gameObject.SetActive(false);
        }

        private void RefreshARConnections()
        {
            // AR版本额外功能：刷新连接线显示
            var systemManager = AREventSystemManager.Instance;
            if (systemManager != null)
            {
                systemManager.RefreshAllConnections();
            }
        }

        /// <summary>
        /// 外部调用，显示指定对象的检查器
        /// </summary>
        public static void ShowInspector(ARPlacedObject arObject)
        {
            var inspector = FindObjectOfType<ARPlacedObjectInspector>();
            if (inspector != null)
            {
                inspector.SetData(arObject);
            }
            else
            {
                Debug.LogError("[AR Inspector] 场景中没有找到ARPlacedObjectInspector");
            }
        }
    }
}