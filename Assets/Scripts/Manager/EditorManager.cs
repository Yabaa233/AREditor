using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Windows.Forms;
using Sirenix.OdinInspector;
using UnityEngine;

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
    private Transform levelParent;

    void Start()
    {
        levelParent = GameObject.FindGameObjectWithTag("LevelParent").transform;
        if (levelParent is null) Debug.LogWarning("Cant find LevelParent at start");
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


    public void LoadSceneFromJson(string fileName)
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
            if (template == null || template.prefab == null)
            {
                Debug.LogWarning($"Template not found for ID: {data.templateID}");
                continue;
            }

            GameObject obj = Instantiate(template.prefab);
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

}