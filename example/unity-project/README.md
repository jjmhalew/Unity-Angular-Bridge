# Unity-Angular Bridge — Example Unity Project

This folder contains the **Unity side** of the example. It includes a
`SceneManager` MonoBehaviour that demonstrates both communication patterns
using the bridge attributes from the root of this repository.

## Setup

1. **Create a new Unity project** (Unity 2022.3 LTS or later recommended)
   — use the **3D (URP)** or **3D** template.

2. **Copy the bridge scripts** from the repository root into your project:
   ```
   AngularExposedAttribute.cs   → Assets/Scripts/UnityAngularBridge/
   AngularExposedExport.cs      → Assets/Scripts/UnityAngularBridge/
   JSLibExport.cs               → Assets/Scripts/UnityAngularBridge/
   JSLibVariable.cs             → Assets/Scripts/UnityAngularBridge/Models/
   StringArrayAttribute.cs      → Assets/Scripts/UnityAngularBridge/
   ```

3. **Copy the example scene script:**
   ```
   example/unity-project/Assets/Scripts/SceneManager.cs → Assets/Scripts/
   ```

4. **Create a scene** with a GameObject named **"SceneManager"** and attach
   the `SceneManager` script to it. Add a few 3D objects (Cube, Sphere, etc.)
   to visualize the scene.

5. **Compile** (or press Play) — the bridge generators will auto-create:
   - `~/Documents/UnityClient.ts`
   - `Assets/Plugins/BrowserInteractions.jslib`
   - `Assets/Plugins/unity-jslib-exported.service.ts`

## Building for WebGL

1. Go to **File → Build Settings → WebGL** and click **Switch Platform**.

2. Open **Player Settings → Publishing Settings** and set:
   - **Compression Format** → Disabled (simplest for local dev)

3. Click **Build** and choose an output folder.

4. Copy the build output into the Angular app's public folder:
   ```bash
   cp -r <unity-build-output>/* ../angular-unity-example/public/unity-build/
   ```

5. Start the Angular app — it will automatically detect and load the Unity
   WebGL build, showing the real Unity viewport alongside the Angular controls.

## What the SceneManager Script Does

| Direction | Attribute | Method | Description |
|-----------|-----------|--------|-------------|
| Angular → Unity | `[AngularExposed]` | `LoadObject(string)` | Select an object by ID |
| Angular → Unity | `[AngularExposed]` | `SetColor(string)` | Set color of selected object |
| Angular → Unity | `[AngularExposed]` | `ToggleVisibility()` | Toggle object visibility |
| Angular → Unity | `[AngularExposed]` | `ResetScene()` | Reset and list all objects |
| Unity → Angular | `[DllImport]` | `SendSelectedObject(string)` | Notify Angular of selection |
| Unity → Angular | `[DllImport]` | `SendSceneReady()` | Notify Angular scene is ready |
| Unity → Angular | `[DllImport] + [StringArray]` | `SendObjectsList(string)` | Send object list (pipe-delimited) |
