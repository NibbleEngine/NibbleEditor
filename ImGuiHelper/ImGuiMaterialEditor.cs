using System;
using NbCore;
using NbCore.Common;
using NbCore.Math;
using NbCore.Systems;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace NibbleEditor
{
    public class ImGuiMaterialEditor
    {
        private NbMaterial _ActiveMaterial = null;
        private static int current_material_flag = 0;
        private static int current_material_sampler = 0;
        private int _SelectedId = -1;


        public void Draw()
        {
            var io = ImGui.GetIO();
            //Items
            List<NbMaterial> materialList = RenderState.engineRef.GetSystem<RenderingSystem>().MaterialMgr.Entities;
            string[] items = new string[materialList.Count];
            for (int i = 0; i < items.Length; i++)
                items[i] = materialList[i].Name == "" ? "Material_" + i : materialList[i].Name;

            if (ImGui.Combo("##1", ref _SelectedId, items, items.Length))
                _ActiveMaterial = materialList[_SelectedId];

            ImGui.SameLine();

            if (ImGui.Button("Add"))
            {
                string name = "Material_" + (new Random()).Next(0x1000, 0xFFFF).ToString();
                NbMaterial mat = new();
                mat.Name = name;
                RenderState.engineRef.RegisterEntity(mat);
                SetMaterial(mat);
            }
            ImGui.SameLine();
            if (ImGui.Button("Del"))
            {
                NbMaterial mat = _ActiveMaterial;
                SetMaterial(null);
                RenderState.engineRef.DestroyEntity(mat);
            }

            if (_ActiveMaterial is null)
            {
                ImGui.Text("NULL");
                return;
            }

            if (ImGui.BeginTable("##MatInfo", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Name");
                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("", ref _ActiveMaterial.Name, 30);
                    
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Class");
                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(-1);
                ImGui.Text(_ActiveMaterial.Class.ToString());

                //DiffuseColor
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Diffuse Color");
                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(-1);
                Vector4 c = new(_ActiveMaterial.DiffuseColor.Values.X,
                                _ActiveMaterial.DiffuseColor.Values.Y,
                                _ActiveMaterial.DiffuseColor.Values.Z,
                                _ActiveMaterial.DiffuseColor.Values.W);
                if (ImGui.InputFloat4("##DiffuseColor", ref c))
                {
                    _ActiveMaterial.DiffuseColor.Values.X = c.X;
                    _ActiveMaterial.DiffuseColor.Values.Y = c.Y;
                    _ActiveMaterial.DiffuseColor.Values.Z = c.Z;
                    _ActiveMaterial.DiffuseColor.Values.W = c.W;
                }

                //AmbientColor
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Ambient Color");
                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(-1);
                c = new(_ActiveMaterial.AmbientColor.Values.X,
                        _ActiveMaterial.AmbientColor.Values.Y,
                        _ActiveMaterial.AmbientColor.Values.Z,
                        _ActiveMaterial.AmbientColor.Values.W);
                if (ImGui.InputFloat4("##AmbientColor", ref c))
                {
                    _ActiveMaterial.AmbientColor.Values.X = c.X;
                    _ActiveMaterial.AmbientColor.Values.Y = c.Y;
                    _ActiveMaterial.AmbientColor.Values.Z = c.Z;
                    _ActiveMaterial.AmbientColor.Values.W = c.W;
                }

                //AmbientColor
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Specular Color");
                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(-1);
                c = new(_ActiveMaterial.SpecularColor.Values.X,
                        _ActiveMaterial.SpecularColor.Values.Y,
                        _ActiveMaterial.SpecularColor.Values.Z,
                        _ActiveMaterial.SpecularColor.Values.W);
                if (ImGui.InputFloat4("##SpecularColor", ref c))
                {
                    _ActiveMaterial.SpecularColor.Values.X = c.X;
                    _ActiveMaterial.SpecularColor.Values.Y = c.Y;
                    _ActiveMaterial.SpecularColor.Values.Z = c.Z;
                    _ActiveMaterial.SpecularColor.Values.W = c.W;
                }

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Shader");
                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(-1);

                List<Entity> shaderconfs = RenderState.engineRef.GetEntityTypeList(EntityType.ShaderConfig);
                string[] shaderconfItems = new string[shaderconfs.Count];
                    
                for (int i = 0; i < shaderconfs.Count; i++)
                    shaderconfItems[i] = ((NbShaderConfig)shaderconfs[i]).Name;
                    
                int currentShaderConfigId = _ActiveMaterial.Shader != null ? shaderconfs.IndexOf(_ActiveMaterial.Shader.GetShaderConfig()) : -1;
                if (ImGui.Combo("##MaterialShader", ref currentShaderConfigId, shaderconfItems, shaderconfs.Count))
                {
                    RenderState.engineRef.SetMaterialShader(_ActiveMaterial, shaderconfs[currentShaderConfigId] as NbShaderConfig);
                }

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Shader Hash");
                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(-1);
                ImGui.Text((_ActiveMaterial.Shader != null) ? 
                                        _ActiveMaterial.Shader.Hash.ToString() : "-1");
                ImGui.SameLine();
                if (ImGui.Button("Reload"))
                {
                    Console.WriteLine("Recompile Shader Here");
                }
                    
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("Flags");
                ImGui.TableSetColumnIndex(1);

                //Flags
                //Create string list of flags
                List<string> flags = new();
                List<NbMaterialFlagEnum> mat_flags = _ActiveMaterial.GetFlags();
                for (int i = 0; i < mat_flags.Count; i++)
                    flags.Add(mat_flags[i].ToString());

                string[] allflags = Enum.GetNames(typeof(NbMaterialFlagEnum));
                    
                //ImGuiNET.ImGui.SetNextItemWidth(-1);
                ImGui.Combo("##FlagSelector", ref current_material_flag, allflags, allflags.Length);
                ImGui.SameLine();

                //TODO Add combobox here with all the available flags that can be selected and added to the material
                if (ImGui.Button("Add"))
                {
                    NbMaterialFlagEnum new_flag = (NbMaterialFlagEnum) current_material_flag;
                    _ActiveMaterial.AddFlag(new_flag);
                    //Compile a new shader only if a shader exists
                    if (_ActiveMaterial.Shader != null)
                        RenderState.engineRef.SetMaterialShader(_ActiveMaterial, _ActiveMaterial.Shader.GetShaderConfig());
                }

                ImGui.SetNextItemWidth(-1);
                if (ImGui.BeginListBox("##FlagsListBox"))
                {
                    foreach (string flag in flags)
                    {
                        ImGui.Selectable(flag);

                        if (ImGui.BeginPopupContextItem(flag, ImGuiPopupFlags.MouseButtonRight))
                        {
                            if (ImGui.MenuItem("Remove ##flag"))
                            {
                                _ActiveMaterial.RemoveFlag((NbMaterialFlagEnum)Enum.Parse(typeof(NbMaterialFlagEnum), flag));

                                //Compile a new shader only if a shader exists
                                if (_ActiveMaterial.Shader != null)
                                    RenderState.engineRef.SetMaterialShader(_ActiveMaterial, _ActiveMaterial.Shader.GetShaderConfig());
                            }
                            ImGui.EndPopup();
                        }
                    }

                    ImGui.EndListBox();
                }
                    
                ImGui.EndTable();
            }

            //Samplers
            ImGuiTreeNodeFlags base_flags = ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.AllowItemOverlap;
            
            if (_ActiveMaterial.Samplers.Count == 0)
                base_flags |= ImGuiTreeNodeFlags.Leaf;
            
            Vector2 node_rect_pos = ImGui.GetCursorPos();

            bool samplers_node_open = ImGui.TreeNodeEx("##MatSamplers" + _ActiveMaterial.ID, base_flags, "Samplers");
            Vector2 node_rect_size = ImGui.GetItemRectSize();
            float button_width = node_rect_size.Y;
            float button_height = button_width;
            
            if (_ActiveMaterial.Shader != null)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(node_rect_pos.X + node_rect_size.X - button_width);
                ImGui.PushFont(io.Fonts.Fonts[1]);
                if (ImGui.Button($"+##MatSamplers", new Vector2(button_width, button_height)))
                {
                    Console.WriteLine($"Creating New sampler");
                    NbSampler new_sampler = new();
                    _ActiveMaterial.Samplers.Add(new_sampler);
                }
                ImGui.PopFont();
            }

            if (samplers_node_open)
            {
                for (int i = 0; i < _ActiveMaterial.Samplers.Count; i++)
                {
                    NbSampler current_sampler = _ActiveMaterial.Samplers[i];

                    node_rect_pos = ImGui.GetCursorPos();
                    bool node_open = ImGui.TreeNodeEx(current_sampler.Name + "###Sampler" + i, ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.AllowItemOverlap);
                    node_rect_size = ImGui.GetItemRectSize();
                    button_width = node_rect_size.Y;
                    button_height = button_width;
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(node_rect_pos.X + node_rect_size.X - button_width);
                    ImGui.PushFont(io.Fonts.Fonts[1]);
                    if (ImGui.Button($"-##Sampler{i}", new Vector2(button_width, button_height)))
                    {
                        Callbacks.Log(this, "Removing Sampler " + current_sampler.Name, LogVerbosityLevel.INFO);
                        _ActiveMaterial.RemoveSampler(current_sampler);
                    }
                    ImGui.PopFont();


                    if (node_open)
                    {
                        if (ImGui.BeginTable("##SamplerTable"+i, 2))
                        {
                            ImGui.TableSetupColumn("Info", ImGuiTableColumnFlags.WidthFixed, 120.0f);
                            ImGui.TableSetupColumn("Data");
                            //ImGuiNET.ImGui.TableSetColumnWidth(1, -1);

                            //Sampler Name
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text("Name");
                            ImGui.TableSetColumnIndex(1);
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputText("##SamplerName" + i, ref current_sampler.Name, 30);

                            //Image Preview
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text("Preview");
                            ImGui.TableSetColumnIndex(1);
                            NbTexture samplerTex = current_sampler.Texture;
                            if (samplerTex is not null && samplerTex.Data.target != NbTextureTarget.Texture2DArray)
                            {
                                ImGui.Image((IntPtr) samplerTex.texID, new Vector2(64, 64));

                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    if (samplerTex.Data.target != NbTextureTarget.Texture2DArray)
                                        ImGui.Image((IntPtr)samplerTex.texID, new Vector2(512, 512));
                                    ImGui.Text(current_sampler.Name);
                                    ImGui.Text(current_sampler.Texture.Path);
                                    ImGui.EndTooltip();
                                }
                            }

                            //Texture Selector
                            //Get All Textures
                            List<Entity> textureList = RenderState.engineRef.GetEntityTypeList(EntityType.Texture);
                            string[] textureItems = new string[textureList.Count];
                            for (int j = 0; j < textureItems.Length; j++)
                            {
                                NbTexture tex = (NbTexture)textureList[j];
                                textureItems[j] = tex.Path == "" ? "Texture_" + j : tex.Path;
                            }
                            

                            int currentTexImageID = textureList.IndexOf(samplerTex);
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text("Texture");
                            ImGui.TableSetColumnIndex(1);
                            ImGui.SetNextItemWidth(-1);
                            if (ImGui.Combo("##SamplerTexture" + i, ref currentTexImageID, textureItems, textureItems.Length))
                            {
                                current_sampler.Texture = (NbTexture) textureList[currentTexImageID];
                            }

                            if (samplerTex != null)
                            {
                                //Sampler ID
                                ImGui.TableNextRow();
                                ImGui.TableSetColumnIndex(0);
                                ImGui.Text("Sampler ID");
                                ImGui.TableSetColumnIndex(1);
                                ImGui.SetNextItemWidth(-1);
                                ImGui.Combo("##SamplerID" + i, ref current_sampler.SamplerID,
                                    new string[] { "0", "1", "2", "3", "4", "5", "6", "7" }, 8);
                            }
                                
                            if (_ActiveMaterial.Shader != null && samplerTex != null)
                            {
                                //Sampler Shader Binding
                                List<string> compatibleShaderBindings = new();
                                foreach (var pair in _ActiveMaterial.Shader.uniformLocations)
                                {
                                    if (pair.Value.type == NbUniformType.Sampler2D)
                                        compatibleShaderBindings.Add(pair.Key);
                                }

                                int currentShaderBinding = compatibleShaderBindings.IndexOf(current_sampler.ShaderBinding);
                                ImGui.TableNextRow();
                                ImGui.TableSetColumnIndex(0);
                                ImGui.Text("Shader Binding");
                                ImGui.TableSetColumnIndex(1);
                                ImGui.SetNextItemWidth(-1);
                                if (ImGui.Combo("##SamplerBinding", ref currentShaderBinding, compatibleShaderBindings.ToArray(),
                                    compatibleShaderBindings.Count))
                                {

                                    Console.WriteLine("Change sampler shader binding");
                                    current_sampler.ShaderBinding = compatibleShaderBindings[currentShaderBinding];
                                    _ActiveMaterial.UpdateSampler(current_sampler);
                                }
                            }


                            ImGui.EndTable();
                        }
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }
            

            base_flags = ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.AllowItemOverlap;
            if (_ActiveMaterial.Uniforms.Count == 0)
                base_flags |= ImGuiTreeNodeFlags.Leaf;

            node_rect_pos = ImGui.GetCursorPos();
            bool mat_uniform_tree_open = ImGui.TreeNodeEx("##MatUniforms" + _ActiveMaterial.ID, base_flags, "Uniforms");
            node_rect_size = ImGui.GetItemRectSize();
            button_width = node_rect_size.Y;
            button_height = button_width;
            
            
            if (_ActiveMaterial.Shader != null)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(node_rect_pos.X + node_rect_size.X - button_width);
                ImGui.PushFont(io.Fonts.Fonts[1]);

                if (ImGui.Button($"+##MatUniforms", new Vector2(button_width, button_height)))
                {
                    NbUniform uf = new();
                    _ActiveMaterial.Uniforms.Add(uf);
                    ImGui.End();
                }
                ImGui.PopFont();
            }
            
            if (mat_uniform_tree_open)
            {
                for (int i = 0; i < _ActiveMaterial.Uniforms.Count; i++)
                {
                    NbUniform current_uf = _ActiveMaterial.Uniforms[i];

                    node_rect_pos = ImGui.GetCursorPos();
                    bool node_open = ImGui.TreeNodeEx(current_uf.Name + "###Uniform" + i, ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.AllowItemOverlap);
                    node_rect_size = ImGui.GetItemRectSize();
                    button_width = node_rect_size.Y;
                    button_height = button_width;
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(node_rect_pos.X + node_rect_size.X - button_width);
                    ImGui.PushFont(io.Fonts.Fonts[1]);
                    if (ImGui.Button("-##Uniform{i}", new Vector2(button_width, button_height)))
                    {
                        Callbacks.Log(this, "Removing Uniform " + current_uf.Name, LogVerbosityLevel.INFO);
                        _ActiveMaterial.RemoveUniform(_ActiveMaterial.Uniforms[i]);
                    }
                    ImGui.PopFont();
                    

                    if (node_open)
                    {
                        if (ImGui.BeginTable("##UniformTable" + i, 2))
                        {
                            ImGui.TableSetupColumn("Info", ImGuiTableColumnFlags.WidthFixed, 120.0f);
                            ImGui.TableSetupColumn("Data");
                            //ImGui.TableSetColumnWidth(1, -1);

                            //Name
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text("Name");
                            ImGui.TableSetColumnIndex(1);
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputText("Name##Uniform" + i, ref current_uf.Name, 30);
                            
                            //Format
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text("Format");
                            ImGui.TableSetColumnIndex(1);
                            ImGui.SetNextItemWidth(-1);

                            List<string> formats = Enum.GetNames(typeof(NbUniformType)).ToList();
                            int currentFormat = formats.IndexOf(current_uf.Type.ToString());
                            if (ImGui.Combo("##UniformFormat" + i, ref currentFormat, formats.ToArray(), formats.Count))
                            {
                                current_uf.Type = (NbUniformType)currentFormat;
                                current_uf.ShaderBinding = "";
                                current_uf.ShaderLocation = -1;
                                _ActiveMaterial.ActiveUniforms.Remove(current_uf);
                            }

                            //Bind Status
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(1);
                            ImGui.SetNextItemWidth(-1);
                            
                            //Show local Values
                            //Values
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text("Values");
                            ImGui.TableSetColumnIndex(1);
                            ImGui.SetNextItemWidth(-1);

                            NbVector4 vec = new(current_uf.Values);
                            switch (current_uf.Type)
                            {
                                case NbUniformType.Float:
                                    {
                                        float val = vec.X;
                                        if (ImGui.DragFloat("##UniformValues" + i, ref val, 0.001f))
                                            current_uf.Values.X = val;
                                        break;
                                    }
                                case NbUniformType.Vector2:
                                    {
                                        Vector2 val = new(vec.X, vec.Y);
                                        if (ImGui.DragFloat2("##UniformValues" + i, ref val, 0.001f))
                                        {
                                            current_uf.Values.X = val.X;
                                            current_uf.Values.Y = val.Y;
                                        }
                                        break;
                                    }
                                case NbUniformType.Vector3:
                                    {
                                        Vector3 val = new(vec.X, vec.Y, vec.Z);
                                        if (ImGui.DragFloat3("##UniformValues" + i, ref val, 0.01f))
                                        {
                                            current_uf.Values.X = val.X;
                                            current_uf.Values.Y = val.Y;
                                            current_uf.Values.Z = val.Z;
                                        }
                                        break;
                                    }
                                case NbUniformType.Vector4:
                                    {
                                        Vector4 val = new(vec.X, vec.Y, vec.Z, vec.W);
                                        if (ImGui.DragFloat4("##UniformValues" + i, ref val, 0.01f))
                                        {
                                            current_uf.Values.X = val.X;
                                            current_uf.Values.Y = val.Y;
                                            current_uf.Values.Z = val.Z; 
                                            current_uf.Values.W = val.W;
                                        }
                                        break;
                                    }
                            }
                            
                                

                            //Sampler Shader Binding
                            List<string> compatibleShaderBindings = new();
                            foreach (var pair in _ActiveMaterial.Shader.uniformLocations)
                            {
                                if (pair.Value.type == current_uf.Type)
                                    compatibleShaderBindings.Add(pair.Key);
                            }

                            int currentShaderBinding = compatibleShaderBindings.IndexOf(current_uf.ShaderBinding);
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text("Shader Binding");
                            ImGui.TableSetColumnIndex(1);
                            ImGui.SetNextItemWidth(-1);
                            if (ImGui.Combo("##SamplerBinding", ref currentShaderBinding, compatibleShaderBindings.ToArray(),
                                compatibleShaderBindings.Count))
                            {
                                current_uf.ShaderBinding = compatibleShaderBindings[currentShaderBinding];
                                current_uf.ShaderLocation = _ActiveMaterial.Shader.uniformLocations[compatibleShaderBindings[currentShaderBinding]].loc;
                                if (!_ActiveMaterial.ActiveUniforms.Contains(current_uf))
                                    _ActiveMaterial.ActiveUniforms.Add(current_uf);
                            }

                            ImGui.EndTable();
                        }
                        ImGui.TreePop();
                    }
                    
                }
                ImGui.TreePop();
            }
            
        }

        public void SetMaterial(NbMaterial mat)
        {
            _ActiveMaterial = mat;
            List<NbMaterial> materialList = RenderState.engineRef.GetSystem<RenderingSystem>().MaterialMgr.Entities;
            _SelectedId = materialList.IndexOf(mat);
        }
    }
}
