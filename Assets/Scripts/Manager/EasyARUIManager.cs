using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Manager;
using UI.AR;
using UnityEngine;
using UnityEngine.UI;
public class EasyARUIManager : singleton<EasyARUIManager>
{
    [Header("Panels")]
    public GameObject ParentPanel;
    public GameObject ARMapSidePanel;
    public GameObject TwoDMapSidePanel;
    public GameObject ARObjectListSidePanel;
    public GameObject ObjectSelectionPanel;
    public GameObject ObjectInspectorPanel;
    public GameObject CreateARMapPanel;
    public GameObject PlayPanel;
    public GameObject MeshAlignmentPanel; // Mesh对齐配置面板

    [Header("AR Map List")]
    public GameObject ARMapListItem;
    public GameObject ARMapListContent;

    [Header("2D Map List")]
    public GameObject TwoDMapListItem;
    public GameObject TwoDMapListContent;

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
        Update2DMapList();

        // 显示mesh（如果地图已本地化且mesh已配置）
        // if (EasyARSpatialMapEditorManager.Instance != null)
        // {
        //     Debug.Log($"[2D Map Panel] IsMapLocalized: {EasyARSpatialMapEditorManager.Instance.IsMapLocalized}");

        //     if (EasyARSpatialMapEditorManager.Instance.IsMapLocalized)
        //     {
        //         EasyARSpatialMapEditorManager.Instance.ShowMesh(true);
        //         Debug.Log("[2D Map Panel] 已调用ShowMesh(true)");
        //     }
        // }

        EasyARSpatialMapEditorManager.Instance.SetMeshVisualVisibility(true);
    }
    public void Close2DMapSidePanel()
    {
        TwoDMapSidePanel.SetActive(false);

        // 根据 showMeshInEditMode 设置决定是否隐藏mesh
        // if (EasyARSpatialMapEditorManager.Instance != null)
        // {
        //     bool showMesh = EasyARSpatialMapEditorManager.Instance.showMeshInEditMode;
        //     EasyARSpatialMapEditorManager.Instance.ShowMesh(showMesh);
        // }
        EasyARSpatialMapEditorManager.Instance.SetMeshVisualVisibility(false);

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

            if (tempObject == null)
            {
                Debug.LogWarning("临时AR对象未设置，无法赋值");
                return;
            }

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

    /// <summary>
    /// 更新 2D 地图列表
    /// </summary>
    private void Update2DMapList()
    {
        if (TwoDMapListContent == null)
        {
            Debug.LogError("TwoDMapListContent 未赋值");
            return;
        }

        // 清空现有列表项
        foreach (Transform child in TwoDMapListContent.transform)
        {
            Destroy(child.gameObject);
        }

        // 扫描所有 .json 文件
        string savePath = Application.persistentDataPath;
        Debug.Log($"[2D Map List] 扫描路径: {savePath}");

        if (!System.IO.Directory.Exists(savePath))
        {
            Debug.LogWarning("保存路径不存在: " + savePath);
            return;
        }

        string[] jsonFiles = System.IO.Directory.GetFiles(savePath, "*.json");
        Debug.Log($"[2D Map List] 找到 {jsonFiles.Length} 个 JSON 文件");

        if (jsonFiles.Length == 0)
        {
            Debug.Log("没有找到任何关卡文件");
            return;
        }

        // 为每个文件创建列表项
        foreach (string filePath in jsonFiles)
        {
            string fileName = System.IO.Path.GetFileName(filePath);
            Debug.Log($"[2D Map List] 处理文件: {fileName} (完整路径: {filePath})");

            // 读取并显示JSON内容
            try
            {
                string jsonContent = System.IO.File.ReadAllText(filePath);
                Debug.Log($"[2D Map List] JSON 内容:\n{jsonContent}");

                // 尝试获取文件大小
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(filePath);
                Debug.Log($"[2D Map List] 文件大小: {fileInfo.Length} 字节, 创建时间: {fileInfo.CreationTime}, 修改时间: {fileInfo.LastWriteTime}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[2D Map List] 读取文件失败: {e.Message}");
            }

            if (TwoDMapListItem == null)
            {
                Debug.LogError("TwoDMapListItem Prefab 未赋值");
                continue;
            }

            // 创建列表项
            GameObject item = Instantiate(TwoDMapListItem, TwoDMapListContent.transform);
            Debug.Log($"[2D Map List] 已创建列表项 GameObject: {item.name}");

            // 设置列表项数据
            var itemScript = item.GetComponent<UI.AR.TwoDMapListItem>();
            if (itemScript != null)
            {
                itemScript.fileName = fileName;
                Debug.Log($"[2D Map List] 已设置文件名: {fileName}");
            }
            else
            {
                Debug.LogError("TwoDMapListItem 上未找到 TwoDMapListItem 脚本组件");
            }
        }

        Debug.Log($"[2D Map List] 列表更新完成，共加载 {jsonFiles.Length} 个关卡文件");
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
    public void OpenPlayPanel()
    {
        PlayPanel.SetActive(true);
    }
    public void ClosePlayPanel()
    {
        PlayPanel.SetActive(false);
    }

    public void OpenGamePLay()
    {
        if (!EasyARSpatialMapEditorManager.Instance.isMapLocalized)
        {
            Debug.LogWarning("[EasyAR Spatial Map Editor] 地图未本地化，无法进入播放模式");
            return;
        }
        CloseARObjectListSidePanel();
        CloseParentSidePanel();
        OpenPlayPanel();
        EasyARSpatialMapEditorManager.Instance.EnterPlayMode();
    }
    public void CloseGamePlay()
    {
        ClosePlayPanel();
        // TODO 应该恢复编辑模式,在editor manager中处理
        OpenParentSidePanel();
        EasyARSpatialMapEditorManager.Instance.ExitPlayMode();
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

    #region Mesh Alignment UI Methods

    /// <summary>
    /// 打开Mesh配置面板并开始对齐模式
    /// </summary>
    public void OpenMeshAlignmentPanel()
    {
        if (!EasyARSpatialMapEditorManager.Instance.IsMapLocalized)
        {
            Debug.LogWarning("[EasyAR UI] 地图未本地化，无法配置Mesh");
            return;
        }

        if (EasyARSpatialMapEditorManager.Instance.denseMeshPrefab == null)
        {
            Debug.LogWarning("[EasyAR UI] denseMeshPrefab未指定，无法配置Mesh");
            return;
        }

        // 按照OnAddARMap的逻辑：先关闭父面板 -> 打开目标面板 -> 启动功能
        CloseParentSidePanel();
        MeshAlignmentPanel.SetActive(true);

        EasyARSpatialMapEditorManager.Instance.StartMeshAlignment();
    }

    /// <summary>
    /// 关闭Mesh配置面板（不保存）
    /// </summary>
    public void CloseMeshAlignmentPanel()
    {
        if (MeshAlignmentPanel != null)
        {
            MeshAlignmentPanel.SetActive(false);
        }

        // 恢复主面板
        OpenParentSidePanel();

        Debug.Log("[EasyAR UI] 已关闭Mesh配置面板");
    }

    /// <summary>
    /// 确认Mesh对齐（保存）
    /// </summary>
    public void OnConfirmMeshAlignment()
    {
        Debug.Log("[EasyAR UI] 确认Mesh对齐");

        // 完成对齐并保存
        EasyARSpatialMapEditorManager.Instance.FinalizeMeshAlignment();

        // 关闭面板
        CloseMeshAlignmentPanel();
    }

    /// <summary>
    /// 取消Mesh对齐（不保存）
    /// </summary>
    public void OnCancelMeshAlignment()
    {
        Debug.Log("[EasyAR UI] 取消Mesh对齐");

        // 取消对齐
        EasyARSpatialMapEditorManager.Instance.CancelMeshAlignment();

        // 关闭面板
        CloseMeshAlignmentPanel();
    }

    #endregion
}