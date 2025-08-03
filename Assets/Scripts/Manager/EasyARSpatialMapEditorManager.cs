using System;
using System.Collections.Generic;
using UnityEngine;
using easyar;
using SpatialMap_SparseSpatialMap;

namespace Assets.Scripts.Manager
{
    /// <summary>
    /// EasyAR空间地图编辑器管理器
    /// 专门处理基于稀疏空间地图的AR编辑功能
    /// </summary>
    public class EasyARSpatialMapEditorManager : singleton<EasyARSpatialMapEditorManager>
    {
        [Header("EasyAR Components")]
        public ARSession arSession;
        public SparseSpatialMapWorkerFrameFilter mapWorker;
        public SparseSpatialMapController mapControllerPrefab;
        
        [Header("Editor Settings")]
        public bool showPointCloud = true;
        public bool autoSaveOnEdit = false;
        
        // 当前地图会话
        private MapSession currentMapSession;
        private List<MapMeta> availableMaps = new List<MapMeta>();
        
        // 编辑器状态
        private bool isMapLocalized = false;
        private bool isMapBuilding = false;
        private bool isEditMode = false;
        
        // 事件
        public event Action OnMapLocalized;
        public event Action OnMapBuildingStarted;
        public event Action OnMapBuildingCompleted;
        public event Action<GameObject> OnObjectPlaced;
        public event Action<GameObject> OnObjectRemoved;
        
        public bool IsMapLocalized => isMapLocalized;
        public bool IsMapBuilding => isMapBuilding;
        public bool IsEditMode => isEditMode;
        public MapSession CurrentMapSession => currentMapSession;

        private void Start()
        {
            InitializeEditor();
            LoadAvailableMaps();
        }

        private void InitializeEditor()
        {
            Debug.Log("[EasyAR Spatial Map Editor] 初始化编辑器");
        }

        /// <summary>
        /// 开始构建新地图
        /// </summary>
        public void StartMapBuilding()
        {
            if (currentMapSession != null)
            {
                currentMapSession.Dispose();
            }

            currentMapSession = new MapSession(arSession, mapWorker, null);
            currentMapSession.SetupMapBuilder(mapControllerPrefab);
            
            isMapBuilding = true;
            isMapLocalized = false;
            isEditMode = false;
            
            OnMapBuildingStarted?.Invoke();
            Debug.Log("[EasyAR Spatial Map Editor] 开始构建地图");
        }

        /// <summary>
        /// 加载现有地图
        /// </summary>
        public void LoadMap(MapMeta mapMeta)
        {
            if (currentMapSession != null)
            {
                currentMapSession.Dispose();
            }

            currentMapSession = new MapSession(arSession, mapWorker, new List<MapMeta> { mapMeta });
            currentMapSession.LoadMapMeta(mapControllerPrefab, showPointCloud);
            
            isMapBuilding = false;
            isMapLocalized = false;
            isEditMode = false;
            
            // 监听本地化状态
            StartCoroutine(WaitForLocalization());
            
            Debug.Log($"[EasyAR Spatial Map Editor] 加载地图: {mapMeta.Map.Name}");
        }

        /// <summary>
        /// 保存当前地图
        /// </summary>
        public void SaveCurrentMap()
        {
            if (currentMapSession == null || !isMapBuilding)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 没有可保存的地图");
                return;
            }

            string mapName = $"GameMap_{DateTime.Now:yyyyMMdd_HHmmss}";
            currentMapSession.Save(mapName, null);
            
            Debug.Log($"[EasyAR Spatial Map Editor] 保存地图: {mapName}");
        }

        /// <summary>
        /// 保存对象信息（与EasyAR示例保持一致）
        /// </summary>
        public void SaveObjectsInfo()
        {
            if (currentMapSession == null || currentMapSession.Maps.Count == 0)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 没有可保存的对象信息");
                return;
            }

            var mapData = currentMapSession.Maps[0];
            var propInfos = new List<MapMeta.PropInfo>();

            foreach (var prop in mapData.Props)
            {
                if (prop != null)
                {
                    var position = prop.transform.localPosition;
                    var rotation = prop.transform.localRotation;
                    var scale = prop.transform.localScale;

                    propInfos.Add(new MapMeta.PropInfo()
                    {
                        Name = prop.name,
                        Position = new float[3] { position.x, position.y, position.z },
                        Rotation = new float[4] { rotation.x, rotation.y, rotation.z, rotation.w },
                        Scale = new float[3] { scale.x, scale.y, scale.z }
                    });
                }
            }

            mapData.Meta.Props = propInfos;
            MapMetaManager.Save(mapData.Meta);
            
            Debug.Log($"[EasyAR Spatial Map Editor] 保存对象信息: {propInfos.Count} 个对象");
        }

        /// <summary>
        /// 清除当前地图
        /// </summary>
        public void ClearCurrentMap()
        {
            if (currentMapSession != null)
            {
                currentMapSession.Dispose();
                currentMapSession = null;
            }
            
            ClearAllObjects();
            
            isMapLocalized = false;
            isMapBuilding = false;
            isEditMode = false;
            
            Debug.Log("[EasyAR Spatial Map Editor] 清除地图");
        }

        /// <summary>
        /// 进入编辑模式
        /// </summary>
        public void EnterEditMode()
        {
            if (!isMapLocalized)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 地图未本地化，无法进入编辑模式");
                return;
            }
            
            isEditMode = true;
            Debug.Log("[EasyAR Spatial Map Editor] 进入编辑模式");
        }

        /// <summary>
        /// 退出编辑模式
        /// </summary>
        public void ExitEditMode()
        {
            isEditMode = false;
            Debug.Log("[EasyAR Spatial Map Editor] 退出编辑模式");
        }

        /// <summary>
        /// 在空间地图上放置游戏对象
        /// </summary>
        public bool PlaceGameObjectOnMap(GameObject gameObject, Vector2 screenPosition)
        {
            if (!isMapLocalized || currentMapSession == null || currentMapSession.Maps.Count == 0)
            {
                Debug.LogWarning("[EasyAR Spatial Map Editor] 地图未本地化，无法放置对象");
                return false;
            }

            // 使用EasyAR的碰撞检测
            var hitResult = currentMapSession.HitTestOne(screenPosition);
            if (hitResult.OnSome)
            {
                // 按照EasyAR示例的方式：将对象挂在MapController下
                var mapData = currentMapSession.Maps[0];
                gameObject.transform.parent = mapData.Controller.transform;
                gameObject.transform.localPosition = mapData.Controller.transform.InverseTransformPoint(hitResult.Value);
                
                // 添加到MapData的Props列表（与示例保持一致）
                mapData.Props.Add(gameObject);
                
                OnObjectPlaced?.Invoke(gameObject);
                
                Debug.Log($"[EasyAR Spatial Map Editor] 对象已放置: {gameObject.name} at {hitResult.Value}");
                return true;
            }
            
            Debug.LogWarning("[EasyAR Spatial Map Editor] 未找到有效的放置点");
            return false;
        }

        /// <summary>
        /// 获取地图上的碰撞点
        /// </summary>
        public Optional<Vector3> GetMapHitPoint(Vector2 screenPoint)
        {
            if (!isMapLocalized || currentMapSession == null)
            {
                return Optional<Vector3>.CreateNone();
            }
            
            return currentMapSession.HitTestOne(screenPoint);
        }

        /// <summary>
        /// 注册对象（与EasyAR示例保持一致）
        /// </summary>
        public void RegisterObject(GameObject obj)
        {
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                var mapData = currentMapSession.Maps[0];
                if (!mapData.Props.Contains(obj))
                {
                    mapData.Props.Add(obj);
                    Debug.Log($"[EasyAR Spatial Map Editor] 注册对象: {obj.name}");
                }
            }
        }

        /// <summary>
        /// 注销对象（与EasyAR示例保持一致）
        /// </summary>
        public void UnregisterObject(GameObject obj)
        {
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                var mapData = currentMapSession.Maps[0];
                if (mapData.Props.Contains(obj))
                {
                    mapData.Props.Remove(obj);
                    OnObjectRemoved?.Invoke(obj);
                    Debug.Log($"[EasyAR Spatial Map Editor] 注销对象: {obj.name}");
                }
            }
        }

        /// <summary>
        /// 清除所有对象（与EasyAR示例保持一致）
        /// </summary>
        public void ClearAllObjects()
        {
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                var mapData = currentMapSession.Maps[0];
                foreach (var obj in mapData.Props)
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                mapData.Props.Clear();
                Debug.Log("[EasyAR Spatial Map Editor] 清除所有对象");
            }
        }

        /// <summary>
        /// 获取所有放置的对象（与EasyAR示例保持一致）
        /// </summary>
        public List<GameObject> GetAllPlacedObjects()
        {
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                return new List<GameObject>(currentMapSession.Maps[0].Props);
            }
            return new List<GameObject>();
        }

        /// <summary>
        /// 根据ID查找对象（与EasyAR示例保持一致）
        /// </summary>
        public GameObject GetObjectByID(string id)
        {
            if (currentMapSession != null && currentMapSession.Maps.Count > 0)
            {
                var mapData = currentMapSession.Maps[0];
                foreach (var obj in mapData.Props)
                {
                    if (obj != null && obj.name == id)
                    {
                        return obj;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 等待地图本地化
        /// </summary>
        private System.Collections.IEnumerator WaitForLocalization()
        {
            while (!isMapLocalized && currentMapSession != null)
            {
                if (mapWorker.LocalizedMap != null)
                {
                    isMapLocalized = true;
                    OnMapLocalized?.Invoke();
                    OnMapBuildingCompleted?.Invoke();
                    
                    Debug.Log($"[EasyAR Spatial Map Editor] 地图已本地化: {mapWorker.LocalizedMap.MapInfo.Name}");
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>
        /// 加载可用地图列表
        /// </summary>
        private void LoadAvailableMaps()
        {
            availableMaps = MapMetaManager.LoadAll();
            Debug.Log($"[EasyAR Spatial Map Editor] 加载了 {availableMaps.Count} 个地图");
        }

        /// <summary>
        /// 获取可用地图列表
        /// </summary>
        public List<MapMeta> GetAvailableMaps()
        {
            return new List<MapMeta>(availableMaps);
        }

        /// <summary>
        /// 设置点云显示
        /// </summary>
        public void SetPointCloudVisibility(bool visible)
        {
            showPointCloud = visible;
            if (currentMapSession != null && currentMapSession.MapWorker != null)
            {
                // 更新点云显示状态
                Debug.Log($"[EasyAR Spatial Map Editor] 点云显示: {(visible ? "开启" : "关闭")}");
            }
        }

        /// <summary>
        /// 获取编辑器状态信息
        /// </summary>
        public string GetEditorStatus()
        {
            string status = "";
            status += $"地图构建: {(isMapBuilding ? "进行中" : "未开始")}\n";
            status += $"地图本地化: {(isMapLocalized ? "已完成" : "未完成")}\n";
            status += $"编辑模式: {(isEditMode ? "开启" : "关闭")}\n";
            
            if (currentMapSession != null && currentMapSession.MapWorker != null)
            {
                var localizedMap = currentMapSession.MapWorker.LocalizedMap;
                if (localizedMap != null)
                {
                    status += $"当前地图: {localizedMap.MapInfo.Name}\n";
                    status += $"点云数量: {localizedMap.PointCloud.Count}";
                }
            }
            
            return status;
        }

        private void OnDestroy()
        {
            if (currentMapSession != null)
            {
                currentMapSession.Dispose();
            }
            
            ClearAllObjects();
        }
    }
} 