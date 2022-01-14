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
        //Engine
        private Engine engine;

        //Workers
        private readonly WorkThreadDispacher workDispatcher = new();
        private readonly RequestHandler requestHandler = new();
        
        //Application Layers
        private InputLayer _inputLayer;
        private RenderLayer _renderLayer;
        private UILayer _uiLayer;

        //Events
        public delegate void NewSceneEventHandler(Scene s);
        public NewSceneEventHandler NewSceneEvent;
        
        public Window() : base(GameWindowSettings.Default, 
            new NativeWindowSettings() { Size = new Vector2i(800, 600), APIVersion = new System.Version(4, 5) })
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


            //Initialize Application Layers
            _inputLayer = new(engine);
            _renderLayer = new(engine);
            _uiLayer = new(this, engine);

            //Attach Layers to events
            KeyDown += _inputLayer.OnKeyDown;
            KeyUp += _inputLayer.OnKeyUp;
            MouseDown += _inputLayer.OnMouseDown;
            MouseMove += _inputLayer.OnMouseMove;
            MouseUp += _inputLayer.OnMouseUp;
            MouseWheel += _inputLayer.OnMouseWheel;
            _uiLayer.CaptureInput += _inputLayer.OnCaptureInputChanged;
            _uiLayer.CloseWindowEvent += CloseWindow;

            NewSceneEvent += new NewSceneEventHandler(_uiLayer.OnNewScene);
            TextInput += _uiLayer.OnTextInput;
            Resize += _uiLayer.OnResize;

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
            NewSceneEvent?.Invoke(scene);
            
            //Check if Temp folder exists
#if DEBUG
            if (!Directory.Exists("Temp")) Directory.CreateDirectory("Temp");
#endif
            //Set active Components
            Util.activeWindow = this;

        }

        private void CloseWindow()
        {
            engine.CleanUp();
            Close();
        }
        
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            
            Queue<object> data = new();
            
            _inputLayer.OnFrameUpdate(ref data, e.Time);
            _renderLayer.OnFrameUpdate(ref data, e.Time);
            _uiLayer.OnFrameUpdate(ref data, e.Time);
            
            
            //Pass Global rendering settings
            VSync = RenderState.settings.renderSettings.UseVSync ? VSyncMode.On : VSyncMode.Off;
            RenderFrequency = RenderState.settings.renderSettings.FPS;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            //Render data
            Queue<object> data = new();

            //_inputLayer.OnRenderFrameUpdate(ref data, e.Time);
            _renderLayer.OnRenderFrameUpdate(ref data, e.Time);
            _uiLayer.OnRenderFrameUpdate(ref data, e.Time);

            RenderStats.fpsCount = 1.0f / (float)e.Time;
            SwapBuffers();
        }

        private static void Log(string msg, LogVerbosityLevel lvl)
        {
            Callbacks.Log("* WINDOW : " + msg, lvl);
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

       
        
        

    }




}
