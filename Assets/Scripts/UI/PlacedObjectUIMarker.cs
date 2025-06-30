using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlacedObjectUIMarker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [ShowInInspector, ReadOnly]
    private GameObject ParentObject;

    [Required]
    public GameObject focusedIcon;
    private bool isDragging = false;

    public void SetParent(GameObject parentObject)
    {
        ParentObject = parentObject;
    }

    public void Focus()
    {
        if (UIManager.Instance.ifInPicker)
        {
            //TODO 选到自己的处理，UI提示？        
            UIManager.Instance.onPick?.Invoke(ParentObject);
            return;
        }
        EditorManager.Instance.SetFocus(ParentObject);
        UIManager.Instance.OpenInspectorUI();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isDragging = true;
            Focus(); // 设置 focus
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isDragging = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || ParentObject == null)
            return;

        // 检查是否在 RawImage 内
        if (RectTransformUtility.RectangleContainsScreenPoint(UIManager.Instance.targetRawImage.rectTransform, eventData.position))
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(UIManager.Instance.targetRawImage.rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                Vector2 uv = RectPointToUV(UIManager.Instance.targetRawImage.rectTransform, localPoint);
                Ray ray = UIManager.Instance.raycastCamera.ViewportPointToRay(uv);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector3 newPos = hit.point;
                    ParentObject.transform.position = newPos;
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
