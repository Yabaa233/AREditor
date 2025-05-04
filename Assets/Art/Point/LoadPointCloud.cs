//using UnityEngine;
//using Pcx;

//public class LoadPointCloud : MonoBehaviour
//{
//    public TextAsset plyFile;
//    public Material pointMaterial;

//    void Start()
//    {
//        var go = new GameObject("PointCloud");

//        // 正确创建 PointCloudData（ScriptableObject）
//        var data = ScriptableObject.CreateInstance<PointCloudData>();
//        data.sourceFormat = PointCloudData.SourceFormat.PLY;
//        data.sourceData = plyFile;

//        // 添加渲染器
//        var renderer = go.AddComponent<PointCloudRenderer>();
//        renderer.sourceData = data;
//        renderer.material = pointMaterial;
//        renderer.renderMode = PointCloudRenderer.RenderMode.Point;
//        renderer.pointSize = 0.02f;

//        // 安卓兼容设置
//        var unityRenderer = renderer.GetComponent<Renderer>();
//        unityRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
//        unityRenderer.receiveShadows = false;
//    }
//}
