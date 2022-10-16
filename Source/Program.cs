using NbCore.Utils;
using NbCore;
using System;
using System.Reflection;

namespace NibbleEditor
{
    class Program
    {   
        static void Main()
        {
            Engine nibble = new Engine();
            
            Window wnd = new Window(nibble);
            wnd.Run();
        }
    }
}
 