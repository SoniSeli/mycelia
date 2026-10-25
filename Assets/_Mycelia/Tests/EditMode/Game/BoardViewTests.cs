using Mycelia.Content;
using Mycelia.Domain;
using Mycelia.Domain.Grid;
using NUnit.Framework;
using UnityEngine;

namespace Mycelia.Game.Tests
{
    public class BoardViewTests
    {
        GameObject sporeTemplate;
        GameObject boardObject;
        BoardView view;
        SporeCatalog catalog;
        Board board;

        static GridPosition At(int column, int row) => new GridPosition(column, row);

        static SporeTypeId Id(byte value) => new SporeTypeId(value);

        [SetUp]
        public void CreateView()
        {
            sporeTemplate = TestViews.SporeTemplate();
            boardObject = new GameObject("Board");
            view = boardObject.AddComponent<BoardView>();
            TestViews.SetReference(view, "sporePrefab", sporeTemplate.GetComponent<SporeView>());
            catalog = TestViews.ProjectSpores;

            // 3 × 2: (2, 1) is void, (1, 1) is empty.
            board = new Board(3, 2);
            board.SetOccupant(At(0, 0), Id(1));
            board.SetOccupant(At(1, 0), Id(2));
            board.SetOccupant(At(2, 0), Id(3));
            board.SetOccupant(At(0, 1), Id(4));
            board.SetVoid(At(2, 1));
        }

        [TearDown]
        public void DestroyObjects()
        {
            Object.DestroyImmediate(boardObject);
            Object.DestroyImmediate(sporeTemplate);
        }

        [Test]
        public void EverySpore_IsShownAtItsCellCentre_InItsTypesLook()
        {
            view.Init(board, catalog);

            foreach (GridPosition cell in new[] { At(0, 0), At(1, 0), At(2, 0), At(0, 1) })
            {
                SporeView spore = view.SporeAt(cell);
                SporeType type = catalog.TypeOf(board.GetOccupant(cell));
                Assert.That(spore, Is.Not.Null, cell.ToString());
                Assert.That(spore.gameObject.activeSelf, Is.True, cell.ToString());
                Assert.That((Vector2)spore.transform.localPosition, Is.EqualTo(BoardLayout.CellCentre(cell)), cell.ToString());
                Assert.That(spore.Renderer.sprite, Is.SameAs(type.Sprite), cell.ToString());
                Assert.That(spore.Renderer.color, Is.EqualTo(type.Color), cell.ToString());
            }
        }

        [Test]
        public void EmptyAndVoidCells_ShowNoSpore()
        {
            view.Init(board, catalog);

            Assert.That(view.SporeAt(At(1, 1)), Is.Null);
            Assert.That(view.SporeAt(At(2, 1)), Is.Null);
        }

        // Design §8.3: no tiles or boxes behind the spores; the backdrop shows between them.
        [Test]
        public void TheBoard_DrawsOnlySpores_WithNoTilesBehindThem()
        {
            view.Init(board, catalog);

            foreach (Renderer renderer in boardObject.GetComponentsInChildren<Renderer>(includeInactive: true))
                Assert.That(renderer.GetComponent<SporeView>(), Is.Not.Null, renderer.name);
        }

        [Test]
        public void ThePool_IsFilledWithThreeTimesTheCells_BeforePlayStarts()
        {
            view.Init(board, catalog);

            var spores = boardObject.GetComponentsInChildren<SporeView>(includeInactive: true);
            Assert.That(spores.Length, Is.EqualTo(3 * 3 * 2));
            Assert.That(view.Pool.Available, Is.EqualTo(3 * 3 * 2 - 4));
        }
    }
}
