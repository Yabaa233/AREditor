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
    [SerializeField]
    public string templateID;

    [SerializeField]
    public string ID;

    [SerializeField]
    public bool ifHiddenAtGameStart;

    [SerializeField]
    public Vector3 position;

    [SerializeField]
    public Vector3 rotation;

    [SerializeField]
    public Vector3 scale;

    [SerializeField]
    public List<TriggerActionEventData> events = new();  // the list of events bound to this object

}

[System.Serializable]
/// <summary>
/// Event data
/// </summary>
public class TriggerActionEventData
{
    [SerializeField]
    public TriggerType triggerType;

    [SerializeField]
    public ActionType actionType;

    [SerializeField]
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
