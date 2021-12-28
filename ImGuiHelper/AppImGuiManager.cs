﻿using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System;
using NbCore;
using NbCore.UI.ImGui;

namespace NibbleEditor
{
    class AppImGuiManager : ImGuiManager
    {
        //ImGui Variables
        private readonly ImGuiObjectViewer ObjectViewer;
        private readonly ImGuiSceneGraphViewer SceneGraphViewer;
        private readonly ImGuiMaterialEditor MaterialEditor = new();
        private readonly ImGuiShaderEditor ShaderEditor = new();
        private readonly ImGuiAboutWindow AboutWindow = new();
        private readonly ImGuiSettingsWindow SettingsWindow = new();
        private bool show_open_file_dialog = false;
        private bool show_settings_window = false;
        private bool show_about_window = false;
        private bool show_test_components = false;
        
        
        public AppImGuiManager(Window win, Engine engine) : base(win, engine)
        {
            SceneGraphViewer = new(this);
            ObjectViewer = new(this);
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

        public void ShowOpenFileDialog()
        {
            show_open_file_dialog = true;
        }

        //SceneGraph Related Methods

        public override void ProcessModals(GameWindow win, ref string current_file_path, ref bool OpenFileDialogFinished)
        {
            //Functionality

            if (show_open_file_dialog)
            {
                ImGui.OpenPopup("open-file");
                show_open_file_dialog = false;
            }

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
            
            var isOpen = true;
            var winsize = new System.Numerics.Vector2(800, 400);
            ImGui.SetNextWindowSize(winsize);
            if (ImGui.BeginPopupModal("open-file", ref isOpen, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                var picker = FilePicker.GetFilePicker(win, current_file_path, ".SCENE.MBIN|.SCENE.EXML");
                if (picker.Draw(new System.Numerics.Vector2(winsize.X, winsize.Y - 60)))
                {
                    Console.WriteLine(picker.SelectedFile);
                    current_file_path = picker.CurrentFolder;
                    FilePicker.RemoveFilePicker(win);
                }
                ImGui.EndPopup();
            }

            
            if (ImGui.BeginPopupModal("show-about", ref isOpen, ImGuiWindowFlags.NoResize))
            {

                ImGuiNative.igSetNextWindowSize(new System.Numerics.Vector2(256 + 36, 256 + 60), ImGuiCond.Appearing);
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Escape)))
                {
                    ImGui.CloseCurrentPopup();
                }

                AboutWindow.Draw();

                ImGui.EndPopup();
            }

            if (ImGui.BeginPopupModal("show-settings", ref isOpen))
            {

                ImGuiNative.igSetNextWindowSize(new System.Numerics.Vector2(800, 256 + 60), ImGuiCond.Always);
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Escape)))
                {
                    ImGui.CloseCurrentPopup();
                }

                SettingsWindow.Draw();

                ImGui.EndPopup();
            }

        }

    }

    

    
    
}
