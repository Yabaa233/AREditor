using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Manager;
using UI.AR;
using UnityEngine;
using UnityEngine.UI;
public class EasyARUIController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject ParentPanel;
    public GameObject ARMapSidePanel;
    public GameObject TwoDMapSidePanel;
    public GameObject ARObjectListSidePanel;
    public GameObject ObjectSelectionPanel;
    public GameObject ObjectInspectorPanel;
    public GameObject CreateARMapPanel;

    [Header("AR Map List")]
    public GameObject ARMapListItem;
    public GameObject ARMapListContent;

    [Header("Placed Object Template Database")]
    public PlacedObjectTemplateDatabase templateDB;


    public ARPlacedObject tempObject;
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
        ARObjectListSidePanel.GetComponent<RectTransform>()
        .SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400f);
        ARObjectListSidePanel.SetActive(true);
        ObjectSelectionPanel.SetActive(true);
        ObjectInspectorPanel.SetActive(false);

        ARObjectListSidePanel.SetActive(false);
        ARObjectListSidePanel.SetActive(true);

        EasyARSpatialMapEditorManager.Instance.EnterEditMode();
    }
    public void CloseARObjectListSidePanel()
    {
        ARObjectListSidePanel.SetActive(false);
        EasyARSpatialMapEditorManager.Instance.ExitEditMode();
    }

    public void OpenARObjectInspector()
    {
        ARObjectListSidePanel.GetComponent<RectTransform>()
        .SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 800f);
        ObjectSelectionPanel.SetActive(false);
        ObjectInspectorPanel.SetActive(true);

        ARObjectListSidePanel.SetActive(false);
        ARObjectListSidePanel.SetActive(true);

        if (EasyARSpatialMapEditorManager.Instance.currentSelectedObject == null)
        {
            Debug.LogWarning("没有选中的AR对象，无法正常赋值");

            ObjectInspectorPanel.GetComponent<ARPlacedObjectInspector>()
            .SetData(tempObject);
            return;
        }
        ObjectInspectorPanel.GetComponent<ARPlacedObjectInspector>()
        .SetData(EasyARSpatialMapEditorManager.Instance.currentSelectedObject.GetComponent<ARPlacedObject>());

    }

    public void CloseARObjectInspector()
    {
        ARObjectListSidePanel.GetComponent<RectTransform>()
        .SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400f);
        ObjectSelectionPanel.SetActive(true);
        ObjectInspectorPanel.SetActive(false);

        ARObjectListSidePanel.SetActive(false);
        ARObjectListSidePanel.SetActive(true);

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