using System.Linq;
using Mycelia.Content;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Mycelia.App.Tests
{
    /// <summary>Guards the game font (design §8.3, Milestone 4 slice 1): Shantell Sans, one family for every text.</summary>
    public class ProjectFontTests
    {
        const string Folder = "Assets/_Mycelia/Art/Fonts/";

        static TMP_FontAsset Bold => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "ShantellSans-Bold SDF.asset");
        static TMP_FontAsset SemiBold => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "ShantellSans-SemiBold SDF.asset");

        public static bool IsGameFont(TMP_FontAsset font) => font != null && (font == Bold || font == SemiBold);

        // Characters a font asset can't draw (whitespace needs no glyph).
        public static string Missing(TMP_FontAsset font, string text) =>
            new string((text ?? "").Where(c => !char.IsWhiteSpace(c) && !font.characterLookupTable.ContainsKey(c)).Distinct().ToArray());

        [Test]
        public void TheGameFont_ComesInTwoStaticWeights_SetAsChosen()
        {
            foreach (TMP_FontAsset font in new[] { Bold, SemiBold })
            {
                Assert.That(font, Is.Not.Null);
                Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static), $"{font.name}: no glyphs made while playing.");
                Assert.That(font.sourceFontFile, Is.Null, $"{font.name}: the .otf stays out of the build.");
                Assert.That(font.normalSpacingOffset, Is.EqualTo(-2f), $"{font.name}: Soni's spacing.");
            }
        }

        [Test]
        public void NewTexts_StartInTheGameFont()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
            var defaultFont = (TMP_FontAsset)new SerializedObject(settings).FindProperty("m_defaultFontAsset").objectReferenceValue;
            Assert.That(defaultFont, Is.SameAs(SemiBold));
        }

        [Test]
        public void EveryPrefabsText_UsesTheGameFont_AndCanDrawItsText()
        {
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Mycelia" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
            foreach (TMP_Text text in prefabs.SelectMany(prefab => prefab.GetComponentsInChildren<TMP_Text>(true)))
            {
                Assert.That(IsGameFont(text.font), Is.True, $"{text.name} uses {(text.font != null ? text.font.name : "no font")}.");
                Assert.That(Missing(text.font, text.text), Is.Empty, text.name);
            }
        }

        // The fact card shows these in the body weight (ADR-0012 Refinements).
        [Test]
        public void EveryLevelsFactAndCredit_CanBeDrawnWithTheGameFont()
        {
            var levels = AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid)));
            foreach (LevelDefinition level in levels)
            {
                Assert.That(Missing(SemiBold, level.FactText), Is.Empty, $"{level.Key} fact");
                Assert.That(Missing(SemiBold, level.FactImageCredit), Is.Empty, $"{level.Key} credit");
            }
        }
    }
}
