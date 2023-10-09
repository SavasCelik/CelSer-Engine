using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting.Template;
using CelSerEngine.Wpf.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.Services;
public interface IScriptService
{
    public Task InsertScriptAsync(Script script, string targetProcessName);
    public Task UpdateScriptAsync(IScript script);
    public Task DeleteScriptAsync(IScript script);
    public Task<IList<Script>> GetScriptsByTargetProcessNameAsync(string targetProcessName);
    public Task<IScript> ImportScriptAsync(string pathToFile, string targetProcessName);
    public Task ExportScriptAsync(IScript script, string exportPath);
    public ILoopingScript ValidateScript(IScript script);
    public void RunScript(ObservableScript script);
    public void StopScript(ObservableScript script);
}
