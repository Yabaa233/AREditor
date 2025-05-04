using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class VuforiaImageTrackerHandler : MonoBehaviour
{
    public Transform contenttransform;
    public Transform temp1;
    public Transform temp2;
    public Transform temp3;
    public Camera worldSpaceCanvasCamera;
    public Text debugtext;

    void Start()
    {
        var observer = GetComponent<ObserverBehaviour>();
        if (observer)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED)
        {
            UpdateInfo(behaviour);
        }
    }

    void UpdateInfo(ObserverBehaviour imageTarget)
    {
        temp3.position = imageTarget.transform.position;
        temp3.rotation = imageTarget.transform.rotation;
        temp3.localScale = Vector3.one;
        temp3.Rotate(0, 180, 0, Space.Self);

        temp1.position = worldSpaceCanvasCamera.transform.position;
        temp1.position = objectconvert(temp1.position, temp3);
        temp2.position = worldconvert(temp1.position, contenttransform);
        temp2.position = temp2.position - contenttransform.position;

        temp1.rotation = worldSpaceCanvasCamera.transform.rotation;
        temp1.rotation = Quaternion.Inverse(temp3.rotation) * temp1.rotation;
        temp2.rotation = contenttransform.rotation * temp1.rotation;
        temp2.rotation = temp2.rotation * Quaternion.Inverse(worldSpaceCanvasCamera.transform.rotation);

        if (debugtext != null)
        {
            Vector3 distance = temp3.position - worldSpaceCanvasCamera.transform.position;
            debugtext.text = $"图像跟踪状态：{imageTarget.TargetName} | 相对旋转角: {temp1.eulerAngles} | 距离: {distance.magnitude}";
        }
    }

    public void teleport()
    {
        contenttransform.position -= temp2.position;
        contenttransform.Rotate(temp2.eulerAngles);
    }

    public void contentreecover()
    {
        contenttransform.position = Vector3.zero;
        contenttransform.rotation = Quaternion.identity;
    }

    public Vector3 objectconvert(Vector3 worldposition, Transform localobject)
    {
        return localobject.InverseTransformPoint(worldposition);
    }

    public Vector3 worldconvert(Vector3 localposition, Transform localobject)
    {
        return localobject.TransformPoint(localposition);
    }
}
