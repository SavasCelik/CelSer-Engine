using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting;
using CelSerEngine.Core.Scripting.Template;
using CelSerEngine.Wpf.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.Services;

/// <summary>
/// Provides methods to manage and interact with scripts.
/// </summary>
public interface IScriptService
{
    /// <summary>
    /// Inserts a new script for a specified target process.
    /// </summary>
    /// <param name="script">The script to be inserted.</param>
    /// <param name="targetProcessName">The name of the target process.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task InsertScriptAsync(Script script, string targetProcessName);

    /// <summary>
    /// Updates an existing script.
    /// </summary>
    /// <param name="script">The script to be updated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task UpdateScriptAsync(IScript script);

    /// <summary>
    /// Duplicates an existing script.
    /// </summary>
    /// <param name="script">The script to be duplicated.</param>
    /// <returns>The a new script instance</returns>
    public Task<Script> DuplicateScriptAsync(IScript script);

    /// <summary>
    /// Deletes a specified script.
    /// </summary>
    /// <param name="script">The script to be deleted.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeleteScriptAsync(IScript script);

    /// <summary>
    /// Retrieves scripts associated with a specified target process name.
    /// </summary>
    /// <param name="targetProcessName">The name of the target process.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of scripts.</returns>
    public Task<IList<Script>> GetScriptsByTargetProcessNameAsync(string targetProcessName);

    /// <summary>
    /// Imports a script from a specified file path and associates it with a target process name.
    /// </summary>
    /// <param name="pathToFile">The path to the script file.</param>
    /// <param name="targetProcessName">The name of the target process.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the imported script.</returns>
    public Task<IScript> ImportScriptAsync(string pathToFile, string targetProcessName);

    /// <summary>
    /// Exports a specified script to a given path.
    /// </summary>
    /// <param name="script">The script to be exported.</param>
    /// <param name="exportPath">The path where the script will be exported.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ExportScriptAsync(IScript script, string exportPath);

    /// <summary>
    /// Validates the given script.
    /// </summary>
    /// <param name="script">The script to be validated.</param>
    /// <returns>The looping script if valid, otherwise throws an exception.</returns>
    public ILoopingScript ValidateScript(IScript script);

    /// <summary>
    /// Runs the specified observable script using a given memory manager.
    /// The <paramref name="memoryManager"/> is used for <see cref="ILoopingScript"/>'s methods
    /// </summary>
    /// <param name="script">The observable script to be run.</param>
    /// <param name="memoryManager">The memory manager used at execution.</param>

    public void RunScript(ObservableScript script, MemoryManager memoryManager);

    /// <summary>
    /// Stops the execution of a specified observable script using a given memory manager.
    /// The <paramref name="memoryManager"/> is used for <see cref="ILoopingScript"/>'s methods
    /// </summary>
    /// <param name="script">The observable script to be stopped.</param>
    /// <param name="memoryManager">The memory manager used at execution.</param>
    public void StopScript(ObservableScript script, MemoryManager memoryManager);
}
