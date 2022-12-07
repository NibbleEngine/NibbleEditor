using System;
using System.Collections.Generic;
using NbCore.Common;
using ImGuiCore = ImGuiNET.ImGui;

namespace NbCore.UI.ImGui
{
    public class ImGuiShaderSourceEditor
    {
        private NbShaderSource ActiveShaderSource = null;
        private string SourceText = "";
        private int selectedId = -1;

        public ImGuiShaderSourceEditor()
        {
            
        }
        
        public void Draw()
        {
            //TODO: Make this static if possible or maybe maintain a list of shaders in the resource manager
            
            //Items
            List<Entity> shaderSourceList = RenderState.engineRef.GetEntityTypeList(EntityType.ShaderSource);
            string[] items = new string[shaderSourceList.Count];
            for (int i = 0; i < items.Length; i++)
            {
                NbShaderSource ss = (NbShaderSource) shaderSourceList[i];
                items[i] = ss.SourceFilePath;
            }
                
            if (ImGuiCore.Combo("##1", ref selectedId, items, items.Length))
            {
                SetShader((NbShaderSource)shaderSourceList[selectedId]);
            }
                
            ImGuiCore.SameLine();

            if (ImGuiCore.Button("Add"))
            {
                Console.WriteLine("Todo Create Shader");
            }
            ImGuiCore.SameLine();
            if (ImGuiCore.Button("Del"))
            {
                Console.WriteLine("Todo Delete Shader");
            }

            if (ActiveShaderSource is null)
                return;

            ImGuiCore.InputTextMultiline("##2", ref SourceText, 50000,
                    new System.Numerics.Vector2(-1, -20));

            var io = ImGuiCore.GetIO();
            bool save_changes = false;
            if (io.WantCaptureKeyboard && ImGuiCore.IsKeyDown((int)NbKey.LeftCtrl) && ImGuiCore.IsKeyPressed((int) NbKey.S))
            {
                save_changes = true;    
            }

            save_changes |= ImGuiCore.Button("Save");
            
            if (save_changes)
            {
                Console.WriteLine($"Saving Changes to {ActiveShaderSource.SourceFilePath}");
                System.IO.File.WriteAllText(ActiveShaderSource.SourceFilePath, SourceText);
            }
        }

        public void SetShader(NbShaderSource conf)
        {
            ActiveShaderSource = conf;
            SourceText = conf.SourceText;
            List<Entity> shaderList = RenderState.engineRef.GetEntityTypeList(EntityType.ShaderSource);
            selectedId = shaderList.IndexOf(conf);
        }
    }
    
    
}