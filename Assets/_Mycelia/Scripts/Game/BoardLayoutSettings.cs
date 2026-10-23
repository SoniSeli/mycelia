using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>The board's insets, tuned on device without code changes (ADR-0011).</summary>
    [CreateAssetMenu(menuName = "Mycelia/Board Layout Settings", fileName = "BoardLayoutSettings")]
    public sealed class BoardLayoutSettings : ScriptableObject
    {
        [SerializeField, Range(0, 0.45f), Tooltip("Fraction of the safe area's height kept free above the board, for the HUD.")]
        float top = 0.2f;

        [SerializeField, Range(0, 0.45f), Tooltip("Fraction of the safe area's height kept free below the board.")]
        float bottom = 0.08f;

        [SerializeField, Range(0, 0.25f), Tooltip("Fraction of the safe area's width kept free on each side.")]
        float side = 0.04f;

        public BoardInsets Insets => new BoardInsets(top, bottom, side);
    }
}
