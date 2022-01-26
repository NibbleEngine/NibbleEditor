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

                engine.LoadPlugin(filename);
            }
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            
            //OVERRIDE SETTINGS
            //FileUtils.dirpath = "I:\\SteamLibrary1\\steamapps\\common\\No Man's Sky\\GAMEDATA\\PCBANKS";

            //Initialize Engine backend
            engine = new Engine(this);
            RenderState.engineRef = engine; //Set reference to engine [Should do before initialization]

            //Add Default Resources
            AddDefaultShaderSources();
            AddDefaultShaderConfigs();
            SetupDefaultCamera();

            engine.init(Size.X, Size.Y); //Initialize Engine

            //Now that the renderer is initialized, we can do shit
            CompileMainShaders();
            AddDefaultMaterials();
            AddDefaultPrimitives();

            //Load EnginePlugins
            LoadPlugins();
            
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

            TextInput += _uiLayer.OnTextInput;
            Resize += _uiLayer.OnResize;

            RenderState.settings = Settings.loadFromDisk();
            
            //Pass rendering settings to the Window
            RenderFrequency = RenderState.settings.renderSettings.FPS;
            UpdateFrequency = 60;
            VSync = VSyncMode.Off;


            //Create Default SceneGraph
            engine.sceneMgmtSys.CreateSceneGraph();
            engine.sceneMgmtSys.SetActiveScene(engine.sceneMgmtSys.SceneGraphs[0]);
            SceneGraph graph = engine.GetActiveSceneGraph();

            //Create Test Scene
            SceneGraphNode test1 = engine.CreateLocatorNode("Test Locator 1");
            SceneGraphNode test2 = engine.CreateLocatorNode("Test Locator 2");
            test1.AddChild(test2);
            SceneGraphNode test3 = engine.CreateLocatorNode("Test Locator 3");
            test2.AddChild(test3);

            SceneGraphNode light = engine.CreateLightNode("Default Light", 200.0f, ATTENUATION_TYPE.QUADRATIC, LIGHT_TYPE.POINT);
            NbCore.Systems.TransformationSystem.SetEntityLocation(light, new NbVector3(100.0f, 100.0f, 100.0f));
            test1.AddChild(light);

            //Request tranform update for the added nodes
            engine.RegisterSceneGraphNode(test1);
            engine.RequestEntityTransformUpdate(test1);

            graph.AddNode(test1);
            
            //Populate SceneGraphView
            engine.NewSceneEvent?.Invoke(graph);
            
            //Check if Temp folder exists
#if DEBUG
            if (!Directory.Exists("Temp")) Directory.CreateDirectory("Temp");
#endif
            //Set active Components
            Util.activeWindow = this;

        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
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

            SwapBuffers();
        }

        private static void Log(string msg, LogVerbosityLevel lvl)
        {
            Callbacks.Log("* WINDOW : " + msg, lvl);
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
            engine.RegisterEntity(cam);

            //Set global reference to cam
            RenderState.activeCam = cam;

            //Set Camera Initial State
            TransformController tcontroller = engine.transformSys.GetEntityTransformController(cam);
            tcontroller.AddFutureState(new NbVector3(), NbQuaternion.FromEulerAngles(0.0f, -3.14f / 2.0f, 0.0f, "XYZ"), new NbVector3(1.0f));
        }


        private void AddDefaultShaderSources()
        {
            //Local function
            void WalkDirectory(DirectoryInfo dir)
            {
                FileInfo[] files = dir.GetFiles("*.glsl");
                DirectoryInfo[] subdirs = dir.GetDirectories();

                if (subdirs.Length != 0)
                {
                    foreach (DirectoryInfo subdir in subdirs)
                        WalkDirectory(subdir);
                }

                if (files.Length != 0)
                {
                    foreach (FileInfo file in files)
                    {
                        //Convert filepath to single 
                        string filepath = FileUtils.FixPath(file.FullName);
                        //Add source file
                        Log($"Working On {filepath}", LogVerbosityLevel.INFO);
                        if (engine.GetShaderSourceByFilePath(filepath) == null)
                        {
                            //Construction includes registration
                            GLSLShaderSource ss = new(filepath, true);
                        }
                    }
                }
            }

            DirectoryInfo dirInfo = new("Shaders");
            WalkDirectory(dirInfo);

            //Now that all sources are loaded we can start processing all of them
            //Step 1: Process Shaders
            List<Entity> sourceList = engine.GetEntityTypeList(EntityType.ShaderSource);
            int i = 0;
            while (i < sourceList.Count) //This way can account for new entries 
            {
                ((GLSLShaderSource) sourceList[i]).Process();
                i++;
            }

        }


        private void AddDefaultShaderConfigs()
        {
            //Create Debug Shader Config
            //Debug Shader
            GLSLShaderConfig conf;
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Simple_VSEmpty.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/Simple_FSEmpty.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/Simple_GS.glsl"), null, null,
                                      new() { }, NbShaderMode.DEFFERED, "Debug");
            engine.RegisterEntity(conf);

            //Test Shader
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Simple_VSEmpty.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/Test_fs.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.DEFFERED, "Test");
            engine.RegisterEntity(conf);

            //UberShader Shader
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Simple_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/Simple_FS.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.DEFFERED, "UberShader_Deferred");
            engine.RegisterEntity(conf);

            //UberShader Lit Shader
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Simple_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/Simple_FS.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.DEFFERED | NbShaderMode.LIT, "UberShader_Deferred_Lit");
            engine.RegisterEntity(conf);

            //UNLIT
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/light_pass_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/light_pass_FS.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.DEFFERED, "LightPass_Unlit_Forward"); ;
            engine.RegisterEntity(conf);

            //UNLIT
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/light_pass_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/light_pass_FS.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.FORWARD, "LightPass_Unlit_Forward"); ;
            engine.RegisterEntity(conf);


            //LIT
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/light_pass_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/light_pass_FS.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.FORWARD | NbShaderMode.LIT, "LightPass_Lit_Forward"); ;
            engine.RegisterEntity(conf);


            //GAUSSIAN HORIZONTAL BLUR SHADER
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/gaussian_horizontalBlur_FS.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.FORWARD, "Horizontal_Gaussian_Blur");
            engine.RegisterEntity(conf);


            //GAUSSIAN VERTICAL BLUR SHADER
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/gaussian_verticalBlur_FS.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.DEFAULT, "Vertical_Gaussian_Blur");
            engine.RegisterEntity(conf);

            //Brightness Extraction Shader
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/brightness_extract_shader_fs.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.FORWARD, "Brightness_Extract");
            engine.RegisterEntity(conf);

            //ADDITIVE BLEND
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/additive_blend_fs.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.FORWARD, "Additive_Blend");
            engine.RegisterEntity(conf);

            //FXAA
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/fxaa_shader_fs.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.FORWARD, "FXAA");
            engine.RegisterEntity(conf);

            //TONE MAPPING + GAMMA CORRECTION
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/tone_mapping_fs.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.FORWARD, "ToneMapping");
            engine.RegisterEntity(conf);

            //INV TONE MAPPING
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/inv_tone_mapping_fs.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.FORWARD, "InverseToneMapping");
            engine.RegisterEntity(conf);

            //BWOIT SHADER
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/bwoit_shader_fs.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.FORWARD, "BWOIT");
            engine.RegisterEntity(conf);

            //Text Shaders
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Text_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/Text_FS.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.FORWARD, "Text");
            engine.RegisterEntity(conf);

            //Pass Shader
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/PassThrough_FS.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.FORWARD, "Passthrough");
            engine.RegisterEntity(conf);

            //Red Shader
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/RedFill.glsl"),
                                      null, null, null,
                                      new() { }, NbShaderMode.FORWARD, "RedFill");
            engine.RegisterEntity(conf);

        }


        private void CompileMainShaders()
        {
            //Populate shader list

#if (DEBUG)
            //Query GL Extensions
            Log("OPENGL AVAILABLE EXTENSIONS:", LogVerbosityLevel.INFO);
            string[] ext = GL.GetString(StringNameIndexed.Extensions, 0).Split(' ');
            foreach (string s in ext)
            {
                if (s.Contains("explicit"))
                    Log(s, LogVerbosityLevel.INFO);
                if (s.Contains("texture"))
                    Log(s, LogVerbosityLevel.INFO);
                if (s.Contains("16"))
                    Log(s, LogVerbosityLevel.INFO);
            }

            //Query maximum buffer sizes
            Log($"MaxUniformBlock Size {GL.GetInteger(GetPName.MaxUniformBlockSize)}", LogVerbosityLevel.INFO);
#endif

            GLSLShaderConfig shader_conf;
            NbShader shader;

            //Light Pass Shaders

            //LIT
            shader_conf = engine.GetShaderConfigByName("LightPass_Lit_Forward");
            shader = new()
            {
                Type = NbShaderType.LIGHT_PASS_LIT_SHADER
            };
            engine.renderSys.Renderer.CompileShader(ref shader, shader_conf);
            engine.RegisterEntity(shader);

            //GAUSSIAN HORIZONTAL BLUR SHADER
            shader_conf = engine.GetShaderConfigByName("Horizontal_Gaussian_Blur");
            shader = new();
            engine.renderSys.Renderer.CompileShader(ref shader, shader_conf);
            engine.RegisterEntity(shader);

            //GAUSSIAN VERTICAL BLUR SHADER
            shader_conf = engine.GetShaderConfigByName("Vertical_Gaussian_Blur");
            shader = new();
            engine.renderSys.Renderer.CompileShader(ref shader, shader_conf);
            engine.RegisterEntity(shader);

            //BRIGHTNESS EXTRACTION SHADER
            shader_conf = engine.GetShaderConfigByName("Brightness_Extract");
            shader = new();
            engine.renderSys.Renderer.CompileShader(ref shader, shader_conf);
            engine.RegisterEntity(shader);

            //ADDITIVE BLEND
            shader_conf = engine.GetShaderConfigByName("Additive_Blend");
            shader = new();
            engine.renderSys.Renderer.CompileShader(ref shader, shader_conf);
            engine.RegisterEntity(shader);

            //FXAA
            shader_conf = engine.GetShaderConfigByName("FXAA");
            shader = new();
            engine.renderSys.Renderer.CompileShader(ref shader, shader_conf);
            engine.RegisterEntity(shader);

            //TONE MAPPING + GAMMA CORRECTION
            shader_conf = engine.GetShaderConfigByName("ToneMapping");
            shader = new()
            {
                Type = NbShaderType.TONE_MAPPING
            };
            engine.renderSys.Renderer.CompileShader(ref shader, shader_conf);
            engine.RegisterEntity(shader);

            //INV TONE MAPPING + GAMMA CORRECTION
            shader_conf = engine.GetShaderConfigByName("InverseToneMapping");
            shader = new()
            {
                Type = NbShaderType.INV_TONE_MAPPING
            };
            engine.renderSys.Renderer.CompileShader(ref shader, shader_conf);
            engine.RegisterEntity(shader);

            //BWOIT SHADER
            shader_conf = engine.GetShaderConfigByName("BWOIT");
            shader = new();
            engine.renderSys.Renderer.CompileShader(ref shader, shader_conf);
            engine.RegisterEntity(shader);

            //Text Shaders
            shader_conf = engine.GetShaderConfigByName("Text");
            shader = new();
            engine.renderSys.Renderer.CompileShader(ref shader, shader_conf);
            engine.RegisterEntity(shader);

            //FILTERS - EFFECTS

            //Pass Shader
            shader_conf = engine.GetShaderConfigByName("Passthrough");
            shader = new()
            {
                Type = NbShaderType.PASSTHROUGH_SHADER
            };
            engine.renderSys.Renderer.CompileShader(ref shader, shader_conf);
            engine.RegisterEntity(shader);

            //Red Shader
            shader_conf = engine.GetShaderConfigByName("RedFill");
            shader = new()
            {
                Type = NbShaderType.RED_FILL_SHADER
            };
            engine.renderSys.Renderer.CompileShader(ref shader, shader_conf);
            engine.RegisterEntity(shader);

        }


        private void AddDefaultMaterials()
        {
            //Cross Material
            MeshMaterial mat;
            GLSLShaderConfig config_deferred = engine.GetShaderConfigByName("UberShader_Deferred");
            GLSLShaderConfig config_deferred_lit = engine.GetShaderConfigByName("UberShader_Deferred_Lit");
            NbShader shader;

            mat = new();
            mat.Name = "crossMat";
            mat.add_flag(MaterialFlagEnum._F07_UNLIT);
            mat.add_flag(MaterialFlagEnum._F21_VERTEXCOLOUR);
            NbUniform uf = new()
            {
                Name = "gMaterialColourVec4",
                State = new()
                {
                    Type = NbUniformType.Vector4,
                    ShaderBinding = "mpCustomPerMaterial.uniforms[0]",
                },
                Values = new(1.0f, 1.0f, 1.0f, 1.0f)
            };
            mat.Uniforms.Add(uf);
            mat.ShaderConfig = config_deferred;
            shader = engine.CompileMaterialShader(mat);

#if DEBUG
            //Report UBOs
            engine.renderSys.Renderer.ShaderReport(shader);
#endif
            engine.RegisterEntity(mat.Shader); //Register Shader
            engine.RegisterEntity(mat); //Register Material
            
            //Joint Material
            mat = new MeshMaterial
            {
                Name = "jointMat"
            };
            mat.add_flag(MaterialFlagEnum._F07_UNLIT);

            uf = new()
            {
                Name = "gMaterialColourVec4",
                State = new()
                {
                    Type = NbUniformType.Vector4,
                    ShaderBinding = "mpCustomPerMaterial.uniforms[0]",
                },
                Values = new(1.0f, 0.0f, 0.0f, 1.0f)
            };

            mat.Uniforms.Add(uf);
            mat.ShaderConfig = config_deferred;
            shader = engine.CompileMaterialShader(mat);

            engine.RegisterEntity(mat.Shader); //Register Shader
            engine.RegisterEntity(mat);
            

            //Light Material
            mat = new()
            {
                Name = "lightMat"
            };
            mat.add_flag(MaterialFlagEnum._F07_UNLIT);

            uf = new()
            {
                Name = "gMaterialColourVec4",
                State = new()
                {
                    Type = NbUniformType.Vector4,
                    ShaderBinding = "mpCustomPerMaterial.uniforms[0]",
                },
                Values = new(1.0f, 1.0f, 0.0f, 1.0f)
            };

            mat.Uniforms.Add(uf);
            mat.ShaderConfig = config_deferred;
            shader = engine.CompileMaterialShader(mat);

            engine.RegisterEntity(mat.Shader); //Register Shader
            engine.RegisterEntity(mat);
            

            //Default Material
            mat = new()
            {
                Name = "defaultMat"
            };
            mat.add_flag(MaterialFlagEnum._F07_UNLIT);


            uf = new()
            {
                Name = "gMaterialColourVec4",
                State = new()
                {
                    Type = NbUniformType.Vector4,
                    ShaderBinding = "mpCustomPerMaterial.uniforms[0]",
                },
                Values = new(0.7f, 0.7f, 0.7f, 1.0f)
            };

            mat.Uniforms.Add(uf);
            mat.ShaderConfig = config_deferred;
            shader = engine.CompileMaterialShader(mat);

            engine.RegisterEntity(mat.Shader); //Register Shader
            engine.RegisterEntity(mat);
            

            //Red  Material
            mat = new()
            {
                Name = "redMat"
            };

            uf = new()
            {
                Name = "gMaterialColourVec4",
                State = new()
                {
                    Type = NbUniformType.Vector4,
                    ShaderBinding = "mpCustomPerMaterial.uniforms[0]",
                },
                Values = new(0.7f, 0.7f, 0.7f, 1.0f)
            };

            mat.Uniforms.Add(uf);
            mat.ShaderConfig = config_deferred_lit;
            shader = engine.CompileMaterialShader(mat);

            engine.RegisterEntity(mat.Shader); //Register Shader
            engine.RegisterEntity(mat);
            

            //Collision Material
            mat = new();
            mat.Name = "collisionMat";
            mat.add_flag(MaterialFlagEnum._F07_UNLIT);

            uf = new()
            {
                Name = "gMaterialColourVec4",
                State = new()
                {
                    Type = NbUniformType.Vector4,
                    ShaderBinding = "mpCustomPerMaterial.uniforms[0]",
                },
                Values = new(0.8f, 0.8f, 0.2f, 1.0f)
            };

            mat.Uniforms.Add(uf);
            mat.ShaderConfig = config_deferred;
            shader = engine.CompileMaterialShader(mat);

            engine.RegisterEntity(mat.Shader); //Register Shader
            engine.RegisterEntity(mat);
            

        }

        private void AddDefaultPrimitives()
        {
            //Setup Primitive Vaos

            //Default quad
            NbCore.Primitives.Quad q = new(1.0f, 1.0f);

            NbMesh mesh = new()
            {
                Hash = (ulong)"default_quad".GetHashCode(),
                Data = q.geom.GetData(),
                MetaData = q.geom.GetMetaData(),
                Material = engine.renderSys.MaterialMgr.GetByName("defaultMat")
            };

            engine.renderSys.Renderer.AddMesh(mesh);
            engine.RegisterEntity(mesh);
            engine.renderSys.GeometryMgr.AddPrimitiveMesh(mesh);
            q.Dispose();

            //Default render quad
            q = new NbCore.Primitives.Quad();

            mesh = new()
            {
                Hash = (ulong)"default_renderquad".GetHashCode(),
                Data = q.geom.GetData(),
                MetaData = q.geom.GetMetaData(),
            };
            engine.renderSys.Renderer.AddMesh(mesh);
            engine.RegisterEntity(mesh);
            engine.renderSys.GeometryMgr.AddPrimitiveMesh(mesh);
            q.Dispose();

            //Default cross
            NbCore.Primitives.Cross c = new(0.1f, true);

            mesh = new()
            {
                Type = NbMeshType.Locator, //Explicitly set as locator mesh
                Hash = (ulong)"default_cross".GetHashCode(),
                Data = c.geom.GetData(),
                MetaData = c.geom.GetMetaData(),
                Material = engine.renderSys.MaterialMgr.GetByName("crossMat")
            };
            engine.renderSys.Renderer.AddMesh(mesh);
            engine.RegisterEntity(mesh);
            engine.renderSys.GeometryMgr.AddPrimitiveMesh(mesh);
            c.Dispose();


            //Default cube
            NbCore.Primitives.Box bx = new(1.0f, 1.0f, 1.0f, new NbVector3(1.0f), true);

            mesh = new()
            {
                Hash = (ulong)"default_box".GetHashCode(),
                Data = bx.geom.GetData(),
                MetaData = bx.geom.GetMetaData(),
                Material = engine.renderSys.MaterialMgr.GetByName("defaultMat")
            };
            engine.renderSys.Renderer.AddMesh(mesh);
            engine.RegisterEntity(mesh);
            engine.renderSys.GeometryMgr.AddPrimitiveMesh(mesh);
            bx.Dispose();

            //Default sphere
            NbCore.Primitives.Sphere sph = new(new NbVector3(0.0f, 0.0f, 0.0f), 100.0f);

            mesh = new()
            {
                Hash = (ulong)"default_sphere".GetHashCode(),
                Data = sph.geom.GetData(),
                MetaData = sph.geom.GetMetaData(),
                Material = engine.renderSys.MaterialMgr.GetByName("defaultMat")
            };

            engine.renderSys.Renderer.AddMesh(mesh);
            engine.RegisterEntity(mesh);
            engine.renderSys.GeometryMgr.AddPrimitiveMesh(mesh);
            sph.Dispose();

            //Light Sphere Mesh
            NbCore.Primitives.Sphere lsph = new(new NbVector3(0.0f, 0.0f, 0.0f), 1.0f);

            mesh = new()
            {
                Hash = (ulong)"default_light_sphere".GetHashCode(),
                Data = lsph.geom.GetData(),
                MetaData = lsph.geom.GetMetaData(),
                Material = engine.renderSys.MaterialMgr.GetByName("lightMat")
            };

            engine.renderSys.Renderer.AddMesh(mesh);
            engine.RegisterEntity(mesh);
            engine.renderSys.GeometryMgr.AddPrimitiveMesh(mesh);
            lsph.Dispose();

            GenerateGizmoParts();



        }


        private void GenerateGizmoParts()
        {
            //Translation Gizmo
            NbCore.Primitives.Arrow translation_x_axis = new(0.015f, 0.25f, new NbVector3(1.0f, 0.0f, 0.0f), false, 20);
            //Move arrowhead up in place
            NbMatrix4 t = NbMatrix4.CreateRotationZ(MathUtils.radians(90));
            translation_x_axis.applyTransform(t);

            NbCore.Primitives.Arrow translation_y_axis = new(0.015f, 0.25f, new NbVector3(0.0f, 1.0f, 0.0f), false, 20);
            NbCore.Primitives.Arrow translation_z_axis = new(0.015f, 0.25f, new NbVector3(0.0f, 0.0f, 1.0f), false, 20);
            t = NbMatrix4.CreateRotationX(MathUtils.radians(90));
            translation_z_axis.applyTransform(t);

            //Generate Geom objects
            translation_x_axis.geom = translation_x_axis.getGeom();
            translation_y_axis.geom = translation_y_axis.getGeom();
            translation_z_axis.geom = translation_z_axis.getGeom();


            //GLVao x_axis_vao = translation_x_axis.getVAO();
            //GLVao y_axis_vao = translation_y_axis.getVAO();
            //GLVao z_axis_vao = translation_z_axis.getVAO();


            //Generate PrimitiveMeshVaos
            for (int i = 0; i < 3; i++)
            {
                string name = "";
                NbCore.Primitives.Primitive arr = null;
                switch (i)
                {
                    case 0:
                        arr = translation_x_axis;
                        name = "default_translation_gizmo_x_axis";
                        break;
                    case 1:
                        arr = translation_y_axis;
                        name = "default_translation_gizmo_y_axis";
                        break;
                    case 2:
                        arr = translation_z_axis;
                        name = "default_translation_gizmo_z_axis";
                        break;
                }

                NbMesh mesh = new()
                {
                    Hash = (ulong)name.GetHashCode(),
                    Data = arr.geom.GetData(),
                    MetaData = arr.geom.GetMetaData()
                };

                if (!engine.renderSys.GeometryMgr.AddPrimitiveMesh(mesh))
                    mesh.Dispose();
                arr.Dispose();
            }

        }



        #endregion


    }




}
