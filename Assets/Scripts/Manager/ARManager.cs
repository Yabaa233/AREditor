using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

namespace Assets.Scripts.Manager
{
    public class ARManager : singleton<ARManager>
    {
        public ARSession session;

        void Start()
        {
            StartCoroutine(InitAR());
        }

        IEnumerator InitAR()
        {
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            if (session != null)
            {
                while (ARSession.state < ARSessionState.SessionInitializing)
                {
                    Debug.Log("Waiting for ARSession to initialize... State=" + ARSession.state);
                    yield return null;
                }

                if (ARSession.state >= ARSessionState.SessionInitializing)
                {
                    Debug.Log("Resetting ARSession now, state=" + ARSession.state);
                    session.Reset();
                }
            }
        }

        void OnDisable()
        {
            if (session != null)
            {
                Debug.Log("Disabling ARSession on ARManager...");
                session.enabled = false;
            }
        }

    }
}