using UnityEngine;

namespace UI.AR
{
    /// <summary>
    /// 2D 地图列表项
    /// </summary>
    public class TwoDMapListItem : MonoBehaviour
    {
        public string fileName;

        public void OnLoadButtonClicked()
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("文件名为空，无法加载");
                return;
            }

            if (EditorManager.Instance != null)
            {
                EditorManager.Instance.LoadSceneFromJsonAR(fileName);

                if (EasyARUIManager.Instance != null)
                {
                    EasyARUIManager.Instance.Close2DMapSidePanel();
                }
            }
        }

        public void OnDeleteButtonClicked()
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("文件名为空，无法删除");
                return;
            }

            string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);

                if (EasyARUIManager.Instance != null)
                {
                    EasyARUIManager.Instance.Open2DMapSidePanel();
                }
            }
        }
    }
}
