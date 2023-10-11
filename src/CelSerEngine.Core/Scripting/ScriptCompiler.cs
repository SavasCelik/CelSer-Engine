using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting.Template;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;

namespace CelSerEngine.Core.Scripting;

/// <summary>
/// Provides a mechanism to compile C# scripts dynamically at runtime. It uses Roslyn, the .NET Compiler, to achieve this.
/// </summary>
public class ScriptCompiler
{
    private readonly List<PortableExecutableReference> _references;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptCompiler"/> class
    /// </summary>
    public ScriptCompiler()
    {
        _references = new List<PortableExecutableReference>();
        AddNetCoreDefaultReferences();
        AddAssembly(typeof(ILoopingScript));
    }

    /// <summary>
    /// Compiles a script, provided as an IScript interface, into a dynamically linked library (DLL).
    /// It then loads the DLL into memory and creates an instance of a type named "LoopingScript" expected to implement <see cref="ILoopingScript"/>.
    /// </summary>
    /// <param name="script">An instance that implements the IScript interface.</param>
    /// <returns>An instance of the compiled script implementing the <see cref="ILoopingScript"/> interface.</returns>
    /// <exception cref="ScriptValidationException">Thrown if the script validation fails.</exception>
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

    /// <summary>
    /// Adds default .NET Core assemblies as references for the script compilation.
    /// </summary>
    private void AddNetCoreDefaultReferences()
    {
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location) +
                     Path.DirectorySeparatorChar;

        AddAssembly(runtimePath + "System.Private.CoreLib.dll");
        AddAssembly(runtimePath + "System.Runtime.dll");
        AddAssembly(runtimePath + "System.Console.dll");
    }

    /// <summary>
    /// Adds the assembly of a given type to the list of references used for script compilation.
    /// </summary>
    /// <param name="type">The type whose assembly will be added as a reference.</param>
    private void AddAssembly(Type type)
    {
        if (_references.Any(r => r.FilePath == type.Assembly.Location))
            return;

        var systemReference = MetadataReference.CreateFromFile(type.Assembly.Location);
        _references.Add(systemReference);
    }

    /// <summary>
    /// Adds the assembly specified by the path to the list of references used for script compilation.
    /// </summary>
    /// <param name="assemblyDll">The path to the DLL of the assembly to be added.</param>
    private void AddAssembly(string assemblyDll)
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
        
        var reference = MetadataReference.CreateFromFile(file);
        _references.Add(reference);
    }
}