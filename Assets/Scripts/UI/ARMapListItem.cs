using Assets.Scripts.Manager;
using SpatialMap_SparseSpatialMap;
using UnityEngine;

public class ARMapListItem : MonoBehaviour
{
    public MapMeta meta;

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
        FindObjectOfType<EasyARUIController>().OpenARMapSidePanel();
        Destroy(gameObject);
    }
}