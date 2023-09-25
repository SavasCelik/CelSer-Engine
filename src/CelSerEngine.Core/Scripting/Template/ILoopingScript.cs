using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine.Core.Scripting.Template;
public interface ILoopingScript
{
    public void OnStart();
    public void OnLoop();
    public void OnStop();
}
