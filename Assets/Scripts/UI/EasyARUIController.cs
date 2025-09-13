using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Manager;
using UnityEngine;
public class EasyARUIController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject ParentPanel;
    public GameObject ARMapSidePanel;
    public GameObject TwoDMapSidePanel;
    public GameObject ARObjectListSidePanel;
    public GameObject CreateARMapPanel;

    [Header("AR Map List")]
    public GameObject ARMapListItem;
    public GameObject ARMapListContent;
    public void OpenARMapSidePanel()
    {
        ARMapSidePanel.SetActive(true);
        UpdateARMapList();
    }
    public void CloseARMapSidePanel()
    {
        ARMapSidePanel.SetActive(false);
    }

    public void Open2DMapSidePanel()
    {
        TwoDMapSidePanel.SetActive(true);
    }
    public void Close2DMapSidePanel()
    {
        TwoDMapSidePanel.SetActive(false);
    }
    public void OpenARObjectListSidePanel()
    {
        ARObjectListSidePanel.SetActive(true);
    }
    public void CloseARObjectListSidePanel()
    {
        ARObjectListSidePanel.SetActive(false);
    }

    private void UpdateARMapList()
    {
        EasyARSpatialMapEditorManager.Instance.RefreshAvailableMaps();
        var availableMaps = EasyARSpatialMapEditorManager.Instance.GetAvailableMaps();

        // 清空现有列表
        foreach (Transform child in ARMapListContent.transform)
        {
            Destroy(child.gameObject);
        }

        // 创建新的列表项
        foreach (var map in availableMaps)
        {
            var listItem = Instantiate(ARMapListItem);
            listItem.transform.SetParent(ARMapListContent.transform);
            listItem.transform.GetComponentInChildren<UnityEngine.UI.Text>().text = $"{map.Map.Name}\nID: {map.Map.ID}";
            var mapListItemComponent = listItem.GetComponent<ARMapListItem>();
            mapListItemComponent.meta = map;
        }

    }

    public void CloseParentSidePanel()
    {
        ParentPanel.SetActive(false);
    }
    public void OpenParentSidePanel()
    {
        ParentPanel.SetActive(true);
    }
    public void OpenCreatePanel()
    {
        CreateARMapPanel.SetActive(true);
    }
    public void CloseCreatePanel()
    {
        CreateARMapPanel.SetActive(false);
    }

    public void OnAddARMap()
    {
        CloseParentSidePanel();
        OpenCreatePanel();

        EasyARSpatialMapEditorManager.Instance.StartMapBuilding();

    }

    public void OnSaveAddARMap()
    {
        // 结束建图
        if (!EasyARSpatialMapEditorManager.Instance.IsMapBuilding)
        {
            Debug.LogWarning("没有正在构建的地图可以保存");
            return;
        }
        // ShowStatusMessage("正在保存地图，请稍候...", 3f);
        EasyARSpatialMapEditorManager.Instance.SaveCurrentMap();
        CloseCreatePanel();
        OpenParentSidePanel();

    }
}