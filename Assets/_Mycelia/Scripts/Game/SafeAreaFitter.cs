using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// Fits a canvas's root to the device's safe area, clear of notches and gesture bars (ADR-0012 rule 4), and fits
    /// it again if the safe area changes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        Rect fittedSafeArea;
        Vector2Int fittedScreen;

        public static (Vector2 Min, Vector2 Max) AnchorsFor(Rect safeArea, Vector2 screenSize) =>
            (new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y),
             new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y));

        public void Fit(Rect safeArea, Vector2 screenSize)
        {
            var rect = (RectTransform)transform;
            (rect.anchorMin, rect.anchorMax) = AnchorsFor(safeArea, screenSize);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void Start() => FitToScreen();

        void LateUpdate()
        {
            if (Screen.safeArea != fittedSafeArea || Screen.width != fittedScreen.x || Screen.height != fittedScreen.y)
                FitToScreen();
        }

        void FitToScreen()
        {
            fittedSafeArea = Screen.safeArea;
            fittedScreen = new Vector2Int(Screen.width, Screen.height);
            // No screen yet (batch mode); LateUpdate fits once there is one.
            if (fittedScreen.x > 0 && fittedScreen.y > 0)
                Fit(fittedSafeArea, fittedScreen);
        }
    }
}
