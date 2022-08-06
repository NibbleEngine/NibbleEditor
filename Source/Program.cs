using NbCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NibbleEditor
{
    class Program
    {   
        static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += LibUtils.LoadAssembly;
            Window wnd = new Window();
            wnd.Run();
        }
    }
}
 