using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Windows.Forms;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.AR;

/// <summary>
/// Manager of 2D Eitor
/// </summary>
public class EditorManager : singleton<EditorManager>
{
    public PlacedObjectTemplateDatabase templateDB;

    [ReadOnly]
    public List<GameObject> LevelObjects = new();

    [ReadOnly]
    public GameObject focusedObject;

    // Parent of level objects
    [Header("Set at 2d interface")]
    public Transform levelParent;

    [Header("Set at AR interface")]
    public ARPlacementInteractable ARplacement;
    public ARAnchorManager anchorManager;

    void Start()
    {
    }

    /// <summary>
    /// Register an Object
    /// </summary>
    public void RegisterObject(GameObject obj)
    {
        if (!LevelObjects.Contains(obj))
        {
            LevelObjects.Add(obj);
        }
    }

    /// <summary>
    /// Remove a placed object (and clear focus if it's currently focused)
    /// </summary>
    public void UnregisterObject(GameObject obj)
    {
        if (LevelObjects.Contains(obj))
        {
            LevelObjects.Remove(obj);
        }

        if (focusedObject == obj)
        {
            focusedObject = null;
        }

        DestroyImmediate(obj);
    }

    /// <summary>
    /// Set focus (must be an object from LevelObjects)
    /// </summary>
    public void SetFocus(GameObject obj)
    {
        if (obj == null || LevelObjects.Contains(obj))
        {
            foreach (var item in LevelObjects)
            {
                item.GetComponent<PlacedObjectPlaneViewer>().uiMarker.focusedIcon.SetActive(false);
            }
            focusedObject = obj;
            focusedObject.GetComponent<PlacedObjectPlaneViewer>().uiMarker.focusedIcon.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Trying to focus object not registered in LevelObjects.");
        }
    }

    /// <summary>
    /// Cancel cur focus
    /// </summary>
    public void ClearFocus()
    {
        focusedObject.GetComponent<PlacedObjectPlaneViewer>().uiMarker.focusedIcon.SetActive(false);
        focusedObject = null;
    }

    /// <summary>
    /// Check if the object is the current focus
    /// </summary>
    public bool IsFocused(GameObject obj)
    {
        return focusedObject == obj;
    }

    // Generate True Event Logic for data
    public void GenerateEventLogic()
    {
        foreach (var item in LevelObjects)
        {
            var data = item.GetComponent<PlacedObject>().runtimeData;
            if (data is null)
            {
                return;
            }

            foreach (var evt in data.events)
            {

                switch (evt.triggerType)
                {
                    case TriggerType.OnEnter:
                        AddTriggerHandler(item, evt, true);
                        break;
                    case TriggerType.OnExit:
                        AddTriggerHandler(item, evt, false);
                        break;
                }

            }

        }

    }

    private void AddTriggerHandler(GameObject source, TriggerActionEventData evt, bool onEnter)
    {
        var collider = source.GetComponent<Collider>();
        if (collider is not null)
        {
            DestroyImmediate(collider);
        }

        collider = source.AddComponent<BoxCollider>();
        collider.isTrigger = true;

        var handler = source.GetComponent<EventActionHandler>();
        if (handler == null)
        {
            handler = source.AddComponent<EventActionHandler>();
            handler.eventList = new List<TriggerActionEventData>();
        }

        // Add Event
        handler.eventList.Clear();
        handler.eventList.Add(evt);
        handler.Register(onEnter);
    }

    public void SaveSceneToJson(string fileName)
    {
        SceneSaveData sceneData = new();

        foreach (var item in LevelObjects)
        {
            var placed = item.GetComponent<PlacedObject>();
            if (placed == null || placed.runtimeData == null)
                continue;

            var data = placed.runtimeData;

            data.position = item.transform.localPosition;
            data.rotation = item.transform.localEulerAngles;
            data.scale = item.transform.localScale;

            sceneData.objects.Add(data);
        }

        string json = JsonUtility.ToJson(sceneData, true);

        string savePath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(savePath, json);

        Debug.Log("Scene saved to: " + savePath);
    }

    public void LoadSceneFromJsonAR(string fileName)
    {
        LoadSceneFromJson(fileName, true);
    }

    public void LoadSceneFromJson2D(string fileName)
    {
        LoadSceneFromJson(fileName, false);
    }


    public void LoadSceneFromJson(string fileName,bool isAR)
    {

        if (levelParent is null)
        {
            levelParent = GameObject.FindGameObjectWithTag("LevelParent").transform;
        }
        if (levelParent is null)
        {
            Debug.LogError("Cant find LevelParent at Load");
        }

        string fullPath = Path.Combine(Application.persistentDataPath, fileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogError("Scene file not found: " + fullPath);
            return;
        }

        string json = File.ReadAllText(fullPath);

        SceneSaveData sceneData = JsonUtility.FromJson<SceneSaveData>(json);

        // Clear existing LevelObjects
        foreach (var obj in LevelObjects.ToList())
        {
            UnregisterObject(obj);
        }
        LevelObjects.Clear();

        foreach (var data in sceneData.objects)
        {
            var template = templateDB.GetTemplateByID(data.templateID);
            if (template == null || template.TwoDPrefab == null ||template.ARPrefab == null)
            {
                Debug.LogWarning($"Template not found for ID: {data.templateID}");
                continue;
            }

            GameObject obj;
            if (isAR)
            {
                obj = Instantiate(template.ARPrefab);
            }
            else
            {
                obj = Instantiate(template.TwoDPrefab);

            }
            obj.transform.SetParent(levelParent, worldPositionStays: false); // Automatically apply parent’s TRS (Translation, Rotation, Scale)

            obj.transform.localPosition = data.position;
            obj.transform.localEulerAngles = data.rotation;
            obj.transform.localScale = data.scale;

            // TODO: Handle initial visibility
            obj.SetActive(!data.ifHiddenAtGameStart); 

            // Set runtimeData
            var placed = obj.GetComponent<PlacedObject>();
            if (placed != null)
            {
                placed.runtimeData = data;
                placed.InitializeFromJson();
            }

            LevelObjects.Add(obj);

        }

        Debug.Log("Scene loaded from: " + fullPath);

        //TODO: Viewer in AR mode

        //if(isAR&&levelParent is not null)
        //{
        //    if (levelParent.TryGetComponent(out MeshRenderer renderer))
        //        renderer.enabled = false;

        //    if (levelParent.parent != null && levelParent.parent.TryGetComponent(out BoxCollider collider))
        //        collider.enabled = false;

        //    if (ARplacement is not null) ARplacement.placementPrefab = null;
        //}
    }

    public void Set2AR()
    {
        foreach (var obj in LevelObjects)
        {
            if (obj == null) continue;

            // Record the original world position and rotation
            Vector3 worldPos = obj.transform.position;
            Quaternion worldRot = obj.transform.rotation;

            // Detach from the current parent (e.g., levelParent) and preserve world transform
            obj.transform.SetParent(null, worldPositionStays: true);

            // Reapply world position and rotation (for safety)
            obj.transform.position = worldPos;
            obj.transform.rotation = worldRot;

            // Force a fixed local scale (e.g., default value)
            obj.transform.localScale = Vector3.one;

            // Add ARAnchor to improve tracking stability (if not already added)
            if (anchorManager != null && obj.GetComponent<ARAnchor>() == null)
            {
                obj.AddComponent<ARAnchor>();
            }
        }

        Debug.Log("✅ All objects moved to AR world space and anchors attached (scale reset)");

        // Hide the levelParent visual and collider if present
        if (levelParent != null)
        {
            if (levelParent.TryGetComponent(out MeshRenderer renderer))
                renderer.enabled = false;

            if (levelParent.parent != null && levelParent.parent.TryGetComponent(out BoxCollider collider))
                collider.enabled = false;
        }

        // Disable ARPlacement behavior to avoid interference with loaded objects
        if (ARplacement != null)
        {
            ARplacement.placementPrefab = null;
        }
    }




    public GameObject GetGameObjectByID(string id)
    {
        return LevelObjects
            .FirstOrDefault(item =>
                item.TryGetComponent<PlacedObject>(out var placed) &&
                placed.runtimeData != null &&
                placed.runtimeData.ID == id
            );
    }


    public string GenerateUniqueID()
    {
        List<string> existingIDs = LevelObjects
            .Select(obj => obj.GetComponent<PlacedObject>()?.runtimeData?.ID)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        string newID;
        do
        {
            newID = Guid.NewGuid().ToString();
        }
        while (existingIDs.Contains(newID));

        return newID;
    }

    public void ChangeScene(string name)
    {
        SceneManager.LoadScene(name);
    }


}