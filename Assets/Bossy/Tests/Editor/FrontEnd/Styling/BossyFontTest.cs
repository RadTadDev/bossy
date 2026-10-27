using Bossy.Frontend;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Bossy.Tests.FrontEnd.Styling
{
    /// <summary>
    /// Tests the Bossy font.
    /// </summary>
    public class BossyFontTest
    {
        [Test] public void ApplyTo_SetsFontAsset()
        {
            var element = new VisualElement();

            BossyFont.ApplyTo(element);

            Assert.That(element.style.unityFontDefinition.value.fontAsset, Is.Not.Null);
        }

        [Test] public void ApplyTo_FontAssetIsNotStoredOnDisk()
        {
            var element = new VisualElement();

            BossyFont.ApplyTo(element);

            var asset = element.style.unityFontDefinition.value.fontAsset;
            Assert.That(AssetDatabase.Contains(asset), Is.False);
        }

        [Test] public void ApplyTo_HasBoldAndItalicTypefaces()
        {
            var element = new VisualElement();

            BossyFont.ApplyTo(element);

            var weights = element.style.unityFontDefinition.value.fontAsset.fontWeightTable;
            Assert.That(weights[7].regularTypeface, Is.Not.Null);
            Assert.That(weights[4].italicTypeface, Is.Not.Null);
        }
    }
}
