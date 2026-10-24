namespace Mycelia.Game
{
    /// <summary>Says when touches must start nothing, such as while the screen changes to another scene (design §8.3).</summary>
    public interface IInputBlock
    {
        bool IsBlockingInput { get; }
    }
}
