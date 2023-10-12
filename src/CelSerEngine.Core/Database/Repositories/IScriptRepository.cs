using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Database.Repositories;

/// <summary>
/// Defines the contract for a script repository, which provides CRUD operations on scripts.
/// </summary>
public interface IScriptRepository
{
    /// <summary>
    /// Retrieves a script from the repository by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the script.</param>
    /// <returns>The script with the specified ID.</returns>
    public Task<Script> GetScriptById(int id);
    /// <summary>
    /// Adds a new script to the repository.
    /// </summary>
    /// <param name="script">The script to be added.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    public Task AddScriptAsync(Script script);

    /// <summary>
    /// Updates an existing script in the repository.
    /// </summary>
    /// <param name="script">The script with updated information.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    public Task UpdateScriptAsync(Script script);

    /// <summary>
    /// Deletes a script from the repository based on its ID.
    /// </summary>
    /// <param name="id">The ID of the script to be deleted.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    public Task DeleteScriptByIdAsync(int id);

    /// <summary>
    /// Retrieves a list of scripts that are associated with a given target process name.
    /// </summary>
    /// <param name="targetProcessName">The name of the target process.</param>
    /// <returns>
    /// A task that represents the asynchronous retrieval operation and contains 
    /// the list of scripts associated with the specified target process name.
    /// </returns>
    public Task<IList<Script>> GetScriptsByTargetProcessNameAsync(string targetProcessName);

    /// <summary>
    /// Retrieves a target process from the repository by its name.
    /// </summary>
    /// <param name="targetProcessName">The name of the target process.</param>
    /// <returns>The target process with the specified name, or null if not found.</returns>
    public Task<TargetProcess?> GetTargetProcessByNameAsync(string targetProcessName);
}
