using UnityEngine;

namespace Assets.Scripts.DebugTools
{
    /// <summary>
    /// 碰撞体可视化组件
    /// 在运行时显示碰撞体边框
    /// </summary>
    public class ColliderVisualizer : MonoBehaviour
    {
        public Color wireframeColor = Color.green;
        public float lineWidth = 0.02f;

        private Collider targetCollider;
        private GameObject wireframeContainer;
        private LineRenderer[] lineRenderers;

        private void Awake()
        {
            targetCollider = GetComponent<Collider>();
        }

        private void Start()
        {
            if (targetCollider != null)
            {
                CreateWireframe();
            }
        }

        private void OnDestroy()
        {
            if (wireframeContainer != null)
            {
                Destroy(wireframeContainer);
            }
        }

        private void LateUpdate()
        {
            // 实时更新线框位置以跟随碰撞体
            UpdateWireframeTransform();
        }

        private void CreateWireframe()
        {
            wireframeContainer = new GameObject("ColliderWireframe");
            wireframeContainer.transform.SetParent(transform);
            wireframeContainer.transform.localPosition = Vector3.zero;
            wireframeContainer.transform.localRotation = Quaternion.identity;
            wireframeContainer.transform.localScale = Vector3.one;

            if (targetCollider is BoxCollider)
            {
                CreateBoxWireframe(targetCollider as BoxCollider);
            }
            else if (targetCollider is SphereCollider)
            {
                CreateSphereWireframe(targetCollider as SphereCollider);
            }
            else if (targetCollider is CapsuleCollider)
            {
                CreateCapsuleWireframe(targetCollider as CapsuleCollider);
            }
        }

        private void UpdateWireframeTransform()
        {
            if (wireframeContainer == null || targetCollider == null)
                return;

            // 更新位置为碰撞体的中心
            if (targetCollider is BoxCollider box)
            {
                wireframeContainer.transform.localPosition = box.center;
            }
            else if (targetCollider is SphereCollider sphere)
            {
                wireframeContainer.transform.localPosition = sphere.center;
            }
            else if (targetCollider is CapsuleCollider capsule)
            {
                wireframeContainer.transform.localPosition = capsule.center;
            }
        }

        private void CreateBoxWireframe(BoxCollider box)
        {
            Vector3 size = box.size;
            Vector3 hs = size * 0.5f; // half size

            // 立方体的8个顶点
            Vector3[] corners = new Vector3[8]
            {
                new Vector3(-hs.x, -hs.y, -hs.z),
                new Vector3(hs.x, -hs.y, -hs.z),
                new Vector3(hs.x, -hs.y, hs.z),
                new Vector3(-hs.x, -hs.y, hs.z),
                new Vector3(-hs.x, hs.y, -hs.z),
                new Vector3(hs.x, hs.y, -hs.z),
                new Vector3(hs.x, hs.y, hs.z),
                new Vector3(-hs.x, hs.y, hs.z)
            };

            // 12条边
            int[][] edges = new int[][]
            {
                new int[] {0, 1}, new int[] {1, 2}, new int[] {2, 3}, new int[] {3, 0}, // 底面
                new int[] {4, 5}, new int[] {5, 6}, new int[] {6, 7}, new int[] {7, 4}, // 顶面
                new int[] {0, 4}, new int[] {1, 5}, new int[] {2, 6}, new int[] {3, 7}  // 竖边
            };

            lineRenderers = new LineRenderer[12];
            for (int i = 0; i < 12; i++)
            {
                lineRenderers[i] = CreateLine(
                    $"Edge{i}",
                    corners[edges[i][0]],
                    corners[edges[i][1]]
                );
            }
        }

        private void CreateSphereWireframe(SphereCollider sphere)
        {
            float radius = sphere.radius;
            int segments = 32;

            lineRenderers = new LineRenderer[3];

            // XY平面圆
            lineRenderers[0] = CreateCircle("CircleXY", radius, segments, Vector3.forward);
            // XZ平面圆
            lineRenderers[1] = CreateCircle("CircleXZ", radius, segments, Vector3.up);
            // YZ平面圆
            lineRenderers[2] = CreateCircle("CircleYZ", radius, segments, Vector3.right);
        }

        private void CreateCapsuleWireframe(CapsuleCollider capsule)
        {
            float radius = capsule.radius;
            int segments = 24;

            lineRenderers = new LineRenderer[3];

            // 根据胶囊方向创建圆环
            Vector3 axis = Vector3.up;
            if (capsule.direction == 0) axis = Vector3.right;
            else if (capsule.direction == 2) axis = Vector3.forward;

            float halfHeight = Mathf.Max(0, capsule.height * 0.5f - radius);

            // 顶部圆
            lineRenderers[0] = CreateCircle("Top", radius, segments, axis, axis * halfHeight);
            // 底部圆
            lineRenderers[1] = CreateCircle("Bottom", radius, segments, axis, -axis * halfHeight);
            // 中间圆
            lineRenderers[2] = CreateCircle("Middle", radius, segments, axis, Vector3.zero);
        }

        private LineRenderer CreateLine(string name, Vector3 start, Vector3 end)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.SetParent(wireframeContainer.transform);
            lineObj.transform.localPosition = Vector3.zero;
            lineObj.transform.localRotation = Quaternion.identity;
            lineObj.transform.localScale = Vector3.one;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;

            // 使用Sprites/Default着色器（与选中框相同）
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = wireframeColor;
            lr.endColor = wireframeColor;

            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            return lr;
        }

        private LineRenderer CreateCircle(string name, float radius, int segments, Vector3 normal, Vector3 offset = default)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.SetParent(wireframeContainer.transform);
            lineObj.transform.localPosition = Vector3.zero;
            lineObj.transform.localRotation = Quaternion.identity;
            lineObj.transform.localScale = Vector3.one;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = segments + 1;
            lr.useWorldSpace = false;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.loop = true;

            // 使用Sprites/Default着色器（与选中框相同）
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = wireframeColor;
            lr.endColor = wireframeColor;

            // 计算垂直向量
            Vector3 perp1, perp2;
            if (Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.9f)
            {
                perp1 = Vector3.Cross(normal, Vector3.up).normalized;
            }
            else
            {
                perp1 = Vector3.Cross(normal, Vector3.right).normalized;
            }
            perp2 = Vector3.Cross(normal, perp1).normalized;

            // 生成圆上的点
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 point = offset + perp1 * Mathf.Cos(angle) * radius + perp2 * Mathf.Sin(angle) * radius;
                lr.SetPosition(i, point);
            }

            return lr;
        }
    }
}
