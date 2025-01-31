using System.Net;
using UnityEngine;

public class SFTPUploader : MonoBehaviour
{
    public void UploadFile(string localPath, string remotePath)
    {
        try
        {
            WebClient client = new WebClient();
            client.Credentials = new NetworkCredential("sftpuser", "yourpassword");

            // 拼接 SFTP 地址（确保 IP 地址和目录正确）
            string ftpUri = "sftp://150.65.60.21" + remotePath;

            // 上传文件
            client.UploadFile(ftpUri, localPath);
            Debug.Log("File uploaded successfully!");
        }
        catch (WebException ex)
        {
            Debug.LogError("File upload failed: " + ex.Message);
        }
    }
}
