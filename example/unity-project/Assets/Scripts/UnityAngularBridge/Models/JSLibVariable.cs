namespace Assets.UnityAngularBridge.SwaggerAttribute.Models
{
    /// <summary>
    /// Used in <seealso cref="JSLibExport"/> to set Unity methods to JS functions.
    /// </summary>
    public class JSLibVariable
    {
        /// <summary>
        /// Variable name.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Return type.
        /// </summary>
        public ReturnType ReturnType { get; set; }

        /// <summary>
        /// Default value.
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// First parameter name (if has any).
        /// Only possible with ReturnType String or StringArray.
        /// </summary>
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// Method documentation.
        /// </summary>
        public string MethodDocumentation { get; set; }
    }

    /// <summary>
    /// TypeScript return type enum for JSLibVariable.
    /// </summary>
    public enum ReturnType
    {
        /// <summary>
        /// void.
        /// </summary>
        Void,

        /// <summary>
        /// string.
        /// </summary>
        String,

        /// <summary>
        /// string[].
        /// </summary>
        StringArray,
    }
}
