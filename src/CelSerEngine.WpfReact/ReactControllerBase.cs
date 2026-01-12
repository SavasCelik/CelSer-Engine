namespace CelSerEngine.WpfReact;

public abstract class ReactControllerBase
{
    public string ComponentId { get; set; } = string.Empty;

    /// <summary>
    /// This method is called when the component registers its javascript methods.
    /// </summary>
    public virtual void OnComponentRegisteredMethods() {}
}
