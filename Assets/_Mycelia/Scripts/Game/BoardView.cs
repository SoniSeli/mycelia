using System;
using Mycelia.Content;
using Mycelia.Domain;
using Mycelia.Domain.Grid;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>
    /// Draws the board: a tile under every playable cell and a pooled <see cref="SporeView"/> for every spore.
    /// Everything it will ever need is created in <see cref="Init"/>, before play starts (ADR-0011).
    /// The board's origin is this object's position, which must be the world origin for <see cref="BoardLayout"/>.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        public const int CellSortingOrder = 0;

        [SerializeField] SporeView sporePrefab;
        [SerializeField] SpriteRenderer cellPrefab;

        SporeCatalog catalog;
        SporeView[] spores;
        int width;

        public SporeViewPool Pool { get; private set; }
        public SporeCatalog Catalog => catalog;
        public Transform CellsRoot { get; private set; }

        public void Init(IReadOnlyBoard board, SporeCatalog catalog)
        {
            if (Pool != null)
                throw new InvalidOperationException("The board view is already showing a board.");

            this.catalog = catalog;
            width = board.Width;
            int cells = board.Width * board.Height;
            CellsRoot = CreateChild("Cells");
            // Three times the cells: playback rents new spores while cleared ones still animate out, and a move can
            // clear its chain and then regenerate the whole board (ADR-0011, amended).
            Pool = new SporeViewPool(sporePrefab, CreateChild("Spores"), 3 * cells);
            spores = new SporeView[cells];

            for (int row = 0; row < board.Height; row++)
            for (int column = 0; column < board.Width; column++)
            {
                var cell = new GridPosition(column, row);
                if (!board.IsPlayable(cell))
                    continue;

                Vector2 centre = BoardLayout.CellCentre(cell);
                SpriteRenderer tile = Instantiate(cellPrefab, CellsRoot);
                tile.name = $"Cell {column},{row}";
                tile.sortingOrder = CellSortingOrder;
                tile.transform.localPosition = centre;

                SporeTypeId occupant = board.GetOccupant(cell);
                if (occupant == SporeTypeId.None)
                    continue;

                SporeView spore = Pool.Rent();
                spore.Show(catalog.TypeOf(occupant), centre);
                spores[IndexOf(cell)] = spore;
            }
        }

        /// <summary>The view showing the spore in <paramref name="cell"/>, or null if the cell has none.</summary>
        public SporeView SporeAt(GridPosition cell) => spores[IndexOf(cell)];

        /// <summary>Removes the view from <paramref name="cell"/>, for playback to animate it away or elsewhere.</summary>
        internal SporeView Take(GridPosition cell)
        {
            int index = IndexOf(cell);
            SporeView view = spores[index] ?? throw new InvalidOperationException($"No spore view at {cell} to move.");
            spores[index] = null;
            return view;
        }

        /// <summary>Puts <paramref name="view"/> on <paramref name="cell"/>, which must be free.</summary>
        internal void Place(GridPosition cell, SporeView view)
        {
            int index = IndexOf(cell);
            if (spores[index] != null)
                throw new InvalidOperationException($"{cell} already shows a spore.");
            spores[index] = view;
        }

        int IndexOf(GridPosition cell) => cell.Row * width + cell.Column;

        Transform CreateChild(string childName)
        {
            Transform child = new GameObject(childName).transform;
            child.SetParent(transform, worldPositionStays: false);
            return child;
        }
    }
}
