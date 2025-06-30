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
                Debug.Log("Selected video file path: " + path);
                StartCoroutine(UploadFile(path)); // Start uploading the file
            }
            else
            {
                Debug.LogWarning("No file selected or path is empty");
            }
        }, new[] { "video/*" });

        if (permission == NativeFilePicker.Permission.Denied)
        {
            Debug.LogWarning("File access permission denied");
        }
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Select a video file", "", new[] { new ExtensionFilter("Video Files", "mp4", "avi", "mov", "mkv") }, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string selectedFilePath = paths[0];
            Debug.Log("Selected video file path: " + selectedFilePath);

            // Start uploading the file
            StartCoroutine(UploadFile(selectedFilePath));
        }
        else
        {
            Debug.LogWarning("No file selected or path is empty");
        }
#else
        Debug.LogWarning("File selection is not supported on this platform");
#endif
    }

    // Upload the file to the server via HTTP
    private IEnumerator UploadFile(string localFilePath)
    {
        string serverUrl = "http://150.65.60.21:5000/upload"; // Replace with your server's upload endpoint
        byte[] fileData = File.ReadAllBytes(localFilePath); // Read file content
        string fileName = Path.GetFileName(localFilePath);

        // Create multipart form data
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, fileName, "video/mp4"); // Assume the file type is mp4

        UnityWebRequest request = UnityWebRequest.Post(serverUrl, form);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("File upload successful: " + request.downloadHandler.text);

            // Notify the server to process the uploaded video
            NotifyServer(fileName);
        }
        else
        {
            Debug.LogError("File upload failed: " + request.error);
        }
    }

    // Notify the server to process the uploaded video file
    private void NotifyServer(string fileName)
    {
        string serverUrl = "http://150.65.60.21:5000/process"; // Server's processing endpoint
        string filePath = "D:/SFTP/upload/" + fileName;

        // Create request object
        VideoRequest requestData = new VideoRequest(filePath);

        // Serialize to JSON
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
            Debug.Log("Server notification successful: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Server notification failed: " + request.error);
        }
    }

    // Download the processed file
    public void TestDownload()
    {
        StartCoroutine(DownloadFile("/result/video_topView.png", "D:/Unity/Assets/Resource/video_topView.png"));
    }

    private IEnumerator DownloadFile(string remoteFilePath, string localFilePath)
    {
        string serverUrl = "http://150.65.60.21:5000/result/" + Path.GetFileName(remoteFilePath); // Replace with download endpoint
        UnityWebRequest request = UnityWebRequest.Get(serverUrl);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] fileData = request.downloadHandler.data;

            // Ensure local directory exists
            string localDirectory = Path.GetDirectoryName(localFilePath);
            if (!Directory.Exists(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
                Debug.Log("Created local directory: " + localDirectory);
            }

            // Save the downloaded file
            File.WriteAllBytes(localFilePath, fileData);
            Debug.Log("File downloaded successfully: " + localFilePath);
        }
        else
        {
            Debug.LogError("File download failed: " + request.error);
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
