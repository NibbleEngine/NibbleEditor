using System;
using System.IO;
using System.Collections.Generic;
using NbCore;
using OpenTK.Windowing.Common;
using ImGuiNET;
using NbCore.Common;
using NbCore.Math;
using NbCore.Plugins;
using NbCore.UI.ImGui;
using OpenTK.Graphics.OpenGL4;

namespace NibbleEditor
{
    public delegate void CloseWindowEventHandler();
    public delegate void CaptureInputHandler(bool state);

    public class UILayer : ApplicationLayer
    {
        private AppImGuiManager _ImGuiManager;

        //ImGui stuff
        private NbVector2i SceneViewSize = new();
        private NbVector2i WindowSize = new();
        private bool firstDockSetup = true;
        
        static private bool IsOpenFileDialogOpen = false;
        private string current_file_path = Environment.CurrentDirectory;
        
        //Events
        public event CloseWindowEventHandler CloseWindowEvent;
        public event CaptureInputHandler CaptureInput;

        public UILayer(Window win, Engine e) : base(e)
        {
            //Initialize ImGuiManager
            _ImGuiManager = new(win, e);

            EngineRef.NewSceneEvent += new Engine.NewSceneEventHandler(OnNewScene);

            //Load Settings
            if (!File.Exists("settings.json"))
                _ImGuiManager.ShowSettingsWindow();
        }

        //Event Handlers
        public void OnNewScene(SceneGraph s)
        {
            _ImGuiManager.PopulateSceneGraph(s);
        }

        public void OnResize(ResizeEventArgs e)
        {
            // Tell ImGui of the new size
            _ImGuiManager.Resize(e.Width, e.Height);
            WindowSize = new(e.Width, e.Height);
        }

        public void OnTextInput(TextInputEventArgs e)
        {
            _ImGuiManager.SendChar((char)e.Unicode);
        }

        public override void OnFrameUpdate(ref Queue<object> data, double dt)
        {
            //Fetch state from data
            NbMouseState mouseState = (NbMouseState) data.Dequeue();
            NbKeyboardState keyboardState = (NbKeyboardState) data.Dequeue();

            //Send Input
            //TODO: Move to UI Layer
            _ImGuiManager.SetMouseState(mouseState);
            _ImGuiManager.SetKeyboardState(keyboardState);
            
            RenderState.activeCam.aspect = (float)SceneViewSize.X / SceneViewSize.Y;
            RenderState.activeCam.updateViewMatrix();

        }

        public override void OnRenderFrameUpdate(ref Queue<object> data, double dt)
        {
            //Bind Default Framebuffer
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.Viewport(0, 0, WindowSize.X, WindowSize.Y);

            _ImGuiManager.Update(dt);

            //UI
            DrawUI();


            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            //ImGui.ShowDemoWindow();
            _ImGuiManager.Render();

            //ImGuiUtil.CheckGLError("End of frame");

        }


        public void TestDrawUI()
        {
            //Enable docking in main view
            ImGuiDockNodeFlags dockspace_flags = ImGuiDockNodeFlags.None;
            dockspace_flags |= ImGuiDockNodeFlags.PassthruCentralNode;

            ImGuiWindowFlags window_flags = ImGuiWindowFlags.NoBackground |
                                            ImGuiWindowFlags.NoCollapse |
                                            ImGuiWindowFlags.NoResize |
                                            ImGuiWindowFlags.NoDocking;

            ImGuiViewportPtr vp = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(vp.WorkPos);
            ImGui.SetNextWindowSize(vp.WorkSize);
            ImGui.SetNextWindowViewport(vp.ID);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, 0.0f);

            bool keep_window_open = true;
            int statusBarHeight = (int)(1.75f * ImGui.CalcTextSize("Status").Y);
            ImGui.Begin("MainWindow", ref keep_window_open, window_flags);
            ImGui.PopStyleVar(2);

            //Debugging Information
            if (ImGui.Begin("Statistics"))
            {
                ImGui.Text("test");
                ImGui.Text("test");
                ImGui.Text("test");
                ImGui.End();
            }
        }

        public void DrawUI()
        {
            //Enable docking in main view
            ImGuiDockNodeFlags dockspace_flags = ImGuiDockNodeFlags.None;
            dockspace_flags |= ImGuiDockNodeFlags.PassthruCentralNode;

            ImGuiWindowFlags window_flags = ImGuiWindowFlags.NoBackground |
                                            ImGuiWindowFlags.NoCollapse |
                                            ImGuiWindowFlags.NoResize |
                                            ImGuiWindowFlags.NoTitleBar |
                                            ImGuiWindowFlags.NoDocking;

            ImGuiViewportPtr vp = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(vp.WorkPos);
            ImGui.SetNextWindowSize(vp.WorkSize);
            ImGui.SetNextWindowViewport(vp.ID);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, new System.Numerics.Vector2(0.0f, 0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, new System.Numerics.Vector2(0.0f, 0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0.0f, 0.0f));
            
            bool keep_window_open = true;
            int statusBarHeight = (int)(1.75f * ImGui.CalcTextSize("Status").Y);
            ImGui.Begin("##MainWindow", ref keep_window_open, window_flags);

            ImGui.PopStyleVar(1);

            uint dockSpaceID = ImGui.GetID("MainDockSpace");
            //System.Numerics.Vector2 dockSpaceSize = vp.GetWorkSize();
            System.Numerics.Vector2 dockSpaceSize = new(0.0f, -statusBarHeight);
            ImGui.DockSpace(dockSpaceID, dockSpaceSize, dockspace_flags);

            

            unsafe
            {
                if (firstDockSetup)
                {
                    firstDockSetup = false;
                    dockSpaceID = ImGui.GetID("MainDockSpace");
                    ImGuiNative.igDockBuilderRemoveNode(dockSpaceID);
                    ImGuiNative.igDockBuilderAddNode(dockSpaceID, dockspace_flags);
                    ImGuiNative.igDockBuilderSetNodeSize(dockSpaceID, dockSpaceSize);

                    //Add Right dock
                    uint temp;
                    //uint dockSpaceLeft;
                    uint dockSpaceRight = ImGuiNative.igDockBuilderSplitNode(dockSpaceID, ImGuiDir.Right, 0.3f,
                        null, &temp);
                    dockSpaceID = temp; //Temp holds the main view

                    uint dockSpaceRightDown =
                        ImGuiNative.igDockBuilderSplitNode(dockSpaceRight, ImGuiDir.Down,
                            0.5f, null, &dockSpaceRight);
                    uint dockSpaceRightUp = dockSpaceRight;

                    uint dockSpaceLeftDown = ImGuiNative.igDockBuilderSplitNode(dockSpaceID, ImGuiDir.Down, 0.1f,
                        null, &temp);
                    dockSpaceID = temp; //Temp holds the main view


                    //Set Window Docks
                    //Left 
                    ImGui.DockBuilderDockWindow("Scene", dockSpaceID);
                    ImGui.DockBuilderDockWindow("Statistics", dockSpaceLeftDown);
                    //Right
                    ImGui.DockBuilderDockWindow("SceneGraph", dockSpaceRightUp);
                    ImGui.DockBuilderDockWindow("Camera", dockSpaceRightUp);
                    ImGui.DockBuilderDockWindow("Options", dockSpaceRightUp);
                    ImGui.DockBuilderDockWindow("Test Options", dockSpaceRightUp);
                    ImGui.DockBuilderDockWindow("Tools", dockSpaceRightUp);
                    ImGui.DockBuilderDockWindow("Node Editor", dockSpaceRightDown);
                    ImGui.DockBuilderDockWindow("Shader Editor", dockSpaceRightDown);
                    ImGui.DockBuilderDockWindow("Material Editor", dockSpaceRightDown);
                    ImGui.DockBuilderDockWindow("Texture Editor", dockSpaceRightDown);

                    ImGuiNative.igDockBuilderFinish(dockSpaceID);
                }
            }


            //Main Menu
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New", "Ctrl + N"))
                    {
                        Log("Create new scene not implemented yet", LogVerbosityLevel.INFO);
                    }

                    if (ImGui.MenuItem("Open", "Ctrl + O"))
                    {
                        Log("Open scene not implemented yet", LogVerbosityLevel.INFO);
                    }

                    if (ImGui.BeginMenu("Import"))
                    {
                        foreach (PluginBase plugin in EngineRef.Plugins.Values)
                            plugin.DrawImporters();
                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Export"))
                    {
                        foreach (PluginBase plugin in EngineRef.Plugins.Values)
                            plugin.DrawExporters(EngineRef.GetActiveSceneGraph());
                        ImGui.EndMenu();
                    }

                    if (ImGui.MenuItem("Settings"))
                    {
                        _ImGuiManager.ShowSettingsWindow();
                    }

                    if (ImGui.MenuItem("Close", "Ctrl + Q"))
                    {
                        //Stop the renderer
                        ThreadRequest req = new();
                        req.Type = THREAD_REQUEST_TYPE.ENGINE_TERMINATE_RENDER;
                        EngineRef.SendRequest(ref req);

                        CloseWindowEvent?.Invoke();
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.MenuItem("About"))
                {
                    _ImGuiManager.ShowAboutWindow();
                }

                ImGui.EndMenuBar();
            }

            //Generate StatusBar
            //StatusBar

            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, WindowSize.Y - statusBarHeight));
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(WindowSize.X, statusBarHeight));

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);


            if (ImGui.Begin("StatusBar", ImGuiWindowFlags.NoMove |
                                         ImGuiWindowFlags.NoDocking |
                                         ImGuiWindowFlags.NoDecoration))
            {
                ImGui.Columns(2);
                ImGui.SetCursorPosY(0.25f * statusBarHeight);
                ImGui.Text(RenderState.StatusString);
                ImGui.NextColumn();
                string text = "Created by gregkwaste";
                ImGui.SetCursorPosY(0.25f * statusBarHeight);
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(text).X
                    - ImGui.GetScrollX() - 2 * ImGui.GetStyle().ItemSpacing.X);

                ImGui.Text(text);
                ImGui.End();
            }
            ImGui.PopStyleVar();

            ImGui.End();

            ImGui.SetCursorPosX(0.0f);

            //Scene Render
            bool scene_view = true;
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0.0f, 0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, new System.Numerics.Vector2(0.0f, 0.0f));

            //Cause of ImguiNET that does not yet support DockBuilder. The main Viewport will be docked to the main window.
            //All other windows will be separate.
            if (ImGui.Begin("Scene", ref scene_view, ImGuiWindowFlags.NoScrollbar))
            {
                //Update RenderSize
                System.Numerics.Vector2 csize = ImGui.GetContentRegionAvail();
                csize.X = Math.Max(csize.X, 100);
                csize.Y = Math.Max(csize.Y, 100);
                NbVector2i csizetk = new((int)csize.X, (int)csize.Y);
                ImGui.Image(new IntPtr(EngineRef.renderSys.getRenderFBO().GetChannel(0)),
                                csize,
                                new System.Numerics.Vector2(0.0f, 1.0f),
                                new System.Numerics.Vector2(1.0f, 0.0f));

                bool active_status = ImGui.IsItemHovered();
                CaptureInput?.Invoke(active_status);

                if (csizetk != EngineRef.renderSys.GetViewportSize())
                {
                    SceneViewSize = csizetk;
                    EngineRef.renderSys.Resize(csizetk.X, csizetk.Y);
                }

                ImGui.PopStyleVar();
                ImGui.End();
            }


            if (ImGui.Begin("SceneGraph", ImGuiWindowFlags.NoCollapse))
            {
                _ImGuiManager.DrawSceneGraph();
                ImGui.End();
            }

            if (ImGui.Begin("Node Editor", ImGuiWindowFlags.NoCollapse))
            {
                _ImGuiManager.DrawObjectInfoViewer();
                ImGui.End();
            }

            if (ImGui.Begin("Material Editor", ImGuiWindowFlags.NoCollapse))
            {
                _ImGuiManager.DrawMaterialEditor();
                ImGui.End();
            }

            if (ImGui.Begin("Shader Editor", ImGuiWindowFlags.NoCollapse))
            {
                _ImGuiManager.DrawShaderEditor();
                ImGui.End();
            }

            if (ImGui.Begin("Texture Editor", ImGuiWindowFlags.NoCollapse))
            {
                _ImGuiManager.DrawTextureEditor();
                ImGui.End();
            }

            if (ImGui.Begin("Tools", ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.Button("ProcGen", new System.Numerics.Vector2(80.0f, 40.0f)))
                {
                    //TODO generate proc gen view
                }

                ImGui.SameLine();

                if (ImGui.Button("Reset Pose", new System.Numerics.Vector2(80.0f, 40.0f)))
                {
                    //TODO Reset The models pose
                }

                if (ImGui.Button("Clear Active Scene", new System.Numerics.Vector2(80.0f, 40.0f)))
                {
                    EngineRef.ClearActiveSceneGraph();
                }

                ImGui.End();
            }

#if (DEBUG)
            if (ImGui.Begin("Test Options", ImGuiWindowFlags.NoCollapse))
            {
                ImGui.DragFloat("Test Option 1", ref RenderState.settings.renderSettings.testOpt1);
                ImGui.DragFloat("Test Option 2", ref RenderState.settings.renderSettings.testOpt2);
                ImGui.DragFloat("Test Option 3", ref RenderState.settings.renderSettings.testOpt3);
                ImGui.End();
            }
#endif
            if (ImGui.Begin("Camera", ImGuiWindowFlags.NoCollapse))
            {
                //Camera Settings
                ImGui.BeginGroup();
                ImGui.TextColored(ImGuiManager.DarkBlue, "Camera Settings");
                ImGui.SliderFloat("FOV", ref RenderState.activeCam.fov, 15.0f, 100.0f);
                ImGui.SliderFloat("Sensitivity", ref RenderState.activeCam.Sensitivity, 0.1f, 10.0f);
                ImGui.InputFloat("MovementSpeed", ref RenderState.activeCam.Speed, 1.0f, 500000.0f);
                ImGui.SliderFloat("zNear", ref RenderState.activeCam.zNear, 0.01f, 1.0f);
                ImGui.SliderFloat("zFar", ref RenderState.activeCam.zFar, 101.0f, 30000.0f);

                if (ImGui.Button("Reset Camera"))
                {
                    RenderState.activeCam.Position = new NbVector3(0.0f);
                }

                ImGui.SameLine();

                if (ImGui.Button("Reset Scene Rotation"))
                {
                    //TODO :Maybe enclose all settings in a function
                    RenderState.activeCam.pitch = 0.0f;
                    RenderState.activeCam.yaw = -90.0f;
                }

                ImGui.EndGroup();

                ImGui.Separator();
                ImGui.BeginGroup();
                ImGui.TextColored(ImGuiManager.DarkBlue, "Camera Controls");

                ImGui.Columns(2);

                ImGui.Text("Horizontal Camera Movement");
                ImGui.Text("Vertical Camera Movement");
                ImGui.Text("Camera Rotation");
                ImGui.Text("Scene Rotate (Y Axis)");
                ImGui.NextColumn();
                ImGui.Text("W, A, S, D");
                ImGui.Text("R, F");
                ImGui.Text("Hold RMB +Move");
                ImGui.Text("Q, E");
                ImGui.EndGroup();

                ImGui.Columns(1);

                ImGui.End();
            }

            if (ImGui.Begin("Options", ImGuiWindowFlags.NoCollapse))
            {
                ImGui.LabelText("View Options", "");
                ImGui.Checkbox("Show Lights", ref RenderState.settings.viewSettings.ViewLights);
                ImGui.Checkbox("Show Light Volumes", ref RenderState.settings.viewSettings.ViewLightVolumes);
                ImGui.Checkbox("Show Joints", ref RenderState.settings.viewSettings.ViewJoints);
                ImGui.Checkbox("Show Locators", ref RenderState.settings.viewSettings.ViewLocators);
                ImGui.Checkbox("Show Collisions", ref RenderState.settings.viewSettings.ViewCollisions);
                ImGui.Checkbox("Show Bounding Hulls", ref RenderState.settings.viewSettings.ViewBoundHulls);
                ImGui.Checkbox("Emulate Actions", ref RenderState.settings.viewSettings.EmulateActions);

                ImGui.Separator();
                ImGui.LabelText("Rendering Options", "");

                ImGui.Checkbox("Use Textures", ref RenderState.settings.renderSettings.UseTextures);
                ImGui.Checkbox("Use Lighting", ref RenderState.settings.renderSettings.UseLighting);
                ImGui.Checkbox("Use VSYNC", ref RenderState.settings.renderSettings.UseVSync);
                ImGui.Checkbox("Show Animations", ref RenderState.settings.renderSettings.ToggleAnimations);
                ImGui.Checkbox("Wireframe", ref RenderState.settings.renderSettings.RenderWireFrame);
                ImGui.Checkbox("FXAA", ref RenderState.settings.renderSettings.UseFXAA);
                ImGui.Checkbox("Bloom", ref RenderState.settings.renderSettings.UseBLOOM);
                ImGui.Checkbox("LOD Filtering", ref RenderState.settings.renderSettings.LODFiltering);

                ImGui.InputInt("FPS", ref RenderState.settings.renderSettings.FPS);
                ImGui.InputFloat("HDR Exposure", ref RenderState.settings.renderSettings.HDRExposure);

                ImGui.End();
            }

            _ImGuiManager.ProcessModals(this, ref current_file_path, ref IsOpenFileDialogOpen);

            //Draw plugin panels and popups
            foreach (PluginBase plugin in EngineRef.Plugins.Values)
                plugin.Draw();

            //Debugging Information
            if (ImGui.Begin("Statistics"))
            {
                ImGui.Text(string.Format("FPS : {0, 3:F1}", RenderStats.fpsCount));
                ImGui.Text(string.Format("FrameTime : {0, 3:F6}", RenderStats.FrameTime));
                ImGui.Text(string.Format("VertexCount : {0}", RenderStats.vertNum));
                ImGui.Text(string.Format("TrisCount : {0}", RenderStats.trisNum));
                ImGui.End();
            }



        }



    }
}
