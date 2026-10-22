namespace Mycelia.Domain.Grid
{
    /// <summary>The board as the view sees it: readable, never writable outside the domain's rules.</summary>
    public interface IReadOnlyBoard
    {
        int Width { get; }
        int Height { get; }
        bool IsInside(GridPosition cell);
        bool IsPlayable(GridPosition cell);
        SporeTypeId GetOccupant(GridPosition cell);
    }
}
