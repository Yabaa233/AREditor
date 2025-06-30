using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Placed Object Template Database")]
public class PlacedObjectTemplateDatabase : ScriptableObject
{
    public List<ObjectTemplateData> templates = new();

    public ObjectTemplateData GetTemplateByID(string id)
    {
        return templates.Find(t => t.templateID == id);
    }
}