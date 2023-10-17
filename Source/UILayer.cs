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

namespace NibbleEditor
{
    public delegate void CloseWindowEventHandler();
    public delegate void CaptureInputHandler(bool state);
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

        //Stats
        float TotalProcMemory = 0.0f;
        double CPUUsage = 0.0;
        TimeSpan lastProcTotalTime = new TimeSpan();
        DateTime lastTime = DateTime.Now;

        //UI Privates
        private bool TranslationGizmoToggle = false;
        private bool RotationnGizmoToggle = false;
        private bool ScaleGizmoToggle = false;
        private float[] delta_transform = new float[16];

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

            bool keep_window_open = true;
            int statusBarHeight = (int)(1.75f * ImGui.CalcTextSize("Status").Y);
            float last_window_height = 0.0f;
            ImGui.Begin("##MainWindow", ref keep_window_open, window_flags);

            
            ImGui.PopStyleVar(3);

            //Main Menu
            bool show_settings_window = false;
            Vector2 mainMenuBarPos = ImGui.GetCursorPos();
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


            ImGui.SetNextWindowPos(new Vector2(vp.WorkPos.X, ImGui.GetFrameHeight()));
            ImGui.SetNextWindowSize(new(WindowRef.Size.X, 0.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, 0.0f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, 0x60000000);
            if (ImGui.Begin("SecondaryMenuBar", ImGuiWindowFlags.NoDocking |
                                                ImGuiWindowFlags.NoMove |
                                                ImGuiWindowFlags.NoScrollbar |
                                                ImGuiWindowFlags.NoResize|
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
                if (RenderState.AppMode == ApplicationMode.EDIT)
                {
                    uv0 = new Vector2(atlas_image_id * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 1.0f);
                } else
                {
                    uv0 = new Vector2((atlas_image_id + 1) * atlas_image_uv_step, 0.0f);
                    uv1 = new Vector2((atlas_image_id + 2) * atlas_image_uv_step, 1.0f);
                }

                if (ImGui.ImageButton("Toggle##ApplicationMode",
                                (IntPtr)atlas_tex.GpuID,
                                new Vector2(img_size, img_size),
                                uv0, uv1))
                {
                    RenderState.AppMode = RenderState.AppMode == ApplicationMode.EDIT ? ApplicationMode.GAME : ApplicationMode.EDIT;
                }

                ImGui.SameLine();

                //Toggle Lighting Button
                atlas_image_id = 2;
                if (RenderState.settings.RenderSettings.UseLighting == false)
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
                    RenderState.settings.RenderSettings.UseLighting = !RenderState.settings.RenderSettings.UseLighting;
                }

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

                last_window_height += ImGui.GetWindowHeight();
                ImGui.End();
            }
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(3);


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
                csize.X = System.Math.Max(csize.X, 100);
                csize.Y = System.Math.Max(csize.Y, 100);
                NbVector2i csizetk = new((int)csize.X, (int)csize.Y);

                FBO render_fbo = EngineRef.GetSystem<NbCore.Systems.RenderingSystem>().getRenderFBO();

                //Calculate SceneViewport UVs
                NbVector2 fbo_center = new(render_fbo.Size.X / 2.0f, render_fbo.Size.Y / 2.0f);
                NbVector2 A = fbo_center - new NbVector2(csize.X / 2.0f, csize.Y / 2.0f);
                NbVector2 D = fbo_center + new NbVector2(csize.X / 2.0f, csize.Y / 2.0f);
                NbVector2 uv0 = new NbVector2(A.X / render_fbo.Size.X, A.Y / render_fbo.Size.Y);
                NbVector2 uv1 = new NbVector2(D.X / render_fbo.Size.X, D.Y / render_fbo.Size.Y);
                
                //Workaround to not use the scaled viewport
                uv0.X = 0.0f;
                uv0.Y = 0.0f;
                uv1.X = 1.0f;
                uv1.Y = 1.0f;

                ImGui.Image(new IntPtr(render_fbo.GetTexture(NbFBOAttachment.Attachment3).GpuID),
                                csize,
                                new System.Numerics.Vector2(uv0.X, 1.0f - uv0.Y),
                                new System.Numerics.Vector2(uv1.X, 1.0f - uv1.Y),
                                new System.Numerics.Vector4(1.0f),
                                new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 0.2f));

                bool active_status = ImGui.IsItemHovered();
                CaptureInput?.Invoke(active_status);


                //Imguizmo Setup
                ImGuizmo.SetOrthographic(false);
                ImGuizmo.SetDrawlist();
                ImGuizmo.SetRect(ImGui.GetWindowPos().X, ImGui.GetWindowPos().Y, 
                                                ImGui.GetWindowWidth(), ImGui.GetWindowHeight());
                
                float[] view = RenderState.activeCam.lookMat.ToArray();
                float[] proj = RenderState.activeCam.projMat.ToArray();
                float[] transform = NbMatrix4.Identity().ToArray();

                
                //Draw Grid
                ImGuizmo.DrawGrid(ref view[0], ref proj[0], ref transform[0], 2.0f);

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
                        NbMatrix4 new_transform = NbCore.Math.Matrix4FromArray(delta_transform, 0);
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
                            EngineRef.SetNodeRotation(node, delta_rot * tc.Data.localRotation);
                        }
                        
                        if (op == OPERATION.TRANSLATE)
                        {
                            TransformComponent tc = node.GetComponent<TransformComponent>();
                            EngineRef.SetNodeLocation(node, tc.Data.localTranslation + delta_loc);
                        }

                        if (op == OPERATION.SCALE)
                        {
                            TransformComponent tc = node.GetComponent<TransformComponent>();
                            EngineRef.SetNodeScale(node, tc.Data.localScale * delta_scale); ;
                        }

                    }
                }
                
                ImGui.End();
            }

            ImGui.PopStyleVar(2);

            
            
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
                ImGui.DragFloat("Test Option 1", ref RenderState.settings.RenderSettings.testOpt1);
                ImGui.DragFloat("Test Option 2", ref RenderState.settings.RenderSettings.testOpt2);
                ImGui.DragFloat("Test Option 3", ref RenderState.settings.RenderSettings.testOpt3);
                ImGui.End();
            }
#endif
            if (ImGui.Begin("Camera", ImGuiWindowFlags.NoCollapse |
                                               ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                //Camera Settings
                ImGui.BeginGroup();
                ImGui.TextColored(ImGuiManager.DarkBlue, "Camera Settings");
                ImGui.SliderInt("FOV", ref RenderState.settings.CamSettings.FOV, 15, 100);
                ImGui.SliderFloat("Sensitivity", ref RenderState.settings.CamSettings.Sensitivity, 1f, 10.0f);
                ImGui.InputFloat("MovementSpeed", ref RenderState.settings.CamSettings.Speed, 1.0f, 500000.0f);
                ImGui.SliderFloat("zNear", ref RenderState.settings.CamSettings.zNear, 0.5f, 1000.0f);
                ImGui.SliderFloat("zFar", ref RenderState.settings.CamSettings.zFar, 101.0f, 100000.0f);

                if (ImGui.Button("Reset Camera"))
                {
                    RenderState.activeCam.Reset();
                }

                ImGui.SameLine();
                
                if (ImGui.Button("Reset Scene Rotation"))
                {
                    RenderState.rotAngles = new NbVector3(0.0f);
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
                
                
                ImGui.Checkbox("Show Lights", ref RenderState.settings.ViewSettings.ViewLights);
                ImGui.Checkbox("Show Light Volumes", ref RenderState.settings.ViewSettings.ViewLightVolumes);
                ImGui.Checkbox("Show Joints", ref RenderState.settings.ViewSettings.ViewJoints);
                ImGui.Checkbox("Show Locators", ref RenderState.settings.ViewSettings.ViewLocators);
                ImGui.Checkbox("Show Collisions", ref RenderState.settings.ViewSettings.ViewCollisions);
                ImGui.Checkbox("Show Bounding Hulls", ref RenderState.settings.ViewSettings.ViewBoundHulls);
                ImGui.Checkbox("Emulate Actions", ref RenderState.settings.ViewSettings.EmulateActions);

                ImGui.Separator();
                ImGui.LabelText("Rendering Options", "");

                ImGui.Checkbox("Use Textures", ref RenderState.settings.RenderSettings.UseTextures);
                ImGui.Checkbox("Use Lighting", ref RenderState.settings.RenderSettings.UseLighting);

                bool vsync = RenderState.settings.RenderSettings.UseVSync;
                if (ImGui.Checkbox("Use VSYNC", ref vsync))
                {
                    WindowRef.SetVSync(vsync);
                }
                
                ImGui.Checkbox("Show Animations", ref RenderState.settings.RenderSettings.ToggleAnimations);
                ImGui.Checkbox("Wireframe", ref RenderState.settings.RenderSettings.RenderWireFrame);
                ImGui.Checkbox("FXAA", ref RenderState.settings.RenderSettings.UseFXAA);
                ImGui.Checkbox("Bloom", ref RenderState.settings.RenderSettings.UseBLOOM);
                ImGui.Checkbox("LOD Filtering", ref RenderState.settings.RenderSettings.LODFiltering);

                int fps_selection = Array.IndexOf(fps_settings, RenderState.settings.RenderSettings.FPS.ToString());
                if (ImGui.Combo("FPS", ref fps_selection, fps_settings, fps_settings.Length))
                {
                    WindowRef.SetRenderFrameFrequency(int.Parse(fps_settings[fps_selection]));
                }

                int tick_selection = Array.IndexOf(fps_settings, RenderState.settings.TickRate.ToString());
                if (ImGui.Combo("Engine Tick Rate", ref tick_selection, fps_settings, fps_settings.Length))
                {
                    WindowRef.SetUpdateFrameFrequency(int.Parse(fps_settings[tick_selection]));
                }

                Vector3 col = new Vector3(RenderState.settings.RenderSettings.BackgroundColor.X,
                                          RenderState.settings.RenderSettings.BackgroundColor.Y,
                                          RenderState.settings.RenderSettings.BackgroundColor.Z);

                ImGui.SetNextItemWidth(300.0f);
                if (ImGui.ColorPicker3("Background Color", ref col, 
                    ImGuiColorEditFlags.DefaultOptions))
                {
                    RenderState.settings.RenderSettings.BackgroundColor = new(col.X, col.Y, col.Z);
                }

                ImGui.DragFloat("HDR Exposure", 
                    ref RenderState.settings.RenderSettings.HDRExposure, 0.005f, 0.001f, 100.0f);
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
            EngineRef.GetSystem<NbCore.Systems.TransformationSystem>().RequestEntityUpdate(new_node);
        }





    }
}



