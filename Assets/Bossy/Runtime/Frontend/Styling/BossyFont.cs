using System;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
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

        // Names the created assets so they can be found again after a domain reload
        private const string AssetNamePrefix = "Bossy ";

        // Indices into the font weight table, which is indexed by weight / 100
        private const int RegularWeightIndex = 4;
        private const int BoldWeightIndex = 7;

        // Glyphs are sampled at this size into a single atlas. That leaves room for several hundred
        // glyphs, and stays sharp at console sizes because the atlas stores distance fields.
        private const int SamplingPointSize = 36;
        private const int AtlasPadding = 4;
        private const int AtlasSize = 1024;

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

            // The assets outlive domain reloads, so reuse them rather than creating more each reload
            _regular = FindExisting(RegularPath);
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

        private static FontAsset FindExisting(string resourcePath)
        {
            var name = AssetName(resourcePath);
            return Resources.FindObjectsOfTypeAll<FontAsset>().FirstOrDefault(asset => asset.name == name);
        }

        private static FontAsset CreateFontAsset(string resourcePath)
        {
            var font = Resources.Load<Font>(resourcePath);
            if (font == null)
            {
                throw new InvalidOperationException($"Bossy could not find its font at Resources/{resourcePath}.");
            }

            // A single atlas, because extra atlases are created later as glyphs are added and would not get the flags below
            var asset = FontAsset.CreateFontAsset(font, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);
            asset.name = AssetName(resourcePath);

            // Unity destroys objects created in play mode when play mode ends, and unloads objects nothing
            // serialized refers to. Either would leave the console drawing with a destroyed font.
            asset.hideFlags = HideFlags.DontSave;
            asset.material.hideFlags = HideFlags.DontSave;
            foreach (var atlas in asset.atlasTextures)
            {
                atlas.hideFlags = HideFlags.DontSave;
            }

            return asset;
        }

        private static string AssetName(string resourcePath) => AssetNamePrefix + resourcePath.Substring(resourcePath.LastIndexOf('/') + 1);
    }
}
