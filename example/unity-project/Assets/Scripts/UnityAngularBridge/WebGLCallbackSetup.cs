#if UNITY_EDITOR
using UnityEditor;

namespace UnityAngularBridge
{
    /// <summary>
    /// Editor utility to configure Emscripten args required for callback support.
    /// Unity WebGL/WebGPU builds need ALLOW_TABLE_GROWTH to support runtime callback registration.
    /// </summary>
    public static class WebGLCallbackSetup
    {
        private const string EmscriptenArgs = "-s ALLOW_TABLE_GROWTH";

        [MenuItem("Tools/UnityAngularBridge/Enable Callback Support")]
        public static void EnableCallbackSupport()
        {
            string current = PlayerSettings.WebGL.emscriptenArgs;
            if (!current.Contains(EmscriptenArgs))
            {
                PlayerSettings.WebGL.emscriptenArgs = string.IsNullOrEmpty(current)
                    ? EmscriptenArgs
                    : current + " " + EmscriptenArgs;
            }
            EditorUtility.DisplayDialog("UnityAngularBridge",
                "Callback support enabled.\nEmscripten args: " + PlayerSettings.WebGL.emscriptenArgs,
                "OK");
        }

        [MenuItem("Tools/UnityAngularBridge/Disable Callback Support")]
        public static void DisableCallbackSupport()
        {
            PlayerSettings.WebGL.emscriptenArgs = PlayerSettings.WebGL.emscriptenArgs
                .Replace(EmscriptenArgs, "")
                .Trim();
            EditorUtility.DisplayDialog("UnityAngularBridge",
                "Callback support disabled.\nEmscripten args: " + PlayerSettings.WebGL.emscriptenArgs,
                "OK");
        }
    }
}
#endif
