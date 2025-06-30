using UnityEngine;
using UnityEngine.EventSystems;

public class MouseIconController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Texture2D normalIcon; // 鼠标进入区域时的图标
    public Texture2D zoomIcon;   // 缩放时的图标
    public Vector2 hotspot = Vector2.zero; // 鼠标热点位置（图标中心）

    private bool isMouseInside = false;

    void Update()
    {
        // 检测鼠标滚轮是否缩放
        if (isMouseInside && Input.mouseScrollDelta.y != 0)
        {
            Cursor.SetCursor(zoomIcon, hotspot, CursorMode.Auto);
        }
        else if (isMouseInside)
        {
            Cursor.SetCursor(normalIcon, hotspot, CursorMode.Auto);
        }
    }

    // 鼠标进入区域时
    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseInside = true;
        Cursor.SetCursor(normalIcon, hotspot, CursorMode.Auto);
    }

    // 鼠标离开区域时
    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseInside = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // 恢复默认鼠标
    }
}
