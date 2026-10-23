using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace Mycelia.Game.Tests
{
    /// <summary>
    /// Guards the input setup (ADR-0008): one action asset reading one pointer and the back button, and no global
    /// project-wide actions (ADR-0010).
    /// </summary>
    public class ProjectInputTests
    {
        [Test]
        public void TheOnlyInputAsset_ReadsOnePointer_AndTheBackButton()
        {
            // Only the project's own assets: the Input System package ships DefaultInputActions, which CI's Library
            // imports as an InputActionAsset.
            var asset = AssetDatabase.FindAssets("t:InputActionAsset", new[] { "Assets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .Single();

            Assert.That(asset.name, Is.EqualTo("MyceliaInput"));
            InputActionMap gameplay = asset.FindActionMap("Gameplay", throwIfNotFound: true);
            Assert.That(gameplay.FindAction("Point", true).bindings.Select(b => b.path), Is.EqualTo(new[] { "<Pointer>/position" }));
            Assert.That(gameplay.FindAction("Press", true).bindings.Select(b => b.path), Is.EqualTo(new[] { "<Pointer>/press" }));
            // Android's back button arrives as the Escape key, which also makes it testable in the Editor.
            Assert.That(gameplay.FindAction("Back", true).bindings.Select(b => b.path), Is.EqualTo(new[] { "<Keyboard>/escape" }));
        }

        [Test]
        public void NoActionsAreRegisteredProjectWide()
        {
            Assert.That(EditorBuildSettings.TryGetConfigObject("com.unity.input.settings.actions", out InputActionAsset actions), Is.False,
                actions != null ? $"{actions.name} is registered as project-wide actions." : null);
        }
    }
}
