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

    // TODO Visualize win/lose and visible/invisible status
    // public Sprite winSprite;
    // public Sprite loseSprite;

    void Awake()
    {
        uiParent = GameObject.FindGameObjectWithTag("ObjectIconParent").transform;

        // Instantiate UI marker
        GameObject markerInstance = Instantiate(uiMarkerPrefab, uiParent);
        uiMarker = markerInstance.GetComponent<PlacedObjectUIMarker>();
        myData = gameObject.GetComponent<PlacedObject>();
    }

    void Update()
    {
        UpdateMarkerPosition();
        UpdateLogicViewer(); // Update lines every frame // TODO should not update every frame
    }

    public void SetMarker(Sprite sprite)
    {
        uiMarker.gameObject.GetComponent<Image>().sprite = sprite;
        uiMarker.SetParent(gameObject);
    }

    public void UpdateMarkerPosition()
    {
        // 1. World position → Viewport
        Vector3 viewportPos = UIManager.Instance.raycastCamera.WorldToViewportPoint(transform.position);

        // Check if object is in front of the camera
        if (viewportPos.z < 0)
        {
            uiMarker.gameObject.SetActive(false);
            return;
        }

        // 2. Viewport → RawImage local position
        float u = viewportPos.x;
        float v = viewportPos.y;

        float width = UIManager.Instance.targetRawImage.rectTransform.rect.width;
        float height = UIManager.Instance.targetRawImage.rectTransform.rect.height;

        float x = (u - 0.5f) * width;
        float y = (v - 0.5f) * height;
        Vector2 localPos = new Vector2(x, y);

        // 3. Set green highlight area
        uiMarker.gameObject.SetActive(true);
        uiMarker.GetComponent<RectTransform>().anchoredPosition = localPos;
    }

    private void UpdateLogicViewer()
    {
        // Clear old lines
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

            // Draw line
            RectTransform start = uiMarker.GetComponent<RectTransform>();
            RectTransform end = targetViewer.uiMarker.GetComponent<RectTransform>();

            GameObject lineObj = Instantiate(uiLinePrefab, uiParent);
            RectTransform lineRect = lineObj.GetComponent<RectTransform>();
            eventLines.Add(lineRect);

            DrawLineBetween(start, end, lineRect, evt.actionType == ActionType.Enable ? Color.green : Color.red, 12f);

            // Add arrow
            GameObject arrowObj = Instantiate(uiArrowPrefab, uiParent);
            arrowObj.GetComponent<Image>().color = evt.actionType == ActionType.Enable ? Color.green : Color.red;
            RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowInstances.Add(arrowRect);

            // Slightly move forward to avoid covering the target
            Vector2 dir = (end.anchoredPosition - start.anchoredPosition).normalized;
            arrowRect.anchoredPosition = end.anchoredPosition - dir * 20f; // Adjust arrow position

            // Set rotation
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrowRect.localRotation = Quaternion.Euler(0, 0, angle + 90f);

            // Set size (adjustable)
            arrowRect.sizeDelta = new Vector2(50, 50);

            // // Add Win / Lose icon
            // if (evt.actionType == ActionType.Win || evt.actionType == ActionType.Lose)
            // {
            //     GameObject iconObj = new GameObject(evt.actionType.ToString() + "Icon");
            //     iconObj.transform.SetParent(uiMarker.transform);
            //     iconObj.transform.SetAsLastSibling(); // Make sure it's on top

            //     RectTransform rt = iconObj.AddComponent<RectTransform>();
            //     rt.sizeDelta = new Vector2(30, 30);
            //     rt.anchorMin = new Vector2(0.5f, 0f);
            //     rt.anchorMax = new Vector2(0.5f, 0f);
            //     rt.pivot = new Vector2(0.5f, 0f);
            //     rt.anchoredPosition = new Vector2(0, 50); // Show above the marker

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
        if (uiMarker != null && uiMarker.gameObject != null)
        {
            DestroyImmediate(uiMarker.gameObject);
        }

        foreach (var item in eventLines)
        {
            if (item != null && item.gameObject != null)
                DestroyImmediate(item.gameObject);
        }
        foreach (var item in arrowInstances)
        {
            if (item != null && item.gameObject != null)
                DestroyImmediate(item.gameObject);
        }
    }
}
