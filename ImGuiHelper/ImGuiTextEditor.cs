using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ImGuiNET;
using NbCore.Common;
using NbCore.Platform.Windowing;
using NibbleEditor;
using OpenTK.Graphics.OpenGL;
using ImGuiCore = ImGuiNET.ImGui;


namespace NbCore.UI.ImGui
{
    class TextEditorEntry
    {
        public string Path;
        public string TextBuffer;

        public TextEditorEntry(string path)
        {
            Path = path;
            TextBuffer = File.ReadAllText(path);
        }
    }

    public class ImGuiTextEditor
    {
        private AppImGuiManager _manager;
        private TextEditorEntry ActiveFile = null;
        private string CurrentSourceText = "";
        private int selectedId = -1;
        private int FontOption = 0;
        private int fontIndex = 0;
        private string[] FontOptions = new string[] { "1", "2", "3", "4", "5" };
        private ImGuiInputTextCallback TextCallback = null;
        private Stopwatch callbackStopWatch = new();
        private bool save_changes = false;
        private bool copy_text = false;
        private bool paste_text = false;

        private List<TextEditorEntry> _openedFiles = new();
        private OpenFileDialog _openFileDialog;

        public ImGuiTextEditor(AppImGuiManager manager)
        {
            _manager = manager;
            _openFileDialog = new("text-editor-open-file", ".cs|.txt");
            
            unsafe
            {
                TextCallback += (ImGuiInputTextCallbackData* data) =>
                {
                    if (_manager.WindowRef.IsKeyDown(NbKey.LeftCtrl))
                    {
                        if (_manager.WindowRef.IsKeyPressed(NbKey.S))
                        {   
                            save_changes = true;
                            callbackStopWatch.Restart();
                        }
                        else if (_manager.WindowRef.IsKeyPressed(NbKey.C))
                        {
                            copy_text = true;
                            callbackStopWatch.Restart();
                        }
                        else if (_manager.WindowRef.IsKeyPressed(NbKey.V))
                        {
                            paste_text = true;
                            callbackStopWatch.Restart();
                        }
                        else if (_manager.WindowRef.IsKeyPressed(NbKey.A))
                        {
                            //Select All text
                            data->SelectionStart = 0;
                            data->SelectionEnd = data->BufTextLen;
                        }


                        if (_manager.WindowRef.MouseScrollDelta.Y < 0)
                        {
                            //Decrease font
                            fontIndex = System.Math.Max(fontIndex - 1, 1);
                        } else if (_manager.WindowRef.MouseScrollDelta.Y > 0)
                        {
                            //Increase font
                            fontIndex = System.Math.Min(fontIndex + 1, 5);
                        }

                    }

                    


                    if (_manager.WindowRef.IsKeyPressed(NbKey.Tab))
                    {

                        if (_manager.WindowRef.IsKeyDown(NbKey.LeftShift))
                        {
                            //un-indent
                            Unindent(data);
                        } else
                        {
                            Indent(data);
                        }
                        
                        data->BufDirty = 0x1;
                    }
                        
                    //Management

                    if (callbackStopWatch.ElapsedMilliseconds > 200)
                    {
                        if (save_changes)
                        {
                            SaveFile();
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

                    //Console.WriteLine($"Cursor {data->CursorPos} TextLen{data->BufTextLen}");

                    return 0;
                };

                
            }

            
        }

        private unsafe void Indent(ImGuiInputTextCallbackData* data)
        {
            //Indent with spaces
            //Copy text after the current position
            byte[] new_buf = new byte[4 + data->BufTextLen - data->CursorPos];

            for (int i = 0; i < 4; i++)
                new_buf[i] = 0x20;

            for (int i = 0; i < data->BufTextLen - data->CursorPos; i++)
                new_buf[i + 4] = data->Buf[i + data->CursorPos];

            //Copy new buffer to data->Buf
            for (int i = 0; i < new_buf.Length; i++)
                data->Buf[data->CursorPos + i] = new_buf[i];

            data->CursorPos += 4;
            data->BufTextLen += 4;

        }

        private unsafe void Unindent(ImGuiInputTextCallbackData* data)
        {
            //Remove at most 4 spaces from the start of the current line

            int line_start_index = System.Math.Max(0, data->CursorPos - 1);
            int line_first_literal_index = System.Math.Max(0, data->CursorPos - 1);
            int search_index = System.Math.Max(0, data->CursorPos - 1);
            while (true)
            {
                if (data->Buf[search_index] == 0x0A)
                {
                    line_start_index = search_index;
                    break;
                } else if (data->Buf[search_index] >= 0x21 && data->Buf[search_index] <= 0x7E)
                    line_first_literal_index = search_index;
                search_index--;
            }

            //Calculate the number of leading spaces on the line
            int spaces_num = line_first_literal_index - line_start_index - 1;
            int trimmed_spaces = System.Math.Min(4, spaces_num);
            
            //Bring the test after the cursor back by trimmed_spaces
            for (int i = line_first_literal_index; i < data->BufTextLen; i++)
                data->Buf[i - trimmed_spaces] = data->Buf[i];

            //Clear rest of the buffer just in case
            for (int i = data->BufTextLen - trimmed_spaces; i < data->BufTextLen; i++)
                data->Buf[i] = 0;
            
            data->CursorPos -= trimmed_spaces;
            data->BufTextLen -= trimmed_spaces;
            
        }

        public void Draw()
        {
            string[] items = new string[_openedFiles.Count];
            for (int i = 0; i < _openedFiles.Count; i++)
            {
                items[i] = _openedFiles[i].Path;
            }
                
            if (ImGuiCore.Combo("##1", ref selectedId, items, items.Length))
            {
                SelectFile(items[selectedId]);
            }

            ImGuiCore.SameLine();
            
            if (ImGuiCore.Button("Browse"))
            {
                _openFileDialog.Open();
            }

            //Draw Open File Dialog
            if (_openFileDialog.Draw(new() { X = 640, Y = 480 }))
            {
                OpenFile(_openFileDialog.GetSelectedFile());
            }


            if (ActiveFile is null)
                return;

            var io = ImGuiCore.GetIO();
            ImGuiCore.PushFont(io.Fonts.Fonts[fontIndex]);
            ImGuiCore.InputTextMultiline("##2", ref CurrentSourceText, 50000,
                    new System.Numerics.Vector2(-1, -20), ImGuiInputTextFlags.CallbackAlways, TextCallback);
            ImGuiCore.PopFont();
            
            //ImGuiCore.SameLine();
            if (ImGuiCore.Button("Save"))
            {
                SaveFile();
            }

        }

        private void SaveFile()
        {
            Console.WriteLine($"Saving Changes to {ActiveFile.Path}");
            ActiveFile.TextBuffer = CurrentSourceText;
            File.WriteAllText(ActiveFile.Path, CurrentSourceText);
        }

        public void OpenFile(string path)
        {
            //At first check if such an entry exists
            TextEditorEntry _res = _openedFiles.Find(x=>x.Path == path);

            if (_res == null)
            {
                TextEditorEntry _entry = new(path);
                _openedFiles.Add(_entry);
                _res = _entry;
            }

            SelectFile(_res);
        }

        public void SelectFile(string _fpath)
        {
            ActiveFile = _openedFiles.Find(x=>x.Path == _fpath);
            SelectFile(ActiveFile);
        }

        private void SelectFile(TextEditorEntry _e)
        {
            ActiveFile = _e;
            CurrentSourceText = ActiveFile.TextBuffer;
            selectedId = _openedFiles.IndexOf(ActiveFile);
        }
    }
    
    
}