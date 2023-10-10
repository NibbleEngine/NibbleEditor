﻿using ImGuiNET;
using System;
using NbCore;
using NbCore.UI.ImGui;
using System.Diagnostics.Contracts;
using NbCore.Platform.Windowing;

namespace NibbleEditor
{
    public abstract class ImGuiModal
    {
        public string Name;
        public bool IsOpen = false;
        public bool show_modal = false;
        public abstract void Draw();

        public void ShowModal()
        {
            show_modal = true;
        }

        protected ImGuiModal(string name)
        {
            Name = name;
        }

    }

    public delegate void ImGuiOpenFileTriggerEventHandler(string filepath);
    public class ImGuiOpenFileModal : ImGuiModal
    {
        public string current_file_path;
        public string filter;
        public ImGuiOpenFileTriggerEventHandler open_file_handler;
        
        public ImGuiOpenFileModal(string name, string cfp, string f) : base(name)
        {
            current_file_path = cfp;
            filter = f;
        }

        public override void Draw()
        {
            if (show_modal)
            {
                ImGui.OpenPopup(Name);
                show_modal = false;
            }

            var isOpen = true;
            var winsize = new System.Numerics.Vector2(800, 400);
            ImGui.SetNextWindowSize(winsize);
            if (ImGui.BeginPopupModal(Name, ref isOpen, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                var picker = FilePicker.GetFilePicker(this, current_file_path, filter);
                if (picker.Draw(new System.Numerics.Vector2(winsize.X, winsize.Y - 60)))
                {
                    Console.WriteLine(picker.SelectedFile);
                    current_file_path = picker.CurrentFolder;
                    open_file_handler?.Invoke(picker.SelectedFile);
                    FilePicker.RemoveFilePicker(this);
                }
                ImGui.EndPopup();
            }
        }
    }

    public class AppImGuiManager : ImGuiManager
    {
        //ImGui Variables
        private readonly ImGuiObjectViewer ObjectViewer;
        private readonly ImGuiSceneGraphViewer SceneGraphViewer;
        private readonly ImGuiMaterialEditor MaterialEditor = new();
        private readonly ImGuiShaderEditor ShaderEditor;
        private readonly ImGuiTextureEditor TextureEditor = new();
        private readonly ImGuiScriptEditor ScriptEditor;
        private readonly ImGuiTextEditor TextEditor;
        private readonly ImGuiAboutWindow AboutWindow = new();
        private readonly ImGuiSettingsWindow SettingsWindow = new();

        public ImGuiOpenFileModal OpenFileModal = new("open-scene", "", ".nb");
        private NbWindow WindowRef;

        private bool show_open_scene_dialog = false;
        private bool show_settings_window = false;
        private bool show_about_window = false;
        private bool show_test_components = false;
        
        
        public AppImGuiManager(Window win, Engine engine) : base(win)
        {
            SceneGraphViewer = new(this);
            ObjectViewer = new(this);
            TextEditor = new(this);
            ShaderEditor = new(this);
            ScriptEditor = new(this);
            SetWindowRef(win);
        }

        public void ShowSettingsWindow()
        {
            show_settings_window = true;
        }

        public void ShowAboutWindow()
        {
            show_about_window = true;
        }

        public void ShowTestComponents()
        {
            show_test_components = true;
        }

        public void ShowOpenSceneDialog()
        {
            OpenFileModal.ShowModal();
        }

        //Text Editor Related Methods
        public void DrawTextEditor()
        {
            TextEditor?.Draw();
        }

        public void TextEditFile(string path)
        {
            TextEditor.OpenFile(path);
        }

        //Script Editor Related Methods
        public void DrawScriptEditor()
        {
            ScriptEditor?.Draw();
        }


        //Texture Viewer Related Methods
        public void DrawTextureEditor()
        {
            TextureEditor?.Draw();
        }

        public void SetActiveTexture(NbTexture t)
        {
            TextureEditor.SetTexture(t);
        }

        //Material Viewer Related Methods
        public void DrawMaterialEditor()
        {
            MaterialEditor?.Draw();
        }

        public void SetActiveMaterial(Entity m)
        {
            if (m.HasComponent<MeshComponent>())
            {
                MeshComponent mc = m.GetComponent<MeshComponent>() as MeshComponent;
                MaterialEditor.SetMaterial(mc.Mesh.Material);
            }
        }

        //Shader Editor Related Methods
        public void DrawShaderEditor()
        {
            ShaderEditor?.Draw();
        }

        public void SetActiveShaderConfig(NbShaderConfig s)
        {
            ShaderEditor.SetShader(s);
        }

        //Object Viewer Related Methods

        public void DrawObjectInfoViewer()
        {
            ObjectViewer?.Draw();
        }

        public void SetObjectReference(SceneGraphNode m)
        {
            ObjectViewer.SetModel(m);
        }

        //SceneGraph Related Methods

        public void DrawSceneGraph()
        {
            SceneGraphViewer?.Draw();
        }

        public void PopulateSceneGraph(SceneGraph scn)
        {
            SceneGraphViewer.Init(scn.Root);
        }

        public void ClearSceneGraph()
        {
            SceneGraphViewer.Clear();
        }


        //SceneGraph Related Methods

        public override void ProcessModals(object ob, ref string current_file_path, ref bool OpenFileDialogFinished)
        {
            //Functionality

            OpenFileModal.Draw();

            if (show_about_window)
            {
                ImGui.OpenPopup("show-about");
                show_about_window = false;
            }

            if (show_settings_window)
            {
                ImGui.OpenPopup("show-settings");
                show_settings_window = false;
            }



            bool isOpen = true;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(290, 400), ImGuiCond.Always);
            if (ImGui.BeginPopupModal("show-about", ref isOpen, ImGuiWindowFlags.NoResize))
            {
                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    ImGui.CloseCurrentPopup();
                }

                AboutWindow.Draw();

                ImGui.EndPopup();
            }

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(600, 400), ImGuiCond.Once);
            if (ImGui.BeginPopupModal("show-settings", ref isOpen))
            {
                if (ImGui.IsKeyPressed(ImGuiKey.Escape) && ImGui.IsWindowHovered())
                {
                    ImGui.CloseCurrentPopup();
                }

                SettingsWindow.Draw();

                ImGui.EndPopup();
            }

        }

    }

    

    
    
}
