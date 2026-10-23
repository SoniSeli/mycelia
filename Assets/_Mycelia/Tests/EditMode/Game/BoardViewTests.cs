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
        GameObject cellTemplate;
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
            cellTemplate = TestViews.CellTemplate();
            boardObject = new GameObject("Board");
            view = boardObject.AddComponent<BoardView>();
            TestViews.SetReference(view, "sporePrefab", sporeTemplate.GetComponent<SporeView>());
            TestViews.SetReference(view, "cellPrefab", cellTemplate.GetComponent<SpriteRenderer>());
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
            Object.DestroyImmediate(cellTemplate);
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

        [Test]
        public void EveryPlayableCell_HasATile_AndVoidCellsHaveNone()
        {
            view.Init(board, catalog);

            var tiles = view.CellsRoot.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(tiles.Length, Is.EqualTo(5));
            foreach (SpriteRenderer tile in tiles)
            {
                Assert.That((Vector2)tile.transform.localPosition, Is.Not.EqualTo(BoardLayout.CellCentre(At(2, 1))));
            }
        }

        [Test]
        public void SporesAreDrawnAboveTheirTiles()
        {
            view.Init(board, catalog);

            var tile = view.CellsRoot.GetComponentInChildren<SpriteRenderer>();
            Assert.That(view.SporeAt(At(0, 0)).Renderer.sortingOrder, Is.GreaterThan(tile.sortingOrder));
        }

        [Test]
        public void ThePool_IsFilledWithTwiceTheCells_BeforePlayStarts()
        {
            view.Init(board, catalog);

            var spores = boardObject.GetComponentsInChildren<SporeView>(includeInactive: true);
            Assert.That(spores.Length, Is.EqualTo(2 * 3 * 2));
            Assert.That(view.Pool.Available, Is.EqualTo(2 * 3 * 2 - 4));
        }
    }
}
