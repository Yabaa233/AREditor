using UnityEngine;
using System.Linq;

[ExecuteAlways]
public class HandPoseController : MonoBehaviour
{
    [Header("Auto Bind")]
    public bool autoBindByName = true;
    [Tooltip("在层级中查找包含这些关键词的骨骼名（从掌指根到指尖依次匹配）")]
    public string indexHint = "Index";
    public string middleHint = "Middle";
    public string ringHint = "Ring";
    public string littleHint = "Little";
    public string thumbHint = "Thumb";

    [System.Serializable]
    public class Finger
    {
        public string name;
        public Transform proximal;    // 近节（贴掌）
        public Transform intermediate;// 中节
        public Transform distal;      // 远节（贴指尖）可留空(2节手)
        [HideInInspector] public Quaternion p0, i0, d0;
        public void Cache()
        {
            if (proximal) p0 = proximal.localRotation;
            if (intermediate) i0 = intermediate.localRotation;
            if (distal) d0 = distal.localRotation;
        }
        public void Apply(float t, Vector3 axis, float aP, float aI, float aD)
        {
            t = Mathf.Clamp01(t);
            if (proximal)
                proximal.localRotation = p0 * Quaternion.AngleAxis(aP * t, axis);
            if (intermediate)
                intermediate.localRotation = i0 * Quaternion.AngleAxis(aI * t, axis);
            if (distal)
                distal.localRotation = d0 * Quaternion.AngleAxis(aD * t, axis);
        }
    }

    [Header("Fingers")]
    public Finger index = new Finger { name = "Index" };
    public Finger middle = new Finger { name = "Middle" };
    public Finger ring = new Finger { name = "Ring" };
    public Finger little = new Finger { name = "Little" };
    public Finger thumb = new Finger { name = "Thumb" };

    [Header("Sliders (0..1)")]
    [Range(0, 1)] public float grip = 0f;           // 整体握
    [Range(0, 1)] public float indexCurl = 0f;
    [Range(0, 1)] public float middleCurl = 0f;
    [Range(0, 1)] public float ringCurl = 0f;
    [Range(0, 1)] public float littleCurl = 0f;
    [Range(0, 1)] public float thumbCurl = 0f;
    [Range(-1, 1)] public float thumbSpread = 0f;   // 拇指外展(-) / 内收(+)

    [Header("Kinematics / Limits")]
    [Tooltip("弯曲使用的本地轴（多数模型是 X 或 Z），不对就换轴")]
    public Vector3 curlAxis = new Vector3(1, 0, 0);  // local X
    public float proxMax = 75f;
    public float interMax = 85f;
    public float distMax = 70f;

    [Tooltip("拇指的关节最大角度")]
    public float thumbProxMax = 50f, thumbMidMax = 55f, thumbTipMax = 45f;

    [Header("Thumb Spread")]
    [Tooltip("拇指外展所用本地轴（常见是 Y 或 Z），不对就换轴")]
    public Vector3 thumbSpreadAxis = new Vector3(0, 1, 0);
    public float thumbSpreadMaxDeg = 30f;

    Transform[] allBones;

    void OnEnable()
    {
        CacheInitial();
        if (autoBindByName) TryAutoBind();
        CacheInitial(); // 绑定后再缓存一次初始旋转
        ApplyPose();
    }

    void Update()
    {
        ApplyPose();
    }

    void CacheInitial()
    {
        index.Cache(); middle.Cache(); ring.Cache(); little.Cache(); thumb.Cache();
        allBones = GetComponentsInChildren<Transform>(true);
    }

    void ApplyPose()
    {
        // 叠加整体握与各指单独滑条
        float i = Mathf.Clamp01(grip + indexCurl);
        float m = Mathf.Clamp01(grip + middleCurl);
        float r = Mathf.Clamp01(grip + ringCurl);
        float l = Mathf.Clamp01(grip + littleCurl);
        float t = Mathf.Clamp01(grip * 0.7f + thumbCurl); // 整体握对拇指影响稍小

        Vector3 ax = SafeAxis(curlAxis);

        index.Apply(i, ax, proxMax, interMax, distMax);
        middle.Apply(m, ax, proxMax, interMax, distMax);
        ring.Apply(r, ax, proxMax, interMax, distMax);
        little.Apply(l, ax, proxMax, interMax, distMax);

        // 拇指：弯曲 + 外展
        if (thumb != null)
        {
            Vector3 axT = ax;
            thumb.Apply(t, axT, thumbProxMax, thumbMidMax, thumbTipMax);

            if (thumb.proximal)
            {
                var spreadAxis = SafeAxis(thumbSpreadAxis);
                thumb.proximal.localRotation =
                    thumb.p0 *
                    Quaternion.AngleAxis(thumbProxMax * t, axT) *
                    Quaternion.AngleAxis(thumbSpreadMaxDeg * thumbSpread, spreadAxis);
            }
        }
    }

    Vector3 SafeAxis(Vector3 a) => (a.sqrMagnitude < 1e-6f) ? Vector3.right : a.normalized;

    // --- 简单的按名字自动绑定（可按你资源的命名改关键字） ---
    void TryAutoBind()
    {
        var tfs = GetComponentsInChildren<Transform>(true);
        // 一个帮助函数：在名字含某关键字的 Transform 里，按“从掌→指尖”的顺序取前三个
        Transform[] Pick(string hint)
        {
            var arr = tfs.Where(tf => tf.name.ToLower().Contains(hint.ToLower()))
                         .OrderBy(tf => tf.GetHierarchyPath()) // 自定义排序帮助：见扩展方法
                         .ToArray();
            // 取三个不同层级的
            var chain = arr.Take(3).ToArray();
            if (chain.Length == 0) return new Transform[3];
            // 如果只匹配到 2 节或 1 节，也能用
            var result = new Transform[3];
            for (int i = 0; i < 3 && i < chain.Length; i++) result[i] = chain[i];
            return result;
        }

        // 你可以把下行换成更精确的匹配规则（例如包含 "Prox/Inter/Dist"）
        var I = Pick(indexHint); index.proximal = I[0]; index.intermediate = I[1]; index.distal = I[2];
        var M = Pick(middleHint); middle.proximal = M[0]; middle.intermediate = M[1]; middle.distal = M[2];
        var R = Pick(ringHint); ring.proximal = R[0]; ring.intermediate = R[1]; ring.distal = R[2];
        var L = Pick(littleHint); little.proximal = L[0]; little.intermediate = L[1]; little.distal = L[2];
        var T = Pick(thumbHint); thumb.proximal = T[0]; thumb.intermediate = T[1]; thumb.distal = T[2];
    }
}

// --- 小扩展：用于简单排序（从根到叶的路径长度来近似“从掌到指尖”） ---
static class TfPathExt
{
    public static string GetHierarchyPath(this Transform t)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        while (t != null) { sb.Insert(0, "/" + t.name); t = t.parent; }
        return sb.ToString();
    }
}
