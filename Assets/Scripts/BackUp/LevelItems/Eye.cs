using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EyeWatcher : MonoBehaviour
{
    [Header("View Settings")]
    public float viewAngle = 60f;
    public float viewDistance = 10f;
    public float meshResolution = 1f; // 每度采样次数
    public LayerMask obstacleMask;

    [Header("Rotation")]
    public float rotationInterval = 2f;
    private float rotationTimer;

    [Header("Detection")]
    public Transform player;
    public float detectionTime = 3f;
    private float detectProgress;
    public Image debugFillImage;

    [Header("Mesh Display")]
    public MeshFilter viewMeshFilter;
    private Mesh viewMesh;

    private void Start()
    {
        // 自动查找 Player
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                //Debug.LogError("[EyeWatcher] 找不到 tag 为 'Player' 的对象！");
            }
        }

        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;
    }


    void Update()
    {
        HandleRotation();
        DetectPlayer();
        DrawViewMesh();
    }

    void HandleRotation()
    {
        rotationTimer += Time.deltaTime;
        if (rotationTimer >= rotationInterval)
        {
            transform.Rotate(Vector3.up, 90f);
            rotationTimer = 0f;
        }
    }

    void DetectPlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (angle < viewAngle / 2f)
        {
            float dist = Vector3.Distance(player.position, transform.position);
            if (!Physics.Raycast(transform.position, dirToPlayer, dist, obstacleMask))
            {
                detectProgress += Time.deltaTime;
                if (detectProgress >= detectionTime)
                {
                    Debug.Log("GameOver!");
                    detectProgress = detectionTime;
                }

                UpdateProgressUI();
                return;
            }
        }

        detectProgress -= Time.deltaTime * 2f;
        detectProgress = Mathf.Max(0f, detectProgress);
        UpdateProgressUI();
    }

    void UpdateProgressUI()
    {
        if (debugFillImage)
        {
            debugFillImage.fillAmount = detectProgress / detectionTime;
        }
    }

    void DrawViewMesh()
    {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;

        List<Vector3> viewPoints = new List<Vector3>();
        viewPoints.Add(Vector3.zero); // 视野起点为中心

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = -viewAngle / 2 + stepAngleSize * i;
            Vector3 dir = DirFromAngle(angle);
            RaycastHit hit;

            if (Physics.Raycast(transform.position, transform.rotation * dir, out hit, viewDistance, obstacleMask))
            {
                viewPoints.Add(transform.InverseTransformPoint(hit.point));
            }
            else
            {
                viewPoints.Add(dir.normalized * viewDistance);
            }
        }

        // 生成 Mesh
        int vertexCount = viewPoints.Count;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        for (int i = 0; i < vertexCount; i++)
        {
            vertices[i] = viewPoints[i];
        }

        int triIndex = 0;
        for (int i = 1; i < vertexCount - 1; i++)
        {
            triangles[triIndex++] = 0;
            triangles[triIndex++] = i;
            triangles[triIndex++] = i + 1;
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    Vector3 DirFromAngle(float angleInDegrees)
    {
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
