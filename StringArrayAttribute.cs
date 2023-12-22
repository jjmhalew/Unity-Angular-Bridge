using System;

namespace Assets.UnityAngularBridge.SwaggerAttribute
{
    /// <summary>
    /// Converts string parameter to array in JSLibExport.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class StringArrayAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public StringArrayAttribute()
        {
        }
    }
}
