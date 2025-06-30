using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public enum UIState { ConTopDownView, MovePlacedObject, SetPlacedObject, SetActionTarget }
public class UIManager : singleton<UIManager>//, IPointerEnterHandler, IPointerExitHandler
{

    public GameObject TargetPickerOverlay;
    public bool ifInPicker;
    public Action<GameObject> onPick;

    [ReadOnly]
    public RawImage targetRawImage;       // 显示场景的 RawImage

    [ReadOnly]
    public Camera raycastCamera;          // 对应的渲染相机

    [Required]
    public PlacedObjectInspector inspector;

    protected override void Awake()
    {
        ifInPicker = false;
        TargetPickerOverlay.SetActive(false);

        base.Awake();
        targetRawImage = GameObject.FindFirstObjectByType<RawImage>();
        raycastCamera = GameObject.FindGameObjectWithTag("RawImageCamera").GetComponent<Camera>();
    }

    public void OpenInspectorUI()
    {
        inspector.gameObject.SetActive(true);
        inspector.SetData(EditorManager.Instance.focusedObject.GetComponent<PlacedObject>().runtimeData);
    }

    public void CloseInspectorUI()
    {
        inspector.gameObject.SetActive(false);
    }

    public void StartPick(Action<GameObject> onPickCallback)
    {
        onPick = onPickCallback;
        TargetPickerOverlay.SetActive(true);
        ifInPicker = true;
    }

    public void ClosePick()
    {
        TargetPickerOverlay.SetActive(false);
        ifInPicker = false;
    }

}
