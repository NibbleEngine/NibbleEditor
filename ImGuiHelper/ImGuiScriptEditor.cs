using System;
using NbCore;
using NbCore.Common;
using ImGuiCore = ImGuiNET.ImGui;
using System.Collections.Generic;
using NbCore.Platform.Graphics;
using System.Linq;
using System.IO;
using NibbleEditor;

namespace NbCore.UI.ImGui
{
    public class ImGuiScriptEditor
    {
        private AppImGuiManager _manager;
        private NbScriptAsset _ActiveScript = null;
        private int _SelectedId = -1;
        private string script_path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private OpenFileDialog openFileDialog;
        
        public ImGuiScriptEditor(AppImGuiManager manager)
        {
            _manager = manager;
            openFileDialog = new("script-open-file", ".cs", false); //Initialize OpenFileDialog
            openFileDialog.SetDialogPath(script_path);
        }

        public void Draw()
        {
            //Items
            List<Entity> scriptList = NbRenderState.engineRef.GetEntityTypeList(EntityType.Script);
            string[] items = new string[scriptList.Count];
            for (int i = 0; i < items.Length; i++)
            {
                NbScriptAsset tex = (NbScriptAsset) scriptList[i];
                items[i] = tex.Path == "" ? "Script_" + i : tex.Path;
            }

            if (ImGuiCore.Combo("##1", ref _SelectedId, items, items.Length))
            {
                _ActiveScript = scriptList[_SelectedId] as NbScriptAsset;
            }

            ImGuiCore.SameLine();

            if (ImGuiCore.Button("Add"))
            {
                openFileDialog.Open();
            }

            //Draw Open File Dialog
            if (openFileDialog.Draw(new() { X = 640, Y = 480 }))
            {
                script_path = Path.GetDirectoryName(openFileDialog.GetSelectedFile());
                NbScriptAsset script = NbRenderState.engineRef.CreateScriptAsset(openFileDialog.GetSelectedFile());
                NbRenderState.engineRef.RegisterEntity(script);
                SetScript(script);
            }

            ImGuiCore.SameLine();
            if (ImGuiCore.Button("Del"))
            {
                NbRenderState.engineRef.DestroyEntity(_ActiveScript);
                _SelectedId = -1;
                _ActiveScript = null;
            }

            if (_ActiveScript is null)
            {
                return;
            }


            if (ImGuiCore.BeginTable("##ScriptInfo", 2))
            {
                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("Path");
                ImGuiCore.TableSetColumnIndex(1);
                
                ImGuiCore.InputText("", ref _ActiveScript.Path, 30);

                if (ImGuiCore.IsItemHovered())
                {
                    ImGuiCore.BeginTooltip();
                    ImGuiCore.Text(_ActiveScript.Path);
                    ImGuiCore.EndTooltip();
                }

                ImGuiCore.SameLine();
                if (ImGuiCore.Button("Edit"))
                {
                    //Send the shader source to the text editor
                    _manager.TextEditFile(_ActiveScript.Path);
                }

                ImGuiCore.EndTable();
            }

#if DEBUG
            if (ImGuiCore.Button("Export Script"))
            {
                StreamWriter sw = new("test_script.nbscript");
                Newtonsoft.Json.JsonTextWriter writer = new Newtonsoft.Json.JsonTextWriter(sw);
                writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                _ActiveScript.Serialize(writer);
                sw.Close();
            }
#endif

        }

        public void SetScript(NbScriptAsset script)
        {
            _ActiveScript = script;
            List<Entity> scriptList = NbRenderState.engineRef.GetEntityTypeList(EntityType.Script);
            _SelectedId = scriptList.IndexOf(script);
        }
    }
}