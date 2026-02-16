using Microsoft.Extensions.Logging;

namespace CelSerEngine.WpfReact.ComponentControllers.LogDisplayer;

public record LogItemDto(string Timestamp, LogLevel Level, string CategoryName, string Message);
