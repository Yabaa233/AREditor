using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private RectTransform dragIcon;
    private Image iconImage;
    private Vector2 originalPos;

    //TODO 为了做成预制体，得代码赋值
    public GameObject modelPrefab; // 拖拽代表的 3D 模型

    private RawImage targetRawImage;
    public Camera raycastCamera; // 渲染 rawImage 的相机
    public RectTransform rawImageRect;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        targetRawImage = rawImageRect.GetComponent<RawImage>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragIcon = new GameObject("DragIcon").AddComponent<RectTransform>();
        dragIcon.SetParent(canvas.transform, false);
        dragIcon.sizeDelta = ((RectTransform)transform).sizeDelta;

        iconImage = dragIcon.gameObject.AddComponent<Image>();
        iconImage.sprite = GetComponent<Image>().sprite;
        iconImage.raycastTarget = false;
        iconImage.color = new Color(1, 1, 1, 0.8f); // 半透明

        originalPos = eventData.position;
        dragIcon.position = originalPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon)
            dragIcon.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon)
            Destroy(dragIcon.gameObject);

        // 检查是否在 RawImage 区域内
        if (RectTransformUtility.RectangleContainsScreenPoint(rawImageRect, eventData.position))
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                Vector2 uv = RectPointToUV(rawImageRect, localPoint);
                Ray ray = raycastCamera.ViewportPointToRay(uv);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Instantiate(modelPrefab, hit.point, Quaternion.identity);
                }
            }
        }
    }

    private Vector2 RectPointToUV(RectTransform rect, Vector2 localPos)
    {
        float width = rect.rect.width;
        float height = rect.rect.height;
        float u = (localPos.x + width / 2f) / width;
        float v = (localPos.y + height / 2f) / height;
        return new Vector2(u, v);
    }
}
