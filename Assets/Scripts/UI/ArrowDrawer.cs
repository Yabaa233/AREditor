using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlacedObject))]
public class RuntimeArrowDrawer : MonoBehaviour
{
    [Header("箭头外观设置（运行时可调）")]
    public GameObject arrowHeadPrefab;
    public float lineWidth = 0.05f;
    public Color lineColor = Color.red;
    public float yOffset = 0.2f; // 新增：Y方向偏移量，防止线被遮挡或嵌入模型

    [Header("渲染层设置")]
    public string sortingLayerName = "UI";  // 设置为 "UI" 或你自定义的 Sorting Layer
    public int sortingOrder = 1000;

    private PlacedObject placedObject;
    private List<TriggerActionEventData> currentEvents = new();
    private List<LineRenderer> lines = new();
    private List<Transform> arrowHeads = new();

    void Awake()
    {
        placedObject = GetComponent<PlacedObject>();
    }

    void Update()
    {
        if (placedObject.runtimeData == null)
            return;

        var events = placedObject.runtimeData.events;

        if (NeedRebuild(events))
        {
            RebuildArrows(events);
        }

        for (int i = 0; i < currentEvents.Count; i++)
        {
            var evt = currentEvents[i];
            if (evt.targetObjectID == null) continue;

            // ✅ Y轴偏移处理
            Vector3 from = transform.position + Vector3.up * yOffset;
            Vector3 to = EditorManager.Instance.GetGameObjectByID(evt.targetObjectID).transform.position + Vector3.up * yOffset;

            var line = lines[i];
            line.SetPosition(0, from);
            line.SetPosition(1, to);

            // 实时更新线宽与颜色
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.startColor = lineColor;
            line.endColor = lineColor;

            if (arrowHeads[i] != null)
            {
                arrowHeads[i].position = Vector3.Lerp(to, from, 0.1f);
                arrowHeads[i].rotation = Quaternion.LookRotation(to - from);
                arrowHeads[i].localScale = Vector3.one * 0.2f;
            }
        }
    }

    bool NeedRebuild(List<TriggerActionEventData> newEvents)
    {
        if (newEvents.Count != currentEvents.Count) return true;

        for (int i = 0; i < newEvents.Count; i++)
        {
            if (newEvents[i].targetObjectID != currentEvents[i].targetObjectID)
                return true;
        }

        return false;
    }

    void RebuildArrows(List<TriggerActionEventData> newEvents)
    {
        foreach (var line in lines)
            if (line != null) Destroy(line.gameObject);

        foreach (var arrow in arrowHeads)
            if (arrow != null) Destroy(arrow.gameObject);

        lines.Clear();
        arrowHeads.Clear();
        currentEvents.Clear();

        foreach (var evt in newEvents)
        {
            if (evt.targetObjectID != null)
            {
                currentEvents.Add(evt);

                GameObject lineObj = new GameObject("LineToTarget");
                lineObj.transform.parent = this.transform;
                LineRenderer line = lineObj.AddComponent<LineRenderer>();

                line.material = new Material(Shader.Find("Sprites/Default"));
                line.positionCount = 2;
                line.useWorldSpace = true;

                // ✅ 设置渲染层级
                line.sortingLayerName = sortingLayerName;
                line.sortingOrder = sortingOrder;

                lines.Add(line);

                if (arrowHeadPrefab != null)
                {
                    GameObject arrow = Instantiate(arrowHeadPrefab, this.transform);
                    arrow.layer = LayerMask.NameToLayer(sortingLayerName); // 可选
                    arrowHeads.Add(arrow.transform);
                }
                else
                {
                    arrowHeads.Add(null);
                }
            }
        }
    }
}
