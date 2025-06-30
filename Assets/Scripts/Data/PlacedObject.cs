using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public enum ViewMode { Plane, AR }

public class PlacedObject : MonoBehaviour
{
    [SerializeField, Required]
    private PlacedObjectTemplateDatabase templateDatabase;

    [ValueDropdown(nameof(GetAllTemplateIDs))]
    [SerializeField]
    private string selectedTemplateID;

    [SerializeField, ReadOnly]
    public PlacedObjectData runtimeData;

    private bool initialized = false;

    private PlacedObjectPlaneViewer viewer;

    public ViewMode viewMode = ViewMode.Plane;

    public void InitializeFromJson()
    {

        // if (templateDatabase == null)
        // {
        //     Debug.LogError("Template Database is not assigned.");
        //     return;
        // }

        ObjectTemplateData template = templateDatabase.GetTemplateByID(selectedTemplateID);
        if (template == null)
        {
            Debug.LogError($"Template ID '{selectedTemplateID}' not found in database ");
            return;
        }

        // runtimeData = new PlacedObjectData
        // {
        //     ID = EditorManager.Instance.GenerateUniqueID(),
        //     templateID = selectedTemplateID,
        //     position = transform.position,
        //     rotation = transform.rotation.eulerAngles,
        //     scale = transform.localScale,
        //     events = new(template.defaultEvents)
        // };

        if(viewMode == ViewMode.Plane)
        {
            transform.GetComponent<PlacedObjectPlaneViewer>().enabled = true;
            transform.GetComponent<PlacedObjectPlaneViewer>().SetMarker(template.icon);
        }
        else
        {
            transform.GetComponent<PlacedObjectPlaneViewer>().enabled = false;
        }

        initialized = true;
    }

    void Start()
    {
        if (!initialized)
        {
            InitializeFromTemplate();
        }

        if (ARSession.state == ARSessionState.SessionTracking)
        {
            // Debug.Log("AR mode");
            viewMode = ViewMode.AR;
        }
        else
        {
            // Debug.Log("not AR mode");
            viewMode = ViewMode.Plane;
        }

        if (viewMode == ViewMode.Plane)
        {
            transform.GetComponent<PlacedObjectPlaneViewer>().enabled = true;
            viewer = gameObject.GetComponent<PlacedObjectPlaneViewer>();
        }
        else
        {
            transform.GetComponent<PlacedObjectPlaneViewer>().enabled = false;
        }
    }

    public void InitializeFromTemplate()
    {
        if (templateDatabase == null)
        {
            Debug.LogError("Template Database is not assigned.");
            return;
        }

        ObjectTemplateData template = templateDatabase.GetTemplateByID(selectedTemplateID);
        if (template == null)
        {
            Debug.LogError($"Template ID '{selectedTemplateID}' not found in database ");
            return;
        }

        runtimeData = new PlacedObjectData
        {
            ID = EditorManager.Instance.GenerateUniqueID(),
            templateID = selectedTemplateID,
            position = transform.position,
            rotation = transform.rotation.eulerAngles,
            scale = transform.localScale,
            events = new(template.defaultEvents)
        };

        if (viewMode == ViewMode.Plane)
        {
            transform.GetComponent<PlacedObjectPlaneViewer>().enabled = true;
            transform.GetComponent<PlacedObjectPlaneViewer>().SetMarker(template.icon);
        }
        else
        {
            transform.GetComponent<PlacedObjectPlaneViewer>().enabled = false;
        }

        initialized = true;
    }

    // 用于 Odin 下拉菜单显示模板ID
    private IEnumerable<ValueDropdownItem<string>> GetAllTemplateIDs()
    {
        if (templateDatabase == null) yield break;

        foreach (var template in templateDatabase.templates)
        {
            // 下拉显示内容："bomb_01 - 炸弹"
            string display = $"{template.templateID} . {template.templateName}";
            yield return new ValueDropdownItem<string>(display, template.templateID);
        }
    }

    //TODO 销毁处理，比如从他者事件中移除
    void OnDestroy()
    {

    }

}
