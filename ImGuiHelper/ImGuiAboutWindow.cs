﻿using System;
using System.Reflection;
using NbCore;
using NbCore.Common;
using ImGuiCore = ImGuiNET.ImGui;


namespace NibbleEditor
{
    public class ImGuiAboutWindow
    {
        NbTexture tex;

        public ImGuiAboutWindow()
        {
            //Load Logo Texture to the GPU
            byte[] imgData = Callbacks.getResourceFromAssembly(Assembly.GetExecutingAssembly(),
                "ianm32logo_border.png");
            
            tex = new NbTexture("ianm32logo_border.png", imgData);
            NbCore.Platform.Graphics.GraphicsAPI.GenerateTexture(tex);
            NbCore.Platform.Graphics.GraphicsAPI.UploadTexture(tex);
            tex.Data = null;
        }
        
        private void TextCenter(string text, bool ishyperlink, string url = "")
        {
            float font_size = ImGuiCore.GetFontSize();
            float text_size = ImGuiCore.CalcTextSize(text).X;
            
            ImGuiCore.SameLine(
                (ImGuiCore.GetColumnWidth() - text_size) / 2
            );

            ImGuiCore.Text(text);

            if (ishyperlink)
            {
                var min = ImGuiCore.GetItemRectMin();
                var max = ImGuiCore.GetItemRectMax();
                min.Y = max.Y;

                if (ImGuiCore.IsItemHovered())
                {
                    if (ImGuiCore.IsMouseClicked(0))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
                        {
                            Verb = "open",
                            UseShellExecute = true
                        });
                    }

                    //System.Diagnostics.Process.Start("explorer.exe", new Uri(url).ToString());
                    ImGuiCore.GetWindowDrawList().AddLine(min, max, 0x0010FFFF);
                }
                else
                {
                    ImGuiCore.GetWindowDrawList().AddLine(min, max, 0xFFFFFFFF);
                }
            }

        }



        private void Text(string text, bool ishyperlink, string url = "")
        {
            ImGuiCore.Text(text);

            if (ishyperlink)
            {
                var min = ImGuiCore.GetItemRectMin();
                var max = ImGuiCore.GetItemRectMax();
                min.Y = max.Y;

                if (ImGuiCore.IsItemHovered())
                {
                    if (ImGuiCore.IsMouseClicked(0))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
                        {
                            Verb = "open",
                            UseShellExecute = true
                        });
                    }

                    ImGuiCore.GetWindowDrawList().AddLine(min, max, 0x0010FFFF);
                }
                else
                {
                    ImGuiCore.GetWindowDrawList().AddLine(min, max, 0xFFFFFFFF);
                }
            }

        }


        public void Draw()
        {
            //Assume that a Popup has begun
            ImGuiCore.BeginChild("AboutWindow", ImGuiCore.GetContentRegionAvail(),
                true, ImGuiNET.ImGuiWindowFlags.NoDecoration |
                      ImGuiNET.ImGuiWindowFlags.NoResize |
                      ImGuiNET.ImGuiWindowFlags.NoCollapse);

            ImGuiCore.Image(new IntPtr(tex.GpuID),
                        new System.Numerics.Vector2(256, 256),
                        new System.Numerics.Vector2(0, 0),
                        new System.Numerics.Vector2(1, 1));

            if (ImGuiCore.BeginChildFrame(111, ImGuiCore.GetContentRegionAvail(), 
                ImGuiNET.ImGuiWindowFlags.NoBackground))
            {
                TextCenter("Nibble Editor", false);
                ImGuiCore.NewLine();
                TextCenter(Util.getVersion(), false);
                ImGuiCore.NewLine();
                ImGuiCore.Columns(2, "Links", false);
                //Donation link
                TextCenter("Donate", true, Util.DonateLink);
                ImGuiCore.NextColumn();
                TextCenter("Github", true, "https://github.com/NibbleEngine/NibbleEditor");
                ImGuiCore.NewLine();
                ImGuiCore.Columns(1);
                TextCenter("Created by gregkwaste", false);
                ImGuiCore.EndChildFrame();
            }

            //ImGuiCore.EndChild();

        }

        ~ImGuiAboutWindow()
        {
            tex.Dispose();
        }
    }

}
