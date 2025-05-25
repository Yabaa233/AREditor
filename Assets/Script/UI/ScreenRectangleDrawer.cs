using UnityEngine;
using UnityEngine.UI;

public class ScreenRectangleDrawer : MonoBehaviour
{
    public RectTransform selectionBox;
    public RectTransform canvasRect;

    private Vector2 startPos;
    private Vector2 endPos;

    private void Start()
    {
        if (selectionBox != null)
        {
            selectionBox.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            selectionBox.gameObject.SetActive(true);
        }

        if (Input.GetMouseButton(0))
        {
            endPos = Input.mousePosition;
            UpdateSelectionBox();
        }

        if (Input.GetMouseButtonUp(0))
        {
            selectionBox.gameObject.SetActive(false);
        }
    }

    void UpdateSelectionBox()
    {
        Vector3 worldStart, worldEnd;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, startPos, canvasRect.GetComponent<Canvas>().worldCamera, out worldStart);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, endPos, canvasRect.GetComponent<Canvas>().worldCamera, out worldEnd);

        Vector3 center = (worldStart + worldEnd) / 2f;
        Vector2 size = new Vector2(Mathf.Abs(worldEnd.x - worldStart.x), Mathf.Abs(worldEnd.y - worldStart.y));

        selectionBox.position = center;
        selectionBox.sizeDelta = size;

    }
}
