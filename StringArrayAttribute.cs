using System;

namespace Assets.Unity-Angular-Bridge.SwaggerAttribute
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
