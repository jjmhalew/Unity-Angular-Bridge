using System;

namespace UnityAngularBridge
{
    /// <summary>
    /// Converts string parameter to array in JSLibExport.
    /// </summary>
    [Obsolete("Use [JSLibExport(IsStringArray = true)] instead.")]
    [AttributeUsage(AttributeTargets.Method)]
    public class StringArrayAttribute : Attribute
    {
    }
}
