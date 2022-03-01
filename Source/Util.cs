﻿using System;
using System.Collections.Generic;
using System.IO;
using NbCore;
using NbCore.Common;
using ImGuiNET;
using System.Drawing;
using System.Linq;
using System.Reflection;



namespace NibbleEditor
{
    public static class Util
    {
        public static string DonateLink = @"https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=4365XYBWGTBSU&currency_code=USD&source=url";
        public static readonly Random randgen = new();
        
        //Current GLControl Handle
        public static OpenTK.Windowing.Desktop.NativeWindow activeWindow;
        
        public static string getVersion()
        {
#if DEBUG
            return "v" + Version.AssemblyVersion + " [DEBUG]";
#else
            return "v" + Version.AssemblyVersion;
#endif
        }

        //Update Status strip
        public static void setStatus(string status)
        {
            RenderState.StatusString = status;
        }

        public static void showError(string message, string caption)
        {
            Console.WriteLine($"{message}");
        }
        
        
        public static void showInfo(string message, string caption)
        {

            if (ImGui.BeginPopupModal("show-info"))
            {
                ImGui.Text(string.Format("%s", message));
                ImGui.EndPopup();
            }
        }

        public static void Assert(bool status, string msg)
        {
            if (!status)
                Callbacks.Logger.Log(Assembly.GetCallingAssembly(), msg, LogVerbosityLevel.ERROR);
            System.Diagnostics.Trace.Assert(status);
        }
    
    }

}
