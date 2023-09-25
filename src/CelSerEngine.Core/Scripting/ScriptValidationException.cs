namespace CelSerEngine.Core.Scripting;
public class ScriptValidationException : Exception
{
    public ScriptValidationException(string errorMessage) : base(errorMessage)
    {
    }
}
