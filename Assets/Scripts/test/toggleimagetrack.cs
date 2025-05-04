using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class toggleimagetrack : MonoBehaviour
{
    // Start is called before the first frame update
    public Text m_TogglePlaneDetectionText;
    private ARTrackedImageManager mARTrackedImageManager;
    public Button button;
    public makecontenttest contentappearat;
    void Awake()
    {
        m_TogglePlaneDetectionText.text = "禁用图像跟踪";
        mARTrackedImageManager = GetComponent<ARTrackedImageManager>();
        button.onClick.AddListener(ToggleImageTracking);
    }
    #region 启用与禁用图像跟踪
    
    public void ToggleImageTracking()
    {
        mARTrackedImageManager.enabled = !mARTrackedImageManager.enabled;

        string planeDetectionMessage = "";
        if (mARTrackedImageManager.enabled)
        {
            planeDetectionMessage = "禁用图像跟踪";
            SetAllImagesActive(true);
            contentappearat.contentreecover();
        }
        else
        {
            planeDetectionMessage = "启用图像跟踪";
            SetAllImagesActive(false);
            contentappearat.teleport();
        }

        if (m_TogglePlaneDetectionText != null)
            m_TogglePlaneDetectionText.text = planeDetectionMessage;
    }

    void SetAllImagesActive(bool value)
    {
        foreach (var img in mARTrackedImageManager.trackables)
            img.gameObject.SetActive(value);
    }
    
    #endregion
}
