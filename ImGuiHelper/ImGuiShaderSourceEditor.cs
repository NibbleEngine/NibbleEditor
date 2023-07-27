using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using ImGuiNET;
using NbCore.Common;
using NbCore.Platform.Windowing;
using ImGuiCore = ImGuiNET.ImGui;


namespace NbCore.UI.ImGui
{
    public class ImGuiShaderSourceEditor
    {
        private NbShaderSource ActiveShaderSource = null;
        private string SourceText = "";
        private int selectedId = -1;
        private int FontOption = 0;
        private int fontIndex = 0;
        private string[] FontOptions = new string[] { "1", "2", "3", "4", "5" };
        private ImGuiInputTextCallback TextCallback = null;
        private Stopwatch callbackStopWatch = new();
        private bool save_changes = false;
        private bool copy_text = false;
        private bool paste_text = false;


        public ImGuiShaderSourceEditor()
        {
            unsafe
            {
                TextCallback += (ImGuiInputTextCallbackData* data) =>
                {
                    

                    var io = ImGuiCore.GetIO();
                    if (io.KeyCtrl && io.KeysDown[(int)NbKey.S])
                    {
                        save_changes = true;
                        callbackStopWatch.Restart();
                    }
                    else if (io.KeyCtrl && io.KeysDown[(int)NbKey.C])
                    {
                        copy_text = true;
                        callbackStopWatch.Restart();
                    }
                    else if (io.KeyCtrl && io.KeysDown[(int)NbKey.V])
                    {
                        paste_text = true;
                        callbackStopWatch.Restart();
                    }

                    //Management

                    if (callbackStopWatch.ElapsedMilliseconds > 200)
                    {
                        if (save_changes)
                        {
                            Console.WriteLine($"Saving Changes to {ActiveShaderSource.SourceFilePath}");
                            System.IO.File.WriteAllText(ActiveShaderSource.SourceFilePath, SourceText);
                            save_changes = false;
                        }

                        if (copy_text)
                        {
                            //Construct string from bytes
                            StringBuilder sb = new StringBuilder();
                            int sel_start = System.Math.Min(data->SelectionEnd, data->SelectionStart);
                            int sel_end = System.Math.Max(data->SelectionEnd, data->SelectionStart);


                            for (int i = 0; i < sel_end - sel_start; i++)
                                sb.Append((char)data->Buf[sel_start + i]);

                            Console.WriteLine(sb.ToString());
                            TextCopy.ClipboardService.SetText(sb.ToString());
                            copy_text = false;
                        }

                        if (paste_text)
                        {
                            string new_text = TextCopy.ClipboardService.GetText();

                            //Copy buffer after the cursor to temp
                            byte[] afterBuffer = new byte[data->BufTextLen + new_text.Length];

                            for (int i = 0; i < data->CursorPos; i++)
                                afterBuffer[i] = data->Buf[i];

                            //Add new text to buffer
                            for (int i = 0; i < new_text.Length; i++)
                                afterBuffer[data->CursorPos + i] = (byte) new_text[i];

                            //Copy the rest of the buffer
                            for (int i = data->CursorPos; i < data->BufTextLen; i++)
                                afterBuffer[new_text.Length + i] = data->Buf[i];

                            //Copy afterBuffer to data->Buf
                            for (int i = 0; i < afterBuffer.Length; i++)
                                data->Buf[i] = afterBuffer[i];

                            data->BufDirty = 0x1;
                            data->BufTextLen = afterBuffer.Length;
                            paste_text = false;
                        }


                        callbackStopWatch.Stop();
                        callbackStopWatch.Reset();
                    }




                    return 0;
                };
            }
            
        }

        public void Draw()
        {
            //TODO: Make this static if possible or maybe maintain a list of shaders in the resource manager
            
            //Items
            List<Entity> shaderSourceList = RenderState.engineRef.GetEntityTypeList(EntityType.ShaderSource);
            string[] items = new string[shaderSourceList.Count];
            for (int i = 0; i < items.Length; i++)
            {
                NbShaderSource ss = (NbShaderSource) shaderSourceList[i];
                items[i] = ss.SourceFilePath;
            }
                
            if (ImGuiCore.Combo("##1", ref selectedId, items, items.Length))
            {
                SetShader((NbShaderSource)shaderSourceList[selectedId]);
            }
                
            ImGuiCore.SameLine();

            if (ImGuiCore.Button("Add"))
            {
                Console.WriteLine("Todo Create Shader");
            }
            ImGuiCore.SameLine();
            if (ImGuiCore.Button("Del"))
            {
                Console.WriteLine("Todo Delete Shader");
            }

            if (ActiveShaderSource is null)
                return;

            var io = ImGuiCore.GetIO();
            ImGuiCore.PushFont(io.Fonts.Fonts[fontIndex]);
            ImGuiCore.InputTextMultiline("##2", ref SourceText, 50000,
                    new System.Numerics.Vector2(-1, -20), ImGuiInputTextFlags.CallbackAlways, TextCallback);
            ImGuiCore.PopFont();
            
            //ImGuiCore.SameLine();
            if (ImGuiCore.Button("Save"))
            {
                Console.WriteLine($"Saving Changes to {ActiveShaderSource.SourceFilePath}");
                System.IO.File.WriteAllText(ActiveShaderSource.SourceFilePath, SourceText);
            }

            int _fontIndex = Array.IndexOf(FontOptions, fontIndex.ToString());
            ImGuiCore.SameLine();
            if (ImGuiCore.Combo("##FontSelector", ref _fontIndex, FontOptions, FontOptions.Length))
            {
                fontIndex = int.Parse(FontOptions[_fontIndex]);
            }

        }

        public void SetShader(NbShaderSource conf)
        {
            ActiveShaderSource = conf;
            SourceText = conf.SourceText;
            List<Entity> shaderList = RenderState.engineRef.GetEntityTypeList(EntityType.ShaderSource);
            selectedId = shaderList.IndexOf(conf);
        }
    }
    
    
}