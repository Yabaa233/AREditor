using UnityEngine;

public class VFXDissolveCaller : MonoBehaviour
{
    // 按钮触发或其他方式调用此函数
    public void FindAndTriggerDissolve()
    {
        VFXDissolveController controller = FindObjectOfType<VFXDissolveController>();

        if (controller == null)
        {
            Debug.LogWarning("未找到 VFXDissolveRuntimeController 组件！");
            return;
        }

        controller.SendMessage("TriggerDissolve");
        // 或者更安全地调用：
        // controller.Invoke("TriggerDissolve", 0f);
        // 或者直接调用（如果是 public）：
        // controller.TriggerDissolve(); ← 如果你把 TriggerDissolve 改成 public
    }
}
