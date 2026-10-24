using System;

namespace Mycelia.Game
{
    /// <summary>Covers the screen while scenes change, and uncovers it again (design §8.3, <see cref="ScreenFade"/>).</summary>
    public interface IScreenFade
    {
        /// <summary>Fades the screen out; <paramref name="onCovered"/> runs once it is fully covered, at once if it already is.</summary>
        void FadeOut(Action onCovered);

        /// <summary>Fades the screen back in; <paramref name="onShown"/> runs once it is fully clear.</summary>
        void FadeIn(Action onShown);
    }
}
