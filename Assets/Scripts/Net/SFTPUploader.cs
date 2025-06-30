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

            // Build the full SFTP URL with the correct IP address and path
            string ftpUri = "sftp://150.65.60.21" + remotePath;

            // Upload file
            client.UploadFile(ftpUri, localPath);
            Debug.Log("File uploaded successfully!");
        }
        catch (WebException ex)
        {
            Debug.LogError("File upload failed: " + ex.Message);
        }
    }
}
