using Mycelia.Domain.Chains;
using Mycelia.Domain.Grid;
using Mycelia.Domain.Sessions;
using NUnit.Framework;
using UnityEngine;

namespace Mycelia.Game.Tests
{
    public class ChainViewTests
    {
        const string Grid = @"
            A A A
            A B C
            A A B";

        GameObject sporeTemplate;
        GameObject root;
        BoardView boardView;
        ChainView chainView;
        LineRenderer line;
        LevelSession session;

        static GridPosition At(int column, int row) => new GridPosition(column, row);

        [SetUp]
        public void CreateViews()
        {
            sporeTemplate = TestViews.SporeTemplate();
            root = new GameObject("Board");
            boardView = root.AddComponent<BoardView>();
            TestViews.SetReference(boardView, "sporePrefab", sporeTemplate.GetComponent<SporeView>());
            line = new GameObject("Chain Line").AddComponent<LineRenderer>();
            line.transform.SetParent(root.transform);
            chainView = line.gameObject.AddComponent<ChainView>();
            TestViews.SetReference(chainView, "line", line);

            session = TestSessions.Start(Grid);
            boardView.Init(session.Board, TestViews.ProjectSpores);
            chainView.Init(boardView, session.Board.Width * session.Board.Height);
        }

        [TearDown]
        public void DestroyViews()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(sporeTemplate);
        }

        void Touch(int column, int row)
        {
            GridPosition cell = At(column, row);
            chainView.ChainChanged(session.Chain, session.Chain.Touch(cell), cell);
        }

        [Test]
        public void TheLine_RunsThroughTheChainedSporeCentres_InOrder()
        {
            Touch(0, 0);
            Touch(1, 0);
            Touch(0, 1);

            Assert.That(line.positionCount, Is.EqualTo(3));
            Assert.That((Vector2)line.GetPosition(0), Is.EqualTo(BoardLayout.CellCentre(At(0, 0))));
            Assert.That((Vector2)line.GetPosition(1), Is.EqualTo(BoardLayout.CellCentre(At(1, 0))));
            Assert.That((Vector2)line.GetPosition(2), Is.EqualTo(BoardLayout.CellCentre(At(0, 1))));
        }

        [Test]
        public void ChainedSpores_AreDrawnLarger_AndShrinkBackWhenBacktracked()
        {
            Touch(0, 0);
            Touch(1, 0);
            Assert.That(boardView.SporeAt(At(1, 0)).transform.localScale.x, Is.GreaterThan(1f));

            Touch(0, 0);

            Assert.That(boardView.SporeAt(At(0, 0)).transform.localScale.x, Is.GreaterThan(1f));
            Assert.That(boardView.SporeAt(At(1, 0)).transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(line.positionCount, Is.EqualTo(1));
        }

        [Test]
        public void WhenTheChainEnds_TheLineDisappears_AndSporesReturnToSize()
        {
            Touch(0, 0);
            Touch(1, 0);
            session.Chain.Clear();

            chainView.ChainEnded(session.Chain);

            Assert.That(line.positionCount, Is.Zero);
            Assert.That(boardView.SporeAt(At(0, 0)).transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(boardView.SporeAt(At(1, 0)).transform.localScale, Is.EqualTo(Vector3.one));
        }

        // Soni (2026-10-25): the line joins the spores from behind, so the chain reads as one shape.
        [Test]
        public void TheLine_IsDrawnBehindTheSpores_AndAboveTheBackdrop()
        {
            Assert.That(line.sortingOrder, Is.LessThan(SporeView.SortingOrder));
            Assert.That(line.sortingOrder, Is.GreaterThan(Backdrop.SporeSortingOrder));
        }

        // Soni (2026-10-25): the line is the chained spores' own colour, fully opaque.
        [Test]
        public void TheLine_TakesTheChainedSporesColour()
        {
            Touch(1, 1);
            Color b = boardView.SporeAt(At(1, 1)).Renderer.color;
            Assert.That(line.startColor, Is.EqualTo(b));
            Assert.That(line.endColor, Is.EqualTo(b));

            session.Chain.Clear();
            chainView.ChainEnded(session.Chain);
            Touch(0, 0);
            Color a = boardView.SporeAt(At(0, 0)).Renderer.color;
            Assert.That(a, Is.Not.EqualTo(b), "The test needs two colours.");
            Assert.That(line.startColor, Is.EqualTo(a), "A new chain of another type takes its colour.");
            Assert.That(line.endColor.a, Is.EqualTo(1f));
        }
    }
}
