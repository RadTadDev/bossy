using Bossy.Utils;
using UnityEngine;

namespace Bossy.Settings
{
    /// <summary>
    /// Settings for Bossy input.
    /// </summary>
    public class BossyInputSettings
    {
        [Setting("The key combination used to toggle the main Bossy host.")] 
        public readonly KeyCombination ToggleMainHost = new(KeyCode.Slash);
        
        [Setting("The key used to enter responses.")]
        public readonly KeyCombination SubmitCommand = new(KeyCode.Return);
        
        [Setting("The key used to cancel commands.")]
        public readonly KeyCombination CancelCommand = new(KeyCode.C, KeyModifiers.Control);
        
        [Setting("The key used to go back in history on the CLI.")]
        public readonly KeyCombination HistoryBack = new(KeyCode.UpArrow);

        [Setting("The key used to go forward in history on the CLI.")]
        public readonly KeyCombination HistoryForward = new(KeyCode.DownArrow);
    }
}