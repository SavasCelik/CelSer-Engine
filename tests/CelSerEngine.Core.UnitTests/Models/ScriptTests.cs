using CelSerEngine.Core.Models;
using Xunit;

namespace CelSerEngine.Core.UnitTests.Models;

public class ScriptTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var script = new Script();

        // Assert
        Assert.Equal("Custom Script", script.Name);
        Assert.Equal("", script.Logic);
    }

    [Fact]
    public void Constructor_CreatingNewScript_InitIdAsZero()
    {
        var script = new Script();

        var actualId = script.Id;
        var expectedId = 0;

        Assert.Equal(actualId, expectedId);
    }

    [Theory]
    [InlineData("Console.WriteLine(\"Hello World\");", true)]
    [InlineData("not valid script logic", true)]
    public void SetLogic_AlwaysUpdates(string logic, bool shouldBeEqual)
    {
        // Arrange
        var script = new Script
        {
            Logic = logic
        };

        // Assert
        Assert.Equal(logic == script.Logic, shouldBeEqual);
    }
}
