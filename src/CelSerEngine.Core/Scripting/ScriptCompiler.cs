using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting.Template;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;

namespace CelSerEngine.Core.Scripting;
public class ScriptCompiler
{
    private readonly List<PortableExecutableReference> _references;

    public ScriptCompiler()
    {
        _references = new List<PortableExecutableReference>();
        AddNetCoreDefaultReferences();
        AddAssembly(typeof(ILoopingScript));
    }

    public ILoopingScript CompileScript(IScript script)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(script.Logic);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release);
        CSharpCompilation compilation = CSharpCompilation.Create($"DynamicAssembly.ID{script.Id}")
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(_references)
            .WithOptions(options);

        using var memoryStream = new MemoryStream();
        EmitResult result = compilation.Emit(memoryStream);
        if (!result.Success)
        {
            IEnumerable<Diagnostic> errors = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);
            var errorMessages = errors.Select(x => x.GetMessage() + $" (Line: {x.Location.GetLineSpan().StartLinePosition.Line + 1})").ToArray();

            throw new ScriptValidationException(string.Join("\n", errorMessages));
        }

        Assembly assembly = Assembly.Load(memoryStream.ToArray());
        var instance = assembly.CreateInstance("LoopingScript");

        if (instance is not ILoopingScript loopingScript)
            throw new ScriptValidationException($"Code must implement from {nameof(ILoopingScript)}");

        return loopingScript;
    }

    private void AddNetCoreDefaultReferences()
    {
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location) +
                     Path.DirectorySeparatorChar;

        AddAssembly(runtimePath + "System.Private.CoreLib.dll");
        AddAssembly(runtimePath + "System.Runtime.dll");
        AddAssembly(runtimePath + "System.Console.dll");
    }

    public void AddAssembly(Type type)
    {
        try
        {
            if (_references.Any(r => r.FilePath == type.Assembly.Location))
                return;

            var systemReference = MetadataReference.CreateFromFile(type.Assembly.Location);
            _references.Add(systemReference);
        }
        catch (Exception ex)
        {
            throw new ScriptValidationException(ex.Message);
        }
    }

    public void AddAssembly(string assemblyDll)
    {
        if (string.IsNullOrEmpty(assemblyDll))
            return;

        var file = Path.GetFullPath(assemblyDll);

        if (!File.Exists(file))
        {
            // check framework or dedicated runtime app folder
            var path = Path.GetDirectoryName(typeof(object).Assembly.Location);
            file = Path.Combine(path, assemblyDll);
            if (!File.Exists(file))
                return;
        }

        if (_references.Any(r => r.FilePath == file))
            return;

        try
        {
            var reference = MetadataReference.CreateFromFile(file);
            _references.Add(reference);
        }
        catch (Exception ex)
        {
            throw new ScriptValidationException(ex.Message);
        }
    }
}