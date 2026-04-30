using System;

namespace Bossy.Settings
{
    /// <summary>
    /// Marks a field as a setting.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SettingAttribute : Attribute
    {
        /// <summary>
        /// The setting's description.
        /// </summary>
        public readonly string Description;
        
        /// <summary>
        /// Creates a setting field.
        /// </summary>
        /// <param name="description">The setting's description.</param>
        public SettingAttribute(string description)
        {
            Description = description;
        }
    }
}