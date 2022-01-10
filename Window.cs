using System;
using System.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using ImGuiNET;
using OpenTK.Windowing.Common;
using NbCore;
using NbCore.Math;
using NbCore.Common;
using NbCore.Plugins;
using NbCore.Utils;
using System.Collections.Generic;
using NbCore.UI.ImGui;
using System.IO;


namespace NibbleEditor
{
    public class Window : GameWindow
    {
        //ImGuiController _controller;
        AppImGuiManager _ImGuiManager;

        //Parameters
        private string current_file_path = Environment.CurrentDirectory;

        //Mouse States
        private NbMouseState currentMouseState = new();
        private NbMouseState prevMouseState = new();

        //Keyboard State
        private new NbKeyboardState KeyboardState;

        //Engine
        private Engine engine;

        //Workers
        private readonly WorkThreadDispacher workDispatcher = new();
        private readonly RequestHandler requestHandler = new();
        
        //ImGui stuff
        private NbVector2i SceneViewSize = new();
        private bool firstDockSetup = true;
        private float scrolly = 0.0f;
        
        static private bool IsOpenFileDialogOpen = false;

        public Window() : base(GameWindowSettings.Default, 
            new NativeWindowSettings() { Size = new Vector2i(800, 600), APIVersion = new Version(4, 5) })
        {
            //Set Window Title
            Title = "Nibble Editor " + Util.getVersion();

            //Setup Logger
            Util.loggingSr = new StreamWriter("log.out");

            //SETUP THE Callbacks FOR THE NbCore ENVIRONMENT
            Callbacks.SetDefaultCallbacks();
            Callbacks.updateStatus = Util.setStatus;
            Callbacks.showInfo = Util.showInfo;
            Callbacks.showError = Util.showError;
            Callbacks.Log = Util.Log;
            Callbacks.Assert = Util.Assert;

            SceneViewSize = new(Size.X, Size.Y);
            
            //Start worker thread
            workDispatcher.Start();

        }

        protected override void OnLoad()
        {
            base.OnLoad();
            
            //OVERRIDE SETTINGS
            //FileUtils.dirpath = "I:\\SteamLibrary1\\steamapps\\common\\No Man's Sky\\GAMEDATA\\PCBANKS";

            //Initialize Engine backend
            engine = new Engine(this);
            RenderState.engineRef = engine; //Set reference to engine [Should do before initialization]
            
            engine.init(Size.X, Size.Y); //Initialize Engine

            //Initialize ImGuiManager
            _ImGuiManager = new(this, engine);

            //Load Settings
            if (!File.Exists("settings.json"))
                _ImGuiManager.ShowSettingsWindow();

            RenderState.settings = Settings.loadFromDisk();
            
            //Pass rendering settings to the Window
            RenderFrequency = RenderState.settings.renderSettings.FPS;
            UpdateFrequency = 60;
            
            //Populate GLControl
            SceneGraphNode test1 = engine.CreateLocatorNode("Test Locator 1");
            SceneGraphNode test2 = engine.CreateLocatorNode("Test Locator 2");
            test1.AddChild(test2);
            SceneGraphNode test3 = engine.CreateLocatorNode("Test Locator 3");
            test2.AddChild(test3);


            SceneGraphNode light = engine.CreateLightNode("Default Light", 200.0f, ATTENUATION_TYPE.QUADRATIC, LIGHT_TYPE.POINT);
            NbCore.Systems.TransformationSystem.SetEntityLocation(light, new NbVector3(100.0f, 100.0f, 100.0f));
            test1.AddChild(light);

            //Create Render Scene
            Scene scene = engine.CreateScene();
            scene.Name = "DEFAULT_SCENE";
            engine.sceneMgmtSys.SetActiveScene(scene);
            
            engine.RegisterSceneGraphNode(test1); //Also registers entities

            //Request tranform update for the added nodes
            engine.RequestEntityTransformUpdate(test1);
            
            //Populate SceneGraphView
            _ImGuiManager.PopulateSceneGraph(scene); //Only sets the root node for now

            //Check if Temp folder exists
#if DEBUG
            if (!Directory.Exists("Temp")) Directory.CreateDirectory("Temp");
#endif
            //Set active Components
            Util.activeWindow = this;

        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            //TODO: Make sure all keys are mapped so that we don't need to check everytime
            if (OpenTKKeyMap.ContainsKey(e.Key))
            {
                KeyboardState.SetKeyDownStatus(OpenTKKeyMap[e.Key], true);
            }
        }
        
        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            //TODO: Make sure all keys are mapped so that we don't need to check everytime
            if (OpenTKKeyMap.ContainsKey(e.Key))
            {
                KeyboardState.SetKeyDownStatus(OpenTKKeyMap[e.Key], false);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left:
                    currentMouseState.SetButtonStatus(NbMouseButton.LEFT, true);
                    break;
                case OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right:
                    currentMouseState.SetButtonStatus(NbMouseButton.RIGHT, true);
                    break;
                case OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle:
                    currentMouseState.SetButtonStatus(NbMouseButton.MIDDLE, true);
                    break;
            }
        }
        
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left:
                    currentMouseState.SetButtonStatus(NbMouseButton.LEFT, false);
                    break;
                case OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right:
                    currentMouseState.SetButtonStatus(NbMouseButton.RIGHT, false);
                    break;
                case OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle:
                    currentMouseState.SetButtonStatus(NbMouseButton.MIDDLE, false);
                    break;
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            currentMouseState.Position.X = e.X;
            currentMouseState.Position.Y = e.Y;
            currentMouseState.PositionDelta.X = e.X - prevMouseState.Position.X;
            currentMouseState.PositionDelta.Y = e.Y - prevMouseState.Position.Y;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            currentMouseState.Scroll.X += e.OffsetX;
            currentMouseState.Scroll.Y += e.OffsetY;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            // Tell ImGui of the new size
            _ImGuiManager.Resize(ClientSize.X, ClientSize.Y);
        }

        
        private void CloseWindow()
        {
            engine.CleanUp();
            Close();
        }
        
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);


            //Send Input
            _ImGuiManager.SetMouseState(currentMouseState);
            engine.SetMouseState(currentMouseState);
            _ImGuiManager.SetKeyboardState(KeyboardState);
            engine.SetKeyboardState(KeyboardState);
            
            prevMouseState = currentMouseState;
            currentMouseState.PositionDelta.X = 0.0f;
            currentMouseState.PositionDelta.Y = 0.0f;
            currentMouseState.Scroll.X = 0.0f;
            currentMouseState.Scroll.Y = 0.0f;

            //Pass Global rendering settings
            VSync = RenderState.settings.renderSettings.UseVSync ? VSyncMode.On : VSyncMode.Off;
            RenderFrequency = RenderState.settings.renderSettings.FPS;

            engine.handleRequests();
            handleRequests(); //Handle window requests
            
            if (engine.rt_State == EngineRenderingState.ACTIVE)
            {
                //TODO: Move these two lines in the engine and retrieve the sceneviewsize from the rendersystem
                RenderState.activeCam.aspect = (float) SceneViewSize.X / SceneViewSize.Y;
                RenderState.activeCam.updateViewMatrix();

                //Execute Engine On Frame Update
                engine.OnFrameUpdate(e.Time);

            }
                
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            Camera.UpdateCameraDirectionalVectors(RenderState.activeCam);

            engine.OnRenderUpdate(e.Time);
            _ImGuiManager.Update(e.Time);
            
            //Bind Default Framebuffer
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

            //UI
            DrawUI();
            
            //ImGui.ShowDemoWindow();
            _ImGuiManager.Render();

            //ImGuiUtil.CheckGLError("End of frame");

            RenderStats.fpsCount = 1.0f / (float)e.Time;
            SwapBuffers();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            _ImGuiManager.SendChar((char) e.Unicode);
        }
            
        private void OpenFile(string filename, bool testScene, int testSceneID)
        {
            Log("Importing " + filename, LogVerbosityLevel.INFO);
            ThreadRequest req;
            
            //Pause renderer
            req = new()
            {
                Type = THREAD_REQUEST_TYPE.ENGINE_PAUSE_RENDER
            };
            
            //Send request to engine
            engine.SendRequest(ref req);

            waitForRequest(ref req);

            if (testScene)
                AddTestScene(testSceneID);
            else
                AddScene(filename);
            
            //Populate 
            Util.setStatus("Creating SceneGraph...");

            _ImGuiManager.PopulateSceneGraph(RenderState.engineRef.sceneMgmtSys.ActiveScene);

            //Add to UI
            Util.setStatus("Ready");

            //Generate Request for resuming rendering
            ThreadRequest req2 = new()
            {
                Type = THREAD_REQUEST_TYPE.ENGINE_RESUME_RENDER
            };

            engine.SendRequest(ref req2);
        
        }

        private static void Log(string msg, LogVerbosityLevel lvl)
        {
            Callbacks.Log("* WINDOW : " + msg, lvl);
        }

        public void SendRequest(ref ThreadRequest r)
        {
            requestHandler.AddRequest(ref r);
        }

        public void handleRequests()
        {
            if (requestHandler.HasOpenRequests())
            {
                ThreadRequest req = requestHandler.Peek();
                Log("Peeking Request " + req.Type, LogVerbosityLevel.HIDEBUG);
                
                //Do stuff with requests that need extra work to get started
                if (req.Status == THREAD_REQUEST_STATUS.NULL)
                {
                    switch (req.Type)
                    {
                        case THREAD_REQUEST_TYPE.WINDOW_LOAD_NMS_ARCHIVES:
                            workDispatcher.sendRequest(ref req);
                            break;
                        case THREAD_REQUEST_TYPE.WINDOW_OPEN_FILE:
                            string filename = req.Data as string;
                            OpenFile(filename, false, 0);
                            req.Status = THREAD_REQUEST_STATUS.FINISHED;
                            break;
                        case THREAD_REQUEST_TYPE.WINDOW_CLOSE:
                            CloseWindow();
                            break;
                        default:
                            break; 
                    }
                }
                else if (req.Status != THREAD_REQUEST_STATUS.FINISHED)
                    return;
                
                //At this point the peeked request is finished so its safe to pop it from the queue
                requestHandler.Fetch(); 
            }
        }
        
        public void waitForRequest(ref ThreadRequest req)
        {
            while (true)
            {
                engine.handleRequests(); //Force engine to handle requests
                lock (req)
                {
                    if (req.Status == THREAD_REQUEST_STATUS.FINISHED)
                        return;
                }
            }
        }

        //Scene Loading
        public void AddTestScene(int sceneID)
        {
            //Generate Request for rendering thread
            ThreadRequest req1 = new()
            {
                Type = THREAD_REQUEST_TYPE.ENGINE_OPEN_TEST_SCENE
            };
            req1.Data = sceneID;

            engine.SendRequest(ref req1);

            //Wait for requests to finish before return
            waitForRequest(ref req1);

            //find Animation Capable nodes
            findAnimScenes(RenderState.engineRef.sceneMgmtSys.ActiveScene.Root); //Repopulate animScenes
            findActionScenes(RenderState.engineRef.sceneMgmtSys.ActiveScene.Root); //Re-populate actionSystem

        }

        public void AddScene(string filename)
        {
            //Generate Request for rendering thread
            ThreadRequest req1 = new()
            {
                Type = THREAD_REQUEST_TYPE.ENGINE_OPEN_NEW_SCENE
            };
            req1.Data = filename;
            
            engine.SendRequest(ref req1);
            
            //Wait for requests to finish before return
            waitForRequest(ref req1);
            
            //find Animation Capable nodes
            findAnimScenes(RenderState.engineRef.sceneMgmtSys.ActiveScene.Root); //Repopulate animScenes
            findActionScenes(RenderState.engineRef.sceneMgmtSys.ActiveScene.Root); //Re-populate actionSystem
        }
        
        public void findAnimScenes(SceneGraphNode node)
        {
            if (node.HasComponent<AnimComponent>())
            {
                engine.animationSys.Add(node);
            }

            foreach (SceneGraphNode child in node.Children)
                findAnimScenes(child);
        }

        public void findActionScenes(SceneGraphNode node)
        {
            if (node.HasComponent<TriggerActionComponent>())
            {
                engine.actionSys.Add(node);
            }
            
            foreach (SceneGraphNode child in node.Children)
                findActionScenes(child);
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
                                            ImGuiWindowFlags.NoDocking;

            ImGuiViewportPtr vp = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(vp.WorkPos);
            ImGui.SetNextWindowSize(vp.WorkSize);
            ImGui.SetNextWindowViewport(vp.ID);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, 0.0f);

            bool keep_window_open = true;
            int statusBarHeight = (int) (1.75f * ImGui.CalcTextSize("Status").Y);
            ImGui.Begin("MainWindow", ref keep_window_open, window_flags);
            ImGui.PopStyleVar(2);
            
            
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


            Scene new_scene = null;
            bool import_new_scene = true;

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
                        foreach (PluginBase plugin in engine.Plugins.Values)
                            plugin.DrawImporters();
                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Export"))
                    {
                        foreach (PluginBase plugin in engine.Plugins.Values)
                            plugin.DrawExporters(engine.GetActiveScene());
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
                        engine.SendRequest(ref req);

                        //Send event to close the window
                        ThreadRequest req1 = new();
                        req1.Type = THREAD_REQUEST_TYPE.WINDOW_CLOSE;
                        requestHandler.AddRequest(ref req1);
                        
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

            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, Size.Y - statusBarHeight));
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(Size.X, statusBarHeight));
            
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
                NbVector2i csizetk = new((int) csize.X, (int) csize.Y);
                ImGui.Image(new IntPtr(engine.renderSys.getRenderFBO().GetChannel(0)),
                                csize,
                                new System.Numerics.Vector2(0.0f, 1.0f),
                                new System.Numerics.Vector2(1.0f, 0.0f));

                bool active_status = ImGui.IsItemHovered();
                currentMouseState.UpdateScene = active_status;
                KeyboardState.UpdateScene = active_status;

                if (csizetk != engine.renderSys.GetViewportSize())
                {
                    SceneViewSize = csizetk;
                    engine.renderSys.Resize(csizetk.X, csizetk.Y);
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
                    engine.sceneMgmtSys.ClearScene(engine.GetActiveScene());
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
            foreach (PluginBase plugin in engine.Plugins.Values)
                plugin.Draw();

            //Debugging Information
            if (ImGui.Begin("Statistics"))
            {
                ImGui.Text(string.Format("FPS : {0, 3:F1}", RenderStats.fpsCount));
                ImGui.Text(string.Format("VertexCount : {0}", RenderStats.vertNum));
                ImGui.Text(string.Format("TrisCount : {0}", RenderStats.trisNum));
                ImGui.End();
            }



        }


        private static readonly Dictionary<OpenTK.Windowing.GraphicsLibraryFramework.Keys, NbKey> OpenTKKeyMap = new()
        {
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.A, NbKey.A },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.B, NbKey.B },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.C, NbKey.C },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.D, NbKey.D },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.E, NbKey.E },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.F, NbKey.F },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.G, NbKey.G },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.H, NbKey.H },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.I, NbKey.I },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.J, NbKey.J },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.K, NbKey.K },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.L, NbKey.L },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.M, NbKey.M },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.N, NbKey.N },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.O, NbKey.O },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.P, NbKey.P },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Q, NbKey.Q },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.R, NbKey.R },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.S, NbKey.S },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.T, NbKey.T },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.U, NbKey.U },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.V, NbKey.V },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.W, NbKey.W },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.X, NbKey.X },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Y, NbKey.Y },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Z, NbKey.Z },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left, NbKey.LeftArrow },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right, NbKey.RightArrow },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up, NbKey.UpArrow },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down, NbKey.DownArrow },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftAlt, NbKey.LeftAlt },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightAlt, NbKey.RightAlt },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftControl, NbKey.LeftCtrl },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightControl, NbKey.RightCtrl },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftSuper, NbKey.LeftSuper },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightSuper, NbKey.RightSuper },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backspace, NbKey.Backspace },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space, NbKey.Space },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Home, NbKey.Home },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.End, NbKey.End },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Insert, NbKey.Insert },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Delete, NbKey.Delete },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.PageUp, NbKey.PageUp },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.PageDown, NbKey.PageDown },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter, NbKey.Enter },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape, NbKey.Escape },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPadEnter, NbKey.KeyPadEnter },
        };

        

    }




}
