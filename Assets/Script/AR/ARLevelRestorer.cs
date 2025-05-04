using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

public class ARLevelRestorer_WithRotation : MonoBehaviour
{
    public ARTrackedImageManager trackedImageManager;
    public XROrigin arSessionOrigin;

    [Header("虚拟空间参考点")]
    public Transform virtualStart;
    public Transform virtualEnd;
    public Transform virtualRootTransform;

    private Transform realStart;
    private Transform realEnd;
    private bool aligned = false;

    public GameObject testImageTrack;

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void Update()
    {
        if (Camera.main && GameObject.Find("Boom"))
        {
            var boom = GameObject.Find("Boom").transform.position;
            Debug.DrawLine(Camera.main.transform.position, boom, Color.red);
        }
    }


    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)

    {

        foreach (var trackedImage in eventArgs.added)

        {

            // Give the initial image a reasonable default scale

            var minLocalScalar = Mathf.Min(trackedImage.size.x, trackedImage.size.y) / 2;

            trackedImage.transform.localScale = new Vector3(minLocalScalar, minLocalScalar, minLocalScalar);//对模型缩放

            Instantiate(testImageTrack, trackedImage.transform);//实例化预制件

            Debug.Log("检测到图片" + trackedImage.referenceImage.name);

            //OnImagesChanged(trackedImage);

        }

    }

    //void OnImagesChanged(ARTrackedImagesChangedEventArgs args)
    //{
    //    foreach (var image in args.updated)
    //    {
    //        if (image.trackingState != TrackingState.Tracking) continue;

    //        if (image.referenceImage.name == "start_marker")
    //        {
    //            realStart = image.transform;
    //            Debug.Log("✨ 识别到起点");
    //        }
    //        else if (image.referenceImage.name == "end_marker")
    //        {
    //            realEnd = image.transform;
    //            Debug.Log("✨ 识别到终点" );
    //        }

    //        if (realStart != null && realEnd != null)
    //        {
    //            // AlignWithTwoPoints();
    //            trackedImageManager.trackedImagesChanged -= OnImagesChanged; // 只对齐一次
    //        }
    //    }
    //}

    void AlignWithTwoPoints()
    {
        if (aligned || virtualStart == null || virtualEnd == null || virtualRootTransform == null) return;

        Vector3 virtualDir = (virtualEnd.position - virtualStart.position).normalized;
        Vector3 realDir = (realEnd.position - realStart.position).normalized;

        Quaternion rotation = Quaternion.FromToRotation(virtualDir, realDir);
        Vector3 offset = realStart.position - rotation * virtualStart.position;

        arSessionOrigin.transform.rotation = rotation * arSessionOrigin.transform.rotation;
        arSessionOrigin.transform.position = offset + rotation * (arSessionOrigin.transform.position - virtualRootTransform.position);

        aligned = true;
        Debug.Log("✅ 虚拟空间已位置+旋转对齐");
    }
}
