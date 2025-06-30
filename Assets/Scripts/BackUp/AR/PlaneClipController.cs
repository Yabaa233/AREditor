using UnityEngine;

public class PlaneClipController : MonoBehaviour
{
    public Material clipMaterial;               // Material for the clipped model
    public Transform clipPlaneObject;           // plane in scene for clip

    void Update()
    {
        if (!clipMaterial || !clipPlaneObject) return;

        Vector3 planePos = clipPlaneObject.position;
        Vector3 planeNormal = clipPlaneObject.up; // using up as default dicrection

        clipMaterial.SetVector("_ClipPlanePos", planePos);
        clipMaterial.SetVector("_ClipPlaneNormal", planeNormal);
    }
}
