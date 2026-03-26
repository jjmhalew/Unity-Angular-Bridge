using System;

namespace UnityAngularBridge
{
    /// <summary>
    /// Optional attribute to enrich [DllImport("__Internal")] methods with metadata for TypeScript generation.
    /// Use alongside [DllImport("__Internal")] to control how the method is exported to Angular.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class JSLibExportAttribute : Attribute
    {
        /// <summary>
        /// When true, the string parameter is split by "|" and exposed as string[] in TypeScript.
        /// </summary>
        public bool IsStringArray { get; set; }

        /// <summary>
        /// Category for organizing methods in generated TypeScript (e.g., "Scene", "Data").
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Override documentation for the generated TypeScript. If empty, XML doc comments are used.
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        /// <summary>
        /// When true, marks this method as a callback registration point.
        /// Angular can invoke the registered callback to send data to Unity.
        /// </summary>
        public bool IsCallbackRegistration { get; set; }
    }
}
