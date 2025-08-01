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
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = (T)this;
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
