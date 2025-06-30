using UnityEngine;
using UnityEngine.UI;
using SFB; // 需要导入 StandaloneFileBrowser 插件
using System.IO;

public class IOManager : singleton<IOManager>
{
    public Image targetImage; // 目标 UI 的 Image 组件

    public void OpenFileDialog()
    {
#if UNITY_ANDROID
        // Android 平台使用本地文件选择器
        NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log("选中的文件路径：" + path);
                StartCoroutine(LoadImage(path)); // 加载图片并设置到 Image 组件
            }
            else
            {
                Debug.LogWarning("未选择文件或路径为空");
            }
        }, new[] { "image/*" });

        if (permission == NativeFilePicker.Permission.Denied)
        {
            Debug.LogWarning("文件访问权限被拒绝");
        }
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
        // Windows 平台使用 StandaloneFileBrowser
        string[] paths = StandaloneFileBrowser.OpenFilePanel("选择图片", "",
            new[] { new ExtensionFilter("Image Files", "png", "jpg", "jpeg", "bmp") }, false);

        // 检查是否有文件被选择
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            Debug.Log("选中的文件路径：" + paths[0]);
            StartCoroutine(LoadImage(paths[0])); // 加载图片并设置到 Image 组件
        }
        else
        {
            Debug.LogWarning("未选择文件或路径为空");
        }
#else
        Debug.LogWarning("当前平台不支持文件选择功能");
#endif
    }

    private System.Collections.IEnumerator LoadImage(string filePath)
    {
        // 从文件中读取图片数据
        byte[] fileData = File.ReadAllBytes(filePath);

        // 创建纹理
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(fileData))
        {
            // 将纹理转换为 Sprite
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

            // 设置到目标 Image 上
            targetImage.sprite = sprite;

            // 自适应图片大小
            targetImage.preserveAspect = true;
        }
        else
        {
            Debug.LogError("图片加载失败");
        }

        yield return null;
    }
}
