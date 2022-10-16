using System;
using System.Threading;

using NbCore;
using NbCore.Math;
using NbCore.Common;
using System.Collections.Generic;
using NbCore.Platform.Windowing;
using System.IO;


namespace NibbleEditor
{
    public class Window : NbOpenGLWindow
    {
        //Workers
        private readonly WorkThreadDispacher workDispatcher = new();
        private readonly RequestHandler requestHandler = new();
        
        //Application Layers
        private InputLayer _inputLayer;
        private RenderLayer _renderLayer;
        private UILayer _uiLayer;
        private NbLogger _logger;
        
        public Window(Engine e) : base(new NbVector2i(800,600), e)
        {
            //Set Window Title
            Title = "Nibble Editor " + Util.getVersion();

            //Initialize Logger
            _logger = new NbLogger()
            {
                LogVerbosity = LogVerbosityLevel.INFO
            };
            
            //Initialize Engine backend
            RenderState.engineRef = e; //Set reference to engine [Should do before initialization]
            RenderState.settings = EngineSettings.loadFromDisk(); //Load Settings

            //Connect Window Callbacks
            OnWindowLoad += WindowLoad;
            OnFrameUpdate += UpdateFrame;
            OnRenderUpdate += RenderFrame;
            
            //Start worker thread
            workDispatcher.Start();
        }

        private void LoadPlugins()
        {
            if (!Directory.Exists("Plugins"))
                return;

            foreach (string filename in Directory.GetFiles("Plugins"))
            {
                if (!filename.EndsWith(("dll")))
                    continue;

                if (!Path.GetFileName(filename).StartsWith(("Nibble")))
                    continue;

                Engine.LoadPlugin(filename);
            }
        }

        private void WindowLoad()
        {
            //OVERRIDE SETTINGS
            //FileUtils.dirpath = "I:\\SteamLibrary1\\steamapps\\common\\No Man's Sky\\GAMEDATA\\PCBANKS";

            //Set Window Callbacks
            Callbacks.SetDefaultCallbacks();
            Callbacks.updateStatus = Util.setStatus;
            Callbacks.showInfo = Util.showInfo;
            Callbacks.showError = Util.showError;
            Callbacks.Log = _logger.Log;

            //Add Default Resources
            SetupDefaultCamera();

            Engine.Init(); //Initialize Engine

            //Load EnginePlugins
            LoadPlugins();
            
            //Initialize Application Layers
            _inputLayer = new(Engine);
            _renderLayer = new(Engine);
            _uiLayer = new(this, Engine);

            //Attach Layers to events
            OnKeyDown += _inputLayer.OnKeyDown;
            OnKeyUp += _inputLayer.OnKeyUp;

            OnMouseButtonDown += _inputLayer.OnMouseDown;
            OnMouseMove += _inputLayer.OnMouseMove;
            OnMouseButtonUp += _inputLayer.OnMouseUp;
            OnMouseWheel += _inputLayer.OnMouseWheel;
            _uiLayer.CaptureInput += _inputLayer.OnCaptureInputChanged;
            _uiLayer.CloseWindowEvent += CloseWindow;
            _uiLayer.SaveActiveSceneEvent += SaveActiveScene;

            OnTextInput += _uiLayer.OnTextInput;
            OnResize += _uiLayer.OnResize;
            _logger.LogEvent += _uiLayer.OnLog;
            
            //Pass rendering settings to the Window
            SetRenderFrameFrequency(0);
            SetFrameUpdateFrequency(15);
            SetVSync(RenderState.settings.RenderSettings.UseVSync);
            
            //Create Default SceneGraph
            Engine.GetSystem<NbCore.Systems.SceneManagementSystem>().CreateSceneGraph();
            Engine.GetSystem<NbCore.Systems.SceneManagementSystem>().SetActiveScene(Engine.GetSystem<NbCore.Systems.SceneManagementSystem>().SceneGraphs[0]);
            SceneGraph graph = Engine.GetActiveSceneGraph();

#if DEBUG
            //Create Test Scene
            SceneGraphNode test1 = Engine.CreateLocatorNode("Test Locator 1");
            SceneGraphNode test2 = Engine.CreateLocatorNode("Test Locator 2");
            test1.AddChild(test2);
            SceneGraphNode test3 = Engine.CreateLocatorNode("Test Locator 3");
            test2.AddChild(test3);

            SceneGraphNode light = Engine.CreateLightNode("Default Light", 200.0f, ATTENUATION_TYPE.QUADRATIC, LIGHT_TYPE.POINT);
            NbCore.Systems.TransformationSystem.SetEntityLocation(light, new NbVector3(100.0f, 100.0f, 100.0f));
            test1.AddChild(light);
            test1.SetParent(graph.Root);

            //Request tranform update for the added nodes
            Engine.RegisterSceneGraphTree(test1, true);
            Engine.RequestEntityTransformUpdate(test1);
#endif
            //Populate SceneGraphView
            Engine.NewSceneEvent?.Invoke(graph);
            
            //Check if Temp folder exists
#if DEBUG
            if (!Directory.Exists("Temp")) Directory.CreateDirectory("Temp");
#endif
        }

        private void SaveActiveScene()
        {
            SceneGraph g = Engine.GetActiveSceneGraph();
            Engine.SerializeScene(g, "scene_output.nb");
        }

        private void CloseWindow()
        {
            Engine.CleanUp();
            Close();
        }
        
        public void UpdateFrame(double dt)
        {
            Queue<object> data = new();
            
            _inputLayer.OnFrameUpdate(ref data, dt);
            _renderLayer.OnFrameUpdate(ref data, dt);
            _uiLayer.OnFrameUpdate(ref data, dt);

            //Pass Global rendering settings
            SetVSync(RenderState.settings.RenderSettings.UseVSync);
            SetRenderFrameFrequency(RenderState.settings.RenderSettings.FPS);
            SetFrameUpdateFrequency(RenderState.settings.TickRate);
        }

        public void RenderFrame(double dt)
        {
            //Render data
            Queue<object> data = new();
            
            //_inputLayer.OnRenderFrameUpdate(ref data, e.Time);
            _renderLayer.OnRenderFrameUpdate(ref data, dt);
            _uiLayer.OnRenderFrameUpdate(ref data, dt);
        }

        #region ResourceManager

        private void SetupDefaultCamera()
        {
            //Add Camera
            Camera cam = new(0, true)
            {
                isActive = false
            };

            //Add Necessary Components to Camera
            NbCore.Systems.TransformationSystem.AddTransformComponentToEntity(cam);
            TransformComponent tc = cam.GetComponent<TransformComponent>() as TransformComponent;
            tc.IsControllable = true;
            Engine.RegisterEntity(cam);

            //Set global reference to cam
            RenderState.activeCam = cam;

            //Set Camera Initial State
            TransformController tcontroller = Engine.GetSystem<NbCore.Systems.TransformationSystem>().GetEntityTransformController(cam);
            tcontroller.AddFutureState(new NbVector3(0.0f, 0.2f, 0.5f), NbQuaternion.FromEulerAngles(0.0f, -3.14f / 2.0f, 0.0f, "XYZ"), new NbVector3(1.0f));
        }


        #endregion


    }




}
