using System.Collections.Generic;
using UnityEngine;

public class MeshPlaneFitting : MonoBehaviour
{
    [Header("房间或物体的Mesh")]
    public MeshFilter roomMeshFilter;

    [Header("RANSAC参数")]
    [Tooltip("每一次寻找平面时的随机采样迭代次数")]
    public int maxIterations = 500;
    [Tooltip("顶点到平面的距离阈值，小于此值记为Inlier")]
    public float inlierDistanceThreshold = 0.02f;
    [Tooltip("法线接近向上 (0,1,0) 的阈值，越接近1表示越垂直朝上")]
    public float upwardNormalThreshold = 0.95f;

    [Header("多平面检测参数")]
    [Tooltip("找到一个平面后，其inliers数量必须>=该值才算有效平面")]
    public int minInliers = 20;
    [Tooltip("最多检测多少个平面，防止无限循环")]
    public int maxPlanesToFind = 5;

    [Header("相邻平面过滤")]
    [Tooltip("若两个平面的平均高度相差小于该值，视为同一层或过近，跳过")]
    public float minPlaneGap = 0.05f;

    /// <summary>
    /// 用于保存一个平面及其信息
    /// </summary>
    private class DetectedPlane
    {
        public Plane plane;
        public List<Vector3> inliers;
        public Color color;
        public float avgY; // 平面的平均高度（inlier点的平均Y）
    }

    private List<DetectedPlane> detectedPlanes = new List<DetectedPlane>();

    private void Start()
    {
        if (roomMeshFilter == null || roomMeshFilter.sharedMesh == null)
        {
            Debug.LogError("请在Inspector中指定 roomMeshFilter，并确保它有有效Mesh");
            return;
        }

        // 将 Mesh 顶点复制到一个可修改的 List 以做 RANSAC
        Vector3[] meshVertices = roomMeshFilter.sharedMesh.vertices;
        List<Vector3> vertexList = new List<Vector3>(meshVertices);

        detectedPlanes.Clear();

        // 不断检测新的平面，直到次数用完或顶点不足
        for (int planeIndex = 0; planeIndex < maxPlanesToFind; planeIndex++)
        {
            if (vertexList.Count < 3)
            {
                Debug.Log("剩余顶点<3，无法继续检测新的平面");
                break;
            }

            // 用 RANSAC 寻找“向上”平面
            DetectedPlane newPlane = RansacFindBestUpwardPlane(vertexList);
            if (newPlane == null || newPlane.inliers.Count < minInliers)
            {
                Debug.Log($"第 {planeIndex + 1} 次检测：有效平面未找到或 inlier 太少，停止。");
                break;
            }

            // 计算一下该平面 inlier 点的平均Y
            newPlane.avgY = ComputeAverageY(newPlane.inliers);

            // 判断是否和已检测的平面“过于接近”
            bool isTooClose = false;
            foreach (var existingPlane in detectedPlanes)
            {
                float deltaY = Mathf.Abs(newPlane.avgY - existingPlane.avgY);
                if (deltaY < minPlaneGap)
                {
                    // 如果只想保留“更高的平面”，可再比较 newPlane.avgY vs existingPlane.avgY
                    Debug.LogWarning($"检测到的新平面与已有平面的高度差仅 {deltaY}，跳过此平面。");
                    isTooClose = true;
                    break;
                }
            }

            if (isTooClose)
            {
                // 不创建mesh，不移除点，直接跳过该平面
                continue;
            }

            // 如果不“过近”，就记录这个新平面
            detectedPlanes.Add(newPlane);
            CreatePlaneMeshObject(newPlane, planeIndex);

            // 最后，将其 inliers 从后续点集中移除
            HashSet<Vector3> inlierSet = new HashSet<Vector3>(newPlane.inliers);
            vertexList.RemoveAll(pt => inlierSet.Contains(pt));

            Debug.Log($"第 {planeIndex + 1} 个平面：inliers = {newPlane.inliers.Count}，剩余顶点 = {vertexList.Count}");
        }
    }

    /// <summary>
    /// RANSAC：在给定点集中找一个"向上"的平面
    /// </summary>
    private DetectedPlane RansacFindBestUpwardPlane(List<Vector3> points)
    {
        if (points.Count < 3) return null;

        Plane bestPlane = new Plane();
        int bestScore = 0;
        List<Vector3> bestInliers = new List<Vector3>();

        for (int i = 0; i < maxIterations; i++)
        {
            // 1) 随机取3个点并转换到世界坐标
            Vector3 p1 = TransformToWorld(points[Random.Range(0, points.Count)]);
            Vector3 p2 = TransformToWorld(points[Random.Range(0, points.Count)]);
            Vector3 p3 = TransformToWorld(points[Random.Range(0, points.Count)]);

            // 2) 计算法线
            Vector3 normal = Vector3.Cross(p2 - p1, p3 - p1).normalized;
            if (normal == Vector3.zero)
                continue; // 三点共线，跳过

            // 检查是否足够朝上
            if (Vector3.Dot(normal, Vector3.up) < upwardNormalThreshold)
                continue;

            Plane candidate = new Plane(normal, p1);

            // 3) 计算 inliers
            int count = 0;
            List<Vector3> inliers = new List<Vector3>();
            foreach (var pt in points)
            {
                Vector3 wpos = TransformToWorld(pt);
                float dist = Mathf.Abs(candidate.GetDistanceToPoint(wpos));
                if (dist < inlierDistanceThreshold)
                {
                    count++;
                    inliers.Add(pt);
                }
            }

            // 4) 更新最佳平面
            if (count > bestScore)
            {
                bestScore = count;
                bestPlane = candidate;
                bestInliers = inliers;
            }
        }

        if (bestScore >= 3)  // 至少得有3个点才能构面
        {
            DetectedPlane dp = new DetectedPlane();
            dp.plane = bestPlane;
            dp.inliers = bestInliers;
            dp.color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f);
            return dp;
        }

        return null;
    }

    /// <summary>
    /// 计算 inlier 点的平均Y
    /// </summary>
    private float ComputeAverageY(List<Vector3> inliers)
    {
        float sumY = 0f;
        for (int i = 0; i < inliers.Count; i++)
        {
            // 注意 inliers里存的是模型局部坐标，需要转换到世界坐标再取Y
            float worldY = TransformToWorld(inliers[i]).y;
            sumY += worldY;
        }
        return (sumY / inliers.Count);
    }

    /// <summary>
    /// 生成一个可视化的平面Mesh（简单Quad），放在对应位置并对齐法线
    /// </summary>
    private void CreatePlaneMeshObject(DetectedPlane planeData, int planeIndex)
    {
        // 1) 创建一个空物体
        GameObject planeObj = new GameObject($"DetectedPlane_{planeIndex}");
        planeObj.transform.SetParent(transform);

        MeshFilter mf = planeObj.AddComponent<MeshFilter>();
        MeshRenderer mr = planeObj.AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Standard")) { color = planeData.color };

        // 2) 计算平面中心(世界坐标下 inlier 点的平均)
        Vector3 center = Vector3.zero;
        foreach (var pt in planeData.inliers)
        {
            center += TransformToWorld(pt);
        }
        center /= planeData.inliers.Count;

        planeObj.transform.position = center;
        // 朝向对齐
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, planeData.plane.normal.normalized);
        planeObj.transform.rotation = rot;

        // 3) 将 inlier 点转到该平面的局部坐标系，找 minX,maxX, minZ,maxZ
        Matrix4x4 w2l = planeObj.transform.worldToLocalMatrix;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var pt in planeData.inliers)
        {
            Vector3 localPos = w2l.MultiplyPoint3x4(TransformToWorld(pt));
            if (localPos.x < minX) minX = localPos.x;
            if (localPos.x > maxX) maxX = localPos.x;
            if (localPos.z < minZ) minZ = localPos.z;
            if (localPos.z > maxZ) maxZ = localPos.z;
        }

        // 4) 创建Quad网格
        var vertices = new List<Vector3>
        {
            new Vector3(minX, 0, minZ),
            new Vector3(minX, 0, maxZ),
            new Vector3(maxX, 0, maxZ),
            new Vector3(maxX, 0, minZ)
        };
        var triangles = new List<int> { 0, 1, 2, 0, 2, 3 };

        Mesh planeMesh = new Mesh();
        planeMesh.SetVertices(vertices);
        planeMesh.SetTriangles(triangles, 0);
        planeMesh.RecalculateNormals();
        mf.mesh = planeMesh;
    }

    /// <summary>
    /// 将房间(模型)局部顶点坐标转换到世界坐标
    /// </summary>
    private Vector3 TransformToWorld(Vector3 localPos)
    {
        return roomMeshFilter.transform.TransformPoint(localPos);
    }

    /// <summary>
    /// OnDrawGizmos：在场景里高亮每个平面的 inliers
    /// </summary>
    private void OnDrawGizmos()
    {
        if (detectedPlanes == null) return;
        foreach (var dp in detectedPlanes)
        {
            Gizmos.color = dp.color;
            foreach (var pt in dp.inliers)
            {
                Vector3 wpos = TransformToWorld(pt);
                Gizmos.DrawSphere(wpos, 0.02f);
            }
        }
    }
}
