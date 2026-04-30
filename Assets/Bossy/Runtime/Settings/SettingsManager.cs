using System;
using UnityEngine;

namespace Bossy.Settings
{
    /// <summary>
    /// Database of all Bossy settings.
    /// </summary>
    [Serializable]
    internal class SettingsManager
    {
        // Enumerate all settings containers here
        
        // TODO: Remove = new() from these when bottom TODO is resolved
        
        /// <summary>
        /// The CLI settings.
        /// </summary>
        public BossyCliSettings BossyCliSettings = new();
        
        /// <summary>
        /// Settings for input.
        /// </summary>
        public BossyInputSettings BossyInputSettings = new();
        
        [NonSerialized]
        private ISettingsSource _source;

        /// <summary>
        /// Creates a new settings manager.
        /// </summary>
        /// <param name="source">The source to use.</param>
        public SettingsManager(ISettingsSource source)
        {
            _source = source;
        }
        
        /// <summary>
        /// Loads the settings.
        /// </summary>
        public void Load()
        {
            // TODO: Loading from missing or blank file nulls this object
            // var json = _source.LoadJson();
            // JsonUtility.FromJsonOverwrite(json, this);
        }

        /// <summary>
        /// Saves the settings.
        /// </summary>
        public void Save()
        {
            var json = JsonUtility.ToJson(this, true);
            _source.SaveJson(json);
        }

        /// <summary>
        /// Resets settings to their default values.
        /// </summary>
        public void Reset()
        {
            BossyCliSettings = new BossyCliSettings();
            BossyInputSettings = new BossyInputSettings();
        }
    }
}