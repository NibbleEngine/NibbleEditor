using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiCore = ImGuiNET.ImGui;
using NbCore.Common;
using NbCore.Plugins;


namespace NbCore.UI.ImGui
{
    public class ImGuiSettingsWindow
    {
        private bool show_save_confirm_dialog = false;
        
        public ImGuiSettingsWindow()
        {

        }

        public void Draw()
        {

            //Assume that a Popup has begun
            ImGuiCore.BeginChild("SettingsWindow", ImGuiCore.GetContentRegionAvail(),
                true, ImGuiNET.ImGuiWindowFlags.NoDecoration | 
                      ImGuiNET.ImGuiWindowFlags.NoResize |
                      ImGuiNET.ImGuiWindowFlags.NoCollapse);

            foreach (PluginBase plugin in RenderState.engineRef.Plugins.Values)
            {
                if (plugin.Settings == null)
                    continue;
                
                if (ImGuiCore.CollapsingHeader(plugin.Name + " Settings"))
                {
                    plugin.Settings.Draw();
                }
            }

            if (ImGuiCore.CollapsingHeader("Engine Settings"))
            {
                //Render Settings
                ImGuiCore.BeginGroup();
                ImGuiCore.TextColored(ImGuiManager.DarkBlue, "Rendering Settings");
                ImGuiCore.SliderFloat("HDR Exposure", ref RenderState.settings.renderSettings.HDRExposure, 0.001f, 0.5f);
                ImGuiCore.InputInt("FPS", ref RenderState.settings.renderSettings.FPS);
                ImGuiCore.Checkbox("Vsync", ref RenderState.settings.renderSettings.UseVSync);
                ImGuiCore.EndGroup();
            }
            
            if (ImGuiCore.Button("Save Settings"))
            {
                Settings.saveToDisk(RenderState.settings);
                //Save Plugin Settings to Disk
                foreach (PluginBase plugin in RenderState.engineRef.Plugins.Values)
                {
                    plugin.Settings.SaveToFile();
                }
                show_save_confirm_dialog = true;
            }

            ImGuiCore.EndChild();

            
            if (show_save_confirm_dialog)
            {
                ImGuiCore.OpenPopup("Info");
                show_save_confirm_dialog = false;
            }
            
            //Draw Plugin Modals
            foreach (PluginBase plugin in RenderState.engineRef.Plugins.Values)
            {
                plugin.Settings?.DrawModals();
            }
            
            if (ImGuiCore.BeginPopupModal("Info"))
            {
                ImGuiCore.Text("Settings Saved Successfully!");
                ImGuiCore.EndPopup();
            }

        }

        ~ImGuiSettingsWindow()
        {

        }
    }
}
