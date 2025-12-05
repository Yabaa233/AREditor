//================================================================================================================================
//
//  Copyright (c) 2015-2023 VisionStar Information Technology (Shanghai) Co., Ltd. All Rights Reserved.
//  EasyAR is the registered trademark or trademark of VisionStar Information Technology (Shanghai) Co., Ltd in China
//  and other countries for the augmented reality technology developed by VisionStar Information Technology (Shanghai) Co., Ltd.
//
//================================================================================================================================

using easyar;
using System;
using System.Collections.Generic;

namespace SpatialMap_SparseSpatialMap
{
    [Serializable]
    public class MapMeta
    {
        public SparseSpatialMapController.MapManagerSourceData Map = new SparseSpatialMapController.MapManagerSourceData();
        public List<PropInfo> Props = new List<PropInfo>();
        public MeshAlignmentInfo MeshAlignment = null; // Mesh对齐信息（可选）

        public MapMeta(SparseSpatialMapController.SparseSpatialMapInfo map, List<PropInfo> props)
        {
            Map = new SparseSpatialMapController.MapManagerSourceData() { Name = map.Name, ID = map.ID };
            Props = props;
        }

        [Serializable]
        public class PropInfo
        {
            public string Name = string.Empty;
            public float[] Position = new float[3];
            public float[] Rotation = new float[4];
            public float[] Scale = new float[3];

            public string ObjectID = string.Empty; // ID of the PlacedObjectData
            public List<TriggerActionEventData> Events = new List<TriggerActionEventData>();    // Events attached to this object

            public bool IfHiddenAtGameStart = false; // Whether the object is hidden at game start
        }

        [System.Serializable]
        /// <summary>
        /// Event data
        /// </summary>
        public class TriggerActionEventData
        {
            public TriggerType triggerType;

            public ActionType actionType;

            public string targetObjectID;
        }

        public enum TriggerType { OnEnter, OnExit }
        public enum ActionType { Win, Lose, Enable, Disable }

        [Serializable]
        /// <summary>
        /// Mesh对齐信息（用于混合定位系统）
        /// </summary>
        public class MeshAlignmentInfo
        {
            public string MeshPrefabName = string.Empty; // Mesh预制体名称
            public float[] Position = new float[3];      // 本地位置
            public float[] Rotation = new float[4];      // 本地旋转（四元数）
            public float[] Scale = new float[3];         // 本地缩放
        }
    }
}
