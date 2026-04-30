using Bossy.Utils;
using UnityEngine;

namespace Bossy.Settings
{
    /// <summary>
    /// Settings for Bossy input.
    /// </summary>
    internal class BossyInputSettings
    {
        [Setting("The key combination used to toggle the main Bossy host.")] 
        public readonly KeyCombination ToggleMainHost = new(KeyCode.Slash, KeyModifiers.None);
        
        [Setting("The key used to enter responses.")]
        public readonly KeyCombination SubmitCommand = new(KeyCode.Return, KeyModifiers.None);
        
        [Setting("The key used to cancel commands.")]
        public readonly KeyCombination CancelCommand = new(KeyCode.C, KeyModifiers.Control);
    }
}