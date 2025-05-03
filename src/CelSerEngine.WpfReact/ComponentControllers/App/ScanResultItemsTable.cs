using CelSerEngine.Core.Models;

namespace CelSerEngine.WpfReact.ComponentControllers.App;

public class ScanResultItemsTable
{
    public List<MemorySegment> ScanResultItems { get; set; }

    public ScanResultItemsTable()
    {
        ScanResultItems = [];
    }

    public IEnumerable<MemorySegment> GetScanResultItems(int page, int pageSize)
    {
        return ScanResultItems
            .Skip(page * pageSize)
            .Take(pageSize);
    }
}
