using Bossy.Utils;
using UnityEngine;

namespace Bossy.Settings
{
    /// <summary>
    /// Settings for Bossy input.
    /// </summary>
    public class BossyInputSettings
    {
        [Setting("The key used to enter responses.")]
        public readonly KeyCombination SubmitCommand = new(KeyCode.Return);
        
        [Setting("The key used to cancel commands.")]
        public readonly KeyCombination CancelCommand = new(KeyCode.C, KeyModifiers.Control);
        
        [Setting("The key used to go back in history on the CLI.")]
        public readonly KeyCombination HistoryBack = new(KeyCode.UpArrow);

        [Setting("The key used to go forward in history on the CLI.")]
        public readonly KeyCombination HistoryForward = new(KeyCode.DownArrow);
        
        [Setting("The key used to highlight the next suggestion while suggestions are shown.")]
        public readonly KeyCombination NextSuggestion = new(KeyCode.DownArrow);

        [Setting("The key used to highlight the previous suggestion while suggestions are shown.")]
        public readonly KeyCombination PreviousSuggestion = new(KeyCode.UpArrow);

        [Setting("The key used to accept the highlighted suggestion, or to show suggestions if they are hidden.")]
        public readonly KeyCombination AcceptSuggestion = new(KeyCode.Tab);

        [Setting("The key used to hide suggestions.")]
        public readonly KeyCombination CloseSuggestions = new(KeyCode.Escape);
    }
}