using System;
using System.Collections.Generic;
using NbCore.Common;
using ImGuiCore = ImGuiNET.ImGui;

namespace NbCore.UI.ImGui
{
    public class ImGuiShaderEditor
    {
        private NbShaderConfig ActiveShader = null;
        private int selectedShaderId = -1;
        private int selectedGSSource = -1;
        private int selectedTCSSource = -1;
        private int selectedTESSource = -1;
        private bool showSourceEditor = false;
        private bool Updated = false;
        private ImGuiShaderSourceEditor sourceEditor = new();

        public ImGuiShaderEditor()
        {
            
        }
        
        public void Draw()
        {
            //TODO: Make this static if possible or maybe maintain a list of shaders in the resource manager
            
            //Items
            List<Entity> shaderList = RenderState.engineRef.GetEntityTypeList(EntityType.ShaderConfig);
            string[] items = new string[shaderList.Count];
            for (int i = 0; i < items.Length; i++)
            {
                NbShaderConfig ss = (NbShaderConfig)shaderList[i];
                items[i] = ss.Name;
            }
            
            if (ImGuiCore.Combo("##1", ref selectedShaderId, items, items.Length))
            {
                SetShader((NbShaderConfig) shaderList[selectedShaderId]);
            }
            
            ImGuiCore.SameLine();

            if (ImGuiCore.Button("Add"))
            {
                string name = "ShaderConfig_" + (new Random()).Next(0x1000, 0xFFFF).ToString();
                NbShaderConfig conf = new();
                conf.Name = name;
                RenderState.engineRef.RegisterEntity(conf);
                SetShader(conf);
            }
            
            ImGuiCore.SameLine();
            if (ImGuiCore.Button("Del"))
            {
                NbShaderConfig conf = ActiveShader;
                ActiveShader = null;
                RenderState.engineRef.DestroyEntity(conf);
            }

            if (ActiveShader is null)
                return;

            if (ImGuiCore.BeginTable("##ShaderTable", 3))
            {
                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("Config Name");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.PushItemWidth(-1);
                ImGuiCore.InputText("##ShaderName", ref ActiveShader.Name, 30);

                //Cache ShaderSources
                List<Entity> shaderSourceList = RenderState.engineRef.GetEntityTypeList(EntityType.ShaderSource);
                string[] sourceItems = new string[shaderSourceList.Count];
                for (int i = 0; i < sourceItems.Length; i++)
                {
                    NbShaderSource ss = (NbShaderSource)shaderSourceList[i];
                    sourceItems[i] = ss.SourceFilePath;
                }

                int OriginalVSSourceIndex = -1;
                if (ActiveShader.Sources.ContainsKey(NbShaderSourceType.VertexShader))
                    OriginalVSSourceIndex = shaderSourceList.IndexOf(ActiveShader.Sources[NbShaderSourceType.VertexShader]);

                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("Vertex Shader");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.PushItemWidth(-1);
                if (ImGuiCore.Combo("##VSCombo", ref OriginalVSSourceIndex, sourceItems, sourceItems.Length))
                {
                    ActiveShader.AddSource(NbShaderSourceType.VertexShader,
                        (NbShaderSource)shaderSourceList[OriginalVSSourceIndex]);
                    Updated = true;
                }

                if (ImGuiCore.IsItemHovered() && OriginalVSSourceIndex != -1)
                    ImGuiCore.SetTooltip(sourceItems[OriginalVSSourceIndex]);

                ImGuiCore.PopItemWidth();
                ImGuiCore.TableSetColumnIndex(2);
                if (ImGuiCore.Button("Edit##1"))
                {
                    sourceEditor.SetShader(ActiveShader.Sources[NbShaderSourceType.VertexShader]);
                    showSourceEditor = true;
                }

                int OriginalFSSourceIndex = -1;
                if (ActiveShader.Sources.ContainsKey(NbShaderSourceType.FragmentShader))
                    OriginalFSSourceIndex = shaderSourceList.IndexOf(ActiveShader.Sources[NbShaderSourceType.FragmentShader]);
                
                
                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("Fragment Shader");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.PushItemWidth(-1);
                if(ImGuiCore.Combo("##FSCombo", ref OriginalFSSourceIndex, sourceItems, sourceItems.Length))
                {
                    ActiveShader.AddSource(NbShaderSourceType.FragmentShader, 
                        (NbShaderSource)shaderSourceList[OriginalFSSourceIndex]);
                    Updated = true;
                }

                if (ImGuiCore.IsItemHovered() && OriginalFSSourceIndex != -1)
                    ImGuiCore.SetTooltip(sourceItems[OriginalFSSourceIndex]);

                ImGuiCore.PopItemWidth();
                ImGuiCore.TableSetColumnIndex(2);
                if (ImGuiCore.Button("Edit##2"))
                {
                    sourceEditor.SetShader(ActiveShader.Sources[NbShaderSourceType.FragmentShader]);
                    showSourceEditor = true;
                }

                ImGuiCore.EndTable();
            }

            if (Updated)
            {
                
                if (ImGuiCore.Button("Recompile Shader"))
                {
                    Console.WriteLine("Shader recompilation not supported yet");
                }
            }

            if (ImGuiCore.Begin("Source Editor", ref showSourceEditor, ImGuiNET.ImGuiWindowFlags.NoScrollbar))
            {
                sourceEditor.Draw();
                ImGuiCore.End();
            }

        }

        public void SetShader(NbShaderConfig conf)
        {
            ActiveShader = conf;
            List<Entity> shaderList = RenderState.engineRef.GetEntityTypeList(EntityType.ShaderConfig);
            List<Entity> shaderSourceList = RenderState.engineRef.GetEntityTypeList(EntityType.ShaderSource);
            selectedShaderId = shaderList.IndexOf(conf);
        }
    }
    
    
}