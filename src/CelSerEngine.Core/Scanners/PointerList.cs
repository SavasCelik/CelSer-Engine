namespace CelSerEngine.Core.Scanners;

internal class PointerList
{
    public int MaxSize { get; set; }
    public int ExpectedSize { get; set; }
    public int Pos { get; set; }
    public PointerData[]? List { get; set; }

    //Linked list
    public IntPtr PointerValue { get; set; }
    public PointerList? Previous { get; set; }
    public PointerList? Next { get; set; }
}