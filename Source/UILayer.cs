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
    public delegate void SaveActiveSceneHandler();

    public class UILayer : ApplicationLayer
    {
        private AppImGuiManager _ImGuiManager;

        //ImGui stuff
        private NbVector2i SceneViewSize = new();
        private NbVector2i WindowSize = new();
        private bool firstDockSetup = true;
        private NbKeyboardState keyboardState;
        private NbMouseState mouseState;

        static private bool IsOpenFileDialogOpen = false;
        private string current_file_path = Environment.CurrentDirectory;
        
        //Events
        public event CloseWindowEventHandler CloseWindowEvent;
        public event CaptureInputHandler CaptureInput;
        public event SaveActiveSceneHandler SaveActiveSceneEvent;

        public UILayer(Window win, Engine e) : base(e)
        {
            Name = "UI Layer";
            
            //Initialize ImGuiManager
            _ImGuiManager = new(win, e);
            EngineRef.NewSceneEvent += new Engine.NewSceneEventHandler(OnNewScene);
            _ImGuiManager.OpenFileModal.open_file_handler = new ImGuiOpenFileTriggerEventHandler(EngineRef.DeserializeScene);
            
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

        public void OnLog(LogElement msg)
        {
            _ImGuiManager.Log(msg);
        }
        
        public override void OnFrameUpdate(ref Queue<object> data, double dt)
        {
            //Fetch state from data
            mouseState = (NbMouseState) data.Dequeue();
            keyboardState = (NbKeyboardState) data.Dequeue();
            
            //Send Input
            _ImGuiManager.SetMouseState(mouseState);
            _ImGuiManager.SetKeyboardState(keyboardState);
            
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

            // Update and Render additional Platform Windows
            if (ImGui.GetIO().ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            {
                ImGui.UpdatePlatformWindows();
                ImGui.RenderPlatformWindowsDefault();
            }

            //ImGuiUtil.CheckGLError("End of frame");

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
                    ImGui.DockBuilderDockWindow("Log", dockSpaceLeftDown);
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
                    if (ImGui.MenuItem("New"))
                    {
                        Log("Create new scene not implemented yet", LogVerbosityLevel.INFO);
                    }

                    if (ImGui.MenuItem("Open"))
                    {
                        _ImGuiManager.ShowOpenSceneDialog();
                        IsOpenFileDialogOpen = true;
                    }

                    if (ImGui.MenuItem("Save"))
                    {
                        Log("Saving Scene", LogVerbosityLevel.INFO);
                        SaveActiveSceneEvent.Invoke();
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

            ImGui.End(); //End of main Window

            ImGui.SetCursorPosX(0.0f);

            //Scene Render
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0.0f, 0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, new System.Numerics.Vector2(0.0f, 0.0f));

            //Cause of ImguiNET that does not yet support DockBuilder. The main Viewport will be docked to the main window.
            //All other windows will be separate.
            if (ImGui.Begin("Scene", ImGuiWindowFlags.NoScrollbar))
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

                ImGui.End();
            }

            ImGui.PopStyleVar();

            if (ImGui.Begin("SceneGraph", ImGuiWindowFlags.NoCollapse |
                                          ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                _ImGuiManager.DrawSceneGraph();
                ImGui.End();
            }

            if (ImGui.Begin("Node Editor", ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                _ImGuiManager.DrawObjectInfoViewer();
                ImGui.End();
            }

            if (ImGui.Begin("Material Editor", ImGuiWindowFlags.NoCollapse |
                                               ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                _ImGuiManager.DrawMaterialEditor();
                ImGui.End();
            }

            if (ImGui.Begin("Shader Editor", ImGuiWindowFlags.NoCollapse |
                                             ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                _ImGuiManager.DrawShaderEditor();
                ImGui.End();
            }

            if (ImGui.Begin("Texture Editor", ImGuiWindowFlags.NoCollapse |
                                              ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                _ImGuiManager.DrawTextureEditor();
                ImGui.End();
            }

            if (ImGui.Begin("Tools", ImGuiWindowFlags.NoCollapse |
                                     ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                if (ImGui.CollapsingHeader("Engine Tools"))
                {
                    if (ImGui.Button("Reset Pose"))
                    {
                        //TODO Reset The models pose
                    }
                    ImGui.SameLine();

                    if (ImGui.Button("Clear Active Scene"))
                    {
                        EngineRef.ClearActiveSceneGraph();
                    }
                }

                foreach (PluginBase plugin in EngineRef.Plugins.Values)
                {
                    if (plugin.Tools.Count > 0)
                    {
                        if (ImGui.CollapsingHeader(plugin.Name + " Tools"))
                        {
                            foreach (ToolDescription tool in plugin.Tools)
                            {
                                if (ImGui.Button(tool.Name))
                                {
                                    tool.ToolFunc?.Invoke(EngineRef);
                                }
                            }
                        }
                    }
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
            bool isopen = false;
            if (ImGui.Begin("Camera", ref isopen, ImGuiWindowFlags.NoCollapse))
            {
                //Camera Settings
                ImGui.BeginGroup();
                ImGui.TextColored(ImGuiManager.DarkBlue, "Camera Settings");
                ImGui.SliderInt("FOV", ref RenderState.settings.camSettings.FOV, 15, 100);
                ImGui.SliderFloat("Sensitivity", ref RenderState.settings.camSettings.Sensitivity, 0.01f, 2.0f);
                ImGui.InputFloat("MovementSpeed", ref RenderState.settings.camSettings.Speed, 1.0f, 500000.0f);
                ImGui.SliderFloat("zNear", ref RenderState.settings.camSettings.zNear, 0.01f, 1.0f);
                ImGui.SliderFloat("zFar", ref RenderState.settings.camSettings.zFar, 101.0f, 100000.0f);

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
                ImGui.Text(string.Format("FPS : {0, 3:F1}", 1.0f / EngineRef.renderSys.frameStats.Frametime));
                ImGui.Text(string.Format("FrameTime : {0, 3:F6}", EngineRef.renderSys.frameStats.Frametime));
                ImGui.Text(string.Format("VertexCount : {0}", EngineRef.renderSys.frameStats.RenderedVerts));
                ImGui.Text(string.Format("TrisCount : {0}", EngineRef.renderSys.frameStats.RenderedIndices / 3));
                ImGui.End();
            }

            if (ImGui.Begin("Log"))
            {
                _ImGuiManager.DrawLogger();
                ImGui.End();
            }


        }


        public void AddTextureMixerScene()
        {
            //Create Shader Configuration
            GLSLShaderConfig conf;
            conf = EngineRef.CreateShaderConfig(EngineRef.GetShaderSourceByFilePath("Shaders/Simple_VSEmpty.glsl"),
                                      EngineRef.GetShaderSourceByFilePath("Shaders/texture_mixer_FS.glsl"),
                                      null, null, null,
                                      NbShaderMode.DEFFERED, "TextureMixerConfig");
            EngineRef.RegisterEntity(conf);

            //Create Material
            MeshMaterial mat = new();
            mat.Name = "mixMaterial";
            NbUniform uf = new()
            {
                Name = "UseAlphaTextures",
                State = new()
                {
                    Type = NbUniformType.Float,
                    ShaderBinding = "use_alpha_textures",
                },
                Values = new(1.0f, 1.0f, 1.0f, 1.0f)
            };
            mat.Uniforms.Add(uf);
            uf = new()
            {
                Name = "UseLayer0",
                State = new()
                {
                    Type = NbUniformType.Float,
                    ShaderBinding = "lbaseLayersUsed[0]",
                },
                Values = new(1.0f, 0.0f, 0.0f, 0.0f)
            };
            mat.Uniforms.Add(uf);
            uf = new()
            {
                Name = "UseLayer1",
                State = new()
                {
                    Type = NbUniformType.Float,
                    ShaderBinding = "lbaseLayersUsed[1]",
                },
                Values = new(1.0f, 0.0f, 0.0f, 0.0f)
            };
            mat.Uniforms.Add(uf);
            uf = new()
            {
                Name = "Recolor0",
                State = new()
                {
                    Type = NbUniformType.Vector4,
                    ShaderBinding = "lRecolours[0]",
                },
                Values = new(1.0f, 0.0f, 0.0f, 0.5f)
            };
            mat.Uniforms.Add(uf);
            uf = new()
            {
                Name = "Recolor1",
                State = new()
                {
                    Type = NbUniformType.Vector4,
                    ShaderBinding = "lRecolours[1]",
                },
                Values = new(1.0f, 1.0f, 0.0f, 0.5f)
            };
            mat.Uniforms.Add(uf);

            NbShader shader = EngineRef.CreateShader(conf);
            EngineRef.CompileShader(shader);
            
            //Register material
            EngineRef.RegisterEntity(mat);

            //Add Quad
            //Create Mesh
            NbCore.Primitives.Quad q = new(1.0f, 1.0f);

            NbMeshData md = q.geom.GetMeshData();
            NbMeshMetaData mmd = q.geom.GetMetaData();
            q.Dispose();

            NbMesh nm = new()
            {
                Hash = (ulong)mmd.GetHashCode(),
                MetaData = mmd,
                Data = md,
                Material = EngineRef.GetMaterialByName("mixMaterial")
            };

            //Create and register locator node
            SceneGraphNode new_node = EngineRef.CreateMeshNode("Quad#1", nm);

            //Register new locator node to engine
            EngineRef.RegisterEntity(new_node);

            //Set parent
            new_node.SetParent(EngineRef.GetActiveSceneGraph().Root);
            EngineRef.transformSys.RequestEntityUpdate(new_node);
        }





    }
}
