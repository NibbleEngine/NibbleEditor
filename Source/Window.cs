using System;
using System.Threading;

using NbCore;
using NbCore.Math;
using NbCore.Common;
using NbCore.Plugins;
using NbCore.Utils;
using System.Collections.Generic;
using NbCore.Platform.Graphics;
using System.IO;

namespace NibbleEditor
{
    public class Window : NbWindow
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

        
        public Window() : base()
        {
            //Set Window Title
            Title = "Nibble Editor " + Util.getVersion();

            //SETUP THE Callbacks FOR THE NbCore ENVIRONMENT
            Callbacks.SetDefaultCallbacks();
            Callbacks.updateStatus = Util.setStatus;
            Callbacks.showInfo = Util.showInfo;
            Callbacks.showError = Util.showError;
            Callbacks.Logger = new NbLogger();

            //Connect Window Callbacks
            OnRenderUpdate += RenderFrame;
            OnFrameUpdate += UpdateFrame;
            OnWindowLoad += WindowLoad;

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

        private void WindowLoad()
        {
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
            _uiLayer.SaveActiveSceneEvent += SaveActiveScene;

            TextInput += _uiLayer.OnTextInput;
            Resize += _uiLayer.OnResize;
            Callbacks.Logger.LogEvent += _uiLayer.OnLog;

            RenderState.settings = Settings.loadFromDisk();
            
            //Pass rendering settings to the Window
            RenderFrequency = RenderState.settings.renderSettings.FPS;
            UpdateFrequency = 60;
            SetRenderFrameFrequency(RenderState.settings.renderSettings.FPS);
            SetFrameUpdateFrequency(60);
            SetVSync(false);
            
            //Create Default SceneGraph
            engine.sceneMgmtSys.CreateSceneGraph();
            engine.sceneMgmtSys.SetActiveScene(engine.sceneMgmtSys.SceneGraphs[0]);
            SceneGraph graph = engine.GetActiveSceneGraph();

#if DEBUG
            //Create Test Scene
            SceneGraphNode test1 = engine.CreateLocatorNode("Test Locator 1");
            SceneGraphNode test2 = engine.CreateLocatorNode("Test Locator 2");
            test1.AddChild(test2);
            SceneGraphNode test3 = engine.CreateLocatorNode("Test Locator 3");
            test2.AddChild(test3);

            SceneGraphNode light = engine.CreateLightNode("Default Light", 200.0f, ATTENUATION_TYPE.QUADRATIC, LIGHT_TYPE.POINT);
            NbCore.Systems.TransformationSystem.SetEntityLocation(light, new NbVector3(100.0f, 100.0f, 100.0f));
            test1.AddChild(light);
            test1.SetParent(graph.Root);

            //Request tranform update for the added nodes
            engine.RegisterSceneGraphTree(test1, true);
            engine.RequestEntityTransformUpdate(test1);
#endif
            //Populate SceneGraphView
            engine.NewSceneEvent?.Invoke(graph);
            
            //Check if Temp folder exists
#if DEBUG
            if (!Directory.Exists("Temp")) Directory.CreateDirectory("Temp");
#endif
            //Set active Components
            Util.activeWindow = this;

        }

        private void SaveActiveScene()
        {
            SceneGraph g = engine.GetActiveSceneGraph();
            engine.SerializeScene(g, "scene_output.nb");
        }

        private void CloseWindow()
        {
            engine.CleanUp();
            Close();
        }
        
        public void UpdateFrame(double dt)
        {
            Queue<object> data = new();
            
            _inputLayer.OnFrameUpdate(ref data, dt);
            _renderLayer.OnFrameUpdate(ref data, dt);
            _uiLayer.OnFrameUpdate(ref data, dt);

            //Pass Global rendering settings
            SetVSync(RenderState.settings.renderSettings.UseVSync);
            RenderFrequency = RenderState.settings.renderSettings.FPS;
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
            engine.RegisterEntity(cam);

            //Set global reference to cam
            RenderState.activeCam = cam;

            //Set Camera Initial State
            TransformController tcontroller = engine.transformSys.GetEntityTransformController(cam);
            tcontroller.AddFutureState(new NbVector3(0.0f, 0.2f, 0.5f), NbQuaternion.FromEulerAngles(0.0f, -3.14f / 2.0f, 0.0f, "XYZ"), new NbVector3(1.0f));
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
                        Callbacks.Logger.Log(this, $"Working On {filepath}", LogVerbosityLevel.INFO);
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
                                      NbShaderMode.DEFFERED, "Debug", true);
            engine.RegisterEntity(conf);
            

            //Test Shader
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Simple_VSEmpty.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/Test_fs.glsl"),
                                      null, null, null,
                                      NbShaderMode.DEFFERED, "Test", true);
            engine.RegisterEntity(conf);

            //UberShader Shader
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Simple_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/Simple_FS.glsl"),
                                      null, null, null,
                                      NbShaderMode.DEFFERED, "UberShader_Deferred", true);
            engine.RegisterEntity(conf);

            //UberShader Lit Shader
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Simple_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/Simple_FS.glsl"),
                                      null, null, null,
                                      NbShaderMode.DEFFERED | NbShaderMode.LIT, "UberShader_Deferred_Lit", true);
            engine.RegisterEntity(conf);

            //UNLIT
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/light_pass_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/light_pass_FS.glsl"),
                                      null, null, null,
                                      NbShaderMode.DEFFERED, "LightPass_Unlit_Forward", true); ;
            engine.RegisterEntity(conf);

            //UNLIT
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/light_pass_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/light_pass_FS.glsl"),
                                      null, null, null,
                                      NbShaderMode.FORWARD, "LightPass_Unlit_Forward", true); ;
            engine.RegisterEntity(conf);


            //LIT
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/light_pass_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/light_pass_FS.glsl"),
                                      null, null, null,
                                      NbShaderMode.FORWARD | NbShaderMode.LIT, "LightPass_Lit_Forward", true); ;
            engine.RegisterEntity(conf);


            //GAUSSIAN HORIZONTAL BLUR SHADER
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/gaussian_horizontalBlur_FS.glsl"),
                                      null, null, null,
                                      NbShaderMode.FORWARD, "Horizontal_Gaussian_Blur", true);
            engine.RegisterEntity(conf);


            //GAUSSIAN VERTICAL BLUR SHADER
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/gaussian_verticalBlur_FS.glsl"),
                                      null, null, null,
                                      NbShaderMode.DEFAULT, "Vertical_Gaussian_Blur", true);
            engine.RegisterEntity(conf);

            //Brightness Extraction Shader
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/brightness_extract_shader_fs.glsl"),
                                      null, null, null,
                                      NbShaderMode.FORWARD, "Brightness_Extract", true);
            engine.RegisterEntity(conf);

            //ADDITIVE BLEND
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/additive_blend_fs.glsl"),
                                      null, null, null,
                                      NbShaderMode.FORWARD, "Additive_Blend", true);
            engine.RegisterEntity(conf);

            //FXAA
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/fxaa_shader_fs.glsl"),
                                      null, null, null,
                                      NbShaderMode.FORWARD, "FXAA", true);
            engine.RegisterEntity(conf);

            //TONE MAPPING + GAMMA CORRECTION
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/tone_mapping_fs.glsl"),
                                      null, null, null,
                                      NbShaderMode.FORWARD, "ToneMapping", true);
            engine.RegisterEntity(conf);

            //INV TONE MAPPING
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/inv_tone_mapping_fs.glsl"),
                                      null, null, null,
                                      NbShaderMode.FORWARD, "InverseToneMapping", true);
            engine.RegisterEntity(conf);

            //BWOIT SHADER
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/bwoit_shader_fs.glsl"),
                                      null, null, null,
                                      NbShaderMode.FORWARD, "BWOIT", true);
            engine.RegisterEntity(conf);

            //Text Shaders
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Text_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/Text_FS.glsl"),
                                      null, null, null,
                                      NbShaderMode.FORWARD, "Text", true);
            engine.RegisterEntity(conf);

            //Pass Shader
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/PassThrough_FS.glsl"),
                                      null, null, null,
                                      NbShaderMode.FORWARD, "Passthrough", true);
            engine.RegisterEntity(conf);

            //Red Shader
            conf = engine.CreateShaderConfig(engine.GetShaderSourceByFilePath("Shaders/Gbuffer_VS.glsl"),
                                      engine.GetShaderSourceByFilePath("Shaders/RedFill.glsl"),
                                      null, null, null,
                                      NbShaderMode.FORWARD, "RedFill");
            engine.RegisterEntity(conf);

        }


        private void CompileMainShaders()
        {
            //Populate shader list

            GLSLShaderConfig shader_conf;
            NbShader shader;

            //LIT
            shader = new()
            {
                Type = NbShaderType.LIGHT_PASS_LIT_SHADER,
                IsGeneric = true
            };

            shader.SetShaderConfig(engine.GetShaderConfigByName("LightPass_Lit_Forward"));
            engine.CompileShader(shader);
            engine.RegisterEntity(shader);

            //GAUSSIAN HORIZONTAL BLUR SHADER
            shader = new()
            {
                IsGeneric = true
            };
            shader.SetShaderConfig(engine.GetShaderConfigByName("Horizontal_Gaussian_Blur"));
            engine.CompileShader(shader);
            engine.RegisterEntity(shader);

            //GAUSSIAN VERTICAL BLUR SHADER
            shader = new()
            {
                IsGeneric = true 
            };
            shader.SetShaderConfig(engine.GetShaderConfigByName("Vertical_Gaussian_Blur"));
            engine.CompileShader(shader);
            engine.RegisterEntity(shader);

            //BRIGHTNESS EXTRACTION SHADER
            shader = new() 
            {
                IsGeneric = true 
            };
            
            shader.SetShaderConfig(engine.GetShaderConfigByName("Brightness_Extract"));
            engine.CompileShader(shader);
            engine.RegisterEntity(shader);

            //ADDITIVE BLEND
            shader = new() 
            {
                IsGeneric = true 
            };
            shader.SetShaderConfig(engine.GetShaderConfigByName("Additive_Blend"));
            engine.CompileShader(shader);
            engine.RegisterEntity(shader);

            //FXAA
            shader = new() 
            { 
                IsGeneric = true 
            };
            shader.SetShaderConfig(engine.GetShaderConfigByName("FXAA"));
            engine.CompileShader(shader);
            engine.RegisterEntity(shader);

            //TONE MAPPING + GAMMA CORRECTION
            shader = new()
            {
                Type = NbShaderType.TONE_MAPPING,
                IsGeneric = true
            };
            shader.SetShaderConfig(engine.GetShaderConfigByName("ToneMapping"));
            engine.CompileShader(shader);
            engine.RegisterEntity(shader);

            //INV TONE MAPPING + GAMMA CORRECTION
            shader = new()
            {
                Type = NbShaderType.INV_TONE_MAPPING,
                IsGeneric = true
            };
            shader.SetShaderConfig(engine.GetShaderConfigByName("InverseToneMapping"));
            engine.CompileShader(shader);
            engine.RegisterEntity(shader);

            //BWOIT SHADER
            shader = new() 
            {
                IsGeneric = true 
            };
            shader.SetShaderConfig(engine.GetShaderConfigByName("BWOIT"));
            engine.CompileShader(shader);
            engine.RegisterEntity(shader);

            //Text Shaders
            shader = new() 
            {
                IsGeneric = true 
            };
            shader.SetShaderConfig(engine.GetShaderConfigByName("Text"));
            engine.CompileShader(shader);
            engine.RegisterEntity(shader);

            //FILTERS - EFFECTS

            //Pass Shader
            shader = new()
            {
                Type = NbShaderType.PASSTHROUGH_SHADER,
                IsGeneric = true
            };
            shader.SetShaderConfig(engine.GetShaderConfigByName("Passthrough"));
            engine.CompileShader(shader);
            engine.RegisterEntity(shader);

            //Red Shader
            shader_conf = engine.GetShaderConfigByName("RedFill");
            shader = new()
            {
                Type = NbShaderType.RED_FILL_SHADER,
                IsGeneric = true
            };
            shader.SetShaderConfig(engine.GetShaderConfigByName("RedFill"));
            engine.CompileShader(shader);
            engine.RegisterEntity(shader);

        }

        private void AddDefaultMaterials()
        {
            //Cross Material
            MeshMaterial mat;
            GLSLShaderConfig config_deferred = engine.GetShaderConfigByName("UberShader_Deferred");
            GLSLShaderConfig config_deferred_lit = engine.GetShaderConfigByName("UberShader_Deferred_Lit");
            NbShader shader;

            mat = new()
            {
                Name = "crossMat",
                IsGeneric = true
            };
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

            shader = engine.CreateShader(config_deferred, engine.GetMaterialShaderDirectives(mat));
            engine.CompileShader(shader);
            mat.AttachShader(shader);
            
#if DEBUG
            //Report UBOs
            engine.renderSys.Renderer.ShaderReport(shader);
#endif
            engine.RegisterEntity(mat); //Register Material
            
            //Joint Material
            mat = new MeshMaterial
            {
                Name = "jointMat",
                IsGeneric = true
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

            ulong shader_hash = engine.CalculateShaderHash(config_deferred, engine.GetMaterialShaderDirectives(mat));
            shader = engine.GetShaderByHash(shader_hash);
            if (shader == null)
            {
                shader = engine.CreateShader(config_deferred, engine.GetMaterialShaderDirectives(mat));
                engine.CompileShader(shader);
            }
            
            mat.AttachShader(shader);
            engine.RegisterEntity(mat);
            
            //Light Material
            mat = new()
            {
                Name = "lightMat",
                IsGeneric = true
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

            shader_hash = engine.CalculateShaderHash(config_deferred, engine.GetMaterialShaderDirectives(mat));
            shader = engine.GetShaderByHash(shader_hash);
            if (shader == null)
            {
                shader = engine.CreateShader(config_deferred, engine.GetMaterialShaderDirectives(mat));
                engine.CompileShader(shader);
            }

            mat.AttachShader(shader);
            engine.RegisterEntity(mat);
            
            //Default Material
            mat = new()
            {
                Name = "defaultMat",
                IsGeneric = true
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

            shader_hash = engine.CalculateShaderHash(config_deferred, engine.GetMaterialShaderDirectives(mat));
            shader = engine.GetShaderByHash(shader_hash);
            if (shader == null)
            {
                shader = engine.CreateShader(config_deferred, engine.GetMaterialShaderDirectives(mat));
                engine.CompileShader(shader);
            }

            mat.AttachShader(shader);
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

            shader_hash = engine.CalculateShaderHash(config_deferred, engine.GetMaterialShaderDirectives(mat));
            shader = engine.GetShaderByHash(shader_hash);
            if (shader == null)
            {
                shader = engine.CreateShader(config_deferred, engine.GetMaterialShaderDirectives(mat));
                engine.CompileShader(shader);
            }

            mat.AttachShader(shader);
            engine.RegisterEntity(mat);


            //Collision Material
            mat = new()
            {
                Name = "collisionMat",
                IsGeneric = true
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
                Values = new(0.8f, 0.8f, 0.2f, 1.0f)
            };

            mat.Uniforms.Add(uf);
            shader_hash = engine.CalculateShaderHash(config_deferred, engine.GetMaterialShaderDirectives(mat));
            shader = engine.GetShaderByHash(shader_hash);
            if (shader == null)
            {
                shader = engine.CreateShader(config_deferred, engine.GetMaterialShaderDirectives(mat));
                engine.CompileShader(shader);
            }

            mat.AttachShader(shader);
            engine.RegisterEntity(mat);
            

        }

        private void AddDefaultPrimitives()
        {
            //Setup Primitive Vaos

            //Default quad
            NbCore.Primitives.Quad q = new(1.0f, 1.0f);

            NbMesh mesh = new()
            {
                Hash = NbHasher.Hash("default_quad"),
                Data = q.geom.GetMeshData(),
                MetaData = q.geom.GetMetaData(),
                Material = engine.renderSys.MaterialMgr.GetByName("defaultMat")
            };

            engine.RegisterEntity(mesh);
            q.Dispose();

            //Default render quad
            q = new NbCore.Primitives.Quad();

            mesh = new()
            {
                Hash = NbHasher.Hash("default_renderquad"),
                Data = q.geom.GetMeshData(),
                MetaData = q.geom.GetMetaData(),
            };
            engine.RegisterEntity(mesh);
            q.Dispose();

            //Default cross
            NbCore.Primitives.Cross c = new(0.1f, true);

            mesh = new()
            {
                Type = NbMeshType.Locator, //Explicitly set as locator mesh
                Hash = NbHasher.Hash("default_cross"),
                Data = c.geom.GetMeshData(),
                MetaData = c.geom.GetMetaData(),
                Material = engine.renderSys.MaterialMgr.GetByName("crossMat")
            };
            
            engine.RegisterEntity(mesh);
            c.Dispose();

            //Default cube
            NbCore.Primitives.Box bx = new(1.0f, 1.0f, 1.0f, new NbVector3(1.0f), true);

            mesh = new()
            {
                Hash = NbHasher.Hash("default_box"),
                Data = bx.geom.GetMeshData(),
                MetaData = bx.geom.GetMetaData(),
                Material = engine.renderSys.MaterialMgr.GetByName("defaultMat")
            };
            engine.RegisterEntity(mesh);
            bx.Dispose();

            //Default sphere
            NbCore.Primitives.Sphere sph = new(new NbVector3(0.0f, 0.0f, 0.0f), 100.0f);

            mesh = new()
            {
                Hash = NbHasher.Hash("default_sphere"),
                Data = sph.geom.GetMeshData(),
                MetaData = sph.geom.GetMetaData(),
                Material = engine.renderSys.MaterialMgr.GetByName("defaultMat")
            };

            engine.RegisterEntity(mesh);
            sph.Dispose();

            //Light Sphere Mesh
            NbCore.Primitives.Sphere lsph = new(new NbVector3(0.0f, 0.0f, 0.0f), 1.0f);

            mesh = new()
            {
                Hash = NbHasher.Hash("default_light_sphere"),
                Data = lsph.geom.GetMeshData(),
                MetaData = lsph.geom.GetMetaData(),
                Material = engine.renderSys.MaterialMgr.GetByName("lightMat")
            };

            engine.RegisterEntity(mesh);
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
                    Hash = NbHasher.Hash(name),
                    Data = arr.geom.GetMeshData(),
                    MetaData = arr.geom.GetMetaData()
                };

                engine.RegisterEntity(mesh);
                
                if (!engine.IsEntityRegistered(mesh))
                    mesh.Dispose();
                arr.Dispose();
            }

        }



        #endregion


    }




}
