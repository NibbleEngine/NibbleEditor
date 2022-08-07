using NbCore.Utils;
using System;
using System.Reflection;

namespace NibbleEditor
{
    class Program
    {   
        static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += LibUtils.LoadAssembly;
            var assemblydir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" +
                assemblydir);
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" +
                System.IO.Path.Combine(assemblydir, "lib"));
            
            Window wnd = new Window();
            wnd.Run();
        }
    }
}
 