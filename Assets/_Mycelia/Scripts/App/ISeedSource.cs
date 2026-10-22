namespace Mycelia.App
{
    /// <summary>Chooses the seed for each level attempt; the domain never picks its own (ADR-0007).</summary>
    public interface ISeedSource
    {
        ulong Next();
    }
}
