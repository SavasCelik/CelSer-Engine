using Microsoft.Web.WebView2.Core;
using System.Text.Json;

namespace CelSerEngine.WpfReact;

public class ReactJsRuntime
{
    private CoreWebView2? _coreWebView2;
    private JsonSerializerOptions? _jsonSerializerOptions;

    public void AttachToWebViewAndJsonOptions(CoreWebView2 coreWebView2, JsonSerializerOptions jsonOptions)
    {
        _coreWebView2 = coreWebView2;
        _jsonSerializerOptions = jsonOptions;
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

        if (_jsonSerializerOptions == null)
            throw new NullReferenceException(nameof(_jsonSerializerOptions));

        var argsJson = "[" + string.Join(", ", args.Select(arg => JsonSerializer.Serialize(arg, _jsonSerializerOptions))) + "]";
        var script = $"window.jsInterop.invokeComponentMethod('{objectId}','{functionName}', {argsJson})";
        return await _coreWebView2.ExecuteScriptAsync(script);
    }
}
