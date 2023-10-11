namespace CelSerEngine.Wpf.Models;

/// <summary>
/// Represents various states that a script can be in during its lifecycle, especially in relation to its validation and execution.
/// </summary>
public enum ScriptState
{
    /// <summary>
    /// Indicates that the script has not undergone validation.
    /// </summary>
    NotValidated,
    /// <summary>
    /// Indicates that the script has been validated successfully.
    /// </summary>
    Validated,
    /// <summary>
    /// Indicates that the script execution has started.
    /// </summary>
    Started,
    /// <summary>
    /// Indicates that the script execution has stopped or has been halted.
    /// </summary>
    Stopped
}
