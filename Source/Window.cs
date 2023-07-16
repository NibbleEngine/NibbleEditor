using NbCore;
using NbCore.Math;
using NbCore.Common;
using NbCore.Platform.Graphics;
using NbCore.Platform.Windowing;
using System.IO;

using System.Reflection;
using NbCore.Primitives;
using NbCore.Systems;
using ImGuiNET;
using System;
using System.Threading;

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

            OnTextInput += _uiLayer.OnTextInput;
            OnResize += _uiLayer.OnResize;
            OnResize += _renderLayer.OnResize;
            _logger.LogEvent += _uiLayer.OnLog;
            
            //Pass rendering settings to the Window
            SetRenderFrameFrequency(60);
            SetUpdateFrameFrequency(60);
            SetVSync(RenderState.settings.RenderSettings.UseVSync);



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

            SceneGraphNode light = Engine.CreateLightNode("Default Light", new NbVector3(1.0f), 200.0f, ATTENUATION_TYPE.QUADRATIC, LIGHT_TYPE.POINT);
            TransformationSystem.SetEntityLocation(light, new NbVector3(5.0f, 0.0f, 5.0f));
            TransformationSystem.SetEntityScale(light, new NbVector3(100.0f));
            test1.AddChild(light);
            root.AddChild(test1);

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
            Camera.CalculateNextCameraState(RenderState.activeCam, targetCameraPos);
            targetCameraPos.Reset();
            
            _renderLayer.OnFrameUpdate(dt);
        }

        public void RenderFrame(double dt)
        {
            UpdateInput();

            //Update Camera
            Camera.UpdateCameraDirectionalVectors(RenderState.activeCam);
            RenderState.activeCam.updateViewMatrix();
            
            //Render data
            _renderLayer.OnRenderFrameUpdate(dt);
            _uiLayer.OnRenderFrameUpdate(dt);
        }

        private void SetupResources()
        {
            //TODO: Create assets for the imposter material/texture/meshes
            
            //Initialize Logo Atlas Texture
            byte[] imgData = Callbacks.getResourceFromAssembly(Assembly.GetExecutingAssembly(),
            "lamp.png");
            
            NbTexture tex = new NbTexture("atlas.png", imgData);
            GraphicsAPI.GenerateTexture(tex);
            GraphicsAPI.UploadTexture(tex);
            tex.Data.Data = null; //Release cpu data
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


        private void CreateGizmo()
        {
            //Create Gizmo Material
            NbMaterial gizmo_mat = new NbMaterial();
            gizmo_mat.Class = NbMaterialClass.Transluscent;
            gizmo_mat.AddFlag(NbMaterialFlagEnum._NB_VERTEX_COLOUR);
            gizmo_mat.AddFlag(NbMaterialFlagEnum._NB_UNLIT);
            gizmo_mat.Shader = Engine.GetShaderByHash(Engine.CalculateShaderHash(Engine.GetShaderConfigByName("UberShader_Deferred"), 
                                                                                 Engine.GetMaterialShaderDirectives(gizmo_mat)));
            
            //Translation Gizmo
            TranslationGizmo trans = new();
            
            //Create Meshes
            LineSegment x_line = new LineSegment(new NbVector3(0.0f, 0.0f, 0.0f),
                                                 new NbVector3(1.0f, 0.0f, 0.0f),
                                                 new NbVector3(1.0f, 0.0f, 0.0f));
            LineSegment y_line = new LineSegment(new NbVector3(0.0f, 0.0f, 0.0f),
                                                 new NbVector3(0.0f, 1.0f, 0.0f),
                                                 new NbVector3(0.0f, 1.0f, 0.0f));
            LineSegment z_line = new LineSegment(new NbVector3(0.0f, 0.0f, 0.0f),
                                                 new NbVector3(0.0f, 0.0f, 1.0f),
                                                 new NbVector3(0.0f, 0.0f, 1.0f));

            trans.XAxisMesh = new()
            {
                Hash = NbHasher.Hash("translation_gizmo_x"),
                Data = x_line.geom.GetMeshData(),
                MetaData = x_line.geom.GetMetaData(),
                Material = gizmo_mat
            };

            trans.YAxisMesh = new()
            {
                Hash = NbHasher.Hash("translation_gizmo_y"),
                Data = y_line.geom.GetMeshData(),
                MetaData = y_line.geom.GetMetaData(),
                Material = gizmo_mat
            };

            trans.ZAxisMesh = new()
            {
                Hash = NbHasher.Hash("translation_gizmo_z"),
                Data = z_line.geom.GetMeshData(),
                MetaData = z_line.geom.GetMetaData(),
                Material = gizmo_mat
            };

            //Create Nodes for the gizmo
            trans.XAxis = Engine.CreateMeshNode("XAxis", trans.XAxisMesh);
            trans.YAxis = Engine.CreateMeshNode("YAxis", trans.YAxisMesh);
            trans.ZAxis = Engine.CreateMeshNode("ZAxis", trans.ZAxisMesh);

            //Register Nodes 
            Engine.RegisterEntity(trans.XAxis);
            Engine.RegisterEntity(trans.YAxis);
            Engine.RegisterEntity(trans.ZAxis);


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

        public void UpdateInput()
        {
            keyboardController();
            mouseController();
            //gpController(); //TODO: Re-add controller support

            
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

            RenderState.rotAngles.Y += 100 * step * (keyDownStateToInt(NbKey.E) - keyDownStateToInt(NbKey.Q));
            RenderState.rotAngles.Y %= 360;

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
            RenderState.activeCam = cam;

            //Set Camera Initial State
            TransformController tcontroller = Engine.GetSystem<NbCore.Systems.TransformationSystem>().GetEntityTransformController(cam);
            tcontroller.AddFutureState(new NbVector3(0.0f, 0.2f, 0.5f), NbQuaternion.FromEulerAngles(0.0f, -3.14f / 2.0f, 0.0f, "XYZ"), new NbVector3(1.0f));
        }


        #endregion


    }




}
