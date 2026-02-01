using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Scanners.Serialization;

public interface IPointerReader : IDisposable
{
    public Stream BaseStream { get; }

    /// <summary>
    /// Reads the next <see cref="Pointer"/> from the current stream.
    /// </summary>
    public Pointer Read();

    /// <summary>
    /// Reads <see cref="Pointer"/> from the current stream and advances the position within the stream until the <paramref name="destination"/> is filled.
    /// </summary>
    /// <param name="destination">When this method returns, it contains the read Pointers from the stream.</param>
    /// <param name="startIndex">The start index of the first pointer</param>
    /// <exception cref="EndOfStreamException"></exception>
    public void ReadExactly(Span<Pointer> destination, int startIndex);
}
