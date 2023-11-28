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
private static extern void SendObjectsToWeb(string objectIds);

public void MyMethod()
{
  string myString = "value";
  #if PLATFORM_WEBGL && !UNITY_EDITOR // otherwise crash
    SendObjectsToWeb(myString);
  #endif
}
```
2. Recompile or run play to let `JSLibExport.cs` generate a file.
3. In a Angular component, you can subscribe to this method by importing `UnityJSLibExportedService`.
```ts
constructor(private unityJslibExportedService: UnityJSLibExportedService) {
  this.unityJslibExportedService.objects$.pipe().subscribe((value) => {
    console.log(value);
  });
}
```

## How to update
Recompile or click on 'Play' in Unity editor to trigger `JSLibExport.cs` class.  
This will automatically generate:
1. `BrowserInteractions.jslib` The bridge between calling C# methods from JavaScript
2. `unity-jslib-exported.service.ts` A strongly typed service to function-call from Angular to Unity.  
Both will be placed in the folder `Assets/Plugins`.  
 
**TODO:**
1. The .ts generation does not set documentation of methods in ts yet.
2. Support callbacks, see https://jmschrack.dev/posts/UnityWebGL/
3. The .ts should automatically be placed in a ClientApp folder (via npm package).
4. ~~Create a custom wrapping attribute to simplify usage (Not possible due to sealed attribute)~~  
  ~~- It does not seem possible to extend the `DLLImport`-attribute so `StringArray` could be a parameter instead in a custom wrapping attribute for simplicity reasons.~~  
  ~~- If so, also add parameter which says which category type/scene this belongs to (so all listeners can be organized)~~
```csharp
public class JSLibExportAttribute : DllImportAttribute
{
  public bool IsStringArray { get; set; }
  public string Category { get; set; }

  public JSLibExportAttribute(bool isStringArray = false, string category = "") {
    IsStringArray = isStringArray;
    Category = category;
  }
}
```
To then be used as
```csharp
[JSLibExportAttribute(IsStringArray, Category = "Core")]
private static extern void SendObjectsToWeb(string objectIds);
```
