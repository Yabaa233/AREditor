using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
/// <summary>
/// Predefined level object template
/// </summary>
public class ObjectTemplateData
{
    public string templateID;
    public string templateName;

    public GameObject TwoDPrefab;
    public GameObject ARPrefab;

    public Sprite icon;  //For 2D Editor UI

    public List<TriggerActionEventData> defaultEvents = new();

}


[System.Serializable]
/// <summary>
/// Runtime data attached to level objects
/// </summary>
public class PlacedObjectData
{

    public string templateID;

    public string ID;

    public bool ifHiddenAtGameStart;

    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public List<TriggerActionEventData> events = new();  // the list of events bound to this object

}

[System.Serializable]
/// <summary>
/// Event data
/// </summary>
public class TriggerActionEventData
{
    public TriggerType triggerType;
    public ActionType actionType;
    public string targetObjectID;
}

public enum TriggerType { OnEnter, OnExit }
public enum ActionType { Win, Lose, Enable, Disable }

[System.Serializable]
/// <summary>
/// For serialization
/// </summary>
public class SceneSaveData
{
    public List<PlacedObjectData> objects = new();
}
