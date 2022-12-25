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
                ImGuiCore.TextColored(ImGuiManager.DarkBlue, "Rendering Settings");
                ImGuiCore.SliderFloat("HDR Exposure", ref RenderState.settings.RenderSettings.HDRExposure, 0.001f, 0.5f);
                ImGuiCore.InputInt("FPS", ref RenderState.settings.RenderSettings.FPS);
                ImGuiCore.InputInt("Engine Tick Rate", ref RenderState.settings.TickRate);
                ImGuiCore.Checkbox("Vsync", ref RenderState.settings.RenderSettings.UseVSync);
            }

            if (ImGuiCore.Button("Save Settings"))
            {
                EngineSettings.saveToDisk(RenderState.settings);
                //Save Plugin Settings to Disk
                foreach (PluginBase plugin in RenderState.engineRef.Plugins.Values)
                {
                    plugin.Settings?.SaveToFile();
                }
                show_save_confirm_dialog = true;
            }

            if (show_save_confirm_dialog)
            {
                ImGuiCore.OpenPopup("settings-saved_success");
                show_save_confirm_dialog = false;
            }
            
            //Draw Plugin Modals
            foreach (PluginBase plugin in RenderState.engineRef.Plugins.Values)
            {
                plugin.Settings?.DrawModals();
            }
            
            if (ImGuiCore.BeginPopupModal("settings-saved_success"))
            {
                if (ImGuiCore.IsKeyPressed(ImGuiNET.ImGuiKey.Escape))
                {
                    ImGuiCore.CloseCurrentPopup();
                }
                ImGuiCore.Text("Settings Saved Successfully!");
                ImGuiCore.EndPopup();
            }

            ImGuiCore.EndChild();

        }

        ~ImGuiSettingsWindow()
        {

        }
    }
}
