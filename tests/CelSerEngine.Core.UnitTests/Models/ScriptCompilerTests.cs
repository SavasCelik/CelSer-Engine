using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting;
using CelSerEngine.Core.Scripting.Template;
using Xunit;

namespace CelSerEngine.Core.UnitTests.Models;

public class ScriptCompilerTests
{
    [Fact]
    public void CompileScript_ValidScriptLogic_CompilesSuccessfully()
    {
        // Arrange
        var scriptCompiler = new ScriptCompiler();
        var script = new Script()
        {
            Logic = ScriptTemplates.BasicTemplate
        };
     
        // Act
        var loopingScript = scriptCompiler.CompileScript(script);

        // Assert
        Assert.NotNull(loopingScript);
    }

    [Fact]
    public void CompileScript_InvalidScriptLogic_Throws()
    {
        // Arrange
        var scriptCompiler = new ScriptCompiler();
        var script = new Script()
        {
            Logic = "string myString: 1234;"
        };

        // Act & Assert
        Assert.Throws<ScriptValidationException>(() => scriptCompiler.CompileScript(script));
    }
}
