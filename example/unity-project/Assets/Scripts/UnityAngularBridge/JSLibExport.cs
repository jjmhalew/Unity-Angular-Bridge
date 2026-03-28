#nullable enable
#if UNITY_EDITOR
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using UnityAngularBridge.Models;
using UnityEditor;
using UnityEngine;

namespace UnityAngularBridge
{
    /// <summary>
    /// Scans the assembly for [DllImport("__Internal")] methods and generates:
    /// 1. BrowserInteractions.jslib — JavaScript bridge functions for Unity WebGL/WebGPU
    /// 2. unity-jslib-exported.service.ts — Angular service with signals and callback handlers
    ///
    /// Supports:
    /// - String/void parameters → Angular signals
    /// - [JSLibExport(IsStringArray = true)] → string[] signals
    /// - Action/Action&lt;string&gt; callback parameters → request-response or registration patterns
    /// - XML doc comments → TSDoc in generated TypeScript
    /// - Configurable output paths via Tools &gt; UnityAngularBridge &gt; Settings
    /// </summary>
    [InitializeOnLoad]
    public class JSLibExport
    {
        private static readonly string _jsLibFileName = "BrowserInteractions.jslib";
        private static readonly string _jsLibClientFileName = "unity-jslib-exported.service.ts";
        private static readonly string _tabString = "  ";
        private static readonly List<JSLibVariable> _jSLibVariables = new();
        private static readonly Dictionary<string, string> _xmlDocs = new();

        static JSLibExport()
        {
            LoadXmlDocumentation();
            ScanMethods();
            GenerateJSLib();
            GenerateJSLibClient();
        }

        #region XmlDocumentation

        private static void LoadXmlDocumentation()
        {
            _xmlDocs.Clear();
            try
            {
                string xmlPath = Path.Combine(Application.dataPath, "..", "Library", "ScriptAssemblies", "Assembly-CSharp.xml");
                if (!File.Exists(xmlPath)) return;

                XDocument doc = XDocument.Load(xmlPath);
                foreach (var member in doc.Descendants("member"))
                {
                    string? name = member.Attribute("name")?.Value;
                    string? summary = member.Element("summary")?.Value;
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(summary))
                    {
                        string cleaned = summary.Trim().Replace("\r\n", " ").Replace("\n", " ");
                        while (cleaned.Contains("  "))
                            cleaned = cleaned.Replace("  ", " ");
                        _xmlDocs[name] = cleaned;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UnityAngularBridge] Could not load XML documentation: {e.Message}");
            }
        }

        private static string GetXmlDocumentation(Type type, MethodInfo methodInfo)
        {
            string paramTypes = string.Join(",",
                methodInfo.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name));
            string memberName = methodInfo.GetParameters().Length > 0
                ? $"M:{type.FullName}.{methodInfo.Name}({paramTypes})"
                : $"M:{type.FullName}.{methodInfo.Name}";

            return _xmlDocs.TryGetValue(memberName, out string? doc) ? doc : string.Empty;
        }

        #endregion

        #region Scanning

        private static void ScanMethods()
        {
            _jSLibVariables.Clear();
            Assembly assembly = Assembly.GetExecutingAssembly();
            IEnumerable<Type> publicClasses = assembly.GetExportedTypes().Where(p => p.IsClass);

            foreach (Type type in publicClasses)
            {
                IEnumerable<MethodInfo> methodInfos = type
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(m => m.GetCustomAttributes(typeof(DllImportAttribute), false).Length > 0);

                foreach (MethodInfo methodInfo in methodInfos)
                {
                    JSLibVariable variable = new()
                    {
                        MethodName = methodInfo.Name
                    };

                    // Read [JSLibExport] attribute if present
                    var jsLibExportAttr = methodInfo.GetCustomAttribute<JSLibExportAttribute>();

                    // Read documentation: attribute override > XML docs
                    string attrDoc = jsLibExportAttr?.Documentation ?? string.Empty;
                    variable.MethodDocumentation = !string.IsNullOrEmpty(attrDoc)
                        ? attrDoc
                        : GetXmlDocumentation(type, methodInfo);

                    variable.Category = jsLibExportAttr?.Category ?? string.Empty;

                    // Analyze parameters: classify each as data (string) or callback (Action/Action<string>)
                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    string dataParamName = string.Empty;
                    bool hasCallback = false;
                    bool callbackHasStringParam = false;

                    foreach (ParameterInfo param in parameters)
                    {
                        if (param.ParameterType == typeof(Action))
                        {
                            hasCallback = true;
                            callbackHasStringParam = false;
                        }
                        else if (param.ParameterType == typeof(Action<string>))
                        {
                            hasCallback = true;
                            callbackHasStringParam = true;
                        }
                        else if (param.ParameterType == typeof(string))
                        {
                            dataParamName = param.Name ?? "data";
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"[UnityAngularBridge] Parameter type {param.ParameterType} on {methodInfo.Name} is not supported. " +
                                "Supported types: string, Action, Action<string>.");
                        }
                    }

                    if (hasCallback)
                    {
                        variable.CallbackType = jsLibExportAttr?.IsCallbackRegistration == true
                            ? CallbackType.Registration
                            : CallbackType.RequestResponse;
                        variable.CallbackHasStringParam = callbackHasStringParam;
                        variable.ParameterName = dataParamName;
                        variable.ReturnType = ReturnType.Void;
                    }
                    else if (!string.IsNullOrEmpty(dataParamName))
                    {
                        variable.ParameterName = dataParamName;

                        bool isStringArray = jsLibExportAttr?.IsStringArray == true;

                        if (isStringArray)
                        {
                            variable.ReturnType = ReturnType.StringArray;
                            variable.DefaultValue = "[]";
                        }
                        else
                        {
                            variable.ReturnType = ReturnType.String;
                            variable.DefaultValue = "null";
                        }
                    }
                    else
                    {
                        variable.ReturnType = ReturnType.Void;
                        variable.DefaultValue = string.Empty;
                    }

                    _jSLibVariables.Add(variable);
                }
            }
        }

        #endregion

        #region GenerateJSLib

        private static void GenerateJSLib()
        {
            using IndentedTextWriter writer = new(
                new StreamWriter(Path.Combine(GetPluginsPath(), _jsLibFileName)) { AutoFlush = true },
                _tabString);

            writer.WriteLine("mergeInto(LibraryManager.library, {");
            writer.Indent++;

            foreach (JSLibVariable variable in _jSLibVariables)
            {
                WriteJSLibMethod(writer, variable);
            }

            writer.Indent--;
            writer.WriteLine("});");
        }

        private static void WriteJSLibMethod(IndentedTextWriter writer, JSLibVariable variable)
        {
            string methodName = variable.MethodName;
            string windowFnName = $"{FirstCharToLowerCase(methodName)}FromUnity";
            bool hasDataParam = !string.IsNullOrEmpty(variable.ParameterName);

            if (variable.CallbackType == CallbackType.RequestResponse)
            {
                // Request-response: optional data param + callback
                if (hasDataParam)
                {
                    writer.WriteLine($"{methodName}: function ({variable.ParameterName}Ptr, callbackPtr)" + " {");
                    writer.Indent++;
                    writer.WriteLine($"var {variable.ParameterName} = UTF8ToString({variable.ParameterName}Ptr);");
                }
                else
                {
                    writer.WriteLine($"{methodName}: function (callbackPtr)" + " {");
                    writer.Indent++;
                }

                if (variable.CallbackHasStringParam)
                {
                    string dataArg = hasDataParam ? variable.ParameterName + ", " : "";
                    writer.WriteLine($"window.{windowFnName}({dataArg}function (result)" + " {");
                    writer.Indent++;
                    writer.WriteLine("var bufferSize = lengthBytesUTF8(result) + 1;");
                    writer.WriteLine("var buffer = _malloc(bufferSize);");
                    writer.WriteLine("stringToUTF8(result, buffer, bufferSize);");
                    writer.WriteLine("{{{ makeDynCall('vi', 'callbackPtr') }}}(buffer);");
                    writer.WriteLine("_free(buffer);");
                    writer.Indent--;
                    writer.WriteLine("});");
                }
                else
                {
                    string dataArg = hasDataParam ? variable.ParameterName + ", " : "";
                    writer.WriteLine($"window.{windowFnName}({dataArg}function ()" + " {");
                    writer.Indent++;
                    writer.WriteLine("{{{ makeDynCall('v', 'callbackPtr') }}}();");
                    writer.Indent--;
                    writer.WriteLine("});");
                }

                writer.Indent--;
                writer.WriteLine("},");
                writer.WriteLine();
            }
            else if (variable.CallbackType == CallbackType.Registration)
            {
                // Registration: Unity passes a C# callback pointer, JS wraps it for Angular to call later
                writer.WriteLine($"{methodName}: function (callbackPtr)" + " {");
                writer.Indent++;

                if (variable.CallbackHasStringParam)
                {
                    writer.WriteLine($"window.{windowFnName}(function (data)" + " {");
                    writer.Indent++;
                    writer.WriteLine("var bufferSize = lengthBytesUTF8(data) + 1;");
                    writer.WriteLine("var buffer = _malloc(bufferSize);");
                    writer.WriteLine("stringToUTF8(data, buffer, bufferSize);");
                    writer.WriteLine("{{{ makeDynCall('vi', 'callbackPtr') }}}(buffer);");
                    writer.WriteLine("_free(buffer);");
                    writer.Indent--;
                    writer.WriteLine("});");
                }
                else
                {
                    writer.WriteLine($"window.{windowFnName}(function ()" + " {");
                    writer.Indent++;
                    writer.WriteLine("{{{ makeDynCall('v', 'callbackPtr') }}}();");
                    writer.Indent--;
                    writer.WriteLine("});");
                }

                writer.Indent--;
                writer.WriteLine("},");
                writer.WriteLine();
            }
            else if (hasDataParam)
            {
                // Regular method with string parameter
                writer.WriteLine($"{methodName}: function ({variable.ParameterName}, size)" + " {");
                writer.Indent++;
                writer.WriteLine($"window.{windowFnName}(UTF8ToString({variable.ParameterName}));");
                writer.Indent--;
                writer.WriteLine("},");
                writer.WriteLine();
            }
            else
            {
                // No parameter method
                writer.WriteLine($"{methodName}: function ()" + " {");
                writer.Indent++;
                writer.WriteLine($"window.{windowFnName}();");
                writer.Indent--;
                writer.WriteLine("},");
                writer.WriteLine();
            }
        }

        #endregion

        #region GenerateJSLibClient

        private static void GenerateJSLibClient()
        {
            string outputPath = UnityAngularBridgeSettings.GetJSLibServiceOutputPath();
            using IndentedTextWriter writer = new(
                new StreamWriter(Path.Combine(outputPath, _jsLibClientFileName)) { AutoFlush = true },
                _tabString);

            WriteAutoGeneratedHeader(writer);
            WriteImports(writer);
            WriteModuleScopeSignals(writer);
            WriteModuleScopeCallbackHolders(writer);
            WriteWindowCallbacks(writer);
            WriteServiceClass(writer);
        }

        private static void WriteAutoGeneratedHeader(IndentedTextWriter writer)
        {
            writer.WriteLine("//----------------------");
            writer.WriteLine("// <auto-generated>");
            writer.WriteLine("//    Generated using JSLibExport.cs in UnityAngularBridge project.");
            writer.WriteLine("// </auto-generated>");
            writer.WriteLine("//----------------------");
            writer.WriteLine();
            writer.WriteLine("/* eslint-disable */");
            writer.WriteLine();
        }

        private static void WriteImports(IndentedTextWriter writer)
        {
            writer.WriteLine("import { Injectable, signal, WritableSignal, Signal } from \"@angular/core\";");
            writer.WriteLine();
        }

        private static void WriteModuleScopeSignals(IndentedTextWriter writer)
        {
            var signalVars = _jSLibVariables.Where(v => v.CallbackType == CallbackType.None).ToList();
            if (!signalVars.Any()) return;

            writer.WriteLine("// Module-scope writable signals (accessed by window callbacks below).");
            foreach (JSLibVariable variable in signalVars)
            {
                string name = $"{FirstCharToLowerCase(variable.MethodName)}Signal";
                switch (variable.ReturnType)
                {
                    case ReturnType.Void:
                        writer.WriteLine($"const {name}: WritableSignal<number> = signal<number>(0);");
                        break;
                    case ReturnType.String:
                        writer.WriteLine($"const {name}: WritableSignal<string | null> = signal<string | null>({variable.DefaultValue}, {{ equal: () => false }});");
                        break;
                    case ReturnType.StringArray:
                        writer.WriteLine($"const {name}: WritableSignal<string[]> = signal<string[]>({variable.DefaultValue}, {{ equal: () => false }});");
                        break;
                }
            }
            writer.WriteLine();
        }

        private static void WriteModuleScopeCallbackHolders(IndentedTextWriter writer)
        {
            var callbackVars = _jSLibVariables.Where(v => v.CallbackType != CallbackType.None).ToList();
            if (!callbackVars.Any()) return;

            writer.WriteLine("// Module-scope callback holders.");
            foreach (JSLibVariable variable in callbackVars)
            {
                string name = FirstCharToLowerCase(variable.MethodName)!;
                bool hasData = !string.IsNullOrEmpty(variable.ParameterName);

                if (variable.CallbackType == CallbackType.RequestResponse)
                {
                    if (hasData && variable.CallbackHasStringParam)
                        writer.WriteLine($"let {name}Handler: ((query: string, respond: (result: string) => void) => void) | null = null;");
                    else if (hasData)
                        writer.WriteLine($"let {name}Handler: ((query: string, respond: () => void) => void) | null = null;");
                    else if (variable.CallbackHasStringParam)
                        writer.WriteLine($"let {name}Handler: ((respond: (result: string) => void) => void) | null = null;");
                    else
                        writer.WriteLine($"let {name}Handler: ((respond: () => void) => void) | null = null;");
                }
                else // Registration
                {
                    if (variable.CallbackHasStringParam)
                        writer.WriteLine($"let {name}Callback: ((data: string) => void) | null = null;");
                    else
                        writer.WriteLine($"let {name}Callback: (() => void) | null = null;");
                }
            }
            writer.WriteLine();
        }

        private static void WriteWindowCallbacks(IndentedTextWriter writer)
        {
            writer.WriteLine("// Register window callbacks invoked by Unity's jslib.");
            writer.WriteLine("/* eslint-disable @typescript-eslint/no-explicit-any */");

            foreach (JSLibVariable variable in _jSLibVariables)
            {
                string methodNameLower = FirstCharToLowerCase(variable.MethodName)!;
                string windowFnName = $"{methodNameLower}FromUnity";

                if (variable.CallbackType == CallbackType.RequestResponse)
                {
                    bool hasData = !string.IsNullOrEmpty(variable.ParameterName);
                    string paramNameLower = hasData ? FirstCharToLowerCase(variable.ParameterName)! : "";

                    if (hasData && variable.CallbackHasStringParam)
                    {
                        writer.WriteLine($"(window as any)[\"{windowFnName}\"] = ({paramNameLower}: string, respond: (result: string) => void): void => {{");
                        writer.Indent++;
                        writer.WriteLine($"{methodNameLower}Handler?.({paramNameLower}, respond);");
                    }
                    else if (hasData)
                    {
                        writer.WriteLine($"(window as any)[\"{windowFnName}\"] = ({paramNameLower}: string, respond: () => void): void => {{");
                        writer.Indent++;
                        writer.WriteLine($"{methodNameLower}Handler?.({paramNameLower}, respond);");
                    }
                    else if (variable.CallbackHasStringParam)
                    {
                        writer.WriteLine($"(window as any)[\"{windowFnName}\"] = (respond: (result: string) => void): void => {{");
                        writer.Indent++;
                        writer.WriteLine($"{methodNameLower}Handler?.(respond);");
                    }
                    else
                    {
                        writer.WriteLine($"(window as any)[\"{windowFnName}\"] = (respond: () => void): void => {{");
                        writer.Indent++;
                        writer.WriteLine($"{methodNameLower}Handler?.(respond);");
                    }
                    writer.Indent--;
                    writer.WriteLine("};");
                }
                else if (variable.CallbackType == CallbackType.Registration)
                {
                    if (variable.CallbackHasStringParam)
                    {
                        writer.WriteLine($"(window as any)[\"{windowFnName}\"] = (handler: (data: string) => void): void => {{");
                        writer.Indent++;
                        writer.WriteLine($"{methodNameLower}Callback = handler;");
                    }
                    else
                    {
                        writer.WriteLine($"(window as any)[\"{windowFnName}\"] = (handler: () => void): void => {{");
                        writer.Indent++;
                        writer.WriteLine($"{methodNameLower}Callback = handler;");
                    }
                    writer.Indent--;
                    writer.WriteLine("};");
                }
                else if (!string.IsNullOrEmpty(variable.ParameterName))
                {
                    string paramNameLower = FirstCharToLowerCase(variable.ParameterName)!;
                    if (variable.ReturnType == ReturnType.StringArray)
                    {
                        writer.WriteLine($"(window as any)[\"{windowFnName}\"] = ({paramNameLower}: string): void => {{");
                        writer.Indent++;
                        writer.WriteLine($"{methodNameLower}Signal.set({paramNameLower}.split(\"|\"));");
                    }
                    else
                    {
                        writer.WriteLine($"(window as any)[\"{windowFnName}\"] = ({paramNameLower}: string): void => {{");
                        writer.Indent++;
                        writer.WriteLine($"{methodNameLower}Signal.set({paramNameLower});");
                    }
                    writer.Indent--;
                    writer.WriteLine("};");
                }
                else
                {
                    writer.WriteLine($"(window as any)[\"{windowFnName}\"] = (): void => {{");
                    writer.Indent++;
                    writer.WriteLine($"{methodNameLower}Signal.update(v => v + 1);");
                    writer.Indent--;
                    writer.WriteLine("};");
                }
            }
            writer.WriteLine();
        }

        private static void WriteServiceClass(IndentedTextWriter writer)
        {
            writer.WriteLine("/**");
            writer.WriteLine(" * Auto-generated service for Unity \\u2192 Angular communication.");
            writer.WriteLine(" * Signals are updated when Unity calls the corresponding jslib functions.");
            writer.WriteLine(" * Register callback handlers to respond to Unity requests.");
            writer.WriteLine(" * See: https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html");
            writer.WriteLine(" */");
            writer.WriteLine("@Injectable({");
            writer.Indent++;
            writer.WriteLine("providedIn: \"root\",");
            writer.Indent--;
            writer.WriteLine("})");
            writer.WriteLine("export class UnityJSLibExportedService {");
            writer.Indent++;

            // Signal properties
            var signalVars = _jSLibVariables.Where(v => v.CallbackType == CallbackType.None).ToList();
            foreach (JSLibVariable variable in signalVars)
            {
                string name = FirstCharToLowerCase(variable.MethodName)!;
                string signalName = $"{name}Signal";

                if (!string.IsNullOrEmpty(variable.MethodDocumentation))
                {
                    writer.WriteLine($"/** {variable.MethodDocumentation} */");
                }

                switch (variable.ReturnType)
                {
                    case ReturnType.Void:
                        writer.WriteLine($"readonly {name}: Signal<number> = {signalName}.asReadonly();");
                        break;
                    case ReturnType.String:
                        writer.WriteLine($"readonly {name}: Signal<string | null> = {signalName}.asReadonly();");
                        break;
                    case ReturnType.StringArray:
                        writer.WriteLine($"readonly {name}: Signal<string[]> = {signalName}.asReadonly();");
                        break;
                }
            }

            // Callback methods
            var callbackVars = _jSLibVariables.Where(v => v.CallbackType != CallbackType.None).ToList();
            if (callbackVars.Any() && signalVars.Any())
            {
                writer.WriteLine();
            }

            foreach ((JSLibVariable variable, int index, bool isLast) in
                callbackVars.Select((v, i) => (v, i, i == callbackVars.Count - 1)))
            {
                string name = FirstCharToLowerCase(variable.MethodName)!;

                if (variable.CallbackType == CallbackType.RequestResponse)
                {
                    bool hasData = !string.IsNullOrEmpty(variable.ParameterName);
                    string doc = !string.IsNullOrEmpty(variable.MethodDocumentation)
                        ? variable.MethodDocumentation
                        : $"Register a handler for {variable.MethodName} requests from Unity.";
                    writer.WriteLine($"/** {doc} */");

                    if (hasData && variable.CallbackHasStringParam)
                        writer.WriteLine($"register{variable.MethodName}Handler(handler: (query: string, respond: (result: string) => void) => void): void {{");
                    else if (hasData)
                        writer.WriteLine($"register{variable.MethodName}Handler(handler: (query: string, respond: () => void) => void): void {{");
                    else if (variable.CallbackHasStringParam)
                        writer.WriteLine($"register{variable.MethodName}Handler(handler: (respond: (result: string) => void) => void): void {{");
                    else
                        writer.WriteLine($"register{variable.MethodName}Handler(handler: (respond: () => void) => void): void {{");

                    writer.Indent++;
                    writer.WriteLine($"{name}Handler = handler;");
                    writer.Indent--;
                    writer.WriteLine("}");
                }
                else // Registration
                {
                    string baseName = variable.MethodName.StartsWith("Register")
                        ? variable.MethodName.Substring("Register".Length)
                        : variable.MethodName;
                    string doc = !string.IsNullOrEmpty(variable.MethodDocumentation)
                        ? variable.MethodDocumentation
                        : $"Invoke the callback registered by Unity via {variable.MethodName}.";
                    writer.WriteLine($"/** {doc} */");

                    if (variable.CallbackHasStringParam)
                    {
                        writer.WriteLine($"notify{baseName}(data: string): void {{");
                        writer.Indent++;
                        writer.WriteLine($"{name}Callback?.(data);");
                    }
                    else
                    {
                        writer.WriteLine($"notify{baseName}(): void {{");
                        writer.Indent++;
                        writer.WriteLine($"{name}Callback?.();");
                    }
                    writer.Indent--;
                    writer.WriteLine("}");
                }

                if (!isLast) writer.WriteLine();
            }

            writer.Indent--;
            writer.WriteLine("}");
        }

        #endregion

        #region Utilities

        private static string GetPluginsPath()
        {
            string path = Application.dataPath + "/Plugins";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private static string? FirstCharToLowerCase(string? str)
        {
            if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
            {
                return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..];
            }
            return str;
        }

        #endregion
    }
}
#endif
