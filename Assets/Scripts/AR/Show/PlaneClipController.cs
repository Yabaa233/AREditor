using UnityEngine;

public class PlaneClipController : MonoBehaviour
{
    public Material clipMaterial;               // 模型用的材质
    public Transform clipPlaneObject;           // 场景中任意方向的空物体/Plane

    void Update()
    {
        if (!clipMaterial || !clipPlaneObject) return;

        Vector3 planePos = clipPlaneObject.position;
        Vector3 planeNormal = clipPlaneObject.up; // 默认用 up 方向作为法线

        clipMaterial.SetVector("_ClipPlanePos", planePos);
        clipMaterial.SetVector("_ClipPlaneNormal", planeNormal);
    }
}
