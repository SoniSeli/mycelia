using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// Fits the camera to the board (ADR-0011), and fits it again if the screen or safe area changes,
    /// e.g. a resized Game view or a foldable phone.
    /// </summary>
    public sealed class BoardCamera : MonoBehaviour
    {
        [SerializeField] Camera boardCamera;
        [SerializeField] BoardLayoutSettings settings;

        int columns;
        int rows;
        Vector2Int fittedScreen;
        Rect fittedSafeArea;

        public BoardLayout Layout { get; private set; }

        public void Init(int columns, int rows)
        {
            this.columns = columns;
            this.rows = rows;
            Refit();
        }

        public void Apply(BoardLayout layout)
        {
            Layout = layout;
            boardCamera.orthographicSize = layout.OrthographicSize;
            Vector3 position = boardCamera.transform.position;
            boardCamera.transform.position = new Vector3(layout.CameraPosition.x, layout.CameraPosition.y, position.z);
        }

        void LateUpdate()
        {
            if (columns > 0 && (Screen.width != fittedScreen.x || Screen.height != fittedScreen.y || Screen.safeArea != fittedSafeArea))
                Refit();
        }

        void Refit()
        {
            fittedScreen = new Vector2Int(Screen.width, Screen.height);
            fittedSafeArea = Screen.safeArea;
            // No screen yet (batch mode); LateUpdate fits once there is one.
            if (fittedScreen.x <= 0 || fittedScreen.y <= 0)
                return;

            Apply(BoardLayout.Fit(columns, rows, fittedScreen, fittedSafeArea, settings.Insets));
        }
    }
}
