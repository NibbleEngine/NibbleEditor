using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImGuiNET;
using MathNet.Numerics.Interpolation;
using NbCore;
using NbCore.Systems;
using NbCore.UI.ImGui;
using ImGuiCore = ImGuiNET.ImGui;


namespace NibbleEditor
{
    public class ImGuiObjectViewer
    {
        private SceneGraphNode _model;
        private ImGuiManager _manager;
        
        //Imgui variables to reference 
        private int _selectedComponentId = -1;
        private string[] AttenuationTypes = new string[] {ATTENUATION_TYPE.LINEAR.ToString(),
                                                    ATTENUATION_TYPE.LINEAR_SQRT.ToString(),
                                                    ATTENUATION_TYPE.QUADRATIC.ToString(),
                                                    ATTENUATION_TYPE.CONSTANT.ToString()};
        private string[] LightTypes = new string[] {LIGHT_TYPE.POINT.ToString(),
                                                    LIGHT_TYPE.SPOT.ToString()};

        public ImGuiObjectViewer(ImGuiManager mgr)
        {
            _manager = mgr;
        }

        public void SetModel(SceneGraphNode m)
        {
            if (m == null)
                return;
            _model = m;
        }

        public SceneGraphNode GetModel()
        {
            return _model;
        }

        public void Draw()
        {

            //Assume that a Popup has begun
            //ImGui.Begin("Info", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

            

            if (_model != null)
            {
                if (_model.IsDisposed())
                {
                    _model = null;
                    return;
                }

                DrawModel();
            }
                
            //ImGui.End();
        
        }

        private void RequestNodeUpdateRecursive(SceneGraphNode n)
        {
            _manager.WindowRef.Engine.GetSystem<TransformationSystem>().RequestEntityUpdate(n);
            
            foreach (SceneGraphNode child in n.Children)
                RequestNodeUpdateRecursive(child);
        }

        private void DrawModel()
        {
            string[] avail_components = new string[] { "Script", "Collision" };

            //Name
            if (ImGuiCore.BeginTable("##NodeInfo", 2, ImGuiTableFlags.None))
            {
                ImGuiCore.TableSetupColumn("##NodeInfoTable_PathName", ImGuiTableColumnFlags.WidthFixed);
                ImGuiCore.TableSetupColumn("##NodeInfoTable_Path", ImGuiTableColumnFlags.WidthStretch);
                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("GUID");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.Text(_model.ID.ToString());
                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("Type");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.Text(_model.Type.ToString());
                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("LOD");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.Text("TODO");
                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("Name");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.PushItemWidth(-1.0f);
                ImGuiCore.InputText("##Name", ref _model.Name, 30);
                ImGuiCore.PopItemWidth();
                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("Add Component");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.PushItemWidth(-38.0f);
                ImGuiCore.Combo("##CompList", ref _selectedComponentId, avail_components, avail_components.Length);
                ImGuiCore.SameLine();
                if (ImGuiCore.Button("Add"))
                {
                    Console.WriteLine($"Adding Component {avail_components[_selectedComponentId]}");
                    //Add script Component
                    if (_selectedComponentId == 0)
                    {
                        ScriptComponent sc = new ScriptComponent();
                        _model.AddComponent<ScriptComponent>(sc);
                    }
                }
                ImGuiCore.PopItemWidth();

                ImGuiCore.EndTable();
            }

            //Draw Transform
            TransformData td = TransformationSystem.GetEntityTransformData(_model);
            if (ImGuiCore.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiCore.BeginTable("##TransformTable", 4, ImGuiTableFlags.None))
                {
                    //Draw TransformMatrix
                    bool transform_changed = false;
                    ImGuiCore.TableNextRow();
                    ImGuiCore.TableSetColumnIndex(0);
                    ImGuiCore.Text("Translation");
                    ImGuiCore.TableSetColumnIndex(1);
                    ImGuiCore.PushItemWidth(-1.0f);
                    transform_changed |= ImGuiCore.DragFloat("##TransX", ref td.TransX, 0.005f);
                    ImGuiCore.TableSetColumnIndex(2);
                    ImGuiCore.PushItemWidth(-1.0f);
                    transform_changed |= ImGuiCore.DragFloat("##TransY", ref td.TransY, 0.005f);
                    ImGuiCore.TableSetColumnIndex(3);
                    ImGuiCore.PushItemWidth(-1.0f);
                    transform_changed |= ImGuiCore.DragFloat("##TransZ", ref td.TransZ, 0.005f);
                    ImGuiCore.TableNextRow();
                    ImGuiCore.TableSetColumnIndex(0);
                    ImGuiCore.Text("Rotation");
                    ImGuiCore.TableSetColumnIndex(1);
                    transform_changed |= ImGuiCore.DragFloat("##RotX", ref td.RotX);
                    ImGuiCore.TableSetColumnIndex(2);
                    transform_changed |= ImGuiCore.DragFloat("##RotY", ref td.RotY);
                    ImGuiCore.TableSetColumnIndex(3);
                    transform_changed |= ImGuiCore.DragFloat("##RotZ", ref td.RotZ);
                    ImGuiCore.TableNextRow();
                    ImGuiCore.TableSetColumnIndex(0);
                    ImGuiCore.Text("Scale");
                    ImGuiCore.TableSetColumnIndex(1);
                    transform_changed |= ImGuiCore.DragFloat("##ScaleX", ref td.ScaleX, 0.005f);
                    ImGuiCore.TableSetColumnIndex(2);
                    transform_changed |= ImGuiCore.DragFloat("##ScaleY", ref td.ScaleY, 0.005f);
                    ImGuiCore.TableSetColumnIndex(3);
                    transform_changed |= ImGuiCore.DragFloat("##ScaleZ", ref td.ScaleZ, 0.005f);

                    if (transform_changed)
                        RequestNodeUpdateRecursive(_model);

                    ImGuiCore.EndTable();
                }
            }





            //Draw Components

            //SceneComponent
            if (_model.HasComponent<SceneComponent>())
            {
                SceneComponent sc = _model.GetComponent<SceneComponent>() as SceneComponent;

                if (ImGuiCore.CollapsingHeader("Scene Component", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    //Report Scene Statistics
                    ImGuiCore.Columns(2);
                    ImGuiCore.Text("Node Count");
                    ImGuiCore.Text("Light Count");
                    ImGuiCore.Text("Mesh Count");
                    ImGuiCore.Text("Joint Count");
                    ImGuiCore.NextColumn();
                    ImGuiCore.Text(sc.Nodes.Count.ToString());
                    ImGuiCore.Text(sc.LightNodes.Count.ToString());
                    ImGuiCore.Text(sc.MeshNodes.Count.ToString());
                    ImGuiCore.Text(sc.JointNodes.Count.ToString());
                    ImGuiCore.Columns(1);
                }
            }


            //MeshComponent
            if (_model.HasComponent<MeshComponent>())
            {
                foreach (MeshComponent mc in _model.GetComponents<MeshComponent>())
                {
                    if (ImGuiCore.CollapsingHeader("Mesh Component", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        if (ImGuiCore.BeginTable("##MeshInfo", 2))
                        {
                            ImGuiCore.TableNextRow();
                            ImGuiCore.TableSetColumnIndex(0);
                            ImGuiCore.Text("Instance ID");
                            ImGuiCore.TableSetColumnIndex(1);
                            ImGuiCore.Text(mc.InstanceID.ToString());
                            ImGuiCore.TableNextRow();
                            ImGuiCore.TableSetColumnIndex(0);
                            ImGuiCore.Text("MeshGroup ID");
                            ImGuiCore.TableSetColumnIndex(1);
                            ImGuiCore.Text(mc.Mesh.Group != null ? mc.Mesh.Group.ID.ToString() : "-1");
                            ImGuiCore.TableNextRow();
                            ImGuiCore.TableSetColumnIndex(0);
                            ImGuiCore.Text("Material");
                            ImGuiCore.TableSetColumnIndex(1);

                            //Items
                            List<Entity> materialList = _manager.WindowRef.Engine.GetEntityTypeList(EntityType.Material);
                            materialList = materialList.FindAll(x => ((NbMaterial)x).Shader != null);
                            materialList = materialList.FindAll(x => ((NbMaterial)x).Shader.ProgramID != 0);

                            string[] items = new string[materialList.Count];
                            for (int i = 0; i < items.Length; i++)
                            {
                                NbMaterial mm = (NbMaterial)materialList[i];
                                items[i] = mm.Name == "" ? "Material_" + i : mm.Name;
                            }

                            int material_id = materialList.IndexOf(mc.Mesh.Material);

                            if (ImGuiCore.Combo("##MaterialCombo", ref material_id, items, items.Length))
                                mc.Mesh.Material = (NbMaterial)materialList[material_id];

                            ImGuiCore.EndTable();
                        }

                        if (ImGuiCore.TreeNode("Mesh Uniforms"))
                        {
                            
                            if (ImGuiCore.BeginTable("##MeshUniforms", 2))
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    ImGuiCore.TableNextRow();
                                    ImGuiCore.TableSetColumnIndex(0);
                                    ImGuiCore.Text("Uniform " + i);
                                    ImGuiCore.TableSetColumnIndex(1);
                                    ImGuiCore.PushItemWidth(-1.0f);
                                    NbVector4 uf = mc.InstanceUniforms[i].Values;
                                    var val = new System.Numerics.Vector4();
                                    val.X = uf.X;
                                    val.Y = uf.Y;
                                    val.Z = uf.Z;
                                    val.W = uf.W;

                                    if (ImGuiCore.InputFloat4($"##uf{i}", ref val))
                                    {
                                        mc.InstanceUniforms[i].Values.X = val.X;
                                        mc.InstanceUniforms[i].Values.Y = val.Y;
                                        mc.InstanceUniforms[i].Values.Z = val.Z;
                                        mc.InstanceUniforms[i].Values.W = val.W;
                                        mc.IsUpdated = true;
                                    }

                                    ImGuiCore.PopItemWidth();
                                }
                                ImGuiCore.EndTable();
                            }


                            ImGuiCore.TreePop();
                        }

                        

                        if (ImGuiCore.TreeNode("Mesh"))
                        {
                            NbMesh mesh = mc.Mesh;
                            ImGuiCore.Columns(2);
                            ImGuiCore.Text("Hash");
                            ImGuiCore.NextColumn();
                            ImGuiCore.Text(mesh.Hash.ToString());
                            ImGuiCore.NextColumn();
                            ImGuiCore.Text("Instance Count");
                            ImGuiCore.NextColumn();
                            ImGuiCore.Text(mesh.InstanceCount.ToString());
                            ImGuiCore.Columns(1);
                            if (ImGuiCore.TreeNode("MetaData"))
                            {
                                ImGuiCore.Columns(2);
                                ImGuiCore.Text("BatchCount");
                                ImGuiCore.Text("Vertex Start Graphics");
                                ImGuiCore.Text("Vertex End Graphics");
                                ImGuiCore.NextColumn();
                                
                                ImGuiCore.Text(mesh.MetaData.BatchCount.ToString());
                                ImGuiCore.Text(mesh.MetaData.VertrStartGraphics.ToString());
                                ImGuiCore.Text(mesh.MetaData.VertrEndGraphics.ToString());
                                ImGuiCore.TreePop();
                            }
                            ImGuiCore.Columns(1);
                            ImGuiCore.TreePop();
                        }


#if DEBUG
                        if (ImGuiCore.Button("Export Mesh"))
                        {
                            StreamWriter sw = new("test_mesh.nbmesh");
                            Newtonsoft.Json.JsonTextWriter writer = new Newtonsoft.Json.JsonTextWriter(sw);
                            writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                            mc.Mesh.Serialize(writer);
                            sw.Close();

                            sw = new("test_mesh_data.nbmeshdata");
                            writer = new Newtonsoft.Json.JsonTextWriter(sw);
                            writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                            mc.Mesh.Data.Serialize(writer);
                            sw.Close();
                        }
#endif

                    }
                }
            }
                
            //LightComponent
            if (_model.HasComponent<LightComponent>())
            {
                LightComponent lc = _model.GetComponent<LightComponent>();

                if (ImGuiCore.CollapsingHeader("Light Component", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    bool light_updated = false;
                    ImGuiCore.Columns(2);
                    ImGuiCore.Text("Light Type");
                    ImGuiCore.NextColumn();
                    int ref_LightTypeId = Array.IndexOf(LightTypes, lc.Data.LightType.ToString());
                    ImGuiCore.SetNextItemWidth(-1.0f);
                    if (ImGuiCore.Combo("##LightTypeCombo", ref ref_LightTypeId, LightTypes, LightTypes.Length))
                    {
                        lc.Data.LightType = (LIGHT_TYPE) Enum.Parse(typeof(LIGHT_TYPE), LightTypes[ref_LightTypeId]);
                        light_updated = true;
                    }
                    ImGuiCore.NextColumn();
                    ImGuiCore.Text("Intensity");
                    ImGuiCore.NextColumn();
                    ImGuiCore.SetNextItemWidth(-1.0f);
                    if (ImGuiCore.DragFloat("##Intensity", ref lc.Data.Intensity, 2.5f, 0.0f, 1000000.0f))
                        light_updated = true;
                    ImGuiCore.NextColumn();
                    ImGuiCore.Text("InnerCutoff");
                    ImGuiCore.NextColumn();
                    ImGuiCore.SetNextItemWidth(-1.0f);
                    if (ImGuiCore.DragFloat("##innerCutOff", ref lc.Data.InnerCutOff, 0.01f, 0.0f, System.Math.Min(180.0f, lc.Data.OutterCutOff)))
                        light_updated = true;
                    ImGuiCore.NextColumn();
                    ImGuiCore.Text("OutterCutoff");
                    ImGuiCore.NextColumn();
                    ImGuiCore.SetNextItemWidth(-1.0f);
                    if (ImGuiCore.DragFloat("##outterCutOff", ref lc.Data.OutterCutOff, 0.01f, System.Math.Max(0.0f, lc.Data.InnerCutOff), 180.0f))
                        light_updated = true;
                    
                    ImGuiCore.NextColumn();
                    ImGuiCore.Text("IsRenderable");
                    ImGuiCore.NextColumn();
                    ImGuiCore.SetNextItemWidth(-1.0f);
                    if (ImGuiCore.Checkbox("##renderable", ref lc.Data.IsRenderable))
                        light_updated = true;
                    ImGuiCore.NextColumn();
                    ImGuiCore.Text("FallOff");
                    ImGuiCore.NextColumn();

                    int ref_FalloffId = Array.IndexOf(AttenuationTypes, lc.Data.Falloff.ToString());
                    ImGuiCore.SetNextItemWidth(-1.0f);
                    if (ImGuiCore.Combo("##FallOffCombo", ref ref_FalloffId, AttenuationTypes, AttenuationTypes.Length))
                    {
                        lc.Data.Falloff = (ATTENUATION_TYPE)Enum.Parse(typeof(ATTENUATION_TYPE), AttenuationTypes[ref_FalloffId]);
                        light_updated = true;
                    }
                    ImGuiCore.NextColumn();
                    ImGuiCore.Text("FallOff Radius");
                    ImGuiCore.NextColumn();
                    ImGuiCore.SetNextItemWidth(-1.0f);
                    if (ImGuiCore.DragFloat("##FallOffRadius", ref lc.Data.Falloff_radius, 0.005f, 0.01f))
                        light_updated = true;
                    
                    ImGuiCore.NextColumn();
                    ImGuiCore.Text("Color");
                    ImGuiCore.NextColumn();
                    
                    System.Numerics.Vector3 v = new(lc.Data.Color.X, lc.Data.Color.Y, lc.Data.Color.Z);
                    ImGuiCore.SetNextItemWidth(-1.0f);
                    if (ImGuiCore.ColorEdit3("##Color", ref v, ImGuiColorEditFlags.NoSidePreview))
                    {
                        lc.Data.Color = new(v.X, v.Y, v.Z);
                        light_updated = true;
                    }

                    ImGuiCore.NextColumn();
                    ImGuiCore.Text("Direction");
                    ImGuiCore.NextColumn();

                    v = new(lc.Data.Direction.X, lc.Data.Direction.Y, lc.Data.Direction.Z);
                    ImGuiCore.SetNextItemWidth(-1.0f);
                    if (ImGuiCore.DragFloat3("##Direction", ref v, 0.5f))
                    {
                        lc.Data.Direction = new(v.X, v.Y, v.Z);
                        light_updated = true;
                    }

                    ImGuiCore.Columns(1);

                    if (light_updated)
                        lc.Data.IsUpdated = true;
                }

            }

            //ImposterComponent
            if (_model.HasComponent<ImposterComponent>())
            {
                ImposterComponent cc = _model.GetComponent<ImposterComponent>();

                if (ImGuiCore.CollapsingHeader("Imposter Component", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGuiCore.Columns(2);
                    ImGuiCore.Text("Image ID");
                    ImGuiCore.NextColumn();
                    ImGuiCore.InputInt("##ImposterComponentImageID", ref cc.Data.ImageID);
                    ImGuiCore.NextColumn();
                    ImGuiCore.Text("Size");
                    ImGuiCore.NextColumn();
                    System.Numerics.Vector2 v2 = new(cc.Data.Width, cc.Data.Height);
                    if (ImGuiCore.InputFloat2("##ImposterComponentSize", ref v2))
                    {
                        cc.Data.IsUpdated = true;
                        cc.Data.Width = v2.X;
                        cc.Data.Height = v2.Y;
                    }
                    ImGuiCore.NextColumn();
                    ImGuiCore.Text("Color");
                    ImGuiCore.NextColumn();
                    System.Numerics.Vector3 v3 = new(cc.Data.Color.X, cc.Data.Color.Y, cc.Data.Color.Z);
                    if (ImGuiCore.InputFloat3("##ImposterComponentColor", ref v3))
                    {
                        cc.Data.IsUpdated = true;
                        cc.Data.Color.X = v3.X;
                        cc.Data.Color.Y = v3.Y;
                        cc.Data.Color.Z = v3.Z;
                    }
                    ImGuiCore.Columns(1);
                }
            }

            //CollisionComponent
            if (_model.HasComponent<CollisionComponent>())
            {
                CollisionComponent cc = _model.GetComponent<CollisionComponent>();

                if (ImGuiCore.CollapsingHeader("Collision Component", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGuiCore.Columns(2);
                    ImGuiCore.Text("CollisionType");
                    ImGuiCore.NextColumn();
                    ImGuiCore.Text(cc.CollisionType.ToString());
                    ImGuiCore.Columns(1);
                }
            }

            //ReferenceComponent
            if (_model.HasComponent<ReferenceComponent>())
            {
                ReferenceComponent rc = _model.GetComponent<ReferenceComponent>();

                if (ImGuiCore.CollapsingHeader("Reference Component", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGuiCore.Columns(2);
                    ImGuiCore.Text("Reference");
                    ImGuiCore.NextColumn();
                    ImGuiCore.Text(rc.Reference.ToString());
                    ImGuiCore.Columns(1);
                }
            }

            //JointComponent
            if (_model.HasComponent<JointComponent>())
            {
                JointComponent jc = _model.GetComponent<JointComponent>();

                if (ImGuiCore.CollapsingHeader("Joint Component", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGuiCore.Columns(2);
                    ImGuiCore.Text("JointIndex");
                    ImGuiCore.NextColumn();
                    ImGuiCore.InputInt("##JointIndex", ref jc.JointIndex);
                    ImGuiCore.Columns(1);
                }
            }

            //AnimationComponent
            if (_model.HasComponent<AnimComponent>())
            {
                AnimComponent ac = _model.GetComponent<AnimComponent>();

                float lineheight = ImGuiNET.ImGui.GetTextLineHeight();
                if (ImGuiCore.CollapsingHeader("Animation Component", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGuiCore.TreeNode("Animations"))
                    {
                        if (ImGuiCore.BeginTable("##AnimationsTable", 3, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersOuter))
                        {
                            ImGuiCore.TableSetupColumn("Name");
                            ImGuiCore.TableSetupColumn("Actions");
                            ImGuiCore.TableSetupColumn("Frame");
                            ImGuiCore.TableHeadersRow();

                            foreach (Animation anim in ac.AnimGroup.Animations)
                            {
                                ImGuiCore.TableNextRow();
                                ImGuiCore.TableSetColumnIndex(0);
                                ImGuiCore.Selectable(anim.animData.MetaData.Name);
                                ImGuiCore.TableSetColumnIndex(1);
                                string button_title = anim.IsPlaying ? "Stop##" + anim.animData.MetaData.Name :
                                    "Play##" + anim.animData.MetaData.Name;
                                
                                if (ImGuiCore.Button(button_title))
                                {
                                    if (anim.IsPlaying == false && anim.animData.MetaData.AnimType == AnimationType.OneShot)
                                        anim.ActiveFrameIndex = 0;
                                    anim.IsPlaying = !anim.IsPlaying;
                                    ac.AnimGroup.ActiveAnimation = anim;
                                };
                                ImGuiCore.TableSetColumnIndex(2);
                                ImGuiCore.Checkbox("##Override" + anim.animData.MetaData.Name, ref anim.Override);
                                ImGuiCore.SameLine();


                                ImGuiCore.BeginDisabled(!anim.Override);
                                
                                int temp_frame = anim.ActiveFrameIndex;
                                if (ImGuiCore.SliderInt("##AnimFrame" + anim.animData.MetaData.Name, ref temp_frame, 
                                    0, anim.animData.FrameCount - 1))
                                {
                                    anim.SetFrame(temp_frame); //Kinda stupid but whatever
                                }

                                ImGuiCore.EndDisabled();
                                
                            }

                            ImGuiCore.EndTable();
                        }

                        ImGuiCore.TreePop();
                    }
                    
                }
            }
            
            //ScriptComponent
            if (_model.HasComponent<ScriptComponent>())
            {
                int list_index = 0;
                bool break_at_end_of_loop = false;
                foreach (ScriptComponent sc in _model.GetComponents<ScriptComponent>())
                {
                    float lineheight = ImGuiCore.GetTextLineHeight();

                    System.Numerics.Vector2 node_rect_pos = ImGuiCore.GetCursorPos();
                    bool header_open = ImGuiCore.CollapsingHeader("Script Component##" + list_index, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.AllowOverlap);
                    System.Numerics.Vector2 node_rect_size = ImGuiCore.GetItemRectSize();
                    float button_width = node_rect_size.Y;
                    float button_height = button_width;
                    ImGuiCore.SameLine();
                    ImGuiCore.SetCursorPosX(node_rect_pos.X + node_rect_size.X - button_width - 3);
                    ImGuiCore.PushFont(ImGuiCore.GetIO().Fonts.Fonts[1]);
                    if (ImGuiCore.Button("-##ScriptComponent" + list_index, new System.Numerics.Vector2(button_width, button_height)))
                    {
                        NbCore.Common.Callbacks.Log(this, "REMOVING SCRIPT COMPONENT", LogVerbosityLevel.INFO);
                        _manager.WindowRef.Engine.RemoveScriptComponentFromNode(_model, sc);
                        break_at_end_of_loop = true;
                    }
                    ImGuiCore.PopFont();

                    if (header_open)
                    {
                        if (ImGuiCore.BeginTable("##ScriptCompTable" + list_index, 3, ImGuiTableFlags.None))
                        {
                            ImGuiCore.TableSetupColumn("##ScriptCompTable_Label" + list_index, ImGuiTableColumnFlags.WidthFixed);
                            ImGuiCore.TableSetupColumn("##ScriptCompTable_Selection" + list_index, ImGuiTableColumnFlags.WidthStretch);

                            ImGuiCore.TableNextRow();
                            ImGuiCore.TableSetColumnIndex(0);
                            ImGuiCore.Text("Script: ");
                            ImGuiCore.TableSetColumnIndex(1);

                            //Items
                            List<Entity> scriptList = _manager.WindowRef.Engine.GetEntityTypeList(EntityType.Script);
                            string[] items = new string[scriptList.Count];
                            for (int i = 0; i < items.Length; i++)
                            {
                                NbScriptAsset mm = (NbScriptAsset) scriptList[i];
                                items[i] = mm.Path;
                            }

                            int script_id = scriptList.IndexOf(sc.Asset);

                            if (ImGuiCore.Combo("##ScriptCombo" + list_index, ref script_id, items, items.Length))
                            {
                                if (sc.Script != null)
                                {
                                    _manager.WindowRef.Engine.GetSystem<ScriptingSystem>().Remove(sc);
                                }
                                sc.Asset = (NbScriptAsset) scriptList[script_id];
                                sc.Script = _manager.WindowRef.Engine.CreateScript(sc);
                                _manager.WindowRef.Engine.GetSystem<ScriptingSystem>().RegisterEntity(sc);
                            }


                            ImGuiCore.EndTable();

                            if (sc.Script != null)
                            {
                                if (ImGuiCore.TreeNode("Properties##" + list_index))
                                {

                                    //Load dynamic properties from the script object using reflection\
                                    Type scriptType = sc.Script.GetType();
                                    PropertyInfo[] properties = scriptType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);

                                    for (int i = 0; i < properties.Length; i++)
                                    {
                                        PropertyInfo pInfo = properties[i];

                                        if (pInfo.PropertyType == typeof(int))
                                        {
                                            //Draw int property
                                            int val = (int)pInfo.GetValue(sc.Script);
                                            if (ImGuiCore.InputInt(pInfo.Name + "##" + list_index, ref val))
                                                pInfo.SetValue(sc.Script, val);
                                        }
                                        else if (pInfo.PropertyType == typeof(float))
                                        {
                                            //Draw float property
                                            float val = (float)pInfo.GetValue(sc.Script);
                                            if (ImGuiCore.InputFloat(pInfo.Name + "##" + list_index, ref val))
                                                pInfo.SetValue(sc.Script, val);
                                        }
                                    }


                                    ImGuiCore.TreePop();
                                }
                            }
                            
                        }
                    }

                    list_index++;
                    if (break_at_end_of_loop)
                        break;
                }
            }
        

        }

        ~ImGuiObjectViewer()
        {

        }



    }
}
