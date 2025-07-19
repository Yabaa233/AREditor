using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

// public enum ViewMode { Plane, AR }

public class PlacedObject : MonoBehaviour
{
    [SerializeField, Required]
    protected PlacedObjectTemplateDatabase templateDatabase;

    [ValueDropdown(nameof(GetAllTemplateIDs))]
    [SerializeField]
    protected string selectedTemplateID;

    [SerializeField, ReadOnly]
    public PlacedObjectData runtimeData;

    protected bool initialized = false;

    private PlacedObjectPlaneViewer viewer;

    // public ViewMode viewMode = ViewMode.Plane;

    public virtual void InitializeFromJson()
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

        // if (viewMode == ViewMode.Plane)
        // {
        transform.GetComponent<PlacedObjectPlaneViewer>().enabled = true;
        transform.GetComponent<PlacedObjectPlaneViewer>().SetMarker(template.icon);
        // }
        // else
        // {
        //     transform.GetComponent<PlacedObjectPlaneViewer>().enabled = false;
        // }

        initialized = true;
    }

    protected virtual void Start()
    {
        if (!initialized)
        {
            InitializeFromTemplate();
        }

        // if (ARSession.state == ARSessionState.SessionTracking)
        // {
        //     // Debug.Log("AR mode");
        //     viewMode = ViewMode.AR;
        // }
        // else
        // {
        //     // Debug.Log("not AR mode");
        //     viewMode = ViewMode.Plane;
        // }

        // if (viewMode == ViewMode.Plane)
        // {
        transform.GetComponent<PlacedObjectPlaneViewer>().enabled = true;
        viewer = gameObject.GetComponent<PlacedObjectPlaneViewer>();
        // }
        // else
        // {
        //     transform.GetComponent<PlacedObjectPlaneViewer>().enabled = false;
        // }
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

        // if (viewMode == ViewMode.Plane)
        // {
        transform.GetComponent<PlacedObjectPlaneViewer>().enabled = true;
        transform.GetComponent<PlacedObjectPlaneViewer>().SetMarker(template.icon);
        // }
        // else
        // {
        //     transform.GetComponent<PlacedObjectPlaneViewer>().enabled = false;
        // }

        initialized = true;
    }

    // For Unity display, using Odin to show a dropdown menu of template IDs
    private IEnumerable<ValueDropdownItem<string>> GetAllTemplateIDs()
    {
        if (templateDatabase == null) yield break;

        foreach (var template in templateDatabase.templates)
        {
            // Display format in dropdown: "bomb_01 - Bomb"
            string display = $"{template.templateID} . {template.templateName}";
            yield return new ValueDropdownItem<string>(display, template.templateID);
        }
    }

    void OnDestroy()
    {

    }

}
