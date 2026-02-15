namespace CelSerEngine.WpfReact.ComponentControllers.PointerScanner;

public class PointerScanResultsPageDto
{
    public IReadOnlyCollection<PointerScanResultDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
