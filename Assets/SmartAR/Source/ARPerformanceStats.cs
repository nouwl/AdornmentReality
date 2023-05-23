using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SmartAR
{
    [AddComponentMenu("Matej Vanco/Smart AR/AR Performance Stats")]
    [DisallowMultipleComponent]
    /// <summary>
    /// Main performance stats in realtime (debugging) written by Matej Vanco 2020, updated in April 2022.
    /// Custom editor interface included below
    /// </summary>
    public class ARPerformanceStats : MonoBehaviour
    {
        #region VARIABLES_Performance Stats & Profilling [macro: devStats]

        /// <summary>
        /// Developer Statistics & Profilling general settings [class]
        /// </summary>
        [Serializable]
        public class Developer_Stats
        {
            public Vector2 devStats_ScreenLocation = new Vector2(50, 50); //---left-top
            public int devStats_FontSize = 15;

            public bool devStats_UsePerformanceProfilling = true;

            public Color devStats_GUIColor = Color.white;

            public bool devStats_ShowGUI = true;

            public bool devStats_ShowFPS = true;
            public bool devStats_ShowMemoryStats = true;
            public bool devStats_ShowPCStats = true;

            public const float devStats_memorydivider = 1048576.0f; // 1024^2

            public float devStats_fps;
            public float devStats_fpsNew; //New frame rate after the previous frame
            public float devStats_fpsMS; //1000ms = 1s, optimal refresh rate is 6-26 ms

            public float devStats_memTotal; //Total memory stored for current app
            public float devStats_memAlloc; //Allocated memory for current app
            public float devStats_memMono; //Allocated memory for current objects using Monobehaviour
        }
        public Developer_Stats DeveloperStats;

        #endregion

        private void Awake()
        {
            if (DeveloperStats.devStats_UsePerformanceProfilling)
                StartCoroutine(FrameBuffer());
        }       

        #region SYSTEM_Performance Monitor 'n Stats

        private IEnumerator FrameBuffer()
        {
            //----Calculation for 'how long does it take to calculate next frame in milliseconds'
            while (true)
            {
                var previousUpdateTime = Time.unscaledTime;
                var previousUpdateFrames = Time.frameCount;
                yield return new WaitForEndOfFrame();
                var timeElapsed = Time.unscaledTime - previousUpdateTime;
                var framesChanged = Time.frameCount - previousUpdateFrames;
                DeveloperStats.devStats_fpsNew = (framesChanged / timeElapsed);
            }
        }

        /// <summary>
        /// Show or Hide performance statistics GUI
        /// </summary>
        public void ShowHideGUI()
        {
            DeveloperStats.devStats_ShowGUI = !DeveloperStats.devStats_ShowGUI;
        }

        private void OnGUI()
        {
            if (!DeveloperStats.devStats_ShowGUI)
                return;
          
            GUI.color = DeveloperStats.devStats_GUIColor;
            GUISkin sk = GUI.skin;
            sk.label.fontSize = DeveloperStats.devStats_FontSize;
            GUI.skin = sk;

            GUILayout.BeginArea(new Rect(DeveloperStats.devStats_ScreenLocation.x, DeveloperStats.devStats_ScreenLocation.y, 700, 800));
            if (DeveloperStats.devStats_ShowFPS)
            {
                DeveloperStats.devStats_fps = (1.0f / Time.deltaTime);
                DeveloperStats.devStats_fpsMS = (1000.0f / DeveloperStats.devStats_fpsNew);

                GUILayout.Label("FPS: " + DeveloperStats.devStats_fps.ToString("F1") + $"[{DeveloperStats.devStats_fpsMS.ToString("F1")} ms]");
            }
            GUILayout.Space(8);

            if (DeveloperStats.devStats_ShowMemoryStats)
            {
                DeveloperStats.devStats_memTotal = Profiler.GetTotalReservedMemoryLong() / Developer_Stats.devStats_memorydivider;
                DeveloperStats.devStats_memAlloc = Profiler.GetTotalAllocatedMemoryLong() / Developer_Stats.devStats_memorydivider;
                DeveloperStats.devStats_memMono = GC.GetTotalMemory(false) / Developer_Stats.devStats_memorydivider;

                GUILayout.Label($"Memory Total: {DeveloperStats.devStats_memTotal.ToString("F1")} MB");
                GUILayout.Label($"Memory Allocation: {DeveloperStats.devStats_memAlloc.ToString("F1")} MB");
                GUILayout.Label($"Mono Objects Allocation: {DeveloperStats.devStats_memMono.ToString("F1")} MB");
            }

            GUILayout.Space(8);

            if (DeveloperStats.devStats_ShowPCStats)
            {
                GUILayout.Label($"OS: {SystemInfo.operatingSystem}");
                GUILayout.Label($"CPU: {SystemInfo.processorType} [{SystemInfo.processorCount.ToString()} cores]");
                GUILayout.Label($"TOTAL RAM: {SystemInfo.systemMemorySize}");
                GUILayout.Label($"GPU: {SystemInfo.graphicsDeviceName} [VRAM {SystemInfo.graphicsMemorySize} MB]");
                GUILayout.Label($"Screen: {Screen.currentResolution.width.ToString()}x{Screen.currentResolution.height.ToString()} {Screen.currentResolution.refreshRate.ToString()} Hz");
            }
            GUILayout.EndArea();
        }

        #endregion
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ARPerformanceStats))]
    [CanEditMultipleObjects]
    //---Dev Manager editor interface by Matej Vanco 2020
    public class ARPerformanceStats_Editor : Editor
    {
        private ARPerformanceStats devmanag;

        private GUIStyle guiStyleForTitle;

        private void OnEnable()
        {
            devmanag = (ARPerformanceStats)target;
            guiStyleForTitle = new GUIStyle();
            guiStyleForTitle.fontSize = 16;
            guiStyleForTitle.normal.textColor = Color.white;
        }

        public override void OnInspectorGUI()
        {

            EditorGUILayout.HelpBox("Use AR Performance Stats script in case you would like to monitor the app performance at runtime", MessageType.None);

            BV();
            DrawStatsProp("devStats_UsePerformanceProfilling", "Use Performance Profilling", "Enable fps calculation, information flow etc...");
            if (devmanag.DeveloperStats.devStats_UsePerformanceProfilling)
            {
                EditorGUI.indentLevel++;
                DrawStatsProp("devStats_ScreenLocation", "Screen Location", "Location of the performance stats. Measuring from left-top");
                DrawStatsProp("devStats_FontSize", "Font Size");
                S(3);
                DrawStatsProp("devStats_GUIColor", "GUI Color");
                S(3);
                DrawStatsProp("devStats_ShowGUI", "Show GUI");
                S(3);
                DrawStatsProp("devStats_ShowFPS", "Show FPS");
                DrawStatsProp("devStats_ShowMemoryStats", "Show Memory Stats", "Memory allocation types");
                DrawStatsProp("devStats_ShowPCStats", "Show Advanced PC Stats","OS, PCName, Total RAM etc");
                EditorGUI.indentLevel--;
            }
            EV();

            S(5);

            serializedObject.Update();
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
        private void DrawStatsProp(string prop, string Text, string Tooltip = "", bool childs = false)
        {
            SerializedProperty sz = serializedObject.FindProperty("DeveloperStats");

            if (sz.FindPropertyRelative(prop) == null)
            {
                Debug.Log("Can't find " + prop);
                return;
            }
            EditorGUILayout.PropertyField(sz.FindPropertyRelative(prop), new GUIContent(Text, Tooltip), childs);
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
