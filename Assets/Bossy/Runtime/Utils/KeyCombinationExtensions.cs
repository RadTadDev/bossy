#if UNITY_EDITOR

using UnityEditor.ShortcutManagement;

namespace Bossy.Utils.Editor
{
    public static class KeyCombinationExtensions
    {
        /// <summary>
        /// Converts Bossy key combination to Unity key combination.
        /// </summary>
        /// <param name="combination">The combination to convert.</param>
        /// <returns>The Unity version.</returns>
        public static UnityEditor.ShortcutManagement.KeyCombination BossyToUnity(this KeyCombination combination)
        {
            return new UnityEditor.ShortcutManagement.KeyCombination(combination.KeyCode, (ShortcutModifiers)combination.Modifiers);
        }
    }
}

#endif