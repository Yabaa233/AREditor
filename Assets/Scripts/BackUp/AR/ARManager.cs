using System.Collections;
using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.XR.ARFoundation;
using easyar;
using UnityEngine.UI;
using System;

namespace Assets.Scripts.Manager
{
    public class ARManager : singleton<ARManager>
    {
        //public ARSession session;

        //void Start()　
        //{
        //    StartCoroutine(InitAR());
        //}

        //IEnumerator InitAR()
        //{
        //    yield return null;
        //    yield return null;
        //    yield return new WaitForEndOfFrame();

        //    if (session != null)
        //    {
        //        while (ARSession.state < ARSessionState.SessionInitializing)
        //        {
        //            Debug.Log("Waiting for ARSession to initialize... State=" + ARSession.state);
        //            yield return null;
        //        }

        //        if (ARSession.state >= ARSessionState.SessionInitializing)
        //        {
        //            Debug.Log("Resetting ARSession now, state=" + ARSession.state);
        //            session.Reset();
        //        }
        //    }
        //}

        //void OnDisable()
        //{
        //    if (session != null)
        //    {
        //        Debug.Log("Disabling ARSession on ARManager...");
        //        session.enabled = false;
        //    }
        //}

        public SparseSpatialMapController map;
        public SparseSpatialMapWorkerFrameFilter mapWorker;
        public ARSession session;

        public Button btnSave;

        private void Start()
        {
            btnSave.onClick.AddListener(SaveSpatialMap);
            btnSave.interactable = false;
            session.StateChanged += OnSessionStateChanged;
        }

        /// <summary>
        /// Save SpatialMap
        /// </summary>
        private void SaveSpatialMap()
        {
            mapWorker.BuilderMapController.MapHost += SaveMapHostBack;
            try
            {
                mapWorker.BuilderMapController.Host("SpatialMap250721", null);
                Debug.Log("Start Save SpatialMap");
            }
            catch (Exception ex)
            {
                Debug.Log("Save SpatialMap Error :" + ex.Message);
            }

        }

        private void SaveMapHostBack(
            SparseSpatialMapController.SparseSpatialMapInfo mapinfo,
            bool isSuccess,
            string error)
        {
            if (isSuccess)
            {
                PlayerPrefs.SetString("MapID", mapinfo.ID);
                PlayerPrefs.SetString("MapName", mapinfo.Name);
                Debug.Log("Save SpatialMap Sucess. ID: " + mapinfo.ID);
            }
            else
            {
                Debug.Log("Save SpatialMap Error :" + error);
            }
        }

        private void OnSessionStateChanged(ARSession.SessionState state)
        {
            btnSave.interactable = (state == ARSession.SessionState.Ready || state == ARSession.SessionState.Running);
        }

    }
}