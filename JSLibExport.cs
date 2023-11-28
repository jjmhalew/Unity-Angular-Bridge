#nullable enable
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Assets.UnityAngularBridge.SwaggerAttribute.Models;
using UnityEditor;
using UnityEngine;

namespace Assets.UnityAngularBridge.SwaggerAttribute
{
#if UNITY_EDITOR
    /// <summary>
    /// Export DllImportAttribute Methods to TypeScript as some type of SwaggerClient to Plugins folder.
    /// An example output is in the same directory as this file, referring to unity-jslib-exported.service.ts
    /// .
    /// NOTE: this method is from unity webinteractions lib, meaning it is JavaScript.
    /// In Unity the method is set to the window object, which is why we first create a jslib containing that method.
    /// See: https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
    /// .
    /// Do not put a if PLATFORM_WEBGL && !UNITY_EDITOR check around a method which should be included,
    /// otherwise it will not be recognized by JSLibExport logic.
    /// .
    /// TODO: export JSLibClient file to frontend by placing this in a NPM package.
    /// </summary>
    [InitializeOnLoad]
    public class JSLibExport : MonoBehaviour
    {
        private static readonly string _jsLibFileName = "BrowserInteractions.jslib";
        private static readonly string _jsLibClientFileName = "unity-jslib-exported.service.ts";

        /// <summary>
        /// The tabstring to use, since its for TypeScript it will be 2 spaces.
        /// </summary>
        private static readonly string _tabString = "  ";

        private static readonly List<JSLibVariable> _jSLibVariables = new();

        static JSLibExport()
        {
            GenerateJSLib();
            GenerateJSLibClient();
        }

        #region GenerateJSLib

        /// <summary>
        /// Generates JSLib File.
        /// </summary>
        private static void GenerateJSLib()
        {
            // Lines to write to file
            using IndentedTextWriter writer = new(new StreamWriter(Path.Combine(GetPluginsPath(), _jsLibFileName)) { AutoFlush = true }, _tabString);
            AddJsLibMergeIntoLine(writer);
            AddJSLibMethodLines(writer);
            AddJsLibClosingBracketsLine(writer);
        }

        /// <summary>
        /// Adds "mergeInto(LibraryManager.library, {".
        /// </summary>
        private static void AddJsLibMergeIntoLine(IndentedTextWriter writer)
        {
            writer.WriteLine("mergeInto(LibraryManager.library, {");
            writer.Indent++;
        }

        /// <summary>
        /// Adds all JavaScript method lines.
        /// </summary>
        /// TODO: multi parameter support?
        private static void AddJSLibMethodLines(IndentedTextWriter writer)
        {
            // Get Assembly
            Assembly assembly = Assembly.GetExecutingAssembly();
            // Get Classes Types
            IEnumerable<Type>? publicClasses = assembly.GetExportedTypes().Where(p => p.IsClass);
            foreach (Type? type in publicClasses)
            {
                // Get Class Methods with DllImportAttribute
                IEnumerable<MethodInfo>? methodInfos = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)
                            .Where(m => m.GetCustomAttributes(typeof(DllImportAttribute), false).Length > 0);
                foreach (MethodInfo? methodInfo in methodInfos)
                {
                    // Create method data for JsLibExportService generation
                    JSLibVariable jslibVariable = new();

                    // Get methodName
                    string methodName = methodInfo.Name;
                    jslibVariable.MethodName = methodName;

                    // Get first parameter type and name if there is one
                    ////string parameterType = string.Empty;
                    string parameterName = string.Empty;
                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    if (parameters.Length > 1)
                    {
                        throw new InvalidOperationException($"Method {methodName} is only allowed to have 1 argument");
                    }
                    if (parameters.Length == 1)
                    {
                        // parameterType = ParameterTypeToTypescriptType(parameters[0].ParameterType);
                        parameterName = parameters[0].Name;
                        jslibVariable.ParameterName = parameterName;
                        jslibVariable.ReturnType = ReturnType.String;
                        jslibVariable.DefaultValue = "null";

                        // Get StringArrayAttribute if any
                        IEnumerable<MethodInfo>? methodInfosStringArrayAttribute = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)
                            .Where(m => m.GetCustomAttributes(typeof(StringArrayAttribute), false).Length > 0);
                        foreach (MethodInfo? methodInfoStringArrayAttribute in methodInfosStringArrayAttribute)
                        {
                            jslibVariable.ReturnType = ReturnType.StringArray;
                            jslibVariable.DefaultValue = "[]";
                        }
                    }
                    else
                    {
                        jslibVariable.ReturnType = ReturnType.Void;
                        jslibVariable.DefaultValue = string.Empty;
                    }

                    // Add JsLibExportService variable data
                    _jSLibVariables.Add(jslibVariable);

                    AddJSLibMethodLines(writer, methodName, parameterName);
                }
            }
        }

        /// <summary>
        ///     [objectMethodName]: function ([ParameterName1], [ParameterName2]) {
        ///       window.[objectMethodName]FromUnity(UTF8ToString([ParameterName1]));
        ///     },.
        /// </summary>
        /// <param name="methodName">Method name.</param>
        /// <param name="parameterName">Parameter name.</param>
        /// TODO: add support for multiple params?
        private static void AddJSLibMethodLines(IndentedTextWriter writer, string methodName, string? parameterName)
        {
            // If no parameters
            if (string.IsNullOrEmpty(parameterName))
            {
                writer.WriteLine($"{methodName}: function ()" + " {");
                writer.Indent++;
                writer.WriteLine($"window.{FirstCharToLowerCase(methodName)}FromUnity();");
                writer.Indent--;
                writer.WriteLine("},");
                writer.WriteLine();
            }
            else // Has 1 parameter
            {
                // Add Accessibility modifier and MethodName and first parameter
                writer.WriteLine($"{methodName}: function ({parameterName}, size)" + " {");
                writer.Indent++;
                writer.WriteLine($"window.{FirstCharToLowerCase(methodName)}FromUnity(UTF8ToString({parameterName}));");
                writer.Indent--;
                writer.WriteLine("},");
                writer.WriteLine();
            }
        }

        /// <summary>
        /// Adds end of file "});".
        /// </summary>
        private static void AddJsLibClosingBracketsLine(IndentedTextWriter writer)
        {
            writer.Indent--;
            writer.WriteLine("});");
        }
        #endregion

        #region GenerateJSLibClient
        private static void GenerateJSLibClient()
        {
            // Lines to write to file
            using IndentedTextWriter writer = new(new StreamWriter(Path.Combine(GetPluginsPath(), _jsLibClientFileName)) { AutoFlush = true }, _tabString);
            AddAutoGeneratedFileCommentLines(writer);
            AddImportLines(writer);
            AddGlobalSubjectVariablesLines(writer);
            AddCommentsAndInjectableAndClassLines(writer);
            AddLocalSubjectVariablesLines(writer);
            AddConstructorLines(writer);
            AddSetupUnityListenersFunctionLines(writer);
            AddUnityListenersFunctionLines(writer);
            AddClassClosingLines(writer);
        }

        /// <summary>
        /// Create autgenerated Text including eslint-disable text.
        /// //----------------------
        /// // <auto-generated>
        /// //    Generated using JSLibExport.cs by Jim Halewijn in UnityAngularBridge project.
        /// // </auto-generated>
        /// //----------------------
        /// //
        /// /* eslint-disable */
        /// //.
        /// </summary>
        private static void AddAutoGeneratedFileCommentLines(IndentedTextWriter writer)
        {
            writer.WriteLine("//----------------------");
            writer.WriteLine("// <auto-generated>");
            writer.WriteLine("//    Generated using JSLibExport.cs by Jim Halewijn in UnityAngularBridge project.");
            writer.WriteLine("// </auto-generated>");
            writer.WriteLine("//----------------------");
            writer.WriteLine();
            writer.WriteLine("/* eslint-disable */");
            writer.WriteLine();
        }

        /// <summary>
        /// import { Injectable } from '@angular/core';
        /// import { BehaviorSubject, Observable, Subject } from "rxjs";
        /// .
        /// </summary>
        private static void AddImportLines(IndentedTextWriter writer)
        {
            writer.WriteLine("import { Injectable } from \"@angular/core\";");
            writer.WriteLine("import { BehaviorSubject, Observable, Subject } from \"rxjs\";");
            writer.WriteLine();
        }

        /// <summary>
        /// // NOTE: These subjects are used as a more global scope, so we can access it in JavaScript function of Unity.
        /// List of: const mySubject: [Behavior?]Subject<T> = new [Behavior?]Subject<T>(optionalDefaultValue);
        /// </summary>
        private static void AddGlobalSubjectVariablesLines(IndentedTextWriter writer)
        {
            writer.WriteLine("// NOTE: These subjects are used as a more global scope, so we can access it in JavaScript function of Unity.");

            // Add for each jslib variable
            foreach(JSLibVariable jslibVariable in _jSLibVariables)
            {
                string? methodNameLowerCase = FirstCharToLowerCase(jslibVariable.MethodName);
                string? returnTypeLowerCase = ConvertReturnTypeEnumToString(jslibVariable.ReturnType);

                // string return type needs behaviorsubject as type
                string? behaviorSubjectText = jslibVariable.ReturnType != ReturnType.Void ? "Behavior" : string.Empty;

                string globalSubjectVariableLine = $"const {methodNameLowerCase}Subject: {behaviorSubjectText}Subject<{returnTypeLowerCase}> = new {behaviorSubjectText}Subject<{returnTypeLowerCase}>({jslibVariable.DefaultValue});";
                writer.WriteLine(globalSubjectVariableLine);
            }

            writer.WriteLine();
        }

        /// <summary>
        /// /**
        ///  * NOTE: these functions are from unity webinteractions lib, meaning it is JavaScript.
        ///  * See: https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
        ///  * In Unity the function is set to the window object, which is why we set it here with:
        ///  * window["myfunctionName"] = this.myfunctionName;
        ///  *
        ///  * 'this' does not work as it is JavaScript.
        ///  * As a workaround, myfunctionNameFromUnitySubject is set outside this class scope to access it and subscribe to these listeners.
        ///  *
        ///  * These names are from the jslib file in UnityAngularBridge repo '/Assets/Plugins/BrowserInteractions.jslib' file.
        ///  */
        /// @Injectable({
        ///   providedIn: "root",
        /// })
        /// export class UnityJSLibExportedService {.
        /// </summary>
        private static void AddCommentsAndInjectableAndClassLines(IndentedTextWriter writer)
        {
            writer.WriteLine("/**");
            writer.WriteLine(" * NOTE: these functions are from unity webinteractions lib, meaning it is JavaScript.");
            writer.WriteLine(" * See: https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html");
            writer.WriteLine(" * In Unity the function is set to the window object, which is why we set it here with:");
            writer.WriteLine(" * window[\"myfunctionName\"] = this.myfunctionName;");
            writer.WriteLine(" *");
            writer.WriteLine(" * 'this' does not work as it is JavaScript.");
            writer.WriteLine(" * As a workaround, myfunctionNameSubject is set outside this class scope to access it and subscribe to these listeners.");
            writer.WriteLine(" *");
            writer.WriteLine(" * These names are from the jslib file in UnityAngularBridge repo '/Assets/Plugins/BrowserInteractions.jslib' file.");
            writer.WriteLine(" */");
            writer.WriteLine("@Injectable({");
            writer.Indent++;
            writer.WriteLine("providedIn: \"root\",");
            writer.Indent--;
            writer.WriteLine("})");
            writer.WriteLine("export class UnityJSLibExportedService {");
            writer.Indent++;
        }

        /// <summary>
        /// List of: public [methodName]$: Observable<T> = [globalSubjectVariableName].asObservable();
        /// </summary>
        private static void AddLocalSubjectVariablesLines(IndentedTextWriter writer)
        {
            // Add for each jslib variable
            foreach (JSLibVariable jslibVariable in _jSLibVariables)
            {
                string? methodNameLowerCase = FirstCharToLowerCase(jslibVariable.MethodName);
                string? returnTypeLowerCase = ConvertReturnTypeEnumToString(jslibVariable.ReturnType);

                string globalSubjectVariableLine = $"public {methodNameLowerCase}$: Observable<{returnTypeLowerCase}> = {methodNameLowerCase}Subject.asObservable();";
                writer.WriteLine(globalSubjectVariableLine);
            }

            writer.WriteLine();
        }

        /// <summary>
        /// constructor() {
        ///   this.setupUnityListeners();
        /// }.
        /// </summary>
        private static void AddConstructorLines(IndentedTextWriter writer)
        {
            writer.WriteLine("constructor() {");
            writer.Indent++;
            writer.WriteLine("this.setupUnityListeners();");
            writer.Indent--;
            writer.WriteLine("}");
            writer.WriteLine();
        }

        /// <summary>
        /// private setupUnityListeners(): void {
        ///   List of: window["[MethodName]FromUnity\"] = this.[MethodName]FromUnity;";
        /// }.
        /// </summary>
        private static void AddSetupUnityListenersFunctionLines(IndentedTextWriter writer)
        {
            writer.WriteLine("private setupUnityListeners(): void {");
            writer.Indent++;

            // Add for each jslib variable
            foreach (JSLibVariable jslibVariable in _jSLibVariables)
            {
                string? methodNameCamelCase = FirstCharToLowerCase(jslibVariable.MethodName);

                writer.WriteLine($"window[\"{methodNameCamelCase}FromUnity\"] = this.{methodNameCamelCase}FromUnity;");
            }

            writer.Indent--;
            writer.WriteLine("}");
            writer.WriteLine();
        }

        /// <summary>
        /// List of:
        /// private [methodName]FromUnity([parameterName?]: [string | string[]): void {
        ///   const partKeys = [parameterName].split("|"); .<-- optional line depending on if has parameter
        ///   [methodName]Subject.next([parameterName?]);
        /// }.
        /// .
        /// </summary>
        private static void AddUnityListenersFunctionLines(IndentedTextWriter writer)
        {
            // Add for each jslib variable
            foreach ((JSLibVariable jslibVariable, int index, bool isLastElement) in _jSLibVariables.Select((value, i) => (value, i, i == _jSLibVariables.Count - 1)))
            {
                string? methodNameCamelCase = FirstCharToLowerCase(jslibVariable.MethodName);

                // Create window implementation lines
                bool hasParameter = !string.IsNullOrEmpty(jslibVariable.ParameterName);
                if (hasParameter && jslibVariable.ReturnType == ReturnType.StringArray)
                {
                    string? parameterNameCamelCase = FirstCharToLowerCase(jslibVariable.ParameterName);

                    writer.WriteLine($"private {methodNameCamelCase}FromUnity({parameterNameCamelCase}: string): void" + " {");
                    writer.Indent++;
                    writer.WriteLine($"const split = {parameterNameCamelCase}.split(\"|\");");
                    writer.WriteLine($"{methodNameCamelCase}Subject.next(split);");
                    writer.Indent--;
                    writer.WriteLine("}");
                }
                else if (hasParameter)
                {
                    string? parameterNameCamelCase = FirstCharToLowerCase(jslibVariable.ParameterName);

                    writer.WriteLine($"private {methodNameCamelCase}FromUnity({parameterNameCamelCase}: string): void" + " {");
                    writer.Indent++;
                    writer.WriteLine($"{methodNameCamelCase}Subject.next({parameterNameCamelCase});");
                    writer.Indent--;
                    writer.WriteLine("}");
                }
                else // has no parameter
                {
                    writer.WriteLine($"private {methodNameCamelCase}FromUnity(): void" + " {");
                    writer.Indent++;
                    writer.WriteLine($"{methodNameCamelCase}Subject.next();");
                    writer.Indent--;
                    writer.WriteLine("}");
                }

                // Write newline if there is another element
                if(!isLastElement)
                {
                    writer.WriteLine();
                }
            }
        }

        /// <summary>
        /// Line to close the class:
        /// }.
        /// </summary>
        private static void AddClassClosingLines(IndentedTextWriter writer)
        {
            writer.Indent--;
            writer.WriteLine("}");
        }

        /// <summary>
        /// Convert value of ReturnType to JS returnType string.
        /// </summary>
        /// <param name="returnType">Return type enum value.</param>
        /// <returns>Return type as JavaScript string.</returns>
        private static string ConvertReturnTypeEnumToString(ReturnType returnType)
        {
            return returnType switch
            {
                ReturnType.Void => "void",
                ReturnType.String => "string",
                ReturnType.StringArray => "string[]",
                _ => throw new InvalidOperationException($"ParameterType {returnType} is not supported." +
                                        $"Only Void, String and StringArray are allowed."),
            };
        }
        #endregion

        #region pathUtilities

        /// <summary>
        /// Get plugins path.
        /// </summary>
        /// <returns>Plugins path.</returns>
        private static string GetPluginsPath()
        {
            // Get the path of the Game data folder
            // Unity Editor: <path to project folder>/Assets
            // https://docs.unity3d.com/ScriptReference/Application-dataPath.html
            return Application.dataPath + "/Plugins";
        }
        #endregion

        #region stringUtilities
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
#endif
}
