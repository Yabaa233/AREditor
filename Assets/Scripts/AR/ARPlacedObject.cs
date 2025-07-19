using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.AR;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(ARSelectionInteractable))]
[RequireComponent(typeof(ARTranslationInteractable))]
[RequireComponent(typeof(ARScaleInteractable))]
[RequireComponent(typeof(ARRotationInteractable))]
public class ARPlacedObject : PlacedObject
{
    private ARSelectionInteractable selection;
    private ARTranslationInteractable translation;
    private ARScaleInteractable scale;
    private ARRotationInteractable rotation;

    protected override void Start()
    {
        // 不调用 base.Start()，因为基类有 PlaneViewer 的引用
        if (!initialized)
        {
            InitializeFromTemplateARSafe();
        }

        EnableARInteraction(true);
    }

    void Awake()
    {
        // 确保 AR 组件存在并被正确启用
        selection = GetComponent<ARSelectionInteractable>() ?? gameObject.AddComponent<ARSelectionInteractable>();
        translation = GetComponent<ARTranslationInteractable>() ?? gameObject.AddComponent<ARTranslationInteractable>();
        scale = GetComponent<ARScaleInteractable>() ?? gameObject.AddComponent<ARScaleInteractable>();
        rotation = GetComponent<ARRotationInteractable>() ?? gameObject.AddComponent<ARRotationInteractable>();
    }

    private void EnableARInteraction(bool enable)
    {
        if (selection) selection.enabled = enable;
        if (translation) translation.enabled = enable;
        if (scale) scale.enabled = enable;
        if (rotation) rotation.enabled = enable;
    }

    private void InitializeFromTemplateARSafe()
    {
        if (templateDatabase == null)
        {
            Debug.LogError("Template Database is not assigned.");
            return;
        }

        var template = templateDatabase.GetTemplateByID(selectedTemplateID);
        if (template == null)
        {
            Debug.LogError($"Template ID '{selectedTemplateID}' not found in database ");
            return;
        }

        runtimeData = new PlacedObjectData
        {
            ID = EditorManager.Instance.GenerateUniqueID(),
            templateID = selectedTemplateID,
            position = transform.position,
            rotation = transform.rotation.eulerAngles,
            scale = transform.localScale,
            events = new(template.defaultEvents)
        };

        initialized = true;
    }

    public override void InitializeFromJson()
    {
        ObjectTemplateData template = templateDatabase.GetTemplateByID(selectedTemplateID);
        if (template == null)
        {
            Debug.LogError($"Template ID '{selectedTemplateID}' not found in database ");
            return;
        }

        initialized = true;
    }


}
