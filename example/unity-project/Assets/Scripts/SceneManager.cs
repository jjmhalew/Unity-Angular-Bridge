using Assets.UnityAngularBridge.SwaggerAttribute;
using System.Runtime.InteropServices;
using UnityAngularBridge.SwaggerAttribute;
using UnityEngine;

/// <summary>
/// Example MonoBehaviour that demonstrates both communication patterns
/// of the Unity-Angular-Bridge.
///
/// 1. Angular → Unity:  Methods marked with [AngularExposed] can be called
///    from Angular via the auto-generated UnityClient.ts.
/// 2. Unity → Angular:  Methods marked with [DllImport("__Internal")] push
///    data to Angular via the auto-generated UnityJSLibExportedService.
/// </summary>
public class SceneManager : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────────
    //  Unity → Angular  (JSLib / DllImport)
    //  These generate BrowserInteractions.jslib + unity-jslib-exported.service.ts
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends the currently selected object ID to Angular.
    /// </summary>
    [DllImport("__Internal")]
    private static extern void SendSelectedObject(string objectId);

    /// <summary>
    /// Notifies Angular that the scene has finished loading.
    /// </summary>
    [DllImport("__Internal")]
    private static extern void SendSceneReady();

    /// <summary>
    /// Sends a pipe-delimited list of object IDs to Angular (split into string[]).
    /// </summary>
    [DllImport("__Internal")]
    [StringArrayAttribute]
    private static extern void SendObjectsList(string objectIds);

    // ──────────────────────────────────────────────────────────────────
    //  Angular → Unity  (AngularExposed)
    //  These generate UnityClient.ts
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Load an object by its ID. Called from Angular.
    /// </summary>
    [AngularExposed(gameObjectName: "SceneManager")]
    public void LoadObject(string objectId)
    {
        Debug.Log($"[SceneManager] LoadObject({objectId})");

        // Example: find and highlight the object, then notify Angular
#if PLATFORM_WEBGL && !UNITY_EDITOR
        SendSelectedObject(objectId);
#endif
    }

    /// <summary>
    /// Set the color of the selected object. Called from Angular.
    /// </summary>
    [AngularExposed(gameObjectName: "SceneManager")]
    public void SetColor(string colorHex)
    {
        Debug.Log($"[SceneManager] SetColor({colorHex})");

        // Example: parse hex color and apply to selected object's material
        if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
        {
            // Apply to selected object...
        }
    }

    /// <summary>
    /// Toggle visibility of all objects. Called from Angular.
    /// </summary>
    [AngularExposed(gameObjectName: "SceneManager")]
    public void ToggleVisibility()
    {
        Debug.Log("[SceneManager] ToggleVisibility()");
    }

    /// <summary>
    /// Reset the scene to its initial state. Called from Angular.
    /// </summary>
    [AngularExposed(gameObjectName: "SceneManager")]
    public void ResetScene()
    {
        Debug.Log("[SceneManager] ResetScene()");

        // Example: gather all object IDs and send to Angular
        string[] objectIds = { "Cube-001", "Sphere-002", "Cylinder-003", "Plane-004" };

#if PLATFORM_WEBGL && !UNITY_EDITOR
        SendObjectsList(string.Join("|", objectIds));
        SendSceneReady();
#endif
    }

    private void Start()
    {
        // Notify Angular that the Unity scene is ready
#if PLATFORM_WEBGL && !UNITY_EDITOR
        SendSceneReady();
#endif
    }
}
