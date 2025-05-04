using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARAnchorManager))]
[RequireComponent(typeof(ARRaycastManager))]
public class AnchorCreator : MonoBehaviour
{
    [SerializeField]
    GameObject m_Prefab;  // 要放置的预制体（如 A/B 点）

    public GameObject prefab
    {
        get => m_Prefab;
        set => m_Prefab = value;
    }

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    List<ARAnchor> m_Anchors = new List<ARAnchor>();
    ARRaycastManager m_RaycastManager;
    ARAnchorManager m_AnchorManager;
    ARPlaneManager m_PlaneManager;

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
        m_AnchorManager = GetComponent<ARAnchorManager>();
        m_PlaneManager = GetComponent<ARPlaneManager>();
    }

    void Update()
    {
        if (Input.touchCount == 0)
            return;

        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began)
            return;

        const TrackableType trackableTypes =
            TrackableType.FeaturePoint | TrackableType.PlaneWithinPolygon;

        if (m_RaycastManager.Raycast(touch.position, s_Hits, trackableTypes))
        {
            var hit = s_Hits[0];
            var anchor = CreateAnchor(hit);

            if (anchor != null)
            {
                m_Anchors.Add(anchor);
            }
            else
            {
                Debug.Log("Failed to create anchor.");
            }
        }
    }

    ARAnchor CreateAnchor(in ARRaycastHit hit)
    {
        ARAnchor anchor = null;

        // 如果命中了 ARPlane，则附着到平面上
        if (hit.trackable is ARPlane plane && m_AnchorManager != null)
        {
            var oldPrefab = m_AnchorManager.anchorPrefab;
            m_AnchorManager.anchorPrefab = m_Prefab;
            anchor = m_AnchorManager.AttachAnchor(plane, hit.pose);
            m_AnchorManager.anchorPrefab = oldPrefab;
        }

        // 如果不是平面（例如特征点），就直接实例化并手动加 ARAnchor 组件
        if (anchor == null)
        {
            var obj = Instantiate(m_Prefab, hit.pose.position, hit.pose.rotation);
            anchor = obj.GetComponent<ARAnchor>();
            if (anchor == null)
            {
                anchor = obj.AddComponent<ARAnchor>();
            }
        }

        return anchor;
    }

    public void RemoveAllAnchors()
    {
        foreach (var anchor in m_Anchors)
        {
            if (anchor != null)
            {
                Destroy(anchor.gameObject);
            }
        }
        m_Anchors.Clear();
    }
}
