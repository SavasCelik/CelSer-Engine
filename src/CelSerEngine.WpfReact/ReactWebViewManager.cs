using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CelSerEngine.WpfReact;

public class ReactWebViewManager : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WebView2 _webView;
    private readonly TaskGenericsUtil _taskGenericsUtil;
    private readonly ConcurrentDictionary<long, object> _trackedRefsById;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private long _nextObjectReferenceId;

    public ReactWebViewManager(IServiceProvider serviceProvider, WebView2 webView)
    {
        _serviceProvider = serviceProvider;
        _webView = webView;
        _taskGenericsUtil = new TaskGenericsUtil();
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
        _trackedRefsById = new ConcurrentDictionary<long, object>();
        webView.CoreWebView2.WebMessageReceived += MessageReceived;
        webView.CoreWebView2.NavigationStarting += (sender, e) =>
        {
            // maybe clear on if e.NavigationKind == CoreWebView2NavigationKind.Reload?

            DisposeTrackedRefs();
        };
    }

    public void MessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string message = e.TryGetWebMessageAsString();
        var receivedMessage = JsonSerializer.Deserialize<ReceivedMessage>(message, _jsonSerializerOptions);

        if (receivedMessage == null || receivedMessage.MethodName == "")
        {
            return;
        }

        if (receivedMessage.MethodName == "AttachDotNetObject")
        {
            AttachDotNetObject(receivedMessage);
        }
        else if (receivedMessage.MethodName == "DetachDotNetObject")
        {
            DetachDotNetObject(receivedMessage);
        }
        else if (receivedMessage.MethodName == "BindComponentReferences")
        {
            BindComponentReferences(receivedMessage);
        }
        else if (receivedMessage.DotNetObjectId != null)
        {
            BeginInvokeDotNet(receivedMessage);
        }
        else
        {
            throw new InvalidOperationException($"Unknown method: {receivedMessage.MethodName}");
        }        
    }

    private void AttachDotNetObject(ReceivedMessage receivedMessage)
    {
        var methodArgs = JsonSerializer.Deserialize<JsonElement[]>(receivedMessage.MethodArguments)!;
        var className = methodArgs[0].GetString()!;
        var componentId = methodArgs[1].GetString()!;
        var classNameType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetExportedTypes())
            .FirstOrDefault(t => t.Name == className);

        if (classNameType == null)
        {
            throw new TypeLoadException($"The class '{className}' does not exist in the current application domain.");
        }

        if (!typeof(ReactControllerBase).IsAssignableFrom(classNameType))
        {
            throw new InvalidOperationException($"The class '{className}' must inherit from '{nameof(ReactControllerBase)}'.");
        }

        var dotNetObjectId = Interlocked.Increment(ref _nextObjectReferenceId);
        var instanceOfClass = (ReactControllerBase)ActivatorUtilities.CreateInstance(_serviceProvider, classNameType!);
        instanceOfClass.ComponentId = componentId;

        _trackedRefsById.TryAdd(dotNetObjectId, instanceOfClass);
        EndInvokeDotNet(receivedMessage.AsyncCallId, dotNetObjectId);
    }

    private void DetachDotNetObject(ReceivedMessage receivedMessage)
    {
        long dotNetObjectId = JsonSerializer.Deserialize<long>(receivedMessage.MethodArguments)!;
        var isDetached = _trackedRefsById.TryRemove(dotNetObjectId, out var detachedObject);

        if (detachedObject is IDisposable disposable)
        {
            disposable.Dispose();
        }

        EndInvokeDotNet(receivedMessage.AsyncCallId, isDetached);
    }

    private void BindComponentReferences(ReceivedMessage receivedMessage)
    {
        var instance = _trackedRefsById[receivedMessage.DotNetObjectId!.Value];

        if (instance == null)
        {
            EndInvokeDotNet(receivedMessage.AsyncCallId, "instance not found!", isSuccess: false);

            return;
        }

        var componentRefsJson = JsonSerializer.Deserialize<JsonElement[]>(receivedMessage.MethodArguments)!;
        var injectPropertiesByName = instance.GetType()
        .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Where(prop => prop.IsDefined(typeof(InjectComponentAttribute)))
        .ToDictionary(x => x.Name);

        foreach (var componentRefJson in componentRefsJson)
        {
            var componentControllerName = componentRefJson[0].GetString();
            var componentInstanceId = componentRefJson[1].GetInt64();
            if (componentControllerName != null && injectPropertiesByName.TryGetValue(componentControllerName, out var property))
            {
                var componentRef = _trackedRefsById[componentInstanceId];
                property.SetValue(instance, componentRef);
            }
            else
            {
                EndInvokeDotNet(receivedMessage.AsyncCallId, $"Component reference '{componentControllerName}' not found!", isSuccess: false);

                return;
            }
        }

        EndInvokeDotNet(receivedMessage.AsyncCallId, true);
    }

    private void BeginInvokeDotNet(ReceivedMessage receivedMessage)
    {
        var instance = _trackedRefsById[receivedMessage.DotNetObjectId!.Value];
        var method = instance.GetType().GetMethod(receivedMessage.MethodName);

        if (method == null)
        {
            EndInvokeDotNet(receivedMessage.AsyncCallId, $"Method: {instance.GetType()}.{receivedMessage.MethodName} not found!", isSuccess: false);
            
            return;
        }

        var methodParams = method.GetParameters().ToArray();
        var paramsSend = JsonSerializer.Deserialize<JsonElement[]>(receivedMessage.MethodArguments)!;
        var convertedParams = new object?[methodParams.Length];

        for (var i = 0; i < methodParams.Length; i++)
        {
            if (paramsSend.Length <= i)
            {
                throw new Exception(methodParams[i].HasDefaultValue 
                    ? "Optional parameters are not supported"
                    : $"Expected argument missing {i} for method {method.Name}");
            }

            convertedParams[i] = ConvertJsonElement(paramsSend[i], methodParams[i].ParameterType);
        }

        var methodResponse = method.Invoke(instance, convertedParams);

        if (methodResponse is Task taskResponse)
        {
            _ = taskResponse.ContinueWith(t => EndInvokeDotNetAfterTask(t, receivedMessage.AsyncCallId), TaskScheduler.Current);
        }
        else
        {
            EndInvokeDotNet(receivedMessage.AsyncCallId, methodResponse);
        }
    }

    private void EndInvokeDotNet(int asyncCallId, object? result, bool isSuccess = true)
    {
        var response = new ResponseMessage
        {
            AsyncCallId = asyncCallId,
            IsSuccess = isSuccess,
            ReposeJson = JsonSerializer.Serialize(result, _jsonSerializerOptions)
        };

        EndInvokeDotNet(response);
    }

    private void EndInvokeDotNet(ResponseMessage responseMessage)
    {
        //_webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(response, _jsonSerializerOptions));
        _webView.Dispatcher.Invoke(() =>
        {
            _webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(responseMessage, _jsonSerializerOptions));
        });
    }

    private void EndInvokeDotNetAfterTask(Task task, int asyncCallId)
    {
        var result = _taskGenericsUtil.GetTaskResult(task);
        EndInvokeDotNet(asyncCallId, result);
    }

    private object? ConvertJsonElement(JsonElement element, Type targetType)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        // Handle basic types
        if (targetType == typeof(int))
        {
            return element.GetInt32();
        }
        if (targetType == typeof(string))
        {
            return element.GetString();
        }
        if (targetType == typeof(bool))
        {
            return element.GetBoolean();
        }
        if (targetType == typeof(double))
        {
            return element.GetDouble();
        }

        return JsonSerializer.Deserialize(element, targetType, _jsonSerializerOptions);
    }

    /// <summary>
    /// Disposes all tracked references.
    /// </summary>
    private void DisposeTrackedRefs()
    {
        foreach (var trackedRefDisposable in _trackedRefsById.Values.OfType<IDisposable>())
        {
            trackedRefDisposable.Dispose();
        }

        _trackedRefsById.Clear();
    }

    public void Dispose()
    {
        DisposeTrackedRefs();
    }
}

internal class ReceivedMessage
{
    public int AsyncCallId { get; set; }
    public int? DotNetObjectId { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string MethodArguments { get; set; } = string.Empty;
}

internal class ResponseMessage
{
    public int AsyncCallId { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string ReposeJson { get; set; } = string.Empty;
}
