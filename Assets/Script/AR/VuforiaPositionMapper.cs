using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class VuforiaPositionMapper_ConfirmEach : MonoBehaviour
{
    public GameObject virtualA;
    public GameObject virtualB;
    public GameObject virtualC;

    public GameObject imageTargetA;
    public GameObject imageTargetB;

    public GameObject cPrefab;

    public Button confirmAButton;
    public Button confirmBButton;
    public Button confirmCButton;

    private Vector3 cachedRealA;
    private Vector3 cachedRealB;
    private bool isAConfirmed = false;
    private bool isBConfirmed = false;
    private bool isCPlaced = false;

    void Start()
    {
        confirmAButton.onClick.AddListener(ConfirmA);
        confirmBButton.onClick.AddListener(ConfirmB);
        confirmCButton.onClick.AddListener(PlaceC);
        confirmCButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isAConfirmed)
        {
            var observer = imageTargetA.GetComponent<ObserverBehaviour>();
            bool isTracking = observer.TargetStatus.Status == Status.TRACKED || observer.TargetStatus.Status == Status.EXTENDED_TRACKED;
            confirmAButton.gameObject.SetActive(isTracking);
        }
        if (!isBConfirmed)
        {
            var observer = imageTargetB.GetComponent<ObserverBehaviour>();
            bool isTracking = observer.TargetStatus.Status == Status.TRACKED || observer.TargetStatus.Status == Status.EXTENDED_TRACKED;
            confirmBButton.gameObject.SetActive(isTracking);
        }

        if (isAConfirmed && isBConfirmed && !isCPlaced)
        {
            confirmCButton.gameObject.SetActive(true);
        }
    }

    void ConfirmA()
    {
        cachedRealA = imageTargetA.transform.position;
        isAConfirmed = true;
        confirmAButton.gameObject.SetActive(false);
    }

    void ConfirmB()
    {
        cachedRealB = imageTargetB.transform.position;
        isBConfirmed = true;
        confirmBButton.gameObject.SetActive(false);
    }

    void PlaceC()
    {
        // 虚拟空间坐标
        Vector3 virtualPosA = virtualA.transform.position;
        Vector3 virtualPosB = virtualB.transform.position;
        Vector3 virtualPosC = virtualC.transform.position;

        // 计算变换矩阵
        Matrix4x4 transformMatrix = ComputeVirtualToRealMatrix(virtualPosA, virtualPosB, cachedRealA, cachedRealB);

        // 映射 C 的位置与旋转
        Vector3 realPosC = transformMatrix.MultiplyPoint3x4(virtualPosC);
        Quaternion realRotC = transformMatrix.rotation * virtualC.transform.rotation;

        GameObject cInstance = Instantiate(cPrefab, realPosC, realRotC);
        cInstance.transform.parent = null;

        isCPlaced = true;
        confirmCButton.gameObject.SetActive(false);
    }

    private Matrix4x4 ComputeVirtualToRealMatrix(Vector3 vA, Vector3 vB, Vector3 rA, Vector3 rB)
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
