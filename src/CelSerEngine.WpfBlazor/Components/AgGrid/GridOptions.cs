using System.Text.Json.Serialization;

namespace CelSerEngine.WpfBlazor.Components.AgGrid;

public class GridOptions
{
    public required ColumnDef[] ColumnDefs { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RowSelection RowSelection { get; set; } = RowSelection.Single;
    public string? GetRowStyleFunc { get; set; }
}

public class ColumnDef
{
    public required string Field { get; set; }
    public string? HeaderName { get; set; }
}

public enum RowSelection
{
    Single,
    Multiple
}