using System;
using PrimeTween;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// The fade between scenes (design §8.3 slice 2): a full-screen overlay in the backdrop's deep blue, above every
    /// other canvas, on the Bootstrap's AppRoot so it outlives scene loads. It starts covered (set in the scene), so the
    /// first screen fades in too. While it isn't fully clear it catches every UI tap; once clear, its canvas is switched
    /// off and costs nothing to draw.
    /// </summary>
    public sealed class ScreenFade : MonoBehaviour, IScreenFade
    {
        [SerializeField] Canvas canvas;
        [SerializeField] CanvasGroup group;
        [SerializeField, Min(0.01f), Tooltip("Seconds for each half: out, then in.")] float duration = 0.25f;

        Tween tween;
        Action then;

        public void FadeOut(Action onCovered)
        {
            Begin(onCovered ?? throw new ArgumentNullException(nameof(onCovered)));
            if (group.alpha >= 1f)
            {
                Finish();
                return;
            }
            // From wherever a fade in had got to, at the same speed.
            tween = Tween.Alpha(group, 1f, duration * (1f - group.alpha)).OnComplete(this, static fade => fade.Finish());
        }

        public void FadeIn(Action onShown)
        {
            Begin(onShown ?? throw new ArgumentNullException(nameof(onShown)));
            if (group.alpha <= 0f)
            {
                Clear();
                return;
            }
            tween = Tween.Alpha(group, 0f, duration * group.alpha).OnComplete(this, static fade => fade.Clear());
        }

        void Begin(Action next)
        {
            tween.Stop();
            then = next;
            canvas.enabled = true;
            group.blocksRaycasts = true;
        }

        void Clear()
        {
            group.blocksRaycasts = false;
            canvas.enabled = false;
            Finish();
        }

        void Finish()
        {
            Action next = then;
            then = null;
            next();
        }
    }
}
