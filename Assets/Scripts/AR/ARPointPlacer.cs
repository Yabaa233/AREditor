using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPointPlacerWithC : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public ARAnchorManager anchorManager;

    [Header("Prefabs")]
    public GameObject pointPrefab; // 放置点 A / B 的物体
    public GameObject cPrefab;     // 放置的 C 物体

    [Header("UI")]
    public TMP_Text instructionText;

    private ARAnchor anchorA;
    private ARAnchor anchorB;
    private bool cPlaced = false;

    [SerializeField] GameObject virtualA;
    [SerializeField] GameObject virtualB;
    [SerializeField] GameObject virtualC;


    void Start()
    {
        instructionText.text = "请点击地面放置点 A";
    }

    void Update()
    {
        if (Input.touchCount == 0 || Input.GetTouch(0).phase != TouchPhase.Began) return;

        if (!TryGetTouchPose(out Pose hitPose)) return;

        if (anchorA == null)
        {
            anchorA = PlaceAnchor(hitPose);
            instructionText.text = "请点击地面放置点 B";
        }
        else if (anchorB == null)
        {
            anchorB = PlaceAnchor(hitPose);
            instructionText.text = "已放置 A 和 B，生成 C";
            PlaceC();
            instructionText.text = "完成：C 已放置";
        }
    }

    bool TryGetTouchPose(out Pose pose)
    {
        var hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(Input.GetTouch(0).position, hits, TrackableType.PlaneWithinPolygon))
        {
            pose = hits[0].pose;
            return true;
        }
        pose = default;
        return false;
    }

    ARAnchor PlaceAnchor(Pose pose)
    {
        ARAnchor anchor = anchorManager.AddAnchor(pose);
        if (anchor != null && pointPrefab != null)
        {
            Instantiate(pointPrefab, anchor.transform.position, anchor.transform.rotation, anchor.transform);
        }
        return anchor;
    }

    void PlaceC()
    {
        if (anchorA == null || anchorB == null || cPlaced) return;

        // 虚拟空间中坐标
        Vector3 vA = virtualA.transform.position;
        Vector3 vB = virtualB.transform.position;
        Vector3 vC = virtualC.transform.position;

        // 现实空间中 Anchor 的位置
        Vector3 rA = anchorA.transform.position;
        Vector3 rB = anchorB.transform.position;

        // 计算虚拟空间中的变换矩阵
        Matrix4x4 transformMatrix = ComputeVirtualToRealMatrix(vA, vB, rA, rB);

        // 用变换矩阵计算 C 的现实坐标和朝向
        Vector3 realPosC = transformMatrix.MultiplyPoint3x4(vC);
        Quaternion realRotC = transformMatrix.rotation * virtualC.transform.rotation;

        Instantiate(cPrefab, realPosC, realRotC);
        cPlaced = true;
    }

    Matrix4x4 ComputeVirtualToRealMatrix(Vector3 vA, Vector3 vB, Vector3 rA, Vector3 rB)
    {
        Vector3 vForward = (vB - vA).normalized;
        Vector3 vRight = Vector3.Cross(Vector3.up, vForward).normalized;
        Vector3 vUp = Vector3.Cross(vForward, vRight);
        float vDist = Vector3.Distance(vA, vB);

        Vector3 rForward = (rB - rA).normalized;
        Vector3 rRight = Vector3.Cross(Vector3.up, rForward).normalized;
        Vector3 rUp = Vector3.Cross(rForward, rRight);
        float rDist = Vector3.Distance(rA, rB);

        float scale = rDist / vDist;

        Matrix4x4 vMatrix = Matrix4x4.TRS(vA, Quaternion.LookRotation(vForward, vUp), Vector3.one * scale);
        Matrix4x4 rMatrix = Matrix4x4.TRS(rA, Quaternion.LookRotation(rForward, rUp), Vector3.one);

        return rMatrix * vMatrix.inverse;
    }


}
