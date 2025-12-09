using Assets.Scripts.Manager;
using SpatialMap_SparseSpatialMap;
using UnityEngine;

public class ARMapListItem : MonoBehaviour
{
    public MapMeta meta;
    public UnityEngine.UI.Text nameText; // 用于显示地图名称的Text组件

    public void OnSelectButtonClicked()
    {
        if (meta == null || meta.Map == null || EasyARSpatialMapEditorManager.Instance == null)
        {
            Debug.LogError("无法加载地图，meta 或 EasyARSpatialMapEditorManager 实例为 null");
            return;
        }
        EasyARSpatialMapEditorManager.Instance.LoadMap(meta);
    }

    public void DeleteSelf()
    {
        EasyARSpatialMapEditorManager.Instance.DeleteMap(meta);
        FindObjectOfType<EasyARUIManager>().OpenARMapSidePanel();
        Destroy(gameObject);
    }

    /// <summary>
    /// 重命名地图（点击按钮后弹出输入框）
    /// </summary>
    public void OnRenameButtonClicked()
    {
        if (meta == null || meta.Map == null)
        {
            Debug.LogError("无法重命名，meta 为 null");
            return;
        }

        string currentName = meta.Map.Name;

        // 使用TouchScreenKeyboard在安卓端弹出输入框
#if UNITY_ANDROID || UNITY_IOS
        TouchScreenKeyboard.Open(currentName, TouchScreenKeyboardType.Default, false, false, false, false, "输入新名称");
        StartCoroutine(WaitForKeyboardInput(currentName));
#else
        // 编辑器中使用简单的方式（可以后续扩展为UI输入框）
        Debug.Log("[ARMapListItem] 在编辑器中，请使用UI输入框进行重命名");
        // 临时：直接添加时间戳作为测试
        string newName = currentName + "_renamed";
        ApplyNewName(newName);
#endif
    }

#if UNITY_ANDROID || UNITY_IOS
    private System.Collections.IEnumerator WaitForKeyboardInput(string originalName)
    {
        TouchScreenKeyboard keyboard = TouchScreenKeyboard.Open(originalName, TouchScreenKeyboardType.Default, false, false, false, false, "输入新名称");

        // 等待键盘关闭
        while (keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Visible)
        {
            yield return null;
        }

        // 检查是否完成输入
        if (keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Done)
        {
            string newName = keyboard.text;
            if (!string.IsNullOrEmpty(newName) && newName != originalName)
            {
                ApplyNewName(newName);
            }
        }
    }
#endif

    /// <summary>
    /// 应用新名称
    /// </summary>
    private void ApplyNewName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogWarning("[ARMapListItem] 新名称为空，取消重命名");
            return;
        }

        string oldName = meta.Map.Name;
        meta.Map.Name = newName;

        // 保存更新后的MapMeta
        MapMetaManager.Save(meta);

        // 更新UI显示
        if (nameText != null)
        {
            nameText.text = $"{newName}\nID: {meta.Map.ID}";
        }
        else
        {
            // 尝试查找子物体中的Text组件
            var text = GetComponentInChildren<UnityEngine.UI.Text>();
            if (text != null)
            {
                text.text = $"{newName}\nID: {meta.Map.ID}";
            }
        }

        Debug.Log($"[ARMapListItem] 地图重命名: {oldName} -> {newName}");
    }
}