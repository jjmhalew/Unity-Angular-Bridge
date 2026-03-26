# Unity-Angular-Bridge

A simplified, type-safe bridge for bidirectional communication between Unity WebGL and Angular.

## Features

- **Angular → Unity**: Call Unity methods from Angular via auto-generated `UnityClient.ts`
- **Unity → Angular**: Receive Unity events in Angular via auto-generated signals (no RxJS required)
- **Callback Support**: Request-response and event registration patterns between C# and JavaScript
- **TSDoc Generation**: C# XML documentation automatically appears in generated TypeScript
- **Custom Attributes**: `[AngularExposed]` and `[JSLibExport]` for clean, declarative setup
- **Configurable Output**: Control where generated files are placed via Editor settings

## Requirements

- Unity 2021.2+ (for `makeDynCall` callback support)
- Angular 16+ (for signals support)

## Quick Start

### Unity Setup

1. Copy all `.cs` files from this repository into your Unity project (e.g., `Assets/Scripts/UnityAngularBridge/`).

2. **Configure the output paths** (optional):
   `Tools > UnityAngularBridge > Settings`
   Set where `UnityClient.ts` and `unity-jslib-exported.service.ts` are generated.
   By default, `UnityClient.ts` goes to your Documents folder.

3. **Enable callback support** (if using callbacks):
   `Tools > UnityAngularBridge > Enable Callback Support`
   This sets the required Emscripten arg (`-s ALLOW_TABLE_GROWTH`).

4. Recompile or click Play in Unity Editor to regenerate all TypeScript files.

### Angular Setup

1. Copy the generated TypeScript files into your Angular project (e.g., `src/app/generated/`).

2. Register `UnityClient` in your app config:
   ```typescript
   import { UnityClient } from './generated/unity-client';

   export const appConfig: ApplicationConfig = {
     providers: [UnityClient],
   };
   ```

3. Inject `UnityJSLibExportedService` wherever you need Unity events:
   ```typescript
   import { UnityJSLibExportedService } from './generated/unity-jslib-exported.service';

   const jsLib = inject(UnityJSLibExportedService);
   // Access signals directly
   const selectedObject = jsLib.sendSelectedObject; // Signal<string | null>
   ```

---

## Calling Unity from Angular

### How to use

Mark Unity methods with `[AngularExposed]`:

```csharp
/// <summary>
/// Load an object by its ID. Called from Angular.
/// </summary>
[AngularExposed(gameObjectName: "SceneManager")]
public void LoadObject(string objectId)
{
    // Your logic here
}
```

This generates `UnityClient.ts` with typed methods and TSDoc:

```typescript
export class UnityClient {
  /** Load an object by its ID. Called from Angular. */
  public sceneManager_LoadObject(unityInstance: IUnityInstance, objectId: string): void {
    unityInstance?.SendMessage("SceneManager", "LoadObject", objectId);
  }
}
```

### Parameters

- Only `string` and `number` parameter types are supported (Unity WebGL limitation)
- Maximum 1 parameter per method
- The `gameObjectName` identifies which Unity GameObject receives the `SendMessage` call
- Optional `Documentation` property overrides the TSDoc output

**Note**: Generation happens at compile time / Play mode, not at runtime. A default GameObject name is used unless overridden in `[AngularExposed]`.

### How to update

Recompile or click on 'Play' in Unity editor to trigger `AngularExposedExport.cs`.
This will automatically generate `UnityClient.ts` to the configured output path.

---

## Subscribing to Unity Events from Angular

### How to use

Declare `[DllImport("__Internal")]` methods with the optional `[JSLibExport]` attribute:

```csharp
/// <summary>
/// Sends the selected object ID to Angular.
/// </summary>
[DllImport("__Internal")]
[JSLibExport(Category = "Selection")]
private static extern void SendSelectedObject(string objectId);

/// <summary>
/// Sends a pipe-separated list as a string array.
/// </summary>
[DllImport("__Internal")]
[JSLibExport(IsStringArray = true, Category = "Objects")]
private static extern void SendObjectsList(string objectIds);

/// <summary>
/// Notifies Angular (no data, event-only).
/// </summary>
[DllImport("__Internal")]
[JSLibExport(Category = "Lifecycle")]
private static extern void SendSceneReady();
```

Call from Unity:

```csharp
#if PLATFORM_WEBGL && !UNITY_EDITOR
    SendSelectedObject(objectId);
    SendObjectsList(string.Join("|", objectIds));
    SendSceneReady();
#endif
```

This generates an Angular service with **pure signals** (no RxJS):

```typescript
@Injectable({ providedIn: "root" })
export class UnityJSLibExportedService {
  /** Sends the selected object ID to Angular. */
  readonly sendSelectedObject: Signal<string | null>;

  /** Sends a pipe-separated list as a string array. */
  readonly sendObjectsList: Signal<string[]>;

  /** Notifies Angular (no data, event-only). Increments on each event. */
  readonly sendSceneReady: Signal<number>;
}
```

### `[JSLibExport]` Attribute

| Property | Type | Default | Description |
|---|---|---|---|
| `IsStringArray` | `bool` | `false` | Split pipe-delimited string into `string[]` |
| `Category` | `string` | `""` | Organize methods (for documentation) |
| `Documentation` | `string` | `""` | Override TSDoc (falls back to XML docs) |
| `IsCallbackRegistration` | `bool` | `false` | Mark as callback registration point |

### How to update

Recompile or click on 'Play' in Unity editor to trigger `JSLibExport.cs`.
This will automatically generate:
1. `BrowserInteractions.jslib` — placed in `Assets/Plugins/`
2. `unity-jslib-exported.service.ts` — placed at the configured output path

---

## Callback Support

Based on [jmschrack.dev/posts/UnityWebGL](https://jmschrack.dev/posts/UnityWebGL/).

### Request-Response (C# → JS → C#)

Unity sends a request to Angular with a callback. Angular processes and responds:

**Unity (C#):**

```csharp
[DllImport("__Internal")]
[JSLibExport(Category = "Data")]
private static extern void RequestDataFromWeb(string query, Action<string> onResult);

[MonoPInvokeCallback(typeof(Action<string>))]
private static void OnDataReceived(string data)
{
    Debug.Log($"Received: {data}");
}

// Usage:
#if PLATFORM_WEBGL && !UNITY_EDITOR
    RequestDataFromWeb("my-query", OnDataReceived);
#endif
```

**Angular (TypeScript):**

```typescript
const jsLib = inject(UnityJSLibExportedService);

// Register a handler that responds to Unity's requests
jsLib.registerRequestDataFromWebHandler((query, respond) => {
    const result = processQuery(query);
    respond(result); // Sends result back to C# callback
});
```

### Event Registration (JS → C#)

Unity registers a callback that Angular can invoke later:

**Unity (C#):**

```csharp
[DllImport("__Internal")]
[JSLibExport(IsCallbackRegistration = true, Category = "Navigation")]
private static extern void RegisterOnNavigationChanged(Action<string> handler);

[MonoPInvokeCallback(typeof(Action<string>))]
private static void OnNavigationChanged(string route)
{
    Debug.Log($"Navigation: {route}");
}

// Register once on start:
#if PLATFORM_WEBGL && !UNITY_EDITOR
    RegisterOnNavigationChanged(OnNavigationChanged);
#endif
```

**Angular (TypeScript):**

```typescript
const jsLib = inject(UnityJSLibExportedService);

// Later, notify Unity of a navigation change:
jsLib.notifyOnNavigationChanged("/new-route");
```

### Important Notes

- Callback methods must be `static` and marked with `[MonoPInvokeCallback]`
- For registration callbacks, enable Emscripten support via `Tools > UnityAngularBridge > Enable Callback Support`
- Callbacks support `Action` (void) and `Action<string>` (string parameter)

---

## Configuration

### Output Paths

Open `Tools > UnityAngularBridge > Settings` to configure:

| Setting | Default | Description |
|---|---|---|
| UnityClient.ts path | MyDocuments | Where the Angular-to-Unity client is generated |
| Service .ts path | Assets/Plugins | Where the Unity-to-Angular service is generated |

Paths can be absolute or relative to the Unity project folder.

### TSDoc / XML Documentation

Generated TypeScript includes JSDoc comments from either:
1. **C# XML documentation comments** (`/// <summary>`) — requires XML docs enabled in Unity
2. **Attribute `Documentation` property** (fallback/override)

---

## Migration from v1

If upgrading from the previous RxJS-based version:

1. **Replace `[StringArrayAttribute]` with `[JSLibExport(IsStringArray = true)]`**
   The old attribute still works but is deprecated.

2. **Update Angular service consumption**:
   ```typescript
   // Before (RxJS):
   this.jsLibService.sendSelectedObject$.pipe().subscribe(value => { ... });

   // After (Signals):
   effect(() => {
     const value = this.jsLibService.sendSelectedObject();
     // React to changes
   });
   ```

3. **Remove `toSignal()` wrappers** — the service now exposes signals directly.

4. **Remove `rxjs` imports** from code that only used the bridge service.

---

## Example

See the [example/](example/) directory for a complete working example with:
- Unity project with all communication patterns
- Angular app with signals and mock Unity instance
- Mock Unity instance for development without WebGL builds

---

## TODO

- [ ] Export generated .ts files as an npm package
- [ ] Support multiple parameters per method
