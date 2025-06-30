//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.XR.ARFoundation;

//public class ARAlignUIController : MonoBehaviour
//{
//    public Camera arCamera;
//    public ARAlignController alignController;

//    public Button btnSetStart;
//    public Button btnSetEnd;
//    public Button btnGenerate;

//    void Start()
//    {
//        btnSetStart.onClick.AddListener(() =>
//        {
//            alignController.SetStart(arCamera.transform.position);
//        });

//        btnSetEnd.onClick.AddListener(() =>
//        {
//            alignController.SetEnd(arCamera.transform.position);
//        });

//        btnGenerate.onClick.AddListener(() =>
//        {
//            alignController.Generate();
//        });
//    }
//}
