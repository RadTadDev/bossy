using System;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore;
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

        // Characters whose glyphs are empty: tab, line feed, carriage return, space, non-breaking space and zero-width
        // space. They are added when the font is created so they can be moved off the atlas. Text can reach any of them,
        // for example through line breaks, and UI Toolkit measures empty text with a zero-width space.
        private static readonly string EmptyGlyphCharacters = new(new[]
        {
            (char)0x0009, (char)0x000A, (char)0x000D, (char)0x0020, (char)0x00A0, (char)0x200B
        });

        // Outside the atlas, past the bottom left corner, so that a quad drawn for an empty glyph samples only the
        // clamped edge texel. No glyph writes that texel, because glyphs are packed at least one texel in.
        private static readonly GlyphRect OffAtlasRect = new(-2 * AtlasPadding - 2, -2 * AtlasPadding - 2, 0, 0);

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

            MoveEmptyGlyphsOffAtlas(asset);

            // Unity destroys objects created in play mode when play mode ends, and unloads objects nothing
            // serialized refers to, including when unused assets are unloaded explicitly. Either would leave
            // the console drawing with a destroyed font. Only DontUnloadUnusedAsset stops the unloading, because
            // the font is referenced from C# alone.
            const HideFlags flags = HideFlags.DontSave | HideFlags.DontUnloadUnusedAsset;
            asset.hideFlags = flags;
            asset.material.hideFlags = flags;
            foreach (var atlas in asset.atlasTextures)
            {
                atlas.hideFlags = flags;
            }

            return asset;
        }

        /// <summary>
        /// Points empty glyphs, such as the space, at a rect outside the atlas.
        /// </summary>
        /// <remarks>
        /// Empty glyphs get a zero size rect at the atlas origin. UI Toolkit still draws a quad for them, padded on
        /// every side, which reached the edge of the first packed glyph and showed as a speck in every space.
        /// Outside the atlas, with the atlas clamped, the quad samples only the blank edge texel.
        /// </remarks>
        private static void MoveEmptyGlyphsOffAtlas(FontAsset asset)
        {
            foreach (var atlas in asset.atlasTextures)
            {
                atlas.wrapMode = TextureWrapMode.Clamp;
            }

            asset.TryAddCharacters(EmptyGlyphCharacters, out _);

            foreach (var glyph in asset.glyphLookupTable.Values)
            {
                if (glyph.glyphRect.width == 0 || glyph.glyphRect.height == 0)
                {
                    glyph.glyphRect = OffAtlasRect;
                }
            }
        }

        private static string AssetName(string resourcePath) => AssetNamePrefix + resourcePath.Substring(resourcePath.LastIndexOf('/') + 1);
    }
}
