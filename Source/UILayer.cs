using System;
using System.IO;
using System.Collections.Generic;
using NbCore;
using ImGuiNET;
using NbCore.Common;
using NbCore;
using NbCore.Plugins;
using NbCore.UI.ImGui;
using System.Diagnostics;
using System.Linq;
using NbCore.Platform.Windowing;
using NbCore.Platform.Graphics;
using System.Numerics;
using System.Security.AccessControl;
using ImGuizmoNET;
using OpenTK.Windowing.Common;

namespace NibbleEditor
{
    public delegate void CloseWindowEventHandler();
    public delegate void CaptureInputHandler();
    public delegate void SaveActiveSceneHandler();

    public class UILayer : ApplicationLayer
    {
        private AppImGuiManager _ImGuiManager;

        //ImGui stuff
        static private bool IsOpenFileDialogOpen = false;
        private string current_file_path = Environment.CurrentDirectory;
        private string[] fps_settings = new string[] { "0", "15", "30", "60", "120", "300", "500" };

        //Events
        public event CloseWindowEventHandler CloseWindowEvent;
        public event CaptureInputHandler CaptureInput;
        public event SaveActiveSceneHandler SaveActiveSceneEvent;
        public NbResizeEventHandler SceneWindowResizeEvent;

        //Stats
        float TotalProcMemory = 0.0f;
        double CPUUsage = 0.0;
        TimeSpan lastProcTotalTime = new TimeSpan();
        DateTime lastTime = DateTime.Now;

        //UI Privates
        private float last_window_height = 0.0f;
        private bool show_settings_window = false;
        private bool TranslationGizmoToggle = false;
        private bool RotationnGizmoToggle = false;
        private bool ScaleGizmoToggle = false;
        private float[] delta_transform = new float[16];
        private NbVector2i OldSceneWinSize = new NbVector2i(800, 600);
        private Stopwatch SceneResizeWatch;

        public UILayer(Window win, Engine e) : base(win, e)
        {
            Name = "UI Layer";
            //Initialize ImGuiManager
            _ImGuiManager = new(win, e);
            EngineRef.NewSceneEvent += new Engine.NewSceneEventHandler(OnNewScene);
            _ImGuiManager.OpenFileModal.open_file_handler = new ImGuiOpenFileTriggerEventHandler(EngineRef.OpenScene);
                
            //Load Settings
            if (!File.Exists("settings.json"))
                _ImGuiManager.ShowSettingsWindow();
            
            //CPU stats timer
            System.Timers.Timer sysPerfTimer = new();
            sysPerfTimer.Elapsed += FetchAppPerformanceStats;
            sysPerfTimer.Interval = 1000;
            sysPerfTimer.Start();

            //Scene resize watch
            SceneResizeWatch = new Stopwatch();
        }

        //Event Handlers
        public void OnNewScene(SceneGraph s)
        {
            _ImGuiManager.PopulateSceneGraph(s);
            _ImGuiManager.SetObjectReference(s.Root);
        }
        
        public void OnTextInput(NbTextInputArgs e)
        {
            _ImGuiManager.SendChar((char)e.Unicode);
        }
        
        public void OnLog(LogElement msg)
        {
            _ImGuiManager.Log(msg);
        }
        
        public override void OnFrameUpdate(double dt)
        {
            
        }

        private void FetchAppPerformanceStats(object sender, System.Timers.ElapsedEventArgs args)
        {
            //Accumulate Stats
            //TODO: Move that to the window class?
            Process prc = Process.GetCurrentProcess();
            TimeSpan curTotalProcessorTime = prc.TotalProcessorTime;
            TotalProcMemory = prc.PrivateMemorySize64 / 1024.0f / 1024.0f;
            DateTime curTime = DateTime.Now;
            CPUUsage = (curTotalProcessorTime.TotalMilliseconds - lastProcTotalTime.TotalMilliseconds) * 100.0 / curTime.Subtract(lastTime).TotalMilliseconds / Environment.ProcessorCount;
            lastTime = curTime;
            lastProcTotalTime = curTotalProcessorTime;
        }

        public override void OnRenderFrameUpdate(double dt)
        {
            //Bind Default Framebuffer
            GraphicsAPI.BindDefaultFrameBuffer();
            GraphicsAPI.SetViewPortSize(0, 0, WindowRef.Size.X, WindowRef.Size.Y);
            
            _ImGuiManager.Update(dt);
            ImGui.DockSpaceOverViewport();

            //UI
            DrawUI();

            GraphicsAPI.ClearDrawBuffer(NbCore.Platform.Graphics.NbBufferMask.Color | NbCore.Platform.Graphics.NbBufferMask.Depth);
            //ImGui.ShowDemoWindow();
            _ImGuiManager.Render();

            // Update and Render additional Platform Windows
            if (ImGui.GetIO().ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            {
                ImGui.UpdatePlatformWindows();
                //ImGui.RenderPlatformWindowsDefault();
            }

            //ImGuiUtil.CheckGLError("End of frame");

        }

        private void DrawMainMenu(ImGuiViewportPtr vp)
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New"))
                    {
                        Log("Create new scene not implemented yet", LogVerbosityLevel.INFO);
                    }

                    if (ImGui.MenuItem("Open", "Ctrl + O"))
                    {
                        _ImGuiManager.ShowOpenSceneDialog();
                        IsOpenFileDialogOpen = true;
                    }

                    if (ImGui.MenuItem("Save", "Ctrl + S"))
                    {
                        Log("Saving Scene", LogVerbosityLevel.INFO);
                        SaveActiveSceneEvent.Invoke();
                    }

                    if (EngineRef.Plugins.Count > 0)
                    {
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
                    }


                    if (ImGui.MenuItem("Settings", "Ctrl + Alt + S"))
                        show_settings_window = true;

                    if (ImGui.MenuItem("Close", "Ctrl + Q"))
                    {
                        CloseWindowEvent?.Invoke();
                        return;
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.MenuItem("About"))
                {
                    _ImGuiManager.ShowAboutWindow();
                }

                last_window_height += ImGui.GetWindowHeight();
                ImGui.EndMenuBar();
            }
        }

        private void DrawSecondBar(ImGuiViewportPtr vp)
        {
            ImGui.SetNextWindowPos(new Vector2(vp.WorkPos.X, ImGui.GetFrameHeight()));
            ImGui.SetNextWindowSize(new(WindowRef.Size.X, 0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, 0.0f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, 0x60000000);
            if (ImGui.Begin("SecondaryMenuBar", ImGuiWindowFlags.NoDocking |
                                                ImGuiWindowFlags.NoMove |
                                                ImGuiWindowFlags.NoScrollbar |
                                                ImGuiWindowFlags.NoResize |
                                                ImGuiWindowFlags.NoTitleBar))
            {
                NbTexture atlas_tex = EngineRef.GetTexture("atlas.png");
                float img_size = 24;
                Vector2 uv0;
                Vector2 uv1;
                float atlas_image_id;
                float atlas_image_count = 20.0f;
                float atlas_image_uv_step = 1.0f / atlas_image_count;

                //Play-Edit button
                atlas_image_id = 0;
                if (NbRenderState.AppMode == ApplicationMode.EDIT)
                {
                    uv0 = new Vector2(atlas_image_id * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 1.0f);
                }
                else
                {
                    uv0 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 2) * atlas_image_uv_step, 1.0f);
                }

                if (ImGui.ImageButton("Toggle##ApplicationMode",
                                (IntPtr)atlas_tex.GpuID,
                                new Vector2(img_size, img_size),
                                uv0, uv1))
                {
                    NbRenderState.AppMode = NbRenderState.AppMode == ApplicationMode.EDIT ? ApplicationMode.GAME : ApplicationMode.EDIT;
                }
                ImGui.SetItemTooltip("Game/Edit Mode toggle");

                ImGui.SameLine();

                //Toggle Lighting Button
                atlas_image_id = 2;
                if (NbRenderState.settings.RenderSettings.UseLighting == false)
                {
                    uv0 = new Vector2(atlas_image_id * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 1.0f);
                }
                else
                {
                    uv0 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 2) * atlas_image_uv_step, 1.0f);
                }

                if (ImGui.ImageButton("Toggle##Lighting",
                                (IntPtr)atlas_tex.GpuID,
                                new Vector2(img_size, img_size),
                                uv0, uv1))
                {
                    NbRenderState.settings.RenderSettings.UseLighting = !NbRenderState.settings.RenderSettings.UseLighting;
                }
                ImGui.SetItemTooltip("Use Lighting");
                ImGui.SameLine();

                //Toggle Wireframe Button
                atlas_image_id = 10;
                if (NbRenderState.settings.RenderSettings.RenderWireFrame == false)
                {
                    uv0 = new Vector2(atlas_image_id * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 1.0f);
                }
                else
                {
                    uv0 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 2) * atlas_image_uv_step, 1.0f);
                }

                if (ImGui.ImageButton("Toggle##Wireframe",
                                (IntPtr)atlas_tex.GpuID,
                                new Vector2(img_size, img_size),
                                uv0, uv1))
                {
                    NbRenderState.settings.RenderSettings.RenderWireFrame = !NbRenderState.settings.RenderSettings.RenderWireFrame;
                }
                ImGui.SetItemTooltip("Toggle Wireframe mode");
                ImGui.SameLine();

                //Toggle Textures Button
                atlas_image_id = 12;
                if (NbRenderState.settings.RenderSettings.UseTextures)
                {
                    uv0 = new Vector2(atlas_image_id * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 1.0f);
                }
                else
                {
                    uv0 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 2) * atlas_image_uv_step, 1.0f);
                }

                if (ImGui.ImageButton("Toggle##Textures",
                                (IntPtr)atlas_tex.GpuID,
                                new Vector2(img_size, img_size),
                                uv0, uv1))
                {
                    NbRenderState.settings.RenderSettings.UseTextures = !NbRenderState.settings.RenderSettings.UseTextures;
                }
                ImGui.SetItemTooltip("Toggle Wireframe mode");
                ImGui.SameLine();

                //Toggle Translation Button
                atlas_image_id = 4;
                if (TranslationGizmoToggle == true)
                {
                    uv0 = new Vector2(atlas_image_id * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 1.0f);
                }
                else
                {
                    uv0 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 2) * atlas_image_uv_step, 1.0f);
                }

                if (ImGui.ImageButton("Toggle##TranslationGizmo",
                                (IntPtr)atlas_tex.GpuID,
                                new Vector2(img_size, img_size),
                                uv0, uv1))
                {
                    TranslationGizmoToggle = true;
                    RotationnGizmoToggle = false;
                    ScaleGizmoToggle = false;
                }
                ImGui.SetItemTooltip("Select Translation Gizmo");

                ImGui.SameLine();
                //Toggle Rotation Button
                atlas_image_id = 6;
                if (RotationnGizmoToggle == true)
                {
                    uv0 = new Vector2(atlas_image_id * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 1.0f);
                }
                else
                {
                    uv0 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 2) * atlas_image_uv_step, 1.0f);
                }

                if (ImGui.ImageButton("Toggle##RotationGizmo",
                                (IntPtr)atlas_tex.GpuID,
                                new Vector2(img_size, img_size),
                                uv0, uv1))
                {
                    RotationnGizmoToggle = true;
                    TranslationGizmoToggle = false;
                    ScaleGizmoToggle = false;
                }
                ImGui.SetItemTooltip("Select Rotation Gizmo");

                ImGui.SameLine();
                //Toggle Scale Button
                atlas_image_id = 8;
                if (ScaleGizmoToggle == true)
                {
                    uv0 = new Vector2(atlas_image_id * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 1.0f);
                }
                else
                {
                    uv0 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 2) * atlas_image_uv_step, 1.0f);
                }

                if (ImGui.ImageButton("Toggle##ScaleGizmoToggle",
                                (IntPtr)atlas_tex.GpuID,
                                new Vector2(img_size, img_size),
                                uv0, uv1))
                {
                    ScaleGizmoToggle = true;
                    RotationnGizmoToggle = false;
                    TranslationGizmoToggle = false;
                }
                ImGui.SetItemTooltip("Select Scale Gizmo");

                last_window_height += ImGui.GetWindowHeight();
                ImGui.End();
            }
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(3);
        }

        public void DrawUI()
        {
            //Enable docking in main view
            ImGuiDockNodeFlags dockspace_flags = ImGuiDockNodeFlags.PassthruCentralNode;
            
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

            show_settings_window = false;
            bool keep_window_open = true;
            int statusBarHeight = (int)(1.75f * ImGui.CalcTextSize("Status").Y);
            last_window_height = 0.0f;
            ImGui.Begin("##MainWindow", ref keep_window_open, window_flags);

            
            ImGui.PopStyleVar(3);


            //Main Menu
            
            //Draw Main Menu
            DrawMainMenu(vp);

            //Draw Item Bar
            DrawSecondBar(vp);
            
            //Create Dockspace Node
            ImGui.SetNextWindowPos(new Vector2(0.0f, last_window_height));
            ImGui.SetNextWindowSize(new Vector2(WindowRef.Size.X, WindowRef.Size.Y - last_window_height - statusBarHeight));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, 0.0f);
            if (ImGui.Begin("DockSpaceWindow", ImGuiWindowFlags.NoTitleBar | 
                                               ImGuiWindowFlags.NoResize |
                                               ImGuiWindowFlags.NoDecoration |
                                               ImGuiWindowFlags.NoMove))
            {
                uint dockSpaceID = ImGui.GetID("MainDockSpace");
                //System.Numerics.Vector2 dockSpaceSize = vp.GetWorkSize();
                ImGui.DockSpace(dockSpaceID, Vector2.Zero, dockspace_flags);
                ImGui.End();
            }
            ImGui.PopStyleVar(2);

            //Handle Keyboard Shortcuts
            if (WindowRef.IsKeyDown(NbKey.LeftCtrl) && WindowRef.IsKeyDown(NbKey.LeftAlt) && WindowRef.IsKeyPressed(NbKey.S))
                show_settings_window = true;

            if (show_settings_window)
            {
                _ImGuiManager.ShowSettingsWindow();
                show_settings_window = false;
            }

            //Generate StatusBar
            //StatusBar

            ImGui.SetNextWindowPos(new(0, WindowRef.Size.Y - statusBarHeight));
            ImGui.SetNextWindowSize(new(WindowRef.Size.X, statusBarHeight));

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);

            if (ImGui.Begin("StatusBar", ImGuiWindowFlags.NoMove |
                                         ImGuiWindowFlags.NoDocking |
                                         ImGuiWindowFlags.NoDecoration))
            {
                ImGui.Columns(2);
                ImGui.SetCursorPosY(0.25f * statusBarHeight);
                ImGui.Text(NbRenderState.StatusString);
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
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f, 0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, new Vector2(0.0f, 0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(100, 100));

            //Cause of ImguiNET that does not yet support DockBuilder. The main Viewport will be docked to the main window.
            //All other windows will be separate.
            if (ImGui.Begin("Scene", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                //Update RenderSize
                Vector2 csize = ImGui.GetContentRegionAvail();
                
                if ((int)csize.X != OldSceneWinSize.X || (int)csize.Y != OldSceneWinSize.Y)
                {
                    if (!SceneResizeWatch.IsRunning)
                        SceneResizeWatch.Start();
                    else
                        SceneResizeWatch.Restart();

                    OldSceneWinSize.X = (int)csize.X;
                    OldSceneWinSize.Y = (int)csize.Y;
                }

                //Check if we need to invoke the resize event
                if (SceneResizeWatch.ElapsedMilliseconds > 60)
                {
                    NbResizeArgs new_args = new(new ResizeEventArgs((int) csize.X, (int) csize.Y));
                    SceneWindowResizeEvent?.Invoke(new_args);
                    SceneResizeWatch.Stop();
                    SceneResizeWatch.Reset();
                }
                
                FBO render_fbo = EngineRef.GetSystem<NbCore.Systems.RenderingSystem>().getRenderFBO();

                ImGui.Image(new IntPtr(render_fbo.GetTexture(NbFBOAttachment.Attachment2).GpuID),
                                csize,
                                new Vector2(0.0f, 1.0f),
                                new Vector2(1.0f, 0.0f),
                                new Vector4(1.0f),
                                new Vector4(1.0f, 1.0f, 1.0f, 0.2f));


                //Console.WriteLine($"Scene Hovered {ImGui.IsItemHovered()} {ImGui.IsItemActivated()} {ImGui.IsItemFocused()}");
                if (ImGui.IsItemHovered())
                    CaptureInput?.Invoke();
                
                //Imguizmo Setup
                ImGuizmo.SetOrthographic(false);
                ImGuizmo.SetDrawlist();
                ImGuizmo.SetRect(ImGui.GetWindowPos().X, 
                                 ImGui.GetWindowPos().Y,
                                 csize.X, csize.Y);
                
                float[] view = NbRenderState.activeCam.lookMat.ToArray();
                float[] proj = NbRenderState.activeCam.projMat.ToArray();
                float[] transform = NbMatrix4.Identity().ToArray();

                //Draw Grid
                //ImGuizmo.DrawGrid(ref view[0], ref proj[0], ref transform[0], 2.0f);

                //Get selected object on the scenegraph
                SceneGraphNode node = _ImGuiManager.GetSelectedObject();

                if (node != null)
                {
                    float[] node_transform = EngineRef.GetNodeTransformArray(node);
                    
                    OPERATION op = 0x0;

                    if (TranslationGizmoToggle)
                        op |= OPERATION.TRANSLATE;

                    if (RotationnGizmoToggle)
                        op |= OPERATION.ROTATE;

                    if (ScaleGizmoToggle)
                        op |= OPERATION.SCALE;

                    float[] gizmosnap = new float[3];
                    if (WindowRef.IsKeyDown(NbKey.LeftShift))
                    {
                        gizmosnap[0] = 0.1f;
                        gizmosnap[1] = 0.1f;
                        gizmosnap[2] = 0.1f;
                    }
                    
                    //Draw Transform
                    ImGuizmo.Manipulate(ref view[0], ref proj[0],
                        op, MODE.LOCAL, 
                        ref node_transform[0], ref delta_transform[0], ref gizmosnap[0]);

                    if (ImGuizmo.IsUsing())
                    {
                        NbMatrix4 new_transform = NbCore.Math.Matrix4FromArray(node_transform, 0);
                        NbVector3 delta_loc = NbMatrix4.ExtractTranslation(new_transform);
                        NbQuaternion delta_rot = NbMatrix4.ExtractRotation(new_transform);
                        NbVector3 delta_scale = NbMatrix4.ExtractScale(new_transform);
                        
                        //Console.WriteLine($"{delta_loc.X} {delta_loc.Y} {delta_loc.Z}");
                        //Console.WriteLine($"{gizmosnap[0]}");
                        //Console.WriteLine($"{delta_rot[0]} {delta_rot[1]} {delta_rot[2]} {delta_rot[3]}");
                        //Console.WriteLine(delta_rot.ToString());

                        //Update node transform
                        if (op == OPERATION.ROTATE)
                        {
                            TransformComponent tc = node.GetComponent<TransformComponent>();
                            EngineRef.SetNodeRotation(node, delta_rot);
                        }
                        
                        if (op == OPERATION.TRANSLATE)
                        {
                            TransformComponent tc = node.GetComponent<TransformComponent>();
                            EngineRef.SetNodeLocation(node, delta_loc);
                        }

                        if (op == OPERATION.SCALE)
                        {
                            TransformComponent tc = node.GetComponent<TransformComponent>();
                            EngineRef.SetNodeScale(node, delta_scale); ;
                        }

                    }
                }
                
                ImGui.End();
            }

            ImGui.PopStyleVar(3);

            
            
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

            if (ImGui.Begin("Script Editor", ImGuiWindowFlags.NoCollapse |
                                              ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                _ImGuiManager.DrawScriptEditor();
                ImGui.End();
            }

            if (ImGui.Begin("Text Editor", ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoBringToFrontOnFocus |
                                           ImGuiWindowFlags.NoScrollbar))
            {
                _ImGuiManager.DrawTextEditor();
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
                ImGui.DragFloat("Test Option 1", ref NbRenderState.settings.RenderSettings.testOpt1);
                ImGui.DragFloat("Test Option 2", ref NbRenderState.settings.RenderSettings.testOpt2);
                ImGui.DragFloat("Test Option 3", ref NbRenderState.settings.RenderSettings.testOpt3);
                ImGui.End();
            }
#endif
            if (ImGui.Begin("Camera", ImGuiWindowFlags.NoCollapse |
                                               ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                //Camera Settings
                ImGui.BeginGroup();
                ImGui.TextColored(ImGuiManager.DarkBlue, "Camera Settings");
                ImGui.SliderInt("FOV", ref NbRenderState.settings.CamSettings.FOV, 15, 100);
                ImGui.SliderFloat("Sensitivity", ref NbRenderState.settings.CamSettings.Sensitivity, 1f, 10.0f);
                ImGui.InputFloat("MovementSpeed", ref NbRenderState.settings.CamSettings.Speed, 1.0f, 500000.0f);
                ImGui.SliderFloat("zNear", ref NbRenderState.settings.CamSettings.zNear, 0.5f, 1000.0f);
                ImGui.SliderFloat("zFar", ref NbRenderState.settings.CamSettings.zFar, 101.0f, 100000.0f);

                if (ImGui.Button("Reset Camera"))
                {
                    NbRenderState.activeCam.Reset();
                }

                ImGui.SameLine();
                
                if (ImGui.Button("Reset Scene Rotation"))
                {
                    NbRenderState.rotAngles = new NbVector3(0.0f);
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
                if (ImGui.CollapsingHeader("Viewport Options"))
                {
                    ImGui.Indent(1.0f);
                    ImGui.Checkbox("Show Lights", ref NbRenderState.settings.ViewSettings.ViewLights);
                    ImGui.Checkbox("Show Light Volumes", ref NbRenderState.settings.ViewSettings.ViewLightVolumes);
                    ImGui.Checkbox("Show Joints", ref NbRenderState.settings.ViewSettings.ViewJoints);
                    ImGui.Checkbox("Show Locators", ref NbRenderState.settings.ViewSettings.ViewLocators);
                    ImGui.Checkbox("Show Collisions", ref NbRenderState.settings.ViewSettings.ViewCollisions);
                    ImGui.Checkbox("Show Bounding Hulls", ref NbRenderState.settings.ViewSettings.ViewBoundHulls);
                    ImGui.Checkbox("Emulate Actions", ref NbRenderState.settings.ViewSettings.EmulateActions);
                    Vector3 col = new Vector3(NbRenderState.settings.RenderSettings.BackgroundColor.X,
                                          NbRenderState.settings.RenderSettings.BackgroundColor.Y,
                                          NbRenderState.settings.RenderSettings.BackgroundColor.Z);

                    if (ImGui.ColorEdit3("Background Color", ref col,
                    ImGuiColorEditFlags.DefaultOptions))
                    {
                        NbRenderState.settings.RenderSettings.BackgroundColor = new(col.X, col.Y, col.Z);
                    }
                    ImGui.Unindent(1.0f);
                }

                if (ImGui.CollapsingHeader("Rendering Options"))
                {
                    ImGui.Indent(1.0f);

                    ImGui.Checkbox("Use Textures", ref NbRenderState.settings.RenderSettings.UseTextures);
                    ImGui.Checkbox("Use Lighting", ref NbRenderState.settings.RenderSettings.UseLighting);

                    if (ImGui.Checkbox("Use VSYNC", ref NbRenderState.settings.RenderSettings.UseVSync))
                    {
                        WindowRef.SetVSync(NbRenderState.settings.RenderSettings.UseVSync);
                    }

                    ImGui.Checkbox("Show Animations", ref NbRenderState.settings.RenderSettings.ToggleAnimations);
                    ImGui.Checkbox("Wireframe", ref NbRenderState.settings.RenderSettings.RenderWireFrame);
                    ImGui.Checkbox("FXAA", ref NbRenderState.settings.RenderSettings.UseFXAA);
                    ImGui.Checkbox("Tone Mapping", ref NbRenderState.settings.RenderSettings.UseToneMapping);

                    if (ImGui.CollapsingHeader("Bloom Settings"))
                    {
                        ImGui.Indent();
                        ImGui.Columns(2);
                        ImGui.Text("Enable Bloom");
                        ImGui.NextColumn();
                        ImGui.Checkbox("##BloomFlag", ref NbRenderState.settings.RenderSettings.UseBLOOM);
                        ImGui.NextColumn();
                        ImGui.Text("Bloom Intensity");
                        ImGui.NextColumn();
                        ImGui.DragFloat("##BloomIntensity", ref NbRenderState.settings.RenderSettings.BloomIntensity, 0.01f, 0.0f, 1.0f);
                        ImGui.NextColumn();
                        ImGui.Text("Bloom Filter Radius");
                        ImGui.NextColumn();
                        ImGui.DragFloat("##BloomFilterRadius", ref NbRenderState.settings.RenderSettings.BloomFilterRadius, 0.0001f, 0.0001f, 0.2f);
                        ImGui.Columns(1);
                        ImGui.Unindent();
                    }

                    ImGui.Checkbox("LOD Filtering", ref NbRenderState.settings.RenderSettings.LODFiltering);

                    int fps_selection = Array.IndexOf(fps_settings, NbRenderState.settings.RenderSettings.FPS.ToString());
                    if (ImGui.Combo("FPS", ref fps_selection, fps_settings, fps_settings.Length))
                    {
                        WindowRef.SetRenderFrameFrequency(int.Parse(fps_settings[fps_selection]));
                    }

                    int tick_selection = Array.IndexOf(fps_settings, NbRenderState.settings.TickRate.ToString());
                    if (ImGui.Combo("Engine Tick Rate", ref tick_selection, fps_settings, fps_settings.Length))
                    {
                        WindowRef.SetUpdateFrameFrequency(int.Parse(fps_settings[tick_selection]));
                    }

                    ImGui.DragFloat("HDR Exposure",
                        ref NbRenderState.settings.RenderSettings.HDRExposure, 0.005f, 0.001f, 100.0f);


                    ImGui.Unindent(1.0f);
                }
                
                
                ImGui.End();
            }

            _ImGuiManager.ProcessModals(this, ref current_file_path, ref IsOpenFileDialogOpen);

            
            //Draw plugin panels and popups
            foreach (PluginBase plugin in EngineRef.Plugins.Values)
                plugin.Draw();

            //Debugging Information
            if (ImGui.Begin("Statistics"))
            {

                if (ImGui.CollapsingHeader("Application"))
                {
                    ImGui.Text(string.Format("RAM : {0, 3:F1} MBs", TotalProcMemory));
                    ImGui.Text(string.Format("CPU Usage : {0, 4:F3} ", CPUUsage));
                }
                
                if (ImGui.CollapsingHeader("Rendering"))
                {
                    ImGui.Text(string.Format("FPS : {0, 3:F1}", 1.0f / EngineRef.GetSystem<NbCore.Systems.RenderingSystem>().frameStats.Frametime));
                    ImGui.Text(string.Format("FrameTime : {0, 3:F6}", EngineRef.GetSystem<NbCore.Systems.RenderingSystem>().frameStats.Frametime));
                    ImGui.Text(string.Format("Vertices : {0}", EngineRef.GetSystem<NbCore.Systems.RenderingSystem>().frameStats.RenderedVerts));
                    ImGui.Text(string.Format("Tris : {0}", EngineRef.GetSystem<NbCore.Systems.RenderingSystem>().frameStats.RenderedIndices / 3));
                    ImGui.Text(string.Format("Meshes : {0}", EngineRef.GetEntityListCount(EntityType.Mesh)));
                    ImGui.Text(string.Format("Materials : {0}", EngineRef.GetEntityListCount(EntityType.Material)));
                    ImGui.Text(string.Format("Textures : {0}", EngineRef.GetEntityListCount(EntityType.Texture)));
                    ImGui.Text(string.Format("Shaders : {0}", EngineRef.GetEntityListCount(EntityType.Shader)));
                }

                if (ImGui.CollapsingHeader("Scripting"))
                {
                    ImGui.Text(string.Format("Scripts : {0}", EngineRef.GetEntityListCount(EntityType.Script)));
                    ImGui.Text(string.Format("Script Evaluations : {0}", EngineRef.GetSystem<NbCore.Systems.ScriptingSystem>().GetRegisteredComponents()));
                }

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
            NbShaderConfig conf;
            conf = EngineRef.CreateShaderConfig(EngineRef.GetShaderSourceByFilePath("Shaders/Simple_VSEmpty.glsl"),
                                      EngineRef.GetShaderSourceByFilePath("Shaders/texture_mixer_FS.glsl"),
                                      null, null, null,
                                      NbShaderMode.DEFFERED, "TextureMixerConfig");
            EngineRef.RegisterEntity(conf);

            //Create Material
            NbMaterial mat = new();
            mat.Name = "mixMaterial";
            NbVector4 vec = new(1.0f, 1.0f, 1.0f, 1.0f);
            NbUniform uf = new(NbUniformType.Float, "UseAlphaTextures", 1.0f);
            uf.ShaderBinding = "use_alpha_textures";
            mat.Uniforms.Add(uf);
            
            uf = new(NbUniformType.Float, "UseLayer0", 1.0f);
            uf.ShaderBinding = "lbaseLayersUsed[0]";
            
            mat.Uniforms.Add(uf);
            uf = new(NbUniformType.Float, "UseLayer1", 1.0f);
            uf.ShaderBinding = "lbaseLayersUsed[1]";
            
            mat.Uniforms.Add(uf);
            uf = new(NbUniformType.Vector4, "Recolor0", 1.0f, 0.0f, 0.0f, 0.5f);
            uf.ShaderBinding = "lRecolours[0]";
            
            mat.Uniforms.Add(uf);
            vec = new(1.0f, 1.0f, 0.0f, 0.5f);
            uf = new(NbUniformType.Vector4, "Recolor1", 1.0f, 1.0f, 0.0f, 0.5f);
            uf.ShaderBinding = "lRecolours[1]";
            
            mat.Uniforms.Add(uf);

            NbShader shader = EngineRef.CreateShader(conf);
            EngineRef.CompileShader(shader);
            
            //Register material
            EngineRef.RegisterEntity(mat);

            //Add Quad
            //Create Mesh
            NbCore.Quad q = new(1.0f, 1.0f);

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
            EngineRef.GetSystem<NbCore.Systems.TransformationSystem>().RequestEntityUpdate(new_node);
        }





    }
}



