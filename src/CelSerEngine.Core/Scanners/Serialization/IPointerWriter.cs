namespace CelSerEngine.Core.Scanners.Serialization;

public interface IPointerWriter : IDisposable
{
    /// <summary>
    /// Writes pointer's information to the stream
    /// </summary>
    public void Write(int level, int moduleIndex, long baseOffset, ReadOnlySpan<IntPtr> offsets);
}
