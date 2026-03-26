#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityAngularBridge
{
    /// <summary>
    /// Editor window for configuring UnityAngularBridge output paths.
    /// Access via Tools > UnityAngularBridge > Settings.
    /// </summary>
    public class UnityAngularBridgeSettings : EditorWindow
    {
        private const string UnityClientPathKey = "UnityAngularBridge_UnityClientOutputPath";
        private const string JSLibServicePathKey = "UnityAngularBridge_JSLibServiceOutputPath";

        private string _unityClientOutputPath = string.Empty;
        private string _jsLibServiceOutputPath = string.Empty;

        [MenuItem("Tools/UnityAngularBridge/Settings")]
        public static void ShowWindow()
        {
            GetWindow<UnityAngularBridgeSettings>("UnityAngularBridge Settings");
        }

        private void OnEnable()
        {
            _unityClientOutputPath = EditorPrefs.GetString(UnityClientPathKey, string.Empty);
            _jsLibServiceOutputPath = EditorPrefs.GetString(JSLibServicePathKey, string.Empty);
        }

        private void OnGUI()
        {
            GUILayout.Label("Output Paths", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Configure where generated TypeScript files are placed.\n" +
                "Leave empty to use defaults.\n" +
                "Paths can be absolute or relative to the Unity project folder.",
                MessageType.Info);

            EditorGUILayout.Space();

            // UnityClient.ts output path
            EditorGUILayout.LabelField("UnityClient.ts Output Path");
            EditorGUILayout.LabelField("Default: MyDocuments folder", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            _unityClientOutputPath = EditorGUILayout.TextField(_unityClientOutputPath);
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select UnityClient.ts output folder", _unityClientOutputPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    _unityClientOutputPath = selected;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // JSLib service output path
            EditorGUILayout.LabelField("unity-jslib-exported.service.ts Output Path");
            EditorGUILayout.LabelField("Default: Assets/Plugins (alongside .jslib)", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            _jsLibServiceOutputPath = EditorGUILayout.TextField(_jsLibServiceOutputPath);
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select service output folder", _jsLibServiceOutputPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    _jsLibServiceOutputPath = selected;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Save Settings"))
            {
                EditorPrefs.SetString(UnityClientPathKey, _unityClientOutputPath);
                EditorPrefs.SetString(JSLibServicePathKey, _jsLibServiceOutputPath);
                EditorUtility.DisplayDialog("UnityAngularBridge", "Settings saved. Recompile or press Play to regenerate files.", "OK");
            }

            if (GUILayout.Button("Reset to Defaults"))
            {
                _unityClientOutputPath = string.Empty;
                _jsLibServiceOutputPath = string.Empty;
                EditorPrefs.DeleteKey(UnityClientPathKey);
                EditorPrefs.DeleteKey(JSLibServicePathKey);
            }
        }

        /// <summary>
        /// Gets the configured output path for UnityClient.ts.
        /// Returns MyDocuments if no custom path is set.
        /// </summary>
        public static string GetUnityClientOutputPath()
        {
            string custom = EditorPrefs.GetString(UnityClientPathKey, string.Empty);
            if (!string.IsNullOrEmpty(custom))
            {
                if (!Path.IsPathRooted(custom))
                {
                    custom = Path.GetFullPath(Path.Combine(Application.dataPath, "..", custom));
                }
                if (!Directory.Exists(custom))
                {
                    Directory.CreateDirectory(custom);
                }
                return custom;
            }
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        /// <summary>
        /// Gets the configured output path for unity-jslib-exported.service.ts.
        /// Returns Assets/Plugins if no custom path is set.
        /// </summary>
        public static string GetJSLibServiceOutputPath()
        {
            string custom = EditorPrefs.GetString(JSLibServicePathKey, string.Empty);
            if (!string.IsNullOrEmpty(custom))
            {
                if (!Path.IsPathRooted(custom))
                {
                    custom = Path.GetFullPath(Path.Combine(Application.dataPath, "..", custom));
                }
                if (!Directory.Exists(custom))
                {
                    Directory.CreateDirectory(custom);
                }
                return custom;
            }
            return GetPluginsPath();
        }

        private static string GetPluginsPath()
        {
            string path = Application.dataPath + "/Plugins";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }
}
#endif
