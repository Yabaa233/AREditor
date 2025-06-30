using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class singleton<T> : MonoBehaviour where T : singleton<T>
{
    private static T instance;  //创建单例
    public static T Instance
    {
        get { return instance; }
    }

    protected virtual void Awake()  //允许子类继承和修改
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = (T)this;
        }
    }

    public static bool IsInitialized    //判断单例是否已经生成
    {
        get { return instance != null; }
    }

    protected virtual void OnDestroy()  //在被销毁时设置为空
    {
        if(instance == this)    instance = null;
    }
}
