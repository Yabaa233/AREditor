using UnityEngine;
using UnityEngine.UI;
using SFB; // Requires the StandaloneFileBrowser plugin
using System.IO;

public class IOManager : singleton<IOManager>
{
    public Image targetImage; // Target UI Image component

    public void OpenFileDialog()
    {
#if UNITY_ANDROID
        // Use native file picker on Android
        NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log("Selected file path: " + path);
                StartCoroutine(LoadImage(path)); // Load image and apply to Image component
            }
            else
            {
                Debug.LogWarning("No file selected or path is empty");
            }
        }, new[] { "image/*" });

        if (permission == NativeFilePicker.Permission.Denied)
        {
            Debug.LogWarning("File access permission denied");
        }
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR
        // Use StandaloneFileBrowser on Windows
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Image", "",
            new[] { new ExtensionFilter("Image Files", "png", "jpg", "jpeg", "bmp") }, false);

        // Check if any file was selected
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            Debug.Log("Selected file path: " + paths[0]);
            StartCoroutine(LoadImage(paths[0])); // Load image and apply to Image component
        }
        else
        {
            Debug.LogWarning("No file selected or path is empty");
        }
#else
        Debug.LogWarning("File selection is not supported on this platform");
#endif
    }

    private System.Collections.IEnumerator LoadImage(string filePath)
    {
        // Read image data from file
        byte[] fileData = File.ReadAllBytes(filePath);

        // Create texture
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(fileData))
        {
            // Convert texture to Sprite
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

            // Apply to target Image
            targetImage.sprite = sprite;

            // Preserve aspect ratio
            targetImage.preserveAspect = true;
        }
        else
        {
            Debug.LogError("Failed to load image");
        }

        yield return null;
    }
}
