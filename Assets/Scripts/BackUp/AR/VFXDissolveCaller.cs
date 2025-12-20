using UnityEngine;

public class VFXDissolveCaller : MonoBehaviour
{
    public void FindAndTriggerDissolve()
    {
        VFXDissolveController controller = FindObjectOfType<VFXDissolveController>();

        if (controller == null)
        {
            Debug.LogWarning("Cant find VFXDissolveRuntimeController Component");
            return;
        }
        
        controller.SendMessage("TriggerDissolve");
    }
}
