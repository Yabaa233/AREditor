using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlacedObjectPlaneViewer : MonoBehaviour
{
    private Transform uiParent;
    public GameObject uiMarkerPrefab;

    //private RectTransform uiMarker;
    public PlacedObjectUIMarker uiMarker;

    private PlacedObject myData;

    public GameObject uiLinePrefab;
    public GameObject uiArrowPrefab;
    private List<RectTransform> eventLines = new();
    private List<RectTransform> arrowInstances = new();

    // TODO 胜利/失败，可视/不可视的 可视化
    // public Sprite winSprite;
    // public Sprite loseSprite;


    void Awake()
    {
        uiParent = GameObject.FindGameObjectWithTag("ObjectIconParent").transform;

        // 实例化 UI 标记
        GameObject markerInstance = Instantiate(uiMarkerPrefab, uiParent);
        uiMarker = markerInstance.GetComponent<PlacedObjectUIMarker>();
        myData = gameObject.GetComponent<PlacedObject>();

    }

    void Update()
    {
        UpdateMarkerPosition();
        UpdateLogicViewer(); // 每帧更新线条 //TODO 不应该每帧
    }

    public void SetMarker(Sprite sprite)
    {
        uiMarker.gameObject.GetComponent<Image>().sprite = sprite;
        uiMarker.SetParent(gameObject);
    }

    public void UpdateMarkerPosition()
    {
        // 1. 世界坐标 → Viewport
        Vector3 viewportPos = UIManager.Instance.raycastCamera.WorldToViewportPoint(transform.position);

        // 检查物体是否在摄像机前方
        if (viewportPos.z < 0)
        {
            uiMarker.gameObject.SetActive(false);
            return;
        }

        // 2. Viewport → RawImage 局部坐标
        float u = viewportPos.x;
        float v = viewportPos.y;

        float width = UIManager.Instance.targetRawImage.rectTransform.rect.width;
        float height = UIManager.Instance.targetRawImage.rectTransform.rect.height;

        float x = (u - 0.5f) * width;
        float y = (v - 0.5f) * height;
        Vector2 localPos = new Vector2(x, y);

        // 3. 设置绿色高亮区域
        uiMarker.gameObject.SetActive(true);
        uiMarker.GetComponent<RectTransform>().anchoredPosition = localPos;
    }

    private void UpdateLogicViewer()
    {
        // 清理旧线
        foreach (var line in eventLines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
        eventLines.Clear();

        foreach (var arrow in arrowInstances)
        {
            if (arrow != null)
                Destroy(arrow.gameObject);
        }
        arrowInstances.Clear();

        // foreach (Transform child in uiMarker.transform)
        // {
        //     if (child.name == "WinIcon" || child.name == "LoseIcon")
        //         Destroy(child.gameObject);
        // }

        if (myData == null || myData.runtimeData == null || myData.runtimeData.events == null)
            return;

        foreach (var evt in myData.runtimeData.events)
        {
            if (evt.actionType != ActionType.Enable && evt.actionType != ActionType.Disable)
                continue;

            if (evt.targetObjectID == null) continue;

            var target = EditorManager.Instance.GetGameObjectByID(evt.targetObjectID);
            if (target is null) continue;
            var targetViewer = target.GetComponent<PlacedObjectPlaneViewer>();
            if (targetViewer == null || targetViewer.uiMarker == null) continue;

            // 画线
            RectTransform start = uiMarker.GetComponent<RectTransform>();
            RectTransform end = targetViewer.uiMarker.GetComponent<RectTransform>();

            GameObject lineObj = Instantiate(uiLinePrefab, uiParent);
            RectTransform lineRect = lineObj.GetComponent<RectTransform>();
            eventLines.Add(lineRect);

            DrawLineBetween(start, end, lineRect, evt.actionType == ActionType.Enable ? Color.green : Color.red, 12f);

            // 添加箭头
            GameObject arrowObj = Instantiate(uiArrowPrefab, uiParent);
            arrowObj.GetComponent<Image>().color = evt.actionType == ActionType.Enable ? Color.green : Color.red;
            RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowInstances.Add(arrowRect);

            // 设置位置（略微前移，让箭头不盖住目标）
            Vector2 dir = (end.anchoredPosition - start.anchoredPosition).normalized;
            arrowRect.anchoredPosition = end.anchoredPosition - dir * 20f; // 微调箭头位置

            // 设置角度
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrowRect.localRotation = Quaternion.Euler(0, 0, angle + 90f);

            // 设置大小（可调）
            arrowRect.sizeDelta = new Vector2(50, 50);

            // // 添加 Win / Lose 图标
            // if (evt.actionType == ActionType.Win || evt.actionType == ActionType.Lose)
            // {
            //     GameObject iconObj = new GameObject(evt.actionType.ToString() + "Icon");
            //     iconObj.transform.SetParent(uiMarker.transform);
            //     iconObj.transform.SetAsLastSibling(); // 保证在最上层

            //     RectTransform rt = iconObj.AddComponent<RectTransform>();
            //     rt.sizeDelta = new Vector2(30, 30);
            //     rt.anchorMin = new Vector2(0.5f, 0f);
            //     rt.anchorMax = new Vector2(0.5f, 0f);
            //     rt.pivot = new Vector2(0.5f, 0f);
            //     rt.anchoredPosition = new Vector2(0, 50); // 显示在 marker 上方

            //     Image img = iconObj.AddComponent<Image>();
            //     img.sprite = evt.actionType == ActionType.Win ? winSprite : loseSprite;
            //     img.color = Color.white;
            // }
        }
    }

    private void DrawLineBetween(RectTransform a, RectTransform b, RectTransform line, Color color, float thickness)
    {
        Vector2 dir = (b.anchoredPosition - a.anchoredPosition).normalized;
        Vector2 start = a.anchoredPosition + dir * 20f;
        Vector2 end = b.anchoredPosition - dir * 20f;

        Vector2 diff = end - start;
        float length = diff.magnitude;

        line.sizeDelta = new Vector2(length, thickness);
        line.anchoredPosition = start + diff * 0.5f;
        line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);

        var img = line.GetComponent<Image>();
        if (img != null)
            img.color = color;
    }

    void OnDestroy()
    {
        if (uiMarker != null&&uiMarker.gameObject != null)
        {
            DestroyImmediate(uiMarker.gameObject);
        }

        foreach (var item in eventLines)
        {
            if(item != null && item.gameObject != null)
                DestroyImmediate(item.gameObject);
        }
        foreach (var item in arrowInstances)
        {
            if (item != null && item.gameObject != null)
                DestroyImmediate(item.gameObject);
        }
    }

}
