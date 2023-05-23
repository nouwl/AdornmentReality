using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.EventSystems;

namespace SmartAR
{
    [AddComponentMenu("Matej Vanco/Smart AR/AR Manager")]
    [DisallowMultipleComponent]
    /// <summary>
    /// Main system for SmartAR written by Matej Vanco in June 2020, updated in April 2022.
    /// Please read the official docs for more info
    /// </summary>
    public class ARManager : MonoBehaviour
    {
        //--------------------------------------------------AR Manager General Input & Components
        public Camera arMainCam;
        public ARSession arSessionComponent;
        public int arTargetFPS = 30;

        public bool arUseStreaming = true;
        public ARStreaming arStreamingComponent;
        public bool arStreamingLoadOnStart = false;
        public bool arUsePerformanceStats = true;
        public ARPerformanceStats arPerformanceStatsComponent;
        public bool arHandleARAnchors = false;

        //--------------------------------------------------AR Editor Essential Variables
        public bool arEditor_EditableSpecificObjects = true;
        public List<Transform> arEditor_SpecificObjects = new List<Transform>();
        public enum AREditor_FindObjectsBy { Name, Tag, NameMacro, Component, GetAllChildren};
        public AREditor_FindObjectsBy arEditor_FindObjectsBy = AREditor_FindObjectsBy.Component;
        public string arEditor_findObjNameTagMacro;
        public MonoBehaviour arEditor_findObjComponent;
        public Transform arEditor_getChildren;
        //AR Editor Values & Abstracts
        public Transform arEditor_SelectedObject;

        public float arEditor_MoveMultiplier = 0.2f;
        public float arEditor_RotationMultiplier = 12.0f;
        public bool arEditor_ScaleByFingersPinch = true;
        public float arEditor_ScaleMultiplier = 0.05f;

        public Vector2 arEditor_MoveMultiLimitation = new Vector2(0.01f, 0.5f);
        public Vector2 arEditor_RotationMultiLimitation = new Vector2(1.0f, 20.0f);
        public Vector2 arEditor_ScaleMultiLimitation = new Vector2(0.01f, 0.1f);

        public bool arEditor_RotationMode = false;
        public bool arEditor_MoveDimensionXY = false;
        public bool arEditor_AlignFromUserView = true;

        public bool arEditor_EnableEditorOnStart = false;

        //--------------------------------------------------AR Editor UI Hierarchy
        [System.Serializable]
        public class AREditorUI
        {
            [Header("Essential Editor UI")]
            public GameObject uiFullEditorUI;
            public Transform uiSelectionPointer;
            public bool useInformationalTextDebug = true;
            public Text uiInformationalText;
            [Header("Editor Speed Values UI")]
            public Text uiMoveRotMultiply;
            public Text uiScaleSpeed;
            [Header("Internal Editor Info UI")]
            public Text uiEditorInfo;
            public Text uiEditorSelectedObjInfo;
            public GameObject uiEditorInspectorPanel;
            [Header("Editor Hierarchy UI")]
            public Button uiHierarchy_ButtonPrefab;
            public Transform uiHierarchy_ContentRoot;
        }
        public AREditorUI arEditorUI;

        [System.Serializable]
        public class ARCalibrationPlusAdvanced
        {
            public bool CalibrateOnStart = true;
            public bool PlayCameraRenderDistanceEffect = true;
            public float CameraMaxRenderDistanceEffect = 12f;
            public float CameraRenderEffectDuration = 8.0f;
            public GameObject CalibrationInfoRoot;
            [Space]
            public bool useMaxSteadyTime = true;
            public float maxSteadyTime = 40f;
            public bool useMaxZeroGyroTime = true;
            public float SteadyMinUnbiasedSpeed = 0.05f;
            public float maxZeroGyroTime = 3f;
        }
        public ARCalibrationPlusAdvanced arCalibration;

        //-------------------------------------------------INTERNAL VARIABLES
        private bool internalCalibrationProcess = false;

        private Vector3 internalPreviousVal;
        private float internal_StoredMoveSpeed;
        private float internal_StoredRotationSpeed;

        private float arGyro_recalibrationTimer;
        private float arGyro_UnbiasedRate;

        private void Awake()
        {
            if (arMainCam == null)
            {
                arMainCam = Camera.main;
                if(arMainCam == null)
                    Debug.LogError("AR Manager: There is no main camera!");
            }

            Application.targetFrameRate = arTargetFPS;

            Input.gyro.enabled = true;

            arEditorUI.uiEditorInspectorPanel.SetActive(false);

#if UNITY_EDITOR
            if (arCalibration.useMaxSteadyTime)
                arCalibration.maxSteadyTime = Mathf.Infinity;
            if (arCalibration.useMaxZeroGyroTime)
                arCalibration.maxZeroGyroTime = Mathf.Infinity;
#endif
            //----Getting editable objects in scene
            arEditor_RefreshHierarchy();

            //----Setting up other components
            arEditorUI.uiFullEditorUI.SetActive(arEditor_EnableEditorOnStart);
            if (arUsePerformanceStats)
                arPerformanceStatsComponent.enabled = arEditorUI.uiFullEditorUI.activeInHierarchy;
            else if (arPerformanceStatsComponent)
                arPerformanceStatsComponent.enabled = false;

            //----Loading from start (if possible)
            if (arUseStreaming && arStreamingComponent)
            {
                arStreamingComponent.MainArManager = this;
                if (arStreamingLoadOnStart)
                    arStreamingComponent.AR_Load();
            }

            internal_StoredRotationSpeed = arEditor_RotationMultiplier;
            internal_StoredMoveSpeed = arEditor_MoveMultiplier;

            //----Setting up calibration
            if (!arCalibration.CalibrateOnStart)
            {
                if (arCalibration.PlayCameraRenderDistanceEffect)
                {
                    arMainCam.farClipPlane = 0.2f;
                    StartCoroutine(ARInternal_CamRenderEffect());
                }
                return;
            }

            //----Going for an additional effect
            if (arCalibration.PlayCameraRenderDistanceEffect)
                arMainCam.farClipPlane = 0.2f;
            internalCalibrationProcess = true;
            arCalibration.CalibrationInfoRoot.SetActive(true);
        }

        /// <summary>
        /// Play additional camera render effect
        /// </summary>
        private IEnumerator ARInternal_CamRenderEffect()
        {
            float t = 0.0f;
            arCalibration.CameraRenderEffectDuration = Mathf.Clamp(arCalibration.CameraRenderEffectDuration, 0.1f, arCalibration.CameraRenderEffectDuration);
            while (t < arCalibration.CameraRenderEffectDuration)
            {
                arMainCam.farClipPlane = Mathf.Lerp(0.2f, arCalibration.CameraMaxRenderDistanceEffect, t / arCalibration.CameraRenderEffectDuration);
                t += Time.deltaTime;
                yield return null;
            }
        }


        //AR System Essentials

        /// <summary>
        /// Reset AR Core [recalibrate]
        /// </summary>
        public void ar_ResetARCore()
        {
            internalCalibrationProcess = false;
            arCalibration.CalibrationInfoRoot.SetActive(false);

            arSessionComponent.Reset();

            arEditorInternal_DebugInfo("Calibrated");

            if (arCalibration.PlayCameraRenderDistanceEffect)
                StartCoroutine(ARInternal_CamRenderEffect());
        }

        /// <summary>
        /// Process to gyroscopic calibration
        /// </summary>
        public void ar_ProcessToGyroCalibration()
        {
            internalCalibrationProcess = true;
            arGyro_recalibrationTimer = 0f;

            arCalibration.CalibrationInfoRoot.SetActive(true);

            this.StopAllCoroutines();

            if(arCalibration.PlayCameraRenderDistanceEffect)
                arMainCam.farClipPlane = 0.2f;
        }

        //AR Editor System

        #region AR_EditorSystem

        /// <summary>
        /// Editor - essential object selection
        /// </summary>
        public void arEditor_SelectObject(GameObject obj)
        {
            arEditorUI.uiSelectionPointer.gameObject.SetActive(true);
            arEditor_SelectedObject = obj.transform;
            arEditorUI.uiEditorInspectorPanel.SetActive(true);
        }
        /// <summary>
        /// Editor - essential object deselection
        /// </summary>
        public void arEditor_DeselectObject()
        {
            if (arEditor_SelectedObject)
                arEditorInternal_DebugInfo("Object deselected");
            arEditor_SelectedObject = null;
            arEditorUI.uiSelectionPointer.gameObject.SetActive(false);
            arEditorUI.uiEditorInspectorPanel.SetActive(false);
        }



        /// <summary>
        /// Editor - turn off/on
        /// </summary>
        public void arEditor_OnOff(bool onoff)
        {
            arEditorUI.uiFullEditorUI.SetActive(onoff);
            if (arUsePerformanceStats)
                arPerformanceStatsComponent.enabled = arEditorUI.uiFullEditorUI.activeInHierarchy;
            else if (arPerformanceStatsComponent)
                arPerformanceStatsComponent.enabled = false;
        }



        /// <summary>
        /// Editor - change move/rotation multiply value
        /// </summary>
        public void arEditor_ChangeMoveRotMulti(Slider s)
        {
            if (arEditor_RotationMode)
            {
                arEditor_RotationMultiplier = s.value;
                internal_StoredRotationSpeed = arEditor_RotationMultiplier;
                s.minValue = arEditor_RotationMultiLimitation.x;
                s.maxValue = arEditor_RotationMultiLimitation.y;
                arEditorUI.uiMoveRotMultiply.text = "Rotation Multiplier " + arEditor_RotationMultiplier.ToString("0.00");
            }
            else
            {
                arEditor_MoveMultiplier = s.value;
                internal_StoredMoveSpeed = arEditor_MoveMultiplier;
                s.minValue = arEditor_MoveMultiLimitation.x;
                s.maxValue = arEditor_MoveMultiLimitation.y;
                arEditorUI.uiMoveRotMultiply.text = "Movement Multiplier " + arEditor_MoveMultiplier.ToString("0.00");
            }
        }

        /// <summary>
        /// Editor - change scale multiply value
        /// </summary>
        public void arEditor_ChangeScaleMulti(Slider s)
        {
            arEditor_ScaleMultiplier = s.value;
            s.minValue = arEditor_ScaleMultiLimitation.x;
            s.maxValue = arEditor_ScaleMultiLimitation.y;
            arEditorUI.uiScaleSpeed.text = "Scale " + arEditor_ScaleMultiplier.ToString("0.00");
        }

        /// <summary>
        /// Editor - change object size [true - add, false - subtr]
        /// </summary>
        public void arEditor_ChangeObjectSize(bool add)
        {
            if (!arEditor_SelectedObject)
            {
                arEditorInternal_DebugInfo("There is no object selected");
                return;
            }
            float v = (add) ? arEditor_ScaleMultiplier : -arEditor_ScaleMultiplier;
            Vector3 objs = arEditor_SelectedObject.transform.localScale;
            objs.x += v;
            objs.y += v;
            objs.z += v;
            arEditor_SelectedObject.transform.localScale = objs;
        }

        /// <summary>
        /// Editor - change editor XY dimension
        /// </summary>
        public void arEditor_ChangeXYDimension(bool XY_)
        {
            arEditor_MoveDimensionXY = XY_;
            arEditorInternal_DebugInfo("XY Dimension: " + XY_.ToString());
        }

        /// <summary>
        /// Editor - change to/from rotation mode
        /// </summary>
        public void arEditor_ChangeToRotationMode(bool rotMode)
        {
            arEditor_RotationMode = rotMode;

            if (arEditor_RotationMode)
            {
                arEditor_MoveMultiplier = internal_StoredRotationSpeed;
                arEditorUI.uiMoveRotMultiply.text = "Rotation Multiplier " + arEditor_MoveMultiplier.ToString("0.00");
            }
            else
            {
                arEditor_MoveMultiplier = internal_StoredMoveSpeed;
                arEditorUI.uiMoveRotMultiply.text = "Movement Multiplier " + arEditor_MoveMultiplier.ToString("0.00");
            }
            arEditorInternal_DebugInfo("Rotation Mode: " + arEditor_RotationMode.ToString());
        }

        /// <summary>
        /// Editor - change global align
        /// </summary>
        public void arEditor_ChangeAlign(bool align)
        {
            arEditor_AlignFromUserView = align;
            arEditorInternal_DebugInfo("Align From User: " + arEditor_AlignFromUserView.ToString());
        }

        /// <summary>
        /// Editor - change global align [switch version]
        /// </summary>
        public void arEditor_ChangeAlignSwitch()
        {
            arEditor_AlignFromUserView = !arEditor_AlignFromUserView;
            arEditorInternal_DebugInfo("Align From User: " + arEditor_AlignFromUserView.ToString());
        }

        /// <summary>
        /// Editor - change selected object's activation state ('SetActive' to true or false)
        /// </summary>
        public void arEditor_ChangeObjectActivation(bool onOff)
        {
            if (!arEditor_SelectedObject)
            {
                arEditorInternal_DebugInfo("There is no object selected");
                return;
            }

            arEditor_SelectedObject.gameObject.SetActive(onOff);
        }

        /// <summary>
        /// Editor - reset object transform [tType = 0-location,1-rotation,2-scale]
        /// </summary>
        public void arEditor_ResetTrans(int tType)
        {
            if (!arEditor_SelectedObject)
            {
                arEditorInternal_DebugInfo("Nothing to reset");
                return;
            }
            if (tType == 0)
                arEditor_SelectedObject.transform.localPosition = Vector3.zero;
            else if (tType == 1)
                arEditor_SelectedObject.transform.localRotation = Quaternion.identity;
            else
                arEditor_SelectedObject.transform.localScale = Vector3.one;
        }




        /// <summary>
        /// Editor - refresh current hierarchy content
        /// </summary>
        public void arEditor_RefreshHierarchy()
        {
            if(arEditorUI.uiHierarchy_ContentRoot.childCount != 0)
            {
                for (int i = arEditorUI.uiHierarchy_ContentRoot.childCount - 1; i >= 0; i--)
                    Destroy(arEditorUI.uiHierarchy_ContentRoot.GetChild(i).gameObject);
            }

            if (arEditor_EditableSpecificObjects)
            {
                foreach (Transform gm in arEditor_SpecificObjects)
                {
                    Button newBut = Instantiate(arEditorUI.uiHierarchy_ButtonPrefab, arEditorUI.uiHierarchy_ContentRoot);
                    newBut.gameObject.SetActive(true);
                    newBut.onClick.AddListener(delegate { arEditor_SelectObject(gm.gameObject); });
                    newBut.GetComponentInChildren<Text>().text = gm.name;
                }
            }
            else
            {
                arEditor_SpecificObjects.Clear();
                switch (arEditor_FindObjectsBy)
                {
                    case AREditor_FindObjectsBy.Name:
                        foreach (Transform gm in FindObjectsOfType(typeof(Transform)))
                        {
                            if (gm.name != arEditor_findObjNameTagMacro)
                                continue;
                            arEditor_AddObjectToHierarchy(gm.gameObject);
                        }
                        break;
                    case AREditor_FindObjectsBy.Tag:
                        foreach (Transform gm in FindObjectsOfType(typeof(Transform)))
                        {
                            if (!gm.CompareTag(arEditor_findObjNameTagMacro))
                                continue;
                            arEditor_AddObjectToHierarchy(gm.gameObject);
                        }
                        break;
                    case AREditor_FindObjectsBy.NameMacro:
                        foreach (Transform gm in FindObjectsOfType(typeof(Transform)))
                        {
                            if (!gm.name.Contains(arEditor_findObjNameTagMacro))
                                continue;
                            arEditor_AddObjectToHierarchy(gm.gameObject);
                        }
                        break;
                    case AREditor_FindObjectsBy.Component:
                        foreach (Transform gm in FindObjectsOfType(typeof(Transform)))
                        {
                            if (!gm.GetComponent(arEditor_findObjComponent.name))
                                continue;
                            arEditor_AddObjectToHierarchy(gm.gameObject);
                        }
                        break;
                    case AREditor_FindObjectsBy.GetAllChildren:
                        if(arEditor_getChildren == null)
                        {
                            Debug.LogError("AR Manager: Get all children - root object is empty!");
                            return;
                        }
                        foreach(Transform gm in arEditor_getChildren)
                            arEditor_AddObjectToHierarchy(gm.gameObject);
                        break;
                }
            }
        }

        /// <summary>
        /// Editor - add custom object to the hierarchy
        /// </summary>
        public void arEditor_AddObjectToHierarchy(GameObject gm)
        {
            Button newBut = Instantiate(arEditorUI.uiHierarchy_ButtonPrefab, arEditorUI.uiHierarchy_ContentRoot);
            newBut.gameObject.SetActive(true);
            newBut.onClick.AddListener(delegate { arEditor_SelectObject(gm.gameObject); });
            newBut.GetComponentInChildren<Text>().text = gm.name;
            arEditor_SpecificObjects.Add(gm.transform);
        }

        /// <summary>
        /// Invoke visual debug information
        /// </summary>
        public void arEditorInternal_DebugInfo(string text)
        {
            if (!arEditorUI.useInformationalTextDebug)
                return;
            if (arEditorUI.uiInformationalText == null)
                return;
            arEditorUI.uiInformationalText.gameObject.SetActive(false);
            arEditorUI.uiInformationalText.gameObject.SetActive(true);
            arEditorUI.uiInformationalText.text = text;
            arEditorUI.uiInformationalText.GetComponent<Animation>().Play();
        }

        #endregion


        /// <summary>
        /// AR Editor - selection logic
        /// </summary>
        private void arEditorEssential_ProcessSelectorLogic()
        {
            if (!arEditor_SelectedObject)
                return;
            Vector3 p = arMainCam.WorldToScreenPoint(arEditor_SelectedObject.position);
            p.z = 0f;
            Vector3 screenPoint = arMainCam.WorldToViewportPoint(arEditor_SelectedObject.position);
            bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
            arEditorUI.uiSelectionPointer.GetComponent<Image>().enabled = onScreen;
            arEditorUI.uiSelectionPointer.position = p;
        }

        /// <summary>
        /// AR Editor - rotation logic
        /// </summary>
        private void arEditorEssential_ProcessRotation()
        {
            if (Input.touchCount != 1)
                return;

            Touch touch = Input.GetTouch(0);
            Vector3 tPos = touch.deltaPosition;

            if (touch.phase == TouchPhase.Began)
                internalPreviousVal = tPos;

            Vector3 tcRot = (tPos - internalPreviousVal);
            Vector3 fRot = (arEditor_AlignFromUserView) ? arEditor_SelectedObject.transform.InverseTransformDirection(arMainCam.transform.TransformDirection(new Vector3(tcRot.y, -tcRot.x, 0))) : new Vector3(0, -tcRot.x, 0);
            if (arEditor_AlignFromUserView)
                arEditor_SelectedObject.Rotate(fRot * arEditor_RotationMultiplier * Time.deltaTime);
            else
                arEditor_SelectedObject.localEulerAngles += fRot * arEditor_RotationMultiplier * Time.deltaTime;
        }

        /// <summary>
        /// AR Editor - location logic
        /// </summary>
        private void arEditorEssential_ProcessRelocation()
        {
            if (Input.touchCount != 1)
                return;

            Touch touch = Input.GetTouch(0);
            Vector3 tPos = touch.deltaPosition;

            if (touch.phase == TouchPhase.Began)
            {
                internalPreviousVal = tPos;
                if (arHandleARAnchors && arEditor_SelectedObject.GetComponent<ARAnchor>())
                    Destroy(arEditor_SelectedObject.GetComponent<ARAnchor>());
            }
            else if(arHandleARAnchors && touch.phase == TouchPhase.Ended)
            {
                if (!arEditor_SelectedObject.GetComponent<ARAnchor>())
                    arEditor_SelectedObject.gameObject.AddComponent<ARAnchor>();
            }

            Vector3 tcPos = (tPos - internalPreviousVal);
            Vector3 fPos = (arEditor_MoveDimensionXY) ? new Vector3(tcPos.x, tcPos.y, 0) : new Vector3(tcPos.x, 0, tcPos.y);
            if (arEditor_AlignFromUserView) fPos = arMainCam.transform.TransformDirection(fPos);
            arEditor_SelectedObject.position += fPos * arEditor_MoveMultiplier * Time.deltaTime;
        }

        /// <summary>
        /// AR Editor - scale logic (by fingers pinch)
        /// </summary>
        private void arEditorEssential_ProcessRescaleByFingers()
        {
            if (!arEditor_ScaleByFingersPinch) return;

            Vector3 rescale = arEditorEssential_ProcessRescaleVec(out bool released);
            if (!released)
                arEditor_SelectedObject.localScale += Vector3.one * (rescale.z * arEditor_ScaleMultiplier);
        }

        private float prevPinch = 0.0f;
        private bool pinchBegin = false;
        /// <summary>
        /// Get finger pinch value on the screen. Returns unbiased, raw differential value
        /// </summary>
        private Vector3 arEditorEssential_ProcessRescaleVec(out bool released)
        {
            if (Input.touchCount != 2)
            {
                pinchBegin = false;
                released = true;
                return Vector3.zero;
            }
            Vector3 final;
            float z = 0;
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);

            if (!pinchBegin)
            {
                if (t1.phase == TouchPhase.Moved && t2.phase == TouchPhase.Moved)
                {
                    prevPinch = Vector2.Distance(t1.position, t2.position);
                    pinchBegin = true;
                    released = false;
                }
                else released = false;
            }
            else
            {
                if (t1.phase == TouchPhase.Ended || t2.phase == TouchPhase.Ended)
                {
                    pinchBegin = false;
                    released = true;
                }
                else released = false;
                if (t1.phase == TouchPhase.Moved && t2.phase == TouchPhase.Moved)
                    z = Vector2.Distance(t1.position, t2.position) - prevPinch;
                prevPinch = Vector2.Distance(t1.position, t2.position);
            }

            final = new Vector3(t1.deltaPosition.x, t1.deltaPosition.y, z * 0.15f);
            return final;
        }

        private void Update()
        {
            arInternal_CalibrationChecker();

            if (!arEditorUI.uiFullEditorUI.activeInHierarchy)
                return;

            arEditorEssential_ProcessSelectorLogic();

            arEditorUI.uiEditorInfo.text = "Gyroscope: " + Input.gyro.attitude.x.ToString("0.0") + "X " + Input.gyro.attitude.y.ToString("0.0") + "Y\n" +
                "Bias: " + arGyro_UnbiasedRate.ToString("0.00");
            if (arCalibration.useMaxSteadyTime || arCalibration.useMaxZeroGyroTime)
            {
                arEditorUI.uiEditorInfo.text += "\n\nCondition timer: " + arGyro_recalibrationTimer.ToString("0.0");
                if(arCalibration.useMaxSteadyTime)
                    arEditorUI.uiEditorInfo.text += "\nMax Steady Time " + arCalibration.maxSteadyTime.ToString("0.0");
                if (arCalibration.useMaxZeroGyroTime)
                    arEditorUI.uiEditorInfo.text += "\nMax ZeroGyro Time " + arCalibration.maxZeroGyroTime.ToString("0.0");
            }

            if (arEditor_SelectedObject)
            {
                arEditorUI.uiEditorSelectedObjInfo.text = "<b>"+arEditor_SelectedObject.name +"</b>"+
                    "\nPosition: " + arEditor_SelectedObject.position.ToString() +
                    "\nRotation: " + arEditor_SelectedObject.eulerAngles.ToString() +
                    "\nScale: " + arEditor_SelectedObject.localScale.ToString() +
                    "\nActive: " + arEditor_SelectedObject.gameObject.activeSelf.ToString();

                if (Input.touchCount == 0)
                    return;
                Touch touch = Input.touches[0];
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    return;

                if (!arEditor_RotationMode)
                    arEditorEssential_ProcessRelocation();
                else
                    arEditorEssential_ProcessRotation();

                arEditorEssential_ProcessRescaleByFingers();
            }
            else if (!string.IsNullOrEmpty(arEditorUI.uiEditorSelectedObjInfo.text))
                arEditorUI.uiEditorSelectedObjInfo.text = string.Empty;
        }


        //Calibration Checker

        /// <summary>
        /// Check calibration conditions ['when to calibrate']
        /// </summary>
        private void arInternal_CalibrationChecker()
        {
            //----Calibration Checker
            if (!internalCalibrationProcess)
            {
                bool gyroA = (arCalibration.useMaxZeroGyroTime && (Mathf.Abs(Input.gyro.attitude.x) < 0.1f && Mathf.Abs(Input.gyro.attitude.y) < 0.1f));
                bool gyroB = (arCalibration.useMaxSteadyTime && arGyro_UnbiasedRate <= arCalibration.SteadyMinUnbiasedSpeed);
                if (gyroA || gyroB)
                {
                    arGyro_recalibrationTimer += Time.deltaTime;
                    float max = arCalibration.maxSteadyTime;
                    if (gyroA)
                        max = arCalibration.maxZeroGyroTime;
                    else if (gyroB)
                        max = arCalibration.maxSteadyTime;
                    if (arGyro_recalibrationTimer >= max)
                        ar_ProcessToGyroCalibration();
                }
                else if (arGyro_recalibrationTimer != 0) arGyro_recalibrationTimer = 0;
            }
            arGyro_UnbiasedRate = Mathf.Lerp(arGyro_UnbiasedRate, Input.gyro.rotationRateUnbiased.magnitude, 0.521f * Time.deltaTime); //---0.521 can be changed, depending on how fast the value will be changing
        }       
    }
}