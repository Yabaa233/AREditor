using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class singleton<T> : MonoBehaviour where T : singleton<T>
{
    private static T instance;  // Create singleton
    public static T Instance
    {
        get { return instance; }
    }

    protected virtual void Awake()
    {
        // 如果已有实例存在
        if (instance != null && instance != this)
        {
            // 强制替换为新实例（适用于场景切换）
            Debug.LogWarning($"[Singleton] 发现重复的 {typeof(T).Name} 实例，销毁旧实例并使用新实例");
            Destroy(instance.gameObject);
            instance = (T)this;
            return;
        }

        instance = (T)this;
        Debug.Log($"[Singleton] {typeof(T).Name} 实例已创建");
    }

    public static bool IsInitialized    // Check if singleton has been initialized
    {
        get { return instance != null; }
    }

    protected virtual void OnDestroy()  // Set to null when destroyed
    {
        if (instance == this) instance = null;
    }
}
