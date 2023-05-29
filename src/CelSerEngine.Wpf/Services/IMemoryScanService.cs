using CelSerEngine.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace CelSerEngine.Wpf.Services;
public interface IMemoryScanService
{
    public Task<IReadOnlyCollection<IProcessMemory>> ScanProcessMemory(ScanConstraint scanConstraint, IntPtr processHandle, IProgress<float> progressUpdater);
}
