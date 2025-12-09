using UnityEngine;
using Assets.Scripts.Manager;

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

            // 前置条件检查
            var spatialMapManager = EasyARSpatialMapEditorManager.Instance;
            if (spatialMapManager == null)
            {
                Debug.LogWarning("[2D Map] EasyARSpatialMapEditorManager 未初始化");
                return;
            }

            if (!spatialMapManager.IsMapLocalized)
            {
                Debug.LogWarning("[2D Map] 地图未本地化，请先加载并定位AR地图");
                return;
            }

            // 调用新的加载方法：从JSON加载物体到mesh下再转换到点云空间
            bool success = spatialMapManager.LoadObjectsFromJsonToMesh(fileName);

            if (success)
            {
                Debug.Log($"[2D Map] 成功从 {fileName} 加载关卡数据");

                if (EasyARUIManager.Instance != null)
                {
                    EasyARUIManager.Instance.Close2DMapSidePanel();
                }
            }
            else
            {
                Debug.LogWarning($"[2D Map] 从 {fileName} 加载关卡数据失败，请检查是否已完成Mesh配置");
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
