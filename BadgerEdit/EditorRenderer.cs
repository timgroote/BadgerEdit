using System;
using System.Linq;
using System.Numerics;
using BadgerEdit.FilePicker;
using ImGuiNET;
using NativeWindow = OpenTK.NativeWindow;

namespace BadgerEdit
{
    public class EditorRenderer
    {
        public NativeWindow Nw { get; }
        public Editor Badger { get; }

        public float Fontsize = 16f;

        private FilePicker.FilePicker fp = new FilePicker.FilePicker();
        
        private readonly Palette _palette;

        private const int horizontalContentOffset = 7;
        private const float charSpacing = 1;
        private const float lineSpacing = 1;

        private Font customFont = null;
        private bool isOpened = true;


        public EditorRenderer(NativeWindow nw, Editor badger, Palette p = null)
        {
            _palette = p ?? Palette.Dark;
            Nw = nw;
            Badger = badger;

            customFont = ImGui.GetIO().FontAtlas.AddFontFromFileTTF("fonts/Hack-Regular.ttf", Fontsize);
        }
        
        public void Render()
        {
            ImGui.BeginWindow("BadgerEdit 1.1", ref isOpened, new Vector2(800,600), 0.5f, WindowFlags.MenuBar);

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Open"))
                    {
                        fp.Show(FilePickerMode.Open);
                    }

                    ImGui.Separator();
                    if (ImGui.MenuItem("Save"))
                    {
                        fp.Show(FilePickerMode.Save);
                    }

                    ImGui.EndMenu();
                }
                
                ImGui.EndMenuBar();
            }

            fp.Draw();
            
            RenderWindowContents();
            ImGui.EndWindow();
        }

        private unsafe void RenderWindowContents()
        {
            ImGui.ShowStyleSelector("style");

            ImGui.BeginChild("BadgerEdit11", true, WindowFlags.HorizontalScrollbar | WindowFlags.AlwaysVerticalScrollbar);

            IO io = ImGui.GetIO();
            
            var charSize = ImGui.GetTextSize("F");
//            io.WantTextInput = true;
//            io.WantCaptureKeyboard = true;

            var contentSize = ImGui.GetWindowContentRegionMax();
            var drawList = ImGui.GetOverlayDrawList();

            Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();

            float scrollX = ImGuiNative.igGetScrollX();
            float scrollY = ImGuiNative.igGetScrollY();

            // first visible line
            int lineNo = (int)Math.Floor(scrollY / (Fontsize+ lineSpacing));
            
            //last visible line
            var lineMax = Math.Max(0, Math.Min(Badger.Lines.Count - 1, lineNo + Math.Floor(scrollY + contentSize.Y) / (Fontsize + lineSpacing)));

            if (Badger.Lines.Any())
            {
                if (customFont != null)
                {
                    ImGui.PushFont(customFont);
                }

                //only actually render visible lines
                while (lineNo <= lineMax)
                {
                    Vector2 lineStartScreenPos = new Vector2(cursorScreenPos.X, cursorScreenPos.Y + lineNo * (Fontsize + lineSpacing));
                    Vector2 textScreenPos = new Vector2(lineStartScreenPos.X + charSize.X * horizontalContentOffset, lineStartScreenPos.Y);

                    var line = Badger.Lines[lineNo];
                    
                    //highlight caret column
                    if (Badger.CaretPosition.Y == lineNo)
                    {
                        drawList.AddRectFilled(lineStartScreenPos, new Vector2(lineStartScreenPos.X - Fontsize + contentSize.X, lineStartScreenPos.Y + Fontsize), _palette[PaletteIndex.CurrentLineFill], 1, 1);
                        drawList.AddRect(lineStartScreenPos, new Vector2(lineStartScreenPos.X -Fontsize + contentSize.X, lineStartScreenPos.Y + Fontsize), _palette[PaletteIndex.CurrentLineEdge], 1, 1,1);
                    }

                    //IntVector LineStartCoord = new IntVector(lineNo, 0);
                    //IntVector LineEndCoord = new IntVector(lineNo, line.Count);

                    //render line number
                    drawList.AddText(new Vector2(lineStartScreenPos.X, lineStartScreenPos.Y), lineNo.ToString(), _palette[PaletteIndex.LineNumber]);

                    int colNo = 0;
                    foreach (Glyph g in line)
                    {
                        if (Badger.CaretPosition.Y == lineNo)
                        {
                            if(colNo == Badger.CaretPosition.X)
                            { 
                                drawList.AddLine(textScreenPos, textScreenPos + new Vector2(0, Fontsize), _palette[PaletteIndex.Cursor], 2);
                            }
                        }

                        var textWidthDelta = ImGui.GetTextSize(g.Character.ToString()).X + charSpacing;

                        //render selection
                        if (Badger.Selection != null && Badger.Selection.ContainsCoordinates(colNo, lineNo))
                        {
                            drawList.AddRectFilled(textScreenPos, textScreenPos + new Vector2(textWidthDelta, Fontsize), _palette[PaletteIndex.Selection], 0, -1);
                        }

                        drawList.AddText(textScreenPos, g.Character.ToString(), _palette[PaletteIndex.Default]);
                        
                        textScreenPos.X += textWidthDelta;
                        colNo++;


                        //render caret again just in case it's at the end of the line.
                        if (Badger.CaretPosition.Y == lineNo && colNo == Badger.CaretPosition.X)
                        {
                            drawList.AddLine(textScreenPos, textScreenPos + new Vector2(0, Fontsize), _palette[PaletteIndex.Cursor], 2);
                        }
                    }
                    lineNo++;
                }

                ImGui.Dummy(new Vector2(horizontalContentOffset + (Badger.Lines.Max(ln => ln.Count) + 7) * (charSize.X + charSpacing), Badger.Lines.Count * (Fontsize + lineSpacing)));
                if (customFont != null)
                {
                    ImGui.PopFont();
                }
            }

            ImGui.EndChild();
        }
    }
}
