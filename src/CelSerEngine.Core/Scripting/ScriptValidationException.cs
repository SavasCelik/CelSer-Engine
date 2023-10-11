namespace CelSerEngine.Core.Scripting;

/// <summary>
/// Utilize this exception to handle and signal specific validation failures or issues related to scripts and their compilation.
/// </summary>
public class ScriptValidationException : Exception
{
    /// <summary>
    /// Creates an instance of the <see cref="ScriptValidationException"/> class with a specified error message.
    /// </summary>
    /// <param name="errorMessage">A string that describes the error or the reason the exception is thrown.</param>
    public ScriptValidationException(string errorMessage) : base(errorMessage)
    {
    }
}
