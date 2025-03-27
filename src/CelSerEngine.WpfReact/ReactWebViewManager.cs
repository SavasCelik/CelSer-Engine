﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CelSerEngine.WpfReact;

public class ReactWebViewManager
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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _trackedRefsById = new ConcurrentDictionary<long, object>();
        webView.CoreWebView2.WebMessageReceived += MessageReceived;
        webView.CoreWebView2.NavigationStarting += (sender, e) =>
        {
            // maybe clear on if e.NavigationKind == CoreWebView2NavigationKind.Reload?
            _trackedRefsById.Clear();
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
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == className && typeof(ReactControllerBase).IsAssignableFrom(t));
        var dotNetObjectId = Interlocked.Increment(ref _nextObjectReferenceId);
        var instanceOfClass = (ReactControllerBase)ActivatorUtilities.CreateInstance(_serviceProvider, classNameType!);
        instanceOfClass.ComponentId = componentId;

        _trackedRefsById.TryAdd(dotNetObjectId, instanceOfClass);
        EndInvokeDotNet(receivedMessage.AsyncCallId, dotNetObjectId);
    }

    private void DetachDotNetObject(ReceivedMessage receivedMessage)
    {
        long dotNetObjectId = JsonSerializer.Deserialize<long>(receivedMessage.MethodArguments)!;
        var isDetached = _trackedRefsById.TryRemove(dotNetObjectId, out _);
        var response = new ResponseMessage
        {
            AsyncCallId = receivedMessage.AsyncCallId,
            ReposeJson = JsonSerializer.Serialize(isDetached)
        };
        EndInvokeDotNet(receivedMessage.AsyncCallId, isDetached);
    }

    private void BeginInvokeDotNet(ReceivedMessage receivedMessage)
    {
        var instance = _trackedRefsById[receivedMessage.DotNetObjectId!.Value];
        var method = instance.GetType().GetMethod(receivedMessage.MethodName);

        if (method == null)
        {
            return;
        }

        var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
        var paramsSend = JsonSerializer.Deserialize<JsonElement[]>(receivedMessage.MethodArguments)!;
        var convertedParams = new object?[parameterTypes.Length];

        for (int i = 0; i < parameterTypes.Length; i++)
        {
            convertedParams[i] = ConvertJsonElement(paramsSend[i], parameterTypes[i]);
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

    private void EndInvokeDotNet(int asyncCallId, object? result)
    {
        var response = new ResponseMessage
        {
            AsyncCallId = asyncCallId,
            ReposeJson = JsonSerializer.Serialize(result)
        };
        _webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(response, _jsonSerializerOptions));
    }
    private void EndInvokeDotNetAfterTask(Task task, int asyncCallId)
    {
        var result = _taskGenericsUtil.GetTaskResult(task);
        EndInvokeDotNet(asyncCallId, result);
    }

    private static object? ConvertJsonElement(JsonElement element, Type targetType)
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

        // Add more types as needed

        // For unsupported types, throw an exception
        throw new InvalidOperationException($"Unsupported type: {targetType.Name}");
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
    public string ReposeJson { get; set; } = string.Empty;
}
