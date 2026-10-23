using System;
using System.Collections.Generic;
using Mycelia.Domain.Grid;
using NUnit.Framework;
using UnityEngine;

namespace Mycelia.Game.Tests
{
    public class BoardLayoutTests
    {
        const float Tolerance = 0.01f;

        static readonly BoardInsets Hud = new BoardInsets(top: 0.2f, bottom: 0.08f, side: 0.04f);

        // Portrait screens from 16:9 to 21:9 and tablets (design §6), with notches and gesture bars in the safe area.
        static IEnumerable<TestCaseData> Screens()
        {
            yield return Screen("16:9 phone", 1080, 1920, notch: 0, gestureBar: 0);
            yield return Screen("19.5:9 phone with notch", 1080, 2340, notch: 120, gestureBar: 60);
            yield return Screen("20:9 phone (Redmi Note 12)", 1080, 2400, notch: 100, gestureBar: 48);
            yield return Screen("21:9 phone", 1080, 2520, notch: 130, gestureBar: 60);
            yield return Screen("4:3 tablet", 1536, 2048, notch: 0, gestureBar: 40);
            yield return Screen("16:10 tablet", 1600, 2560, notch: 0, gestureBar: 0);
        }

        static TestCaseData Screen(string name, int width, int height, int notch, int gestureBar) =>
            new TestCaseData(new Vector2(width, height), new Rect(0, gestureBar, width, height - notch - gestureBar)).SetName("{m}: " + name);

        // The area the board may use: the safe area minus the HUD bands and side margins, worked out independently.
        static Rect AllowedArea(Rect safe) => Rect.MinMaxRect(
            safe.xMin + safe.width * Hud.Side,
            safe.yMin + safe.height * Hud.Bottom,
            safe.xMax - safe.width * Hud.Side,
            safe.yMax - safe.height * Hud.Top);

        static Rect BoardOnScreen(BoardLayout layout, int columns, int rows)
        {
            Vector2 min = layout.WorldToScreen(Vector2.zero);
            Vector2 max = layout.WorldToScreen(new Vector2(columns, rows));
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        [TestCaseSource(nameof(Screens))]
        public void TheBoard_StaysClearOfTheHudAndTheSafeAreaEdges(Vector2 screen, Rect safe)
        {
            foreach ((int columns, int rows) in new[] { (6, 6), (7, 9), (8, 5) })
            {
                Rect board = BoardOnScreen(BoardLayout.Fit(columns, rows, screen, safe, Hud), columns, rows);
                Rect allowed = AllowedArea(safe);

                Assert.That(board.xMin, Is.GreaterThanOrEqualTo(allowed.xMin - Tolerance), $"{columns}×{rows} left");
                Assert.That(board.xMax, Is.LessThanOrEqualTo(allowed.xMax + Tolerance), $"{columns}×{rows} right");
                Assert.That(board.yMin, Is.GreaterThanOrEqualTo(allowed.yMin - Tolerance), $"{columns}×{rows} bottom");
                Assert.That(board.yMax, Is.LessThanOrEqualTo(allowed.yMax + Tolerance), $"{columns}×{rows} top");
            }
        }

        [TestCaseSource(nameof(Screens))]
        public void TheBoard_IsAsLargeAsItsAreaAllows(Vector2 screen, Rect safe)
        {
            foreach ((int columns, int rows) in new[] { (6, 6), (7, 9), (8, 5) })
            {
                Rect board = BoardOnScreen(BoardLayout.Fit(columns, rows, screen, safe, Hud), columns, rows);
                Rect allowed = AllowedArea(safe);

                bool fillsWidth = Mathf.Abs(board.width - allowed.width) < Tolerance;
                bool fillsHeight = Mathf.Abs(board.height - allowed.height) < Tolerance;
                Assert.That(fillsWidth || fillsHeight, $"{columns}×{rows}: board {board.size}, allowed {allowed.size}");
            }
        }

        [TestCaseSource(nameof(Screens))]
        public void TheBoard_IsCentredInItsArea(Vector2 screen, Rect safe)
        {
            Rect board = BoardOnScreen(BoardLayout.Fit(6, 6, screen, safe, Hud), 6, 6);
            Vector2 centre = AllowedArea(safe).center;

            Assert.That(board.center.x, Is.EqualTo(centre.x).Within(Tolerance));
            Assert.That(board.center.y, Is.EqualTo(centre.y).Within(Tolerance));
        }

        [TestCaseSource(nameof(Screens))]
        public void CellsAreSquare(Vector2 screen, Rect safe)
        {
            BoardLayout layout = BoardLayout.Fit(7, 9, screen, safe, Hud);
            Vector2 cell = layout.WorldToScreen(Vector2.one) - layout.WorldToScreen(Vector2.zero);

            Assert.That(cell.x, Is.EqualTo(cell.y).Within(Tolerance));
            Assert.That(cell.x, Is.EqualTo(layout.PixelsPerCell).Within(Tolerance));
        }

        [Test]
        public void EachCellCentre_IsHalfACellIn_FromTheBottomLeftOrigin()
        {
            Assert.That(BoardLayout.CellCentre(new GridPosition(0, 0)), Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(BoardLayout.CellCentre(new GridPosition(3, 5)), Is.EqualTo(new Vector2(3.5f, 5.5f)));
        }

        [Test]
        public void ScreenToWorld_UndoesWorldToScreen()
        {
            BoardLayout layout = BoardLayout.Fit(6, 6, new Vector2(1080, 2400), new Rect(0, 48, 1080, 2252), Hud);
            var world = new Vector2(2.3f, 4.7f);

            Vector2 roundTrip = layout.ScreenToWorld(layout.WorldToScreen(world));

            Assert.That(roundTrip.x, Is.EqualTo(world.x).Within(1e-4f));
            Assert.That(roundTrip.y, Is.EqualTo(world.y).Within(1e-4f));
        }

        [Test]
        public void ABoardWithoutCells_CannotBeLaidOut()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BoardLayout.Fit(0, 6, new Vector2(1080, 1920), new Rect(0, 0, 1080, 1920), Hud));
        }

        [Test]
        public void InsetsThatLeaveNoRoom_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new BoardInsets(top: 0.6f, bottom: 0.4f, side: 0.04f));
            Assert.Throws<ArgumentException>(() => new BoardInsets(top: 0.2f, bottom: 0.08f, side: 0.5f));
        }
    }
}
