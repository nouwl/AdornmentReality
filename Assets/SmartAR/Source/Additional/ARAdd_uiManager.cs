using UnityEngine;

namespace SmartAR
{
    [AddComponentMenu("Matej Vanco/Smart AR/AR UI Manager")]

    /// <summary>
    /// Additional UI manager system written by Matej Vanco 2020.
    /// UI system made for visualizing UI on screen or in world to specific target
    /// </summary>
    public class ARAdd_uiManager : MonoBehaviour
    {
        [System.Serializable]
        public class UiTrackerList
        {
            public string ElementTitle;
            public Transform TrackerTarget;
            public Transform ImageTarget;
        }

        [Tooltip("Leave the field empty (mainCamera) if you would like to get the mainCamera from the scene")]
        public Camera mainCam;
        public UiTrackerList[] TrackerList;

        private void Awake()
        {
            if (mainCam == null)
            {
                mainCam = Camera.main;
                if (mainCam == null)
                    Debug.LogError("AR UI Manager: There is no main camera!");
            }
        }

        private void Update()
        {
            if (TrackerList.Length == 0)
                return;
            foreach (UiTrackerList t in TrackerList)
            {
                if (t.ImageTarget == null)
                    continue;
                if (t.TrackerTarget == null)
                    continue;
                Vector3 screenPoint = mainCam.WorldToViewportPoint(t.TrackerTarget.position);
                bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
                t.ImageTarget.gameObject.SetActive(onScreen);

                Vector3 uiPos = mainCam.WorldToScreenPoint(t.TrackerTarget.position);
                uiPos.z = 0;
                t.ImageTarget.position = uiPos;
            }
        }
    }
}