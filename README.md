# Unity-Angular-Bridge
A simplified way to communicate between Unity and Angular

# Calling Unity from Angular
TODO: Rework before committing to this repository - it currently has too many instabilities.

# Subscribing to Unity events from Angular with JsLib
## How to use
1. Use a `MonoBehaviour` class. Only `string` as `parameter` is accepted.
Since you can only send one string technically, there is an option to add the attribute `[StringArrayAttribute]` -> this will make the service split a string into an array by `"|"`.
```csharp
/// <summary>
/// My documentation.
/// </summary>
[DllImport("__Internal")]
private static extern void SendViewerIsLoadingToWeb(string loading);

public void MyMethod()
{
  string myString = "value";
  #if PLATFORM_WEBGL && !UNITY_EDITOR // otherwise crash
    SendViewerIsLoadingToWeb(myString);
  #endif
}
```
2. Run play to let `JSLibExport.cs` generate a file.
3. In a Angular component, you can subscribe to this method by importing `UnityJSLibExportedService`.
```ts
constructor(private unityJslibExportedService: UnityJSLibExportedService) {
  this.unityJslibExportedService.viewerIsLoading$.pipe().subscribe((value) => {
    console.log(value);
  });
}
```

## How to update
Just click on 'Play' in Unity editor to trigger `JSLibExport.cs` class.  
This will automatically generate:
1. `BrowserInterations.jslib` The bridge between calling C# methods from JavaScript
2. `unity-jslib-exported.service.ts` A strongly typed service to function-call from Angular to Unity.  
Both will be placed in the folder `Assets/Plugins`.  
 
**TODO:**
1. Currently the .jslib file generation always assumes there is a string parameter.
2. The .ts generation does not set documentation of methods in ts yet.
3. The .ts should automatically be placed in a ClientApp folder (via npm package).
4. Support callbacks
5. ~~Create a custom wrapping attribute to simplify usage (Not possible due to sealed attribute)~~  
  ~~- It does not seem possible to extend the `DLLImport`-attribute so `StringArray` could be a parameter instead in a custom wrapping attribute for simplicity reasons.~~  
  ~~- If so, also add parameter which says which editor type this is for (so all listeners can be organized)~~
