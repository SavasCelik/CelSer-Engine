namespace CelSerEngine.WpfBlazor.Components.AgGrid;

public class GridOptions
{
    public required ColumnDef[] ColumnDefs { get; set; }
    public string? GetRowStyleFunc { get; set; }
}

public class ColumnDef
{
    public required string Field { get; set; }
    public string? HeaderName { get; set; }
}