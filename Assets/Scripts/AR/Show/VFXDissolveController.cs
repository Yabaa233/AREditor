using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

public class VFXDissolveController : MonoBehaviour
{
    [Required] public VisualEffect vfx;
    [LabelText("起始值")] public float startValue = 0f;
    [LabelText("结束值")] public float endValue = 5f;
    [LabelText("持续时间")] public float duration = 2f;

    private Coroutine coroutine;
#if UNITY_EDITOR
    private EditorCoroutine coroutine_Editor;
#endif

    [Button("触发 Dissolve 渐变")]
    private void TriggerDissolve_Editor()
    {
#if UNITY_EDITOR
        if (vfx == null)
        {
            Debug.LogWarning("VFX 未赋值！");
            return;
        }

        if (coroutine_Editor != null)
            EditorCoroutineUtility.StopCoroutine(coroutine_Editor);

        coroutine_Editor = EditorCoroutineUtility.StartCoroutine(DissolveRoutine_Editor(), this);
#endif
    }

#if UNITY_EDITOR
    private IEnumerator DissolveRoutine_Editor()
    {
        float t = 0f;
        while (t < duration)
        {
            float value = Mathf.Lerp(startValue, endValue, t / duration);
            vfx.SetFloat("dissolve", value);
            t += 0.02f;
            yield return new EditorWaitForSeconds(0.02f);
        }

        vfx.SetFloat("dissolve", endValue);
    }
#endif

    public void TriggerDissolve()
    {
        if (vfx == null)
        {
            Debug.LogWarning("VFX 未赋值！");
            return;
        }

        if (coroutine != null)
            StopCoroutine(coroutine);

        coroutine = StartCoroutine(DissolveRoutine());
    }

    private IEnumerator DissolveRoutine()
    {
        float t = 0f;
        while (t < duration)
        {
            float value = Mathf.Lerp(startValue, endValue, t / duration);
            vfx.SetFloat("dissolve", value);
            t += Time.deltaTime;
            yield return null;
        }

        vfx.SetFloat("dissolve", endValue);
    }
}
