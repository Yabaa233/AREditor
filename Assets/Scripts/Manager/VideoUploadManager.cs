//using SFB;
//using System.Collections;
//using System.IO;
//using UnityEngine;
//using UnityEngine.Networking;

//public class VideoUploadManager : MonoBehaviour
//{
//    public void OpenFileDialog()
//    {
//        string[] paths = StandaloneFileBrowser.OpenFilePanel(
//            "选择视频文件", "", new[] { new ExtensionFilter("Video Files", "mp4", "avi", "mov", "mkv") }, false);

//        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
//        {
//            string selectedFilePath = paths[0];
//            Debug.Log("选中的视频文件路径：" + selectedFilePath);

//            // 开始上传文件
//            StartCoroutine(UploadFile(selectedFilePath));
//        }
//        else
//        {
//            Debug.LogWarning("未选择文件或路径为空");
//        }
//    }

//    // 使用 HTTP 上传文件到服务器
//    private IEnumerator UploadFile(string localFilePath)
//    {
//        string serverUrl = "http://150.65.60.21:5000/upload"; // 替换为服务器的上传接口地址
//        byte[] fileData = File.ReadAllBytes(localFilePath); // 读取文件内容
//        string fileName = Path.GetFileName(localFilePath);

//        // 创建 Multipart 表单数据
//        WWWForm form = new WWWForm();
//        form.AddBinaryData("file", fileData, fileName, "video/mp4"); // 假设文件类型是 mp4

//        UnityWebRequest request = UnityWebRequest.Post(serverUrl, form);

//        yield return request.SendWebRequest();

//        if (request.result == UnityWebRequest.Result.Success)
//        {
//            Debug.Log("文件上传成功: " + request.downloadHandler.text);

//            // 上传完成后，通知服务器处理
//            NotifyServer(fileName);
//        }
//        else
//        {
//            Debug.LogError("文件上传失败: " + request.error);
//        }
//    }

//    // 通知服务器处理上传的视频文件
//    private void NotifyServer(string fileName)
//    {
//        string serverUrl = "http://150.65.60.21:5000/process"; // 服务器的处理接口地址
//        string filePath = "D:/SFTP/upload/" + fileName;

//        // 创建请求对象
//        VideoRequest requestData = new VideoRequest(filePath);

//        // 序列化为 JSON
//        string jsonData = JsonUtility.ToJson(requestData);

//        StartCoroutine(SendProcessRequest(serverUrl, jsonData));
//    }

//    private IEnumerator SendProcessRequest(string serverUrl, string jsonData)
//    {
//        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
//        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
//        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
//        request.downloadHandler = new DownloadHandlerBuffer();
//        request.SetRequestHeader("Content-Type", "application/json");

//        yield return request.SendWebRequest();

//        if (request.result == UnityWebRequest.Result.Success)
//        {
//            Debug.Log("通知服务器成功: " + request.downloadHandler.text);
//        }
//        else
//        {
//            Debug.LogError("通知服务器失败: " + request.error);
//        }
//    }

//    // 下载处理后的文件
//    public void TestDownload()
//    {
//        StartCoroutine(DownloadFile("/result/video_topView.png", "D:/Unity/Assets/Resource/video_topView.png"));
//    }

//    private IEnumerator DownloadFile(string remoteFilePath, string localFilePath)
//    {
//        string serverUrl = "http://150.65.60.21:5000/result/" + Path.GetFileName(remoteFilePath); // 替换为下载接口地址
//        UnityWebRequest request = UnityWebRequest.Get(serverUrl);

//        yield return request.SendWebRequest();

//        if (request.result == UnityWebRequest.Result.Success)
//        {
//            byte[] fileData = request.downloadHandler.data;

//            // 确保本地目录存在
//            string localDirectory = Path.GetDirectoryName(localFilePath);
//            if (!Directory.Exists(localDirectory))
//            {
//                Directory.CreateDirectory(localDirectory);
//                Debug.Log("创建本地目录: " + localDirectory);
//            }

//            // 保存下载的文件
//            File.WriteAllBytes(localFilePath, fileData);
//            Debug.Log("文件下载成功: " + localFilePath);
//        }
//        else
//        {
//            Debug.LogError("文件下载失败: " + request.error);
//        }
//    }
//}

//[System.Serializable]
//public class VideoRequest
//{
//    public string video_path;

//    public VideoRequest(string videoPath)
//    {
//        video_path = videoPath;
//    }
//}


using SFB;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class VideoUploadManager : MonoBehaviour
{
    public void OpenFileDialog()
    {
#if UNITY_ANDROID
        NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log("选中的视频文件路径：" + path);
                StartCoroutine(UploadFile(path)); // 开始上传文件
            }
            else
            {
                Debug.LogWarning("未选择文件或路径为空");
            }
        }, new[] { "video/*" });

        if (permission == NativeFilePicker.Permission.Denied)
        {
            Debug.LogWarning("文件访问权限被拒绝");
        }
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "选择视频文件", "", new[] { new ExtensionFilter("Video Files", "mp4", "avi", "mov", "mkv") }, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string selectedFilePath = paths[0];
            Debug.Log("选中的视频文件路径：" + selectedFilePath);

            // 开始上传文件
            StartCoroutine(UploadFile(selectedFilePath));
        }
        else
        {
            Debug.LogWarning("未选择文件或路径为空");
        }
#else
        Debug.LogWarning("当前平台不支持文件选择功能");
#endif
    }

    // 使用 HTTP 上传文件到服务器
    private IEnumerator UploadFile(string localFilePath)
    {
        string serverUrl = "http://150.65.60.21:5000/upload"; // 替换为服务器的上传接口地址
        byte[] fileData = File.ReadAllBytes(localFilePath); // 读取文件内容
        string fileName = Path.GetFileName(localFilePath);

        // 创建 Multipart 表单数据
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, fileName, "video/mp4"); // 假设文件类型是 mp4

        UnityWebRequest request = UnityWebRequest.Post(serverUrl, form);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("文件上传成功: " + request.downloadHandler.text);

            // 上传完成后，通知服务器处理
            NotifyServer(fileName);
        }
        else
        {
            Debug.LogError("文件上传失败: " + request.error);
        }
    }

    // 通知服务器处理上传的视频文件
    private void NotifyServer(string fileName)
    {
        string serverUrl = "http://150.65.60.21:5000/process"; // 服务器的处理接口地址
        string filePath = "D:/SFTP/upload/" + fileName;

        // 创建请求对象
        VideoRequest requestData = new VideoRequest(filePath);

        // 序列化为 JSON
        string jsonData = JsonUtility.ToJson(requestData);

        StartCoroutine(SendProcessRequest(serverUrl, jsonData));
    }

    private IEnumerator SendProcessRequest(string serverUrl, string jsonData)
    {
        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("通知服务器成功: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("通知服务器失败: " + request.error);
        }
    }

    // 下载处理后的文件
    public void TestDownload()
    {
        StartCoroutine(DownloadFile("/result/video_topView.png", "D:/Unity/Assets/Resource/video_topView.png"));
    }

    private IEnumerator DownloadFile(string remoteFilePath, string localFilePath)
    {
        string serverUrl = "http://150.65.60.21:5000/result/" + Path.GetFileName(remoteFilePath); // 替换为下载接口地址
        UnityWebRequest request = UnityWebRequest.Get(serverUrl);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] fileData = request.downloadHandler.data;

            // 确保本地目录存在
            string localDirectory = Path.GetDirectoryName(localFilePath);
            if (!Directory.Exists(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
                Debug.Log("创建本地目录: " + localDirectory);
            }

            // 保存下载的文件
            File.WriteAllBytes(localFilePath, fileData);
            Debug.Log("文件下载成功: " + localFilePath);
        }
        else
        {
            Debug.LogError("文件下载失败: " + request.error);
        }
    }
}

[System.Serializable]
public class VideoRequest
{
    public string video_path;

    public VideoRequest(string videoPath)
    {
        video_path = videoPath;
    }
}
