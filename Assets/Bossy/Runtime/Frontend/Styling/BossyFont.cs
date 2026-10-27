using System;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// Provides the Bossy font.
    /// </summary>
    /// <remarks>
    /// The font assets are created in memory from the font files instead of being stored as assets.
    /// Dynamic font assets write new glyphs into themselves as text is drawn. When Bossy is installed
    /// as a package its files are read only, so a stored asset changing makes Unity rebuild the package
    /// cache and can leave text pointing at a reset atlas, which renders as invisible text.
    /// </remarks>
    internal static class BossyFont
    {
        private const string RegularPath = "Fonts/JetBrainsMono-Regular";
        private const string BoldPath = "Fonts/JetBrainsMono-Bold";
        private const string ItalicPath = "Fonts/JetBrainsMono-Italic";

        // Indices into the font weight table, which is indexed by weight / 100
        private const int RegularWeightIndex = 4;
        private const int BoldWeightIndex = 7;

        private static FontAsset _regular;

        /// <summary>
        /// Applies the Bossy font to an element. Children inherit it.
        /// </summary>
        /// <param name="element">The element to apply the font to.</param>
        public static void ApplyTo(VisualElement element)
        {
            element.style.unityFontDefinition = new StyleFontDefinition(GetFontAsset());
        }

        private static FontAsset GetFontAsset()
        {
            if (_regular != null)
            {
                return _regular;
            }

            _regular = CreateFontAsset(RegularPath);

            // Without these, rich text bold and italic tags fall back to synthesized styles
            var weights = _regular.fontWeightTable;
            if (weights != null && weights.Length > BoldWeightIndex)
            {
                weights[RegularWeightIndex].italicTypeface = CreateFontAsset(ItalicPath);
                weights[BoldWeightIndex].regularTypeface = CreateFontAsset(BoldPath);
            }

            return _regular;
        }

        private static FontAsset CreateFontAsset(string resourcePath)
        {
            var font = Resources.Load<Font>(resourcePath);
            if (font == null)
            {
                throw new InvalidOperationException($"Bossy could not find its font at Resources/{resourcePath}.");
            }

            var asset = FontAsset.CreateFontAsset(font);
            asset.name = font.name;
            return asset;
        }
    }
}
