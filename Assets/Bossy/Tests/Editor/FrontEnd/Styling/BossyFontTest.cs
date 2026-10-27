using Bossy.Frontend;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Bossy.Tests.FrontEnd.Styling
{
    /// <summary>
    /// Tests the Bossy font.
    /// </summary>
    public class BossyFontTest
    {
        [Test]
        public void ApplyTo_SetsFontAsset()
        {
            var element = new VisualElement();

            BossyFont.ApplyTo(element);

            Assert.That(element.style.unityFontDefinition.value.fontAsset, Is.Not.Null);
        }

        [Test]
        public void ApplyTo_FontAssetIsNotStoredOnDisk()
        {
            var element = new VisualElement();

            BossyFont.ApplyTo(element);

            var asset = element.style.unityFontDefinition.value.fontAsset;
            Assert.That(AssetDatabase.Contains(asset), Is.False);
        }

        [Test]
        public void FontAssets_SurviveUnloadingUnusedAssets()
        {
            var element = new VisualElement();
            BossyFont.ApplyTo(element);
            var asset = element.style.unityFontDefinition.value.fontAsset;

            // The editor unloads unused assets on scene loads and play mode changes, ignoring script references
            EditorUtility.UnloadUnusedAssetsImmediate(true);

            Assert.That(asset != null, Is.True, "Font asset was destroyed.");
            Assert.That(asset.material != null, Is.True, "Font material was destroyed.");
            Assert.That(asset.fontWeightTable[7].regularTypeface != null, Is.True, "Bold font asset was destroyed.");
            Assert.That(asset.fontWeightTable[4].italicTypeface != null, Is.True, "Italic font asset was destroyed.");
        }

        [UnityTest]
        public IEnumerator FontAssets_CreatedInPlayMode_SurviveExitingPlayMode()
        {
            // Keep this test's state alive across play mode instead of reloading scripts
            var optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            var options = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;

            yield return new EnterPlayMode();

            // Entering play mode normally reloads scripts, so the font is created again inside play mode.
            // Existing assets are renamed rather than destroyed, because open windows may still draw with them.
            typeof(BossyFont).GetField("_regular", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
            var existing = Resources.FindObjectsOfTypeAll<FontAsset>().Where(a => a.name.StartsWith("Bossy ")).ToList();
            foreach (var font in existing)
            {
                font.name = "Retired " + font.name;
            }
            var element = new VisualElement();
            BossyFont.ApplyTo(element);
            var asset = element.style.unityFontDefinition.value.fontAsset;

            yield return new ExitPlayMode();

            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            foreach (var font in existing)
            {
                font.name = font.name.Substring("Retired ".Length);
            }

            Assert.That(asset != null, Is.True, "Font asset was destroyed.");
            Assert.That(asset.material != null, Is.True, "Font material was destroyed.");
            Assert.That(asset.atlasTextures.All(t => t != null), Is.True, "Font atlas was destroyed.");
            Assert.That(asset.fontWeightTable[7].regularTypeface != null, Is.True, "Bold font asset was destroyed.");
        }

        [Test]
        public void ApplyTo_HasBoldAndItalicTypefaces()
        {
            var element = new VisualElement();

            BossyFont.ApplyTo(element);

            var weights = element.style.unityFontDefinition.value.fontAsset.fontWeightTable;
            Assert.That(weights[7].regularTypeface, Is.Not.Null);
            Assert.That(weights[4].italicTypeface, Is.Not.Null);
        }
    }
}
