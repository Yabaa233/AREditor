using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelData
{
    public List<PlacedObjectData> objects = new();

}


[System.Serializable]
public class ObjectTemplateData
{
    public string templateID;
    public string templateName;
    public GameObject prefab;  // 对应的Prefab
    public Sprite icon;                   // 图标，用于UI或编辑器展示

    public List<TriggerActionEventData> defaultEvents = new();

}


[System.Serializable]
public class PlacedObjectData
{
    // public GameObject instance;
    public string templateID;

    public string ID;

    public bool ifHiddenAtGameStart;

    //3d位置
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public List<TriggerActionEventData> events = new();  // 这个物体绑定的事件列表

}

[System.Serializable]
public class TriggerActionEventData
{
    public TriggerType triggerType;
    public ActionType actionType;
    public string targetObjectID;
}

public enum TriggerType { OnEnter, OnExit }
public enum ActionType { Win, Lose, Enable, Disable }

[System.Serializable]
public class SceneSaveData
{
    public List<PlacedObjectData> objects = new();
}