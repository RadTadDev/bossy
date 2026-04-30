using Bossy.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    public static class ContentViewUtility
    {
        /// <summary>
        /// Gets a root element from a Uxml document. Must be in a resources folder.
        /// </summary>
        /// <param name="uxmlPath">The path within the resources folder.</param>
        /// <returns>The root.</returns>
        public static VisualElement GetRootFromUxml(string uxmlPath)
        {
            var tree = Resources.Load<VisualTreeAsset>(uxmlPath);

            return tree == null ? throw new BossyNullUxmlDocumentException(uxmlPath) : tree.CloneTree().ElementAt(0);
        }
    }
}