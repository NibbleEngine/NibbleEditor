using System;
using NbCore;
using NbCore.Common;
using ImGuiCore = ImGuiNET.ImGui;
using System.Collections.Generic;
using NbCore.Platform.Graphics;
using System.Linq;

namespace NbCore.UI.ImGui
{
    public class ImGuiTextureEditor
    {
        private NbTexture _ActiveTexture = null;
        private int _SelectedId = -1;
        private string texture_path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private OpenFileDialog openFileDialog;

        private string[] _magFilters = { NbTextureFilter.Nearest.ToString(),
                                         NbTextureFilter.Linear.ToString()};

        private string[] _minFilters = { NbTextureFilter.Nearest.ToString(),
                                         NbTextureFilter.Linear.ToString(),
                                         NbTextureFilter.LinearMipmapLinear.ToString(),
                                         NbTextureFilter.LinearMipmapNearest.ToString(),
                                         NbTextureFilter.NearestMipmapLinear.ToString()};
        
        public ImGuiTextureEditor()
        {
            openFileDialog = new("texture-open-file", ".dds|.png|.jpg|.jpeg", false); //Initialize OpenFileDialog
            openFileDialog.SetDialogPath(texture_path);
        }

        public void Draw()
        {
            //Items
            List<Entity> textureList = NbRenderState.engineRef.GetEntityTypeList(EntityType.Texture);
            string[] items = new string[textureList.Count];
            for (int i = 0; i < items.Length; i++)
            {
                NbTexture tex = (NbTexture) textureList[i];
                items[i] = tex.Path == "" ? "Texture_" + i : tex.Path;
            }
                
            if (ImGuiCore.Combo("##1", ref _SelectedId, items, items.Length))
            {
                _ActiveTexture = textureList[_SelectedId] as NbTexture;
                GraphicsAPI.queryTextureParameters(_ActiveTexture);
            }
                

            ImGuiCore.SameLine();

            if (ImGuiCore.Button("Add"))
            {
                openFileDialog.Open();
            }


            //Draw Open File Dialog
            if (openFileDialog.Draw(new() { X = 640, Y = 480 }))
            {
                texture_path = System.IO.Path.GetDirectoryName(openFileDialog.GetSelectedFile());
                NbTexture tex = NbRenderState.engineRef.CreateTexture(openFileDialog.GetSelectedFile(),
                        NbTextureWrapMode.Repeat, NbTextureFilter.Linear, NbTextureFilter.Linear, false);
                NbRenderState.engineRef.RegisterEntity(tex);
                SetTexture(tex);
            }

            ImGuiCore.SameLine();
            if (ImGuiCore.Button("Del"))
            {
                Console.WriteLine("Todo Delete Texture");
            }

            if (_ActiveTexture is null)
            {
                return;
            }


            if (_ActiveTexture.GpuID != 0 && _ActiveTexture.Data.target != NbTextureTarget.Texture2DArray)
            {
                ImGuiCore.SetNextItemWidth(-1);
                float image_aspect = (float)_ActiveTexture.Data.Width / _ActiveTexture.Data.Height;
                var avail_size = ImGuiCore.GetContentRegionAvail();
                avail_size.X = System.Math.Min(avail_size.X, avail_size.Y);
                avail_size.Y = System.Math.Min(avail_size.X, avail_size.Y);
                
                System.Numerics.Vector2 vpsize;
                if (image_aspect > 1.0f)
                {
                    vpsize = new System.Numerics.Vector2(avail_size.X, avail_size.Y / image_aspect);
                }
                else
                {
                    vpsize = new System.Numerics.Vector2(avail_size.X * image_aspect, avail_size.Y);
                }
                
                ImGuiCore.Image((IntPtr) _ActiveTexture.GpuID, vpsize);
            }

            if (ImGuiCore.BeginTable("##TextureInfo", 2))
            {
                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("Path");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.SetNextItemWidth(-1);
                ImGuiCore.InputText("", ref _ActiveTexture.Path, 30);
                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("Width");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.SetNextItemWidth(-1);
                ImGuiCore.Text(_ActiveTexture.Data.Width.ToString());

                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("Height");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.SetNextItemWidth(-1);
                ImGuiCore.Text(_ActiveTexture.Data.Height.ToString());

                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("MinFilter");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.SetNextItemWidth(-1);

                //Min Filters
                int _minFilter = Array.IndexOf(_minFilters, _ActiveTexture.Data.MinFilter.ToString());
                if (ImGuiCore.Combo("#MinFilterCombo", ref _minFilter, _minFilters, _minFilters.Length))
                {
                    NbTextureFilter _filter = (NbTextureFilter)Enum.Parse(typeof(NbTextureFilter), _minFilters[_minFilter]);
                    _ActiveTexture.Data.MinFilter = _filter;
                    GraphicsAPI.setupTextureMinFilter(_ActiveTexture, _filter);
                }

                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("MagFilter");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.SetNextItemWidth(-1);

                //Mag Filters
                int _magFilter = Array.IndexOf(_magFilters, _ActiveTexture.Data.MagFilter.ToString());
                if (ImGuiCore.Combo("#MagFilterCombo", ref _magFilter, _magFilters, _magFilters.Length))
                {
                    GraphicsAPI.setupTextureMagFilter(_ActiveTexture, (NbTextureFilter) Enum.Parse(typeof(NbTextureFilter), _magFilters[_magFilter]));
                }
                
                ImGuiCore.TableNextRow();
                ImGuiCore.TableSetColumnIndex(0);
                ImGuiCore.Text("WrapMode");
                ImGuiCore.TableSetColumnIndex(1);
                ImGuiCore.SetNextItemWidth(-1);
                
                ImGuiCore.Text(_ActiveTexture.Data.WrapMode.ToString());

                ImGuiCore.TableNextRow();
                ImGuiCore.SetNextItemWidth(-1);
                ImGuiCore.TableSetColumnIndex(1);
                if (ImGuiCore.Button("DumpToDisk"))
                {
                    GraphicsAPI.DumpTexture(_ActiveTexture, "dump_tex");
                }
                
                //TODO: In the future allow for changing on the fly the filters, and the wrap mode
                ImGuiCore.EndTable();
            }

        }

        public void SetTexture(NbTexture tex)
        {
            _ActiveTexture = tex;
            List<Entity> textureList = NbRenderState.engineRef.GetEntityTypeList(EntityType.Texture);
            _SelectedId = textureList.IndexOf(tex);
        }
    }
}