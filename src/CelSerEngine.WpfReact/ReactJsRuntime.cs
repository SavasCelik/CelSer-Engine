using Microsoft.Web.WebView2.Core;
using System.Text.Json;

namespace CelSerEngine.WpfReact;

public class ReactJsRuntime
{
    public CoreWebView2? _coreWebView2;

    public void AttachToWebView(CoreWebView2 coreWebView2)
    {
        _coreWebView2 = coreWebView2;
    }

    public async Task<T?> InvokeAsync<T>(string objectId, string functionName, params object[] args)
    {
        var resultJson = await InvokeAsync(objectId, functionName, args);

        if (resultJson is null)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(resultJson);
    }

    public async Task InvokeVoidAsync(string objectId, string functionName, params object[] args)
    {
        await InvokeAsync(objectId, functionName, args);
    }

    private async Task<string?> InvokeAsync(string objectId, string functionName, params object[] args)
    {
        if (_coreWebView2 == null)
            throw new NullReferenceException(nameof(_coreWebView2));

        var argsJson = "[" + string.Join(", ", args.Select(arg => JsonSerializer.Serialize(arg))) + "]";
        var script = $"window.jsInterop.invokeComponentMethod('{objectId}','{functionName}', {argsJson})";
        return await _coreWebView2.ExecuteScriptAsync(script);
    }
}
