using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SmartAR
{
    [AddComponentMenu("Matej Vanco/Smart AR/AR Streaming")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ARManager))]
    /// <summary>
    /// Specific save-load system for AR App written by Matej Vanco 2020, updated in April 2022.
    /// Streaming transforms (position,rotation,scale) property only
    /// </summary>
    public class ARStreaming : MonoBehaviour
    {
        [HideInInspector] public string MainGeneratedPath;
        [HideInInspector] public ARManager MainArManager;

        public bool UsePersistentDataPath = true;
        [Space]
        [Tooltip("Set main root directory name")] public string RootFolderName = "Data";
        [Tooltip("Set main extension name without dot!")] public string MainExtension = "txt";
        [Tooltip("Set main file name without dot!")] public string DefaultFileName = "AREditorData";
        [System.Serializable]
        public class AdditionalMessages
        {
            public string Message_Saved = "AR Editor Saved";
            public string Message_Loaded = "AR Editor Loaded";
            public string Message_NothingToLoad = "Nothing To Load";
            public string Message_Removed = "AR Editor Data Removed";
            public string Message_Interrupted = "AR Editor Data Interrupted";
        }
        [Space]
        [Tooltip("Customize internal messages in specific cases...")]public AdditionalMessages additionalMessages;
        [HideInInspector] public Transform[] ObjectsToStream;

        private void CheckData()
        {
            string rootPath = (UsePersistentDataPath) ? Application.persistentDataPath : Application.dataPath;

            RootFolderName = RootFolderName.Replace(".", "").Replace("/","");
            MainExtension = MainExtension.Replace(".", "").Replace("/", "");
            DefaultFileName = DefaultFileName.Replace(".", "").Replace("/", "");

            rootPath += Path.DirectorySeparatorChar + RootFolderName;
            try
            {
                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);
            }
            catch(IOException e)
            {
                EDebug(e.Message);
                return;
            }
            rootPath += Path.DirectorySeparatorChar + DefaultFileName + "." + MainExtension;
            try
            {
                if (!File.Exists(rootPath))
                    File.Create(rootPath).Dispose();
            }
            catch (IOException e)
            {
                EDebug(e.Message);
                return;
            }

            MainGeneratedPath = rootPath;
        }

        private void EDebug(string s)
        {
            Debug.LogError("Streaming error: " + s);
        }

        /// <summary>
        /// Streaming system save specific objects
        /// </summary>
        public void AR_Save()
        {
            CheckData();

            ObjectsToStream = MainArManager.arEditor_SpecificObjects.ToArray();

            try
            {
                FileStream fs = new FileStream(MainGeneratedPath, FileMode.Open);
                StreamWriter wr = new StreamWriter(fs);
                wr.WriteLine(System.DateTime.Now.ToLongDateString());

                for (int i = 0; i < ObjectsToStream.Length; i++)
                {
                    Transform gm = ObjectsToStream[i];

                    Vector3 gP = gm.localPosition;
                    Quaternion gR = gm.localRotation;
                    Vector3 gS = gm.localScale;

                    wr.WriteLine($"{gP.x.ToString().Replace(",", ".")},{gP.y.ToString().Replace(",", ".")},{gP.z.ToString().Replace(",", ".")}/" +
                        $"{gR.x.ToString().Replace(",", ".")},{gR.y.ToString().Replace(",", ".")},{gR.z.ToString().Replace(",", ".")},{gR.w.ToString().Replace(",", ".")}/" +
                        $"{gS.x.ToString().Replace(",", ".")},{gS.y.ToString().Replace(",", ".")},{gS.z.ToString().Replace(",", ".")}/" +
                        $"{gm.gameObject.activeSelf}");
                }

                wr.Dispose();
                fs.Dispose();
            }
            catch(IOException e)
            {
                EDebug("INTERNAL [WHILE SAVING] ERROR: " + e.Message);
                MainArManager.arEditorInternal_DebugInfo(additionalMessages.Message_Interrupted + "\n" +e.Message);
                return;
            }
            MainArManager.arEditorInternal_DebugInfo(additionalMessages.Message_Saved);
        }

        /// <summary>
        /// Streaming system clear saved data - removes the file
        /// </summary>
        public void AR_ClearAllData()
        {
            try
            {
                if (File.Exists(MainGeneratedPath))
                    File.Delete(MainGeneratedPath);
            }
            catch (IOException e)
            {
                EDebug("INTERNAL [WHILE REMOVING DATA] ERROR: " + e.Message);
                MainArManager.arEditorInternal_DebugInfo(additionalMessages.Message_Interrupted + "\n" +e.Message);
                return;
            }
            MainArManager.arEditorInternal_DebugInfo(additionalMessages.Message_Removed);
        }

        /// <summary>
        /// Streaming system load specific objects
        /// </summary>
        public void AR_Load()
        {
            CheckData();

            ObjectsToStream = MainArManager.arEditor_SpecificObjects.ToArray();

            if (!File.Exists(MainGeneratedPath))
            {
                MainArManager.arEditorInternal_DebugInfo(additionalMessages.Message_NothingToLoad);
                return;
            }

            try
            {
                string[] allLines = File.ReadAllLines(MainGeneratedPath);
                if (allLines.Length <= 1)
                {
                    MainArManager.arEditorInternal_DebugInfo(additionalMessages.Message_NothingToLoad);
                    return;
                }
                for (int i = 1; i < allLines.Length; i++)
                {
                    string fLine = allLines[i];
                    string[] parts = fLine.Split('/');
                    if (parts.Length < 3)
                    {
                        Debug.Log("Object skipped " + i + "; Cause: Not enough data [" + parts.Length.ToString() + "]");
                        continue;
                    }

                    string[] pos = parts[0].Split(',');
                    string[] rot = parts[1].Split(',');
                    string[] sca = parts[2].Split(',');

                    Vector3 ppos = new Vector3(ReturnFloat(pos[0]), ReturnFloat(pos[1]), ReturnFloat(pos[2]));
                    Quaternion rrot = new Quaternion(ReturnFloat(rot[0]), ReturnFloat(rot[1]), ReturnFloat(rot[2]), ReturnFloat(rot[3]));
                    Vector3 ssca = new Vector3(ReturnFloat(sca[0]), ReturnFloat(sca[1]), ReturnFloat(sca[2]));
                    bool active = true;
                    if(parts.Length >= 4)
                    {
                        if (bool.TryParse(parts[3], out bool res))
                            active = res;
                    }

                    Transform g = ObjectsToStream[i - 1];
                    g.localPosition = ppos;
                    g.localRotation = rrot;
                    g.localScale = ssca;
                    g.gameObject.SetActive(active);
                }
            }
            catch (IOException e)
            {
                EDebug("INTERNAL [WHILE LOADING] ERROR: " + e.Message);
                MainArManager.arEditorInternal_DebugInfo(additionalMessages.Message_Interrupted + "\n" + e.Message);
                return;
            }
            MainArManager.arEditorInternal_DebugInfo(additionalMessages.Message_Loaded);
        }

        private float ReturnFloat(string inp)
        {
            float.TryParse(inp, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ff);
            return ff;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ARStreaming))]
    public class ARStreaming_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Use AR Streaming script in case you would like to save/load your edited scene at runtime", MessageType.None);
            this.DrawDefaultInspector();
        }
    }
#endif
}