using NbCore;
using NbCore;
using NbCore.Common;
using NbCore.Platform.Graphics;
using NbCore.Platform.Windowing;
using System.IO;
using System.Reflection;
using NbCore.Systems;
using ImGuiNET;
using System;
using System.Threading;
using SixLabors.ImageSharp.Processing;

namespace NibbleEditor
{
    public class Window : NbOpenGLWindow
    {
        //Workers
        private readonly WorkThreadDispacher workDispatcher = new();
        private readonly RequestHandler requestHandler = new();
        
        //Application Layers
        private RenderLayer _renderLayer;
        private UILayer _uiLayer;
        private NbLogger _logger;

        //Camera Stuff
        public CameraPos targetCameraPos;


        public Window(Engine e) : base(new NbVector2i(800,600), e)
        {
            //Set Window Title
            Title = "Nibble Editor " + Util.getVersion();

            //Initialize Logger
            _logger = new NbLogger()
            {
                LogVerbosity = LogVerbosityLevel.DEBUG
            };
            
            //Initialize Engine backend
            NbRenderState.engineRef = e; //Set reference to engine [Should do before initialization]
            NbRenderState.settings = EngineSettings.loadFromDisk(); //Load Settings

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

        public void SetRenderFreq(int fps)
        {
            SetRenderFrameFrequency(fps);
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

            Engine.Init(); //Initialize Engine

            //Add Default Resources
            SetupDefaultCamera();

            //Add Editor Resources
            SetupResources();

            //Load EnginePlugins
            LoadPlugins();
            
            //Initialize Application Layers
            _renderLayer = new(this, Engine);
            _uiLayer = new(this, Engine);

            //Attach Layers to events
            _uiLayer.CloseWindowEvent += CloseWindow;
            _uiLayer.SaveActiveSceneEvent += SaveActiveScene;
            _uiLayer.CaptureInput += MoveCamera;

            OnTextInput += _uiLayer.OnTextInput;
            _uiLayer.SceneWindowResizeEvent += _renderLayer.OnResize;
            //OnResize += _renderLayer.OnResize;
            _logger.LogEvent += _uiLayer.OnLog;
            
            //Pass rendering settings to the Window
            SetRenderFrameFrequency(NbRenderState.settings.RenderSettings.FPS);
            SetUpdateFrameFrequency(NbRenderState.settings.TickRate);
            SetVSync(NbRenderState.settings.RenderSettings.UseVSync);



#if (TRUE)
            //Create Default SceneGraph
            SceneGraph graph = Engine.GetSystem<SceneManagementSystem>().CreateSceneGraph();
            
            //Create Test SceneGraph
            SceneGraphNode root = Engine.CreateSceneNode("SceneRoot");
            graph.Root = root;
            
            SceneGraphNode test1 = Engine.CreateLocatorNode("Test Locator 1");
            SceneGraphNode test2 = Engine.CreateLocatorNode("Test Locator 2");
            test1.AddChild(test2);
            SceneGraphNode test3 = Engine.CreateLocatorNode("Test Locator 3");
            test2.AddChild(test3);

            SceneGraphNode light = Engine.CreateLightNode("Default Light", new NbVector3(1.0f), 300.0f, 1000.0f, ATTENUATION_TYPE.QUADRATIC, LIGHT_TYPE.POINT);
            Engine.SetNodeLocation(light, new NbVector3(5.0f, 0.0f, 5.0f));
            Engine.SetNodeScale(light, new NbVector3(1.0f));
            test1.AddChild(light);
            root.AddChild(test1);


            //Test line cross
            NbPrimitive line_cross = new LineCross(0.0008f, 10.0f);

            //Create 
            NbMesh line_cross_mesh = new()
            {

                Data = line_cross.geom.GetMeshData(),
                MetaData = line_cross.geom.GetMetaData(),
                Material = Engine.GetMaterialByName("defaultMat")
            };
            line_cross_mesh.Hash = NbHasher.CombineHash(line_cross_mesh.Data.Hash,
                                                        line_cross_mesh.MetaData.GetHash());

            SceneGraphNode line_cross_node = Engine.CreateMeshNode("test_line_cross", line_cross_mesh);

            root.AddChild(line_cross_node);
            
            Engine.ImportSceneGraph(graph);
            Engine.GetSystem<SceneManagementSystem>().SetActiveScene(graph);
#endif
            //Check if Temp folder exists
            if (!Directory.Exists("Temp")) Directory.CreateDirectory("Temp");

        }

        private void SaveActiveScene()
        {
            SceneGraph g = Engine.GetActiveSceneGraph();
            Engine.SerializeScene(g, "scene_output.nb");
        }

        private void CloseWindow()
        {
            //Detach Scene Update and Rendering
            OnFrameUpdate -= UpdateFrame;
            OnRenderUpdate -= RenderFrame;
            
            //Cleanup and Close
            Engine.CleanUp();
            Close();
        }
        
        public void UpdateFrame(double dt)
        {
            Camera.CalculateNextCameraState(NbRenderState.activeCam, targetCameraPos);
            targetCameraPos.Reset();
            
            _renderLayer.OnFrameUpdate(dt);
        }

        public void RenderFrame(double dt)
        {
            //Update Camera
            Camera.UpdateCameraDirectionalVectors(NbRenderState.activeCam);
            NbRenderState.activeCam.updateViewMatrix();
            
            //Render data
            _renderLayer.OnRenderFrameUpdate(dt);
            _uiLayer.OnRenderFrameUpdate(dt);

            UpdateInput();
        }

        private void SetupResources()
        {
            //TODO: Create assets for the imposter material/texture/meshes
            
            //Initialize Logo Atlas Texture
            byte[] imgData = Callbacks.getResourceFromAssembly(Assembly.GetExecutingAssembly(),
            "atlas.png");


            NbTexture tex = Engine.CreateTexture(imgData, "atlas.png",
                NbTextureWrapMode.Repeat, NbTextureFilter.Linear, NbTextureFilter.Linear, false);
            Engine.RegisterEntity(tex);

            //Create Imposter Shader Config 
            NbShaderSource vs = Engine.GetShaderSourceByFilePath(".//Assets//Shaders//Source//imposter_vs.glsl");
            if (vs == null)
                vs = new NbShaderSource(".//Assets//Shaders//Source//imposter_vs.glsl", true);

            NbShaderSource fs = Engine.GetShaderSourceByFilePath(".//Assets//Shaders//Source//imposter_fs.glsl");
            if (fs == null)
                fs = new NbShaderSource(".//Assets//Shaders//Source//imposter_fs.glsl", true);

            NbShaderConfig conf = Engine.CreateShaderConfig(vs, fs, null, null, null, NbShaderMode.DEFFERED, "Imposter", true);
            
            Engine.RegisterEntity(conf);

            //Create Imposter Material
            NbMaterial imposterMat = new()
            {
                Name = "ImposterMat",
            };

            imposterMat.Samplers.Add(new NbSampler()
            {
                Texture = tex,
                Name = "Tex",
                SamplerID = 0,
                ShaderBinding = "mpCustomPerMaterial.gDiffuseMap"
            });
            
            imposterMat.AddFlag(NbMaterialFlagEnum._NB_DIFFUSE_MAP);
            imposterMat.AddFlag(NbMaterialFlagEnum._NB_UNLIT);
            Engine.RegisterEntity(imposterMat);

            NbShader ImposterShader = Engine.CreateShader(conf, Engine.GetMaterialShaderDirectives(imposterMat));
            
            if (Engine.CompileShader(ImposterShader))
                Engine.SetMaterialShader(imposterMat, conf);
            else
            {
                Log("Error during shader compilation", LogVerbosityLevel.ERROR);
            }
            
            //Create Imposter Mesh
            //Imposter quad
            Quad q = new Quad();

            NbMesh mesh = new()
            {
                Hash = NbHasher.Hash("default_imposter_quad"),
                Data = q.geom.GetMeshData(),
                MetaData = q.geom.GetMetaData(),
                Material = imposterMat
            };

            Engine.RegisterEntity(mesh);
            q.Dispose();

            //Create Gizmo
            //CreateGizmo();



        }


        
        #region INPUT_HANDLERS

        //Gamepad handler
        //private void gamepadController()
        //{
        //    if (gpHandler == null) return;
        //    if (!gpHandler.isConnected()) return;

        //    //Camera Movement
        //    float cameraSensitivity = 2.0f;
        //    float x, y, z, rotx, roty;

        //    x = gpHandler.getAction(ControllerActions.MOVE_X);
        //    y = gpHandler.getAction(ControllerActions.ACCELERATE) - gpHandler.getAction(ControllerActions.DECELERATE);
        //    z = gpHandler.getAction(ControllerActions.MOVE_Y_NEG) - gpHandler.getAction(ControllerActions.MOVE_Y_POS);
        //    rotx = -cameraSensitivity * gpHandler.getAction(ControllerActions.CAMERA_MOVE_H);
        //    roty = cameraSensitivity * gpHandler.getAction(ControllerActions.CAMERA_MOVE_V);

        //    targetCameraPos.PosImpulse.X = x;
        //    targetCameraPos.PosImpulse.Y = y;
        //    targetCameraPos.PosImpulse.Z = z;
        //    targetCameraPos.Rotation.X = rotx;
        //    targetCameraPos.Rotation.Y = roty;
        //}

        //Keyboard handler
        private int keyDownStateToInt(NbKey k)
        {
            bool state = IsKeyDown(k);
            return state ? 1 : 0;
        }

        public void MoveCamera()
        {
            //Move Camera
            keyboardController();
            mouseController();
            //gpController(); //TODO: Re-add controller support
        }

        public void UpdateInput()
        {
            //Check for shortcuts
            if (IsKeyDown(NbKey.LeftCtrl) && IsKeyPressed(NbKey.Q))
            {
                CloseWindow();
            }
        }

        #endregion

        private void keyboardController()
        {
            //Camera Movement
            float step = 0.002f;
            float x, y, z;

            x = keyDownStateToInt(NbKey.D) - keyDownStateToInt(NbKey.A);
            z = keyDownStateToInt(NbKey.W) - keyDownStateToInt(NbKey.S);
            y = keyDownStateToInt(NbKey.R) - keyDownStateToInt(NbKey.F);

            //Camera rotation is done exclusively using the mouse

            //rotx = 50 * step * (kbHandler.getKeyStatus(OpenTK.Input.Key.E) - kbHandler.getKeyStatus(OpenTK.Input.Key.Q));
            //float roty = (kbHandler.getKeyStatus(Key.C) - kbHandler.getKeyStatus(Key.Z));

            //RenderState.rotAngles.Y += 100 * step * (keyDownStateToInt(NbKey.E) - keyDownStateToInt(NbKey.Q));
            //RenderState.rotAngles.Y %= 360;

            //Move Camera
            targetCameraPos.PosImpulse.X = x;
            targetCameraPos.PosImpulse.Y = y;
            targetCameraPos.PosImpulse.Z = z;
        } 

        //Mouse Methods

        public void mouseController()
        {
            if (IsMouseButtonDown(NbMouseButton.RIGHT) | IsKeyDown(NbKey.LeftAlt))
            {
                targetCameraPos.Rotation.X = MouseDelta.X;
                targetCameraPos.Rotation.Y = MouseDelta.Y;
            }
            
            if (IsMouseButtonDown(NbMouseButton.MIDDLE))
            {
                NbVector2 delta = new NbVector2(MouseDelta.X, MouseDelta.Y);
                delta.Normalize();
                if (!float.IsNaN(delta.X))
                    targetCameraPos.PosImpulse.X = -2.0f * delta.X;
                if (!float.IsNaN(delta.Y))
                    targetCameraPos.PosImpulse.Y = 2.0f * delta.Y;
                //Log($"{targetCameraPos.PosImpulse.X} {targetCameraPos.PosImpulse.Y}", LogVerbosityLevel.INFO);
            }
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
            NbRenderState.activeCam = cam;

            //Set Camera Initial State
            TransformController tcontroller = Engine.GetSystem<TransformationSystem>().GetEntityTransformController(cam);
            tcontroller.AddFutureState(new NbVector3(0.0f, 0.2f, 0.5f), NbQuaternion.FromEulerAngles(0.0f, -3.14f / 2.0f, 0.0f, "XYZ"), new NbVector3(1.0f));
        }


        #endregion


    }




}
