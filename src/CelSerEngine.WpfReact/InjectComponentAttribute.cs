namespace CelSerEngine.WpfReact;

/// <summary>
/// An attribute used to mark properties in ReactController for dependency injection of components.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class InjectComponentAttribute : Attribute
{
}
