namespace CelSerEngine.Core.Scanners.Serialization;

public interface IPointerLayout
{
    public const int MaxSupportedLevel = 30;
    public int EntrySizeInBytes { get; init; }
}
