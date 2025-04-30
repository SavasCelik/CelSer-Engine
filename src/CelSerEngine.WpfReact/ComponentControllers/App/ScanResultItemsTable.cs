using CelSerEngine.Core.Models;

namespace CelSerEngine.WpfReact.ComponentControllers.App;

public class ScanResultItemsTable
{
    public List<MemorySegment> ScanResultItems { get; set; }

    public ScanResultItemsTable()
    {
        ScanResultItems = [];
    }

    public List<ScanResultItemReact> GetScanResultItems(int page, int pageSize)
    {
        return ScanResultItems
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select( x => new ScanResultItemReact
            {
                Address = x.Address.ToString("X8"),
                Value = x.Value,
                PreviousValue = x.InitialValue
            }
            ).ToList();
    }
}
