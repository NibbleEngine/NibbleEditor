using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NbCore;
using NbCore.Common;
using ImGuiNET;
using NibbleEditor;

namespace ImGuiHelper
{
    public class ImGuiAboutWindow
    {
        Texture tex;

        public ImGuiAboutWindow()
        {
            //Load Logo Texture to the GPU
            byte[] imgData = Callbacks.getResource("ianm32logo_border.png");

            tex = new Texture();
            tex.textureInit(imgData, "ianm32logo_border.png");
        }

        private void TextCenter(string text, bool ishyperlink, string url = "")
        {
            float font_size = ImGui.GetFontSize() * text.Length / 2;
            ImGui.SameLine(
                ImGui.GetColumnWidth() / 2 -
                font_size + (font_size / 2)
            );

            ImGui.Text(text);

            if (ishyperlink)
            {
                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                min.Y = max.Y;

                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseClicked(0))
                    {

                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
                        {
                            Verb = "open",
                            UseShellExecute = true
                        });
                    }


                    //System.Diagnostics.Process.Start("explorer.exe", new Uri(url).ToString());
                    ImGui.GetWindowDrawList().AddLine(min, max, 0x0010FFFF);
                }
                else
                {
                    ImGui.GetWindowDrawList().AddLine(min, max, 0xFFFFFFFF);
                }
            }

        }



        private void Text(string text, bool ishyperlink, string url = "")
        {
            ImGui.Text(text);

            if (ishyperlink)
            {
                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                min.Y = max.Y;

                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseClicked(0))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
                        {
                            Verb = "open",
                            UseShellExecute = true
                        });
                    }

                    ImGui.GetWindowDrawList().AddLine(min, max, 0x0010FFFF);
                }
                else
                {
                    ImGui.GetWindowDrawList().AddLine(min, max, 0xFFFFFFFF);
                }
            }

        }


        public void Draw()
        {

            //Assume that a Popup has begun
            ImGui.BeginChild("AboutWindow", ImGui.GetContentRegionAvail(),
                true, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

            ImGui.Image(new IntPtr(tex.texID),
                        new System.Numerics.Vector2(256, 256),
                        new System.Numerics.Vector2(0, 0),
                        new System.Numerics.Vector2(1, 1));

            if (ImGui.BeginChildFrame(0, ImGui.GetContentRegionAvail(), ImGuiWindowFlags.NoBackground))
            {
                TextCenter("No Man's Sky Model Viewer", false);
                ImGui.NewLine();
                TextCenter(Util.getVersion(), false);
                ImGui.NewLine();
                ImGui.Columns(2, "Links", false);
                //Donation link
                TextCenter("Donate", true, Util.DonateLink);
                ImGui.NextColumn();
                TextCenter("Github", true, "https://github.com/gregkwaste/NMSMV");
                ImGui.NewLine();
                ImGui.Columns(1);
                TextCenter("Created by gregkwaste", false);
                ImGui.EndChildFrame();
            }
            ImGui.EndChild();

        }

        ~ImGuiAboutWindow()
        {
            tex.Dispose();
        }
    }

}
