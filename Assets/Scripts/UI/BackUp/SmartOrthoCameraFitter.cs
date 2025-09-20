//using System.Collections.Generic;
//using UnityEngine;
//using Sirenix.OdinInspector;

//[ExecuteAlways]
//public class SmartOrthoCameraFitter : MonoBehaviour
//{
//    [Title("设置")]
//    [Required] public Transform levelRoot;
//    [Required] public Camera targetCamera;
//    [MinValue(0f)]
//    public float margin = 1f;

//    [Button("📷 智能自动对齐摄像机", ButtonSizes.Large)]
//    [GUIColor(0.3f, 0.9f, 1f)]
//    public void FitCameraSmart()
//    {
//        if (levelRoot == null || targetCamera == null)
//        {
//            Debug.LogWarning("请设置 levelRoot 和 targetCamera");
//            return;
//        }

//        List<Vector3> points = new();

//        foreach (var mf in levelRoot.GetComponentsInChildren<MeshFilter>())
//        {
//            if (mf.sharedMesh == null) continue;

//            var vertices = mf.sharedMesh.vertices;
//            foreach (var v in vertices)
//            {
//                Vector3 worldV = mf.transform.TransformPoint(v);
//                points.Add(worldV);
//            }
//        }

//        if (points.Count < 3)
//        {
//            Debug.LogWarning("场景点数太少，无法估算方向");
//            return;
//        }

//        // 计算质心
//        Vector3 center = Vector3.zero;
//        foreach (var p in points) center += p;
//        center /= points.Count;

//        // 构造协方差矩阵
//        float[,] cov = new float[3, 3];
//        foreach (var p in points)
//        {
//            Vector3 d = p - center;
//            cov[0, 0] += d.x * d.x; cov[0, 1] += d.x * d.y; cov[0, 2] += d.x * d.z;
//            cov[1, 0] += d.y * d.x; cov[1, 1] += d.y * d.y; cov[1, 2] += d.y * d.z;
//            cov[2, 0] += d.z * d.x; cov[2, 1] += d.z * d.y; cov[2, 2] += d.z * d.z;
//        }

//        // 估算最大主轴方向（最厚的方向）
//        Vector3 forward = EstimateLargestEigenvector(cov);
//        Vector3 lookDir = -forward.normalized; // 相机从该方向看向模型
//        Vector3 upDir = Vector3.up; // 默认竖直向上视觉参考轴（也可自定义）

//        // 计算包围盒大小
//        Bounds bounds = new(points[0], Vector3.zero);
//        foreach (var p in points) bounds.Encapsulate(p);

//        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
//        Vector3 camPos = center + lookDir * (size + margin);

//        // 设置摄像机
//        targetCamera.orthographic = true;
//        targetCamera.transform.position = camPos;
//        targetCamera.transform.rotation = Quaternion.LookRotation(lookDir, upDir);
//        targetCamera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) + margin;

//        Debug.Log($"📷 摄像机已对齐 | lookDir: {lookDir}, center: {center}, orthoSize: {targetCamera.orthographicSize}");
//    }

//    private Vector3 EstimateLargestEigenvector(float[,] cov)
//    {
//        Vector3[] directions = {
//            Vector3.up, Vector3.right, Vector3.forward,
//            (Vector3.up + Vector3.right).normalized,
//            (Vector3.up + Vector3.forward).normalized,
//            (Vector3.right + Vector3.forward).normalized,
//            (Vector3.one).normalized
//        };

//        Vector3 bestDir = directions[0];
//        float maxVar = float.MinValue;

//        foreach (var dir in directions)
//        {
//            float var = VarianceAlong(dir, cov);
//            if (var > maxVar)
//            {
//                maxVar = var;
//                bestDir = dir;
//            }
//        }

//        return bestDir;
//    }

//    private float VarianceAlong(Vector3 dir, float[,] cov)
//    {
//        Vector3 d = dir.normalized;
//        return d.x * (d.x * cov[0, 0] + d.y * cov[0, 1] + d.z * cov[0, 2]) +
//               d.y * (d.x * cov[1, 0] + d.y * cov[1, 1] + d.z * cov[1, 2]) +
//               d.z * (d.x * cov[2, 0] + d.y * cov[2, 1] + d.z * cov[2, 2]);
//    }
//}
