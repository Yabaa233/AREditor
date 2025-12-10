using System;

namespace Assets.Scripts.Manager
{
    /// <summary>
    /// 单条日志记录
    /// </summary>
    [Serializable]
    public class LogEntry
    {
        public string timestamp;        // ISO 8601格式时间戳
        public string mapId;             // 地图ID
        public string mapName;           // 地图名称
        public string eventType;         // 事件类型：ObjectPlaced, ObjectDeleted, ObjectSelected, ModeChanged, TriggerCreated等
        public string objectName;        // 对象名称（如果适用）
        public string details;           // JSON格式的详细信息

        public LogEntry(string mapId, string mapName, string eventType, string objectName = "", string details = "")
        {
            this.timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            this.mapId = mapId;
            this.mapName = mapName;
            this.eventType = eventType;
            this.objectName = objectName;
            this.details = details;
        }
    }
}
