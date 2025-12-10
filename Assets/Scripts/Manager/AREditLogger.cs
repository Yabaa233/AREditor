using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Manager
{
    /// <summary>
    /// AR编辑行为日志记录器（单例）
    /// </summary>
    public class AREditLogger : MonoBehaviour
    {
        private static AREditLogger instance;
        public static AREditLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("AREditLogger");
                    instance = go.AddComponent<AREditLogger>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private List<LogEntry> logs = new List<LogEntry>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 记录日志（立即写入到地图对应的日志文件）
        /// </summary>
        public void Log(string mapId, string mapName, string eventType, string objectName = "", string details = "")
        {
            if (string.IsNullOrEmpty(mapId))
            {
                Debug.LogWarning("[AREditLogger] mapId为空，跳过日志记录");
                return;
            }

            LogEntry entry = new LogEntry(mapId, mapName, eventType, objectName, details);
            logs.Add(entry);

            // 立即追加写入到该地图对应的日志文件
            AppendLogToFile(mapId, entry);

            Debug.Log($"[AREditLogger] 记录: {eventType} | Map: {mapName}({mapId}) | Object: {objectName}");
        }

        /// <summary>
        /// 将日志追加写入到地图对应的文件
        /// </summary>
        private void AppendLogToFile(string mapId, LogEntry entry)
        {
            try
            {
                string fileName = $"AREditLog_{mapId}.json";
                string filePath = Path.Combine(Application.persistentDataPath, fileName);

                // 读取现有日志（如果文件存在）
                LogExportData existingData = null;
                if (File.Exists(filePath))
                {
                    string existingJson = File.ReadAllText(filePath);
                    existingData = JsonUtility.FromJson<LogExportData>(existingJson);
                }

                // 创建或更新日志数据
                if (existingData == null)
                {
                    existingData = new LogExportData
                    {
                        exportTime = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                        totalCount = 0,
                        logs = new List<LogEntry>()
                    };
                }

                // 追加新日志
                existingData.logs.Add(entry);
                existingData.totalCount = existingData.logs.Count;
                existingData.exportTime = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                // 写回文件
                string json = JsonUtility.ToJson(existingData, true);
                File.WriteAllText(filePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AREditLogger] 写入日志文件失败: {e.Message}");
            }
        }

        /// <summary>
        /// 获取所有日志
        /// </summary>
        public List<LogEntry> GetAllLogs()
        {
            return new List<LogEntry>(logs);
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        public void ClearLogs()
        {
            logs.Clear();
            Debug.Log("[AREditLogger] 日志已清空");
        }

        /// <summary>
        /// 删除指定地图的日志文件
        /// </summary>
        public void DeleteMapLog(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                Debug.LogWarning("[AREditLogger] mapId为空，无法删除日志");
                return;
            }

            try
            {
                string fileName = $"AREditLog_{mapId}.json";
                string filePath = Path.Combine(Application.persistentDataPath, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[AREditLogger] 已删除地图日志文件: {fileName}");
                }
                else
                {
                    Debug.Log($"[AREditLogger] 日志文件不存在: {fileName}");
                }

                // 同时清除内存中该地图的日志
                logs.RemoveAll(log => log.mapId == mapId);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AREditLogger] 删除日志文件失败: {e.Message}");
            }
        }

        /// <summary>
        /// 获取日志数量
        /// </summary>
        public int GetLogCount()
        {
            return logs.Count;
        }
    }

    /// <summary>
    /// 导出数据结构
    /// </summary>
    [System.Serializable]
    public class LogExportData
    {
        public string exportTime;
        public int totalCount;
        public List<LogEntry> logs;
    }
}
