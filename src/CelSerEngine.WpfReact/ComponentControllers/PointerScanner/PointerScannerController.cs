using CelSerEngine.Core.Native;

namespace CelSerEngine.WpfReact.ComponentControllers.PointerScanner;

public class PointerScannerController : ReactControllerBase
{
    private readonly INativeApi _nativeApi;

    public PointerScannerController(INativeApi nativeApi)
    {
        _nativeApi = nativeApi;
    }
    public void StartPointerScan(string scanAddress, int maxOffset, int maxLevel)
    {
    }
}
