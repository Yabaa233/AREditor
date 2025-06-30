using UnityEngine;
using UnityEngine.EventSystems;

public class RawImageClickRaycaster : MonoBehaviour, IPointerClickHandler
{
    public Camera raycastCamera;               // 渲染到 RawImage 的相机
    public RectTransform rawImageRect;         // RawImage 的 RectTransform
    public GameObject markerPrefab;            // 你要生成的 marker 模型（可空）

    void OnEnable()
    {
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2 localPoint;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            Vector2 uv = RectPointToUV(rawImageRect, localPoint);

            Ray ray = raycastCamera.ViewportPointToRay(uv);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Raycast hit: " + hit.collider.name + " at " + hit.point);
                SpawnMarkerAt(hit.point);
            }
        }
    }

    // 局部点转 UV 坐标
    private Vector2 RectPointToUV(RectTransform rect, Vector2 localPos)
    {
        float width = rect.rect.width;
        float height = rect.rect.height;

        float u = (localPos.x + width / 2f) / width;
        float v = (localPos.y + height / 2f) / height;

        return new Vector2(u, v);
    }

    // 在命中点生成 marker 模型
    private void SpawnMarkerAt(Vector3 position)
    {
        if (markerPrefab != null)
        {
            Instantiate(markerPrefab, position, Quaternion.identity);
        }
        else
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * 0.1f;

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.red;
            }
        }
    }
}
