using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARFoundation.Extensions;
using UnityEngine.XR.ARSubsystems;
public class makecontenttest : MonoBehaviour
{
    private Camera worldSpaceCanvasCamera;
    private ARSessionOrigin m_aRSessionOrigin;
    public Transform contenttransform;
    public Transform temp1;
    public Transform temp2;
    public Transform temp3;
    private ARTrackedImageManager m_TrackedImageManager;
    public Text debugtext;
    // Start is called before the first frame update
    void Awake()
    {
        m_aRSessionOrigin = GetComponent<ARSessionOrigin>();
        worldSpaceCanvasCamera = m_aRSessionOrigin.camera;
        m_TrackedImageManager = GetComponent<ARTrackedImageManager>();
    }
    void OnEnable()
    {
        m_TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        m_TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    // Update is called once per frame
    void Update()
    {
        
       
    }


    void UpdateInfo(ARTrackedImage trackedImage)
    {
        // Set canvas camera
        var canvas = trackedImage.GetComponentInChildren<Canvas>();
        canvas.worldCamera = worldSpaceCanvasCamera;


        if (trackedImage.trackingState != TrackingState.None)
        {
            
            trackedImage.transform.localScale = new Vector3(trackedImage.size.x, 1f, trackedImage.size.y);


            temp3.position = trackedImage.transform.position;
            temp3.rotation = trackedImage.transform.rotation;
            temp3.localScale = new Vector3(1, 1, 1);
            temp3.Rotate(0, 180, 0, Space.Self);

            temp1.position = m_aRSessionOrigin.transform.position ;                //此时检测到平面的相机世界位置(考虑相机初始位移造成的影响，因此选取坐标原点，抵消ARcamera位移影响)
            temp1.position = objectconvert(temp1.position, temp3);                      //转化为相对于照片的相对位置
            temp2.position = worldconvert(temp1.position , contenttransform);           //以真实照片为基础，将旋转角转化为世界坐标系下
            temp2.position = temp2.position - contenttransform.position;                //再得到的世界位置
            



            temp1.rotation = worldSpaceCanvasCamera.transform.rotation;                 //此时检测平面的相机世界旋转
            temp1.rotation = Quaternion.Inverse(temp3.rotation) * temp1.rotation;       //转化为相对于照片的相对旋转角
            temp2.rotation = contenttransform.rotation * temp1.rotation;                //以真实照片为基础，将旋转角转化为世界坐标系下
            temp2.rotation = temp2.rotation * Quaternion.Inverse(worldSpaceCanvasCamera.transform.rotation);   //启动后造成的相机旋转属性

            //减去启动后造成的AR camera相机的旋转和位置效应是为了保证检测到图像，makecontentappearat后以此时的新姿态作为世界原点。

            var text = canvas.GetComponentInChildren<Text>();

            text.text = string.Format(
                    "{0}\ntrackingState: {1}\nGUID: {2}\nReference size: {3} cm\nDetected size: {4} cm\nSessionOrigin rotation: {5}\nTrackedImage rotation: {6}",
                    trackedImage.referenceImage.name,
                    trackedImage.trackingState,
                    trackedImage.referenceImage.guid,
                    trackedImage.referenceImage.size * 100f,
                    trackedImage.size * 100f,
                    m_aRSessionOrigin.transform.eulerAngles,
                    trackedImage.transform.localEulerAngles);
            Vector3 distance = temp3.position - worldSpaceCanvasCamera.transform.position;
            debugtext.text = "相机追踪的状态：  " + trackedImage.trackingState + "   相对于照片旋转角： " + temp1.eulerAngles + "   距离照片距离" + distance.magnitude;
        }
            
              
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        Debug.Log("changed");
        foreach (var trackedImage in eventArgs.added)
        {
            // Give the initial image a reasonable default scale
            trackedImage.transform.localScale = new Vector3(0.01f, 1f, 0.01f);
            UpdateInfo(trackedImage);
        }
        foreach (var trackedImage in eventArgs.updated)
            UpdateInfo(trackedImage);
    }
    public void teleport()
    {
        
        m_aRSessionOrigin.MakeContentAppearAt(contenttransform, -temp2.position);
        m_aRSessionOrigin.transform.GetChild(0).transform.Rotate(temp2.eulerAngles);
        
    }
    public void contentreecover()
    {
        m_aRSessionOrigin.transform.position = new Vector3(0, 0, 0);
        m_aRSessionOrigin.transform.GetChild(0).transform.position = new Vector3(0,0,0);
        m_aRSessionOrigin.transform.GetChild(0).transform.rotation = Quaternion.identity;

    }
    public Vector3 objectconvert(Vector3 worldposition,Transform localobject)
    {
        Vector3 objectposition = new Vector3();
        objectposition = localobject.InverseTransformPoint(worldposition);
        return objectposition;
    }
    public Vector3 worldconvert(Vector3 localposition,Transform localobject)
    {
        Vector3 worldposition = new Vector3();
        worldposition = localobject.TransformPoint(localposition);
        return worldposition;
    }
}
