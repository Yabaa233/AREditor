//using UnityEngine;

//public class ARAlignController : MonoBehaviour
//{
//    [Header("虚拟空间的两个参考点")]
//    public Transform virtualStart;
//    public Transform virtualEnd;

//    [Header("虚拟空间中的 Boom")]
//    public Transform virtualBoom;

//    [Header("Boom 预制体")]
//    public GameObject boomPrefab;

//    private Vector3 realStart;
//    private Vector3 realEnd;
//    private bool startSet = false;
//    private bool endSet = false;

//    private void OnEnable()
//    {
        
//    }
//    public void SetStart(Vector3 camPos)
//    {
//        realStart = camPos;
//        startSet = true;
//        Debug.Log("✅ 起点已设置：" + camPos);
//    }

//    public void SetEnd(Vector3 camPos)
//    {
//        realEnd = camPos;
//        endSet = true;
//        Debug.Log("✅ 终点已设置：" + camPos);
//    }

//    public void Generate()
//    {
//        if (!startSet || !endSet)
//        {
//            Debug.LogWarning("⚠️ 请先设置起点和终点！");
//            return;
//        }

//        Vector3 virtualDir = (virtualEnd.position - virtualStart.position).normalized;
//        Vector3 realDir = (realEnd - realStart).normalized;

//        Quaternion rotation = Quaternion.FromToRotation(virtualDir, realDir);
//        Vector3 offset = realStart - rotation * virtualStart.position;

//        Vector3 boomWorldPos = rotation * virtualBoom.position + offset;
//        Quaternion boomWorldRot = rotation * virtualBoom.rotation;

//        Instantiate(boomPrefab, boomWorldPos, boomWorldRot);
//        Debug.Log("💣 Boom 已生成于：" + boomWorldPos);
//    }
//}
