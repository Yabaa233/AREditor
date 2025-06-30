using UnityEngine;

public class VFXDissolveCaller : MonoBehaviour
{
    // 按钮触发或其他方式调用此函数
    public void FindAndTriggerDissolve()
    {
        VFXDissolveController controller = FindObjectOfType<VFXDissolveController>();

        if (controller == null)
        {
            Debug.LogWarning("Cant find VFXDissolveRuntimeController conp！");
            return;
        }
        
        controller.SendMessage("TriggerDissolve");
    }
}
