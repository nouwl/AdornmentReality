using UnityEngine;
using UnityEditor;

namespace SmartAR
{
    /// <summary>
    /// Official editor script for the AR Manager
    /// </summary>
    [CustomEditor(typeof(ARManager))]
    [CanEditMultipleObjects]
    public class ARManager_Editor : Editor
    {
        public Texture2D headerImg;
        private ARManager targetm;

        private bool advanced = false;

        private void OnEnable()
        {
            targetm = (ARManager)target;
        }

        public override void OnInspectorGUI()
        {
            S(10f);

            if(headerImg != null) GUILayout.Label(headerImg);
            EditorGUILayout.HelpBox("SmartAR version 3.0 [April 2022]\n- Unity 2022 compatibility\n- Added rescale-by-pinch feature\n- Added AR Anchor handler\n- Documentation update\n- API update", MessageType.None);
            GUILayout.BeginHorizontal("Box");
            if (GUILayout.Button("Documentation"))
                Application.OpenURL("https://docs.google.com/presentation/d/1m_bWcVMXMyPG64ctSPjgAryTGJToyPiVG2sN0icEkC0/edit?usp=sharing");
            if (GUILayout.Button("Discord"))
                Application.OpenURL("https://discord.gg/Ztr8ghQKqC");
            if (GUILayout.Button("Direct Contact"))
                Application.OpenURL("https://matejvanco.com/contact/");
            GUILayout.EndHorizontal();
            S(5f);

            GUILayout.Label("Essentials");
            BV();
            BV();
            DrawProperty("arMainCam", "*Main Camera","Assign main camera for this scene");
            DrawProperty("arSessionComponent", "*AR Session Component");
            DrawProperty("arTargetFPS", "Target FPS", "Max possible: 0, Default & Recommended: 30");
            EV();
            S(5);
            DrawProperty("arUseStreaming", "Use Streaming", "Use save/load feature");
            if (targetm.arUseStreaming)
            {
                BV();
                DrawProperty("arStreamingComponent", "*Streaming Component");
                if(!targetm.GetComponent<ARStreaming>())
                {
                    if (B("Add Streaming script"))
                        targetm.arStreamingComponent = targetm.gameObject.AddComponent<ARStreaming>();
                }
                DrawProperty("arStreamingLoadOnStart", "Load On Start", "Load data from streaming component after startup (if possible)");
                EV();
            }
            DrawProperty("arUsePerformanceStats", "Use Performance Stats");
            if (targetm.arUsePerformanceStats)
            {
                BV();
                DrawProperty("arPerformanceStatsComponent", "*Performance Stats Component");
                if (!targetm.GetComponent<ARPerformanceStats>())
                {
                    if (B("Add Performance Stats script"))
                        targetm.arPerformanceStatsComponent = targetm.gameObject.AddComponent<ARPerformanceStats>();
                }
                EV();
            }
            S(10);
            BV();
            DrawProperty("arHandleARAnchors", "Handle AR Anchors", "If enabled, the Smart AR will handle the AR Anchors automatically. What is ARAnchor? AR Anchor improves the tracking of an virtual object in AR.");
            if (targetm.arHandleARAnchors)
                EditorGUILayout.HelpBox("Make sure your scene contains the AR Anchor Manager component", MessageType.None);
            EV();
            EV();

            S(10f);

            GUILayout.Label("Editable Objects");
            BV();
            DrawProperty("arEditor_EditableSpecificObjects", "Edit Specific Objects", "Choose specific objects or get the desired object by specific macro");
            if (targetm.arEditor_EditableSpecificObjects)
            {
                BV();
                DrawProperty("arEditor_SpecificObjects", "Specific Game Objects", "", true);
                EV();
            }
            else
            {
                BV();
                DrawProperty("arEditor_FindObjectsBy", "Find Objects By", "Select an option that will get your objects");
                switch (targetm.arEditor_FindObjectsBy)
                {
                    case ARManager.AREditor_FindObjectsBy.Name:
                        DrawProperty("arEditor_findObjNameTagMacro", "Find Objects by Name");
                        break;
                    case ARManager.AREditor_FindObjectsBy.NameMacro:
                        DrawProperty("arEditor_findObjNameTagMacro", "Find Objects by Name Macro", "If objects name contain this character");
                        break;
                    case ARManager.AREditor_FindObjectsBy.Tag:
                        DrawProperty("arEditor_findObjNameTagMacro", "Find Objects by Tag");
                        break;
                    case ARManager.AREditor_FindObjectsBy.Component:
                        DrawProperty("arEditor_findObjComponent", "Find Objects by THIS Component");
                        break;
                    case ARManager.AREditor_FindObjectsBy.GetAllChildren:
                        DrawProperty("arEditor_getChildren", "Object Root", "Get all childs/sub-objects of this gameObject");
                        break;
                }
                if(targetm.arEditor_FindObjectsBy != ARManager.AREditor_FindObjectsBy.GetAllChildren)
                    EditorGUILayout.HelpBox("Searching for objects automatically requires to keep all the objects activated!", MessageType.None);
                EV();
            }
            EV();

            S(5f);

            GUILayout.Label("Editor Parameters");
            BV();
            DrawProperty("arEditor_EnableEditorOnStart", "Enable Editor On Start", "Show editor on startup");
            BV();
            DrawProperty("arEditor_MoveMultiplier", "Editor Move Multiplier", "Selected object moving speed multiplier proceeded by finger");
            DrawProperty("arEditor_RotationMultiplier", "Editor Rotation Multiplier", "Selected object rotation speed multiplier proceeded by finger");
            DrawProperty("arEditor_ScaleByFingersPinch", "Scale By Fingers Pinch");
            DrawProperty("arEditor_ScaleMultiplier", "Editor Scaling Multiplier", "Selected object scaling speed multiplier proceeded by finger");
            EV();
            S(2f);
            BV();
            DrawProperty("arEditor_MoveMultiLimitation", "Move Multi Limitation", "Min & Max movement multiplication");
            DrawProperty("arEditor_RotationMultiLimitation", "Rot Multi Limitation", "Min & Max rotation multiplication");
            DrawProperty("arEditor_ScaleMultiLimitation", "Scale Multi Limitation", "Min & Max scale multiplication");
            EV();
            BV();
            DrawProperty("arEditor_RotationMode", "Rotation Mode Enabled", "If enabled, the rotation mode will be enabled [You will be able to rotate the object, otherwise move the object]");
            DrawProperty("arEditor_MoveDimensionXY", "Move Dimension XY", "If enabled, the movement direction will be set to X and Y axis [example: when you move the object forward (swipes from the bottom to the top) the object will move upwards due to the X & Y dimensions. If this option is false, the dimensions will be set to X & Z. In this case, the object will go forward/backward]");
            DrawProperty("arEditor_AlignFromUserView", "Align From User View", "If enabled, the movement & rotation will be related to the user's point of view. [example: If you move the object forward (swipes with the finger from the bottom to the top) and your are looking at the object from the top view, the object will go down in the global dimension]");
            EV();
            EV();

            S(10f);

            BV();
            DrawProperty("arEditorUI", "Editor UI", "", true);
            EV();

            S(10f);

            BV();
            advanced = EditorGUILayout.Foldout(advanced, "Calibration & Advanced Settings");
            if (advanced)
            {
                EditorGUI.indentLevel++;
                S(5f);
                DrawProperty("arCalibration.CalibrateOnStart", "Show Calibration On Start", "If enabled, the system will require calibration & calibration panel will appear");
                DrawProperty("arCalibration.CalibrationInfoRoot", "Calibration Panel Root", "Required root object of the calibration panel itself");

                S(5f);
                DrawProperty("arCalibration.PlayCameraRenderDistanceEffect", "Play Camera Render Distance Effect", "If enabled, the additional 'render-distance' effect will play after successful calibration");
                if (targetm.arCalibration.PlayCameraRenderDistanceEffect)
                {
                    BV();
                    EditorGUI.indentLevel++;
                    DrawProperty("arCalibration.CameraMaxRenderDistanceEffect", "Target Render Distance Value", "Target render distance for target camera (eg. 60)");
                    DrawProperty("arCalibration.CameraRenderEffectDuration", "Effect Duration", "In seconds...");
                    EditorGUI.indentLevel--;
                    EV();
                }
                S(5f);
                DrawProperty("arCalibration.useMaxSteadyTime", "Use 'Max Steady Time' Feature", "If enabled, the system will start a timer when the device is in idle mode (The device is not rotating). If the timer reaches the specific number, the calibration panel will appear... [When the user might keep the device in 'steady' mode? For example keeping the device in one place or putting it on a table - the potential AR targets might be lost and your scene may be broken. Applications on the public (museums, public meetings) might require such an option]");
                if (targetm.arCalibration.useMaxSteadyTime)
                {
                    BV();
                    EditorGUI.indentLevel++;
                    DrawProperty("arCalibration.maxSteadyTime", "Maximum Steady Time", "Maximum timer number reached in 'Steady mode'");
                    DrawProperty("arCalibration.SteadyMinUnbiasedSpeed", "Minimum Steady Speed", "Minimum, unbiased rotation speed of the device. When the 'Steady Timer' will start? What's the minimum speed of the device's rotation? Default is mostly 0.05");
                    EditorGUI.indentLevel--;
                    EV();
                }
                DrawProperty("arCalibration.useMaxZeroGyroTime", "Use 'Max Zero-Gyro Time' Feature", "If enabled, the system will start a timer when the device is facing down - the gyroscope values are zero. If the timer reaches the specific number, the calibration panel will appear... [It's a similar option as above, however the device must be facing down. For example putting the device on the ground or table facing down - the potential AR targets might be lost and your scene may be broken. Applications on the public (museums, public meetings) might require such an option]");
                if (targetm.arCalibration.useMaxZeroGyroTime)
                {
                    BV();
                    EditorGUI.indentLevel++;
                    DrawProperty("arCalibration.maxZeroGyroTime", "Max Zero-Gyro Timer", "Maximum timer number");
                    EditorGUI.indentLevel--;
                    EV();
                }
                EditorGUI.indentLevel--;
            }
            EV();

            S(5f);
        }

        private void DrawProperty(string s, string Text, string ToolTip = "", bool includeChilds = false)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(s), new GUIContent(Text, ToolTip), includeChilds, null);
            serializedObject.ApplyModifiedProperties();
        }

        private void S(float space)
        {
            GUILayout.Space(space);
        }
        private void BV(bool box = true)
        {
            if (box) GUILayout.BeginVertical("Box"); else GUILayout.BeginVertical();
        }
        private void EV()
        {
            GUILayout.EndVertical();
        }

        private bool B(string txt)
        {
            return GUILayout.Button(txt);
        }
    }
}