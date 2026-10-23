using System;
using Mycelia.Domain.Grid;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// Where the board sits on screen (ADR-0011). The board stays in world units, one unit per cell with the origin at
    /// its bottom-left corner, and the orthographic camera is sized and moved so the board fills the area the insets
    /// leave inside the safe area. The same maths maps screen points to the board for input (ADR-0008).
    /// </summary>
    public readonly struct BoardLayout
    {
        BoardLayout(Vector2 screenSize, Rect boardArea, float pixelsPerCell, Vector2 cameraPosition)
        {
            ScreenSize = screenSize;
            BoardArea = boardArea;
            PixelsPerCell = pixelsPerCell;
            CameraPosition = cameraPosition;
        }

        public Vector2 ScreenSize { get; }

        /// <summary>The part of the screen, in pixels, the board may use.</summary>
        public Rect BoardArea { get; }

        public float PixelsPerCell { get; }

        /// <summary>The camera's world position (x, y).</summary>
        public Vector2 CameraPosition { get; }

        /// <summary>Half the screen height in world units, as <see cref="Camera.orthographicSize"/> expects.</summary>
        public float OrthographicSize => ScreenSize.y / PixelsPerCell / 2;

        public static Vector2 CellCentre(GridPosition cell) => new Vector2(cell.Column + 0.5f, cell.Row + 0.5f);

        public static BoardLayout Fit(int columns, int rows, Vector2 screenSize, Rect safeArea, BoardInsets insets)
        {
            if (columns < 1 || rows < 1)
                throw new ArgumentOutOfRangeException(nameof(columns), $"A board needs at least one cell (got {columns} × {rows}).");
            if (screenSize.x <= 0 || screenSize.y <= 0)
                throw new ArgumentOutOfRangeException(nameof(screenSize), screenSize, "The screen has no size.");

            Rect area = Rect.MinMaxRect(
                safeArea.xMin + safeArea.width * insets.Side,
                safeArea.yMin + safeArea.height * insets.Bottom,
                safeArea.xMax - safeArea.width * insets.Side,
                safeArea.yMax - safeArea.height * insets.Top);

            float pixelsPerCell = Mathf.Min(area.width / columns, area.height / rows);

            // Put the board's centre at the area's centre.
            var boardCentre = new Vector2(columns / 2f, rows / 2f);
            Vector2 offsetFromScreenCentre = (area.center - screenSize / 2) / pixelsPerCell;
            return new BoardLayout(screenSize, area, pixelsPerCell, boardCentre - offsetFromScreenCentre);
        }

        public Vector2 WorldToScreen(Vector2 world) => (world - CameraPosition) * PixelsPerCell + ScreenSize / 2;

        public Vector2 ScreenToWorld(Vector2 screen) => (screen - ScreenSize / 2) / PixelsPerCell + CameraPosition;
    }
}
