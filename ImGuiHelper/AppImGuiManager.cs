using ImGuiNET;
using System;
using NbCore;
using NbCore.UI.ImGui;
using System.Diagnostics.Contracts;
using NbCore.Platform.Windowing;

namespace NibbleEditor
{
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

        public OpenFileDialog OpenFileDlg = new("Open Scene###open-scene", ".nb");
        public SaveFileDialog SaveFileDlg = new("Save Scene###save-scene", 
                                            new string[] { "Nibble scene (.nb)" },
                                            new string[] {".nb"});

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

            //Set Handlers
            OpenFileDlg.OnFileSelect = new ImGuiSelectFileTriggerEventHandler(WindowRef.Engine.OpenScene);
            SaveFileDlg.OnFileSelect = new ImGuiSelectFileTriggerEventHandler(WindowRef.Engine.SaveActiveScene);

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
            OpenFileDlg.Open();
        }

        public void ShowSaveSceneDialog()
        {
            SaveFileDlg.Open();
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

        public SceneGraphNode GetSelectedObject()
        {
            return ObjectViewer.GetModel();
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

        public override void ProcessModals()
        {
            //Functionality

            OpenFileDlg.Draw(new(640,480));
            SaveFileDlg.Draw(new(640, 480));

            if (show_about_window)
            {
                ImGui.OpenPopup("About###show-about");
                show_about_window = false;
            }

            if (show_settings_window)
            {
                ImGui.OpenPopup("Settings###show-settings");
                show_settings_window = false;
            }


            bool isOpen = true;
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(290, 400), ImGuiCond.Always);
            if (ImGui.BeginPopupModal("About###show-about", ref isOpen, ImGuiWindowFlags.NoResize))
            {
                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    ImGui.CloseCurrentPopup();
                }

                AboutWindow.Draw();

                ImGui.EndPopup();
            }

            

            //Settings Window
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(600, 400), ImGuiCond.Once);
            if (ImGui.BeginPopupModal("Settings###show-settings", ref isOpen))
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
