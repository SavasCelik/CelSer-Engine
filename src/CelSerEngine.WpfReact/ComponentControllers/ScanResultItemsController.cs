namespace CelSerEngine.WpfReact.ComponentControllers;

public class ScanResultItemReact
{
    public required string Address { get; init; }
    public required string Value { get; init; }
    public required string PreviousValue { get; init; }
}

public class ScanResultItemsController : ReactControllerBase
{
    private List<ScanResultItemReact> _scanResultItems = [];

    public ScanResultItemsController()
    {
        // create 30 items
        for (int i = 0; i < 30; i++)
        {
            _scanResultItems.Add(new ScanResultItemReact
            {
                Address = $"0x{(i * 4):X8}",
                Value = $"{i}",
                PreviousValue = $"{(i - 1) * 4}"
            });
        }
    }

    public List<ScanResultItemReact> GetScanResultItems()
    {
        return _scanResultItems;
    }
}
