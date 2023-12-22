using System;

namespace SmartTwinEditor.SwaggerAttribute
{
    /// <summary>
    /// This attribute will expose marked Method for Angular as a type of SwaggerClient.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AngularExposedAttribute : Attribute
    {
        private const string DefaultGameObjectName = "AngularInformer";

        /// <summary>
        /// GameObject name exposed method is part of.
        /// </summary>
        public string GameObjectName { get; } = DefaultGameObjectName;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AngularExposedAttribute()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectName">GameObject name exposed method is part of.</param>
        public AngularExposedAttribute(string gameObjectName = DefaultGameObjectName)
        {
            GameObjectName = gameObjectName;
        }
    }
}
