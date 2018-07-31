using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ImGuiNET;
using OpenTK;
using OpenTK.Input;
using Vector2 = System.Numerics.Vector2;

// ReSharper disable RedundantUnsafeContext

namespace TGREdit
{
    //working transcription of https://github.com/BalazsJako/ImGuiColorTextEdit/blob/master/TextEditor.cpp
    public class CodeEditor
    {
        //tgr : wth is this for?
        private const int TextStart = 7;

        private readonly bool _readOnly;
        private LanguageDefinition _languageDefinition;
        public float LineSpacing { get; }
        public int TabSize { get; }
        public bool Overwrite { get; }
        public bool WithinRender { get; private set; }
        public bool ScrollToCursor { get; private set; }
        public bool TextCshanged { get; set; }
        public int ColorRangeMin { get; private set; }
        public int ColorRangeMax { get; private set; }
        public SelectionMode Selectionmode { get; set; }

        public EditorState State { get; set; }

        public int UndoIndex { get; private set; }

        #region coordinates and selection
        public Coordinates SelectionStart {
            get => State.SelectionStart;
            set => State.SelectionStart = value;
        }
        public Coordinates SelectionEnd
        {
            get => State.SelectionEnd;
            set => State.SelectionEnd = value;
        }

        public Coordinates InteractiveStart, InteractiveEnd;

        //todo : is that right?
        public bool HasSelection => SelectionStart != SelectionEnd;
        #endregion

        public bool CheckMultilineComments { get; set; }

        //todo : this is technically known as a "caret". not as "cursor".
        private Coordinates _cursorPosition;

        public Coordinates CursorPosition
        {
            get => State.CursorPosition;
            set => State.CursorPosition = value;
        }

        public bool TextChanged { get; set; }

        public List<Line> Lines { get; set; }

        public LanguageDefinition LanguageDefinition
        {
            get => _languageDefinition;
            set
            {
                _languageDefinition = value;
                if (Lines.Any())
                {
                    Colorize();
                }
            }
        }

        public Dictionary<Regex, Identifier> RegexList { get; private set; }

        public Palette Palette { get; set; }

        public Vector2 CharAdvance { get; set; }

        public List<UndoRecord> UndoBuffer { get; set; }

        public string InputBuffer { get; set; }

        public CodeEditor(
            OpenTK.NativeWindow nw,
            float lineSpacing = 0.0f,
            int undoIndex = 0,
            int tabSize = 4,
            bool overwrite = false,
            bool readOnly = false,
            bool withinRender = false,
            bool scrollToCursor = false,
            bool textChanged = false,
            int colorRangeMin = 0,
            int colorRangeMax = 0,
            SelectionMode selectionmode = SelectionMode.Normal,
            bool checkMultilineComments = true)
        {
            Lines = @"THIS is a fully ramblomatic multiline test
with a whole bunch of nonsense
and if else whatever crap init.

and then comes a buncha code.

struct banana{
    carbon,
    hydrogen
};

void eat(banana mybanana){
poop();
}".Split('\n').Select(q => new Line(q)).ToList();
            State = new EditorState()
            {
                CursorPosition = new Coordinates(),
                SelectionEnd = new Coordinates(),
                SelectionStart = new Coordinates()
            };
            _readOnly = readOnly;
            LineSpacing = lineSpacing;
            UndoIndex = undoIndex;
            TabSize = tabSize;
            Overwrite = overwrite;
            WithinRender = withinRender;
            ScrollToCursor = scrollToCursor;
            TextChanged = textChanged;
            ColorRangeMin = colorRangeMin;
            ColorRangeMax = colorRangeMax;
            Selectionmode = selectionmode;
            CheckMultilineComments = checkMultilineComments;

            Palette = Palette.Dark;
            LanguageDefinition = LanguageDefinition.GLSL;

            nw.KeyDown += OnKeyDown;
            nw.KeyUp += OnKeyUp;
            nw.KeyPress += OnKeyPress;

            UndoBuffer = new List<UndoRecord>();
        }

        int AppendBuffer(string aBuffer, char chr, int aIndex)
        {
            if (chr != '\t')
            {
                aBuffer.Append(chr);
                return aIndex + 1;
            }

            var num = TabSize - aIndex % TabSize;
            for (int j = num; j > 0; --j)
                aBuffer.Append(' ');
            return aIndex + num;
        }

        string GetText(Coordinates start, Coordinates end)
        {
            string result = "";
            int prevLineNo = start.mLine;
            for (var it = start; it <= end; Advance(it))
            {
                if (prevLineNo != it.mLine && it.mLine < Lines.Count)
                    result.Append('\n');

                if (it == end)
                    break;

                prevLineNo = it.mLine;
                var line = Lines[it.mLine];
                if (!line.Empty && it.mColumn < line.Count)
                    result.Append(line[it.mColumn].character);
            }

            return result;
        }

        void SetSelection(Coordinates start, Coordinates end, SelectionMode Mode)
        {
            SelectionStart = start;
            SelectionEnd = end;
            if (SelectionStart > SelectionEnd)
            {
                Coordinates m = SelectionEnd;
                SelectionEnd = SelectionStart;
                SelectionStart = m;

            }
            switch (Mode)
            {
                case SelectionMode.Normal:
                    break;
                case SelectionMode.Word:
                {
                    SelectionStart = FindWordStart(SelectionStart);
                    if (!IsOnWordBoundary(SelectionEnd))
                        SelectionEnd = FindWordEnd(FindWordStart(SelectionEnd));
                    break;
                }
                case SelectionMode.Line:
                {
                    var lineNo = SelectionEnd.mLine;
                    var lineSize = lineNo < Lines.Count? Lines[lineNo].Count : 0;
                    SelectionStart = new Coordinates(SelectionStart.mLine, 0);
                    SelectionEnd = new Coordinates(lineNo, (int) lineSize);
                    break;
                }
            }
        }


        public Coordinates Advance(Coordinates c)
        {
            if (c.mLine < Lines.Count)
            {
                var line = Lines[c.mLine];
                return c.mColumn + 1 < line.Count ? new Coordinates(c.mLine, c.mColumn + 1) : new Coordinates(c.mLine + 1, 0);
            }
            return c;
        }

        public void Colorize(int fromLine = 0, int lines = -1)
        {
            int toLine = lines == -1 ? (int)Lines.Count : Math.Min((int)Lines.Count, fromLine + lines);
            ColorRangeMin = Math.Max(ColorRangeMin, fromLine);
            ColorRangeMax = Math.Max(ColorRangeMax, toLine);
            ColorRangeMin = Math.Max(0, ColorRangeMin);
            ColorRangeMax = Math.Max(ColorRangeMin, ColorRangeMax);
            CheckMultilineComments = true;

            //todo : this fuckface doesn't really colorize fuck all. is it done by colorizeinternal then?
        }

        void ColorizeRange(int fromLine, int toLine)
        {
            if (!Lines.Any() || fromLine >= toLine)
                return;

            int endLine = Math.Max(0, Math.Min(Lines.Count, toLine));
            
            for (int i = fromLine; i < endLine; ++i)
            {
                var line = Lines[i];

                var buffer = string.Join("",line.Select(cr => cr.character));
                

                foreach (var g in Lines[i])
                {
                    g.ColorIndex = PaletteIndex.Default;

                    //find em matches yo
                    //todo : this thing has no idea about preprocessors.
                    foreach (var p in LanguageDefinition.tokenRegexStrings)
                    {
                        foreach (Match match in new Regex(p.Key).Matches(buffer))
                        {
                            for (int cstart = match.Index; cstart < match.Index + match.Length; cstart++)
                            {
                                Lines[i][cstart].ColorIndex = p.Value;
                            }
                        }

                        foreach (var k in LanguageDefinition.Keywords)
                        {
                            foreach (var tuple in buffer.AllIndexesOf(k))
                            {
                                for(int tindex = tuple.Item1; tindex < tuple.Item2; tindex++)
                                    Lines[i][tindex].ColorIndex = PaletteIndex.Keyword;
                            }
                        }

                        foreach (var k in LanguageDefinition.Identifiers)
                        {
                            foreach (var tuple in buffer.AllIndexesOf(k))
                            {
                                for(int tindex = tuple.Item1; tindex < tuple.Item2; tindex++)
                                    Lines[i][tindex].ColorIndex = PaletteIndex.KnownIdentifier;
                            }
                        }
                    }
                }
            }
        }

        public void ColorizeInternal()
        {
            if (!Lines.Any())
                return;

            if (CheckMultilineComments)
            {
                var end = new Coordinates(Lines.Count, 0);
                var commentStart = end;
                var withinString = false;
                for (var i = new Coordinates(0, 0); i < end; i=Advance(i))
                {
                    var line = Lines[i.mLine];
                    if (!line.Empty)
                    {
                        Glyph g = line[i.mColumn];
                        char c = g.character;

                        bool inComment = commentStart <= i;

                        if (withinString)
                        {
                            line[i.mColumn].MultiLinecomment = inComment;

                            switch (c)
                            {
                                case '\"':
                                    if (i.mColumn + 1 < (int)line.Count && line[i.mColumn + 1].character == '\"')
                                    {
                                        i = Advance(i);
                                        if (i.mColumn < (int)line.Count)
                                            line[i.mColumn].MultiLinecomment = inComment;
                                    }
                                    else
                                        withinString = false;
                                    break;
                                case '\\':
                                    i = Advance(i);
                                    if (i.mColumn < (int)line.Count)
                                        line[i.mColumn].MultiLinecomment = inComment;
                                    break;
                            }
                        }
                        else
                        {
                            if (c == '\"')
                            {
                                withinString = true;
                                line[i.mColumn].MultiLinecomment = inComment;
                            }
                            else
                            {
                                //tgr : disclaimer.
                                // todo : 
                                // at this point i'm getting pretty tired of the original author's bullshit
                                // and terrible C++.
                                // i'll figure this crap out later. it sucks.

//                                Func<char, Glyph, bool> pred = (char a, Glyph b) => a == b.character;
//
//                                var from = i.mColumn;
//                                var startStr = LanguageDefinition.CommentStart;
//                                if (i.mColumn + startStr.Length <= line.Count &&
//                                    startStr[0] == from && startStr[from + startStr.Length].
//                                        equals(startStr.begin(), startStr.end(), from, from + startStr.size(), pred))
//
//                                    commentStart = i;
//
//                                inComment = commentStart <= i;
//
//                                line[i.mColumn].mMultiLineComment = inComment;
//
//                                auto & endStr = mLanguageDefinition.mCommentEnd;
//                                if (i.mColumn + 1 >= (int)endStr.size() &&
//                                    equals(endStr.begin(), endStr.end(), from + 1 - endStr.size(), from + 1, pred))
//                                    commentStart = end;
                            }
                        }
                    }
                }
                CheckMultilineComments = false;
                return;
            }

            if (ColorRangeMin<ColorRangeMax)
            {
                int to = Math.Min(ColorRangeMin + 10, ColorRangeMax); //wtf?!
                ColorizeRange(ColorRangeMin, to);
                ColorRangeMin = to;

                if (ColorRangeMax != ColorRangeMin)
                    return;

                ColorRangeMin = int.MaxValue;
                ColorRangeMax = 0;
            }
        }

        public void DeleteSelection()
        {
            if (SelectionEnd == SelectionStart && _readOnly)
                return;

            DeleteRange(SelectionStart, SelectionEnd);

            SetSelection(SelectionStart, SelectionStart, SelectionMode.Normal);
            CursorPosition = SelectionStart;
            Colorize(SelectionStart.mLine, 1);
        }

        void Delete()
        {
            if (_readOnly)
                return;

            if (!Lines.Any())
                return;

            UndoRecord u = new UndoRecord(State);
            
            if (HasSelection)
            {
                u.Removed = GetSelectedText();
                u.RemovedStart = State.SelectionStart;
                u.RemovedEnd = State.SelectionEnd;

                DeleteSelection();
            }
            else
            {
                var pos = CursorPosition;
                
                var line = Lines[pos.mLine];

                if (pos.mColumn == line.Count)
                {
                    if (pos.mLine == Lines.Count - 1)
                        return;

                    u.Removed = "\n";
                    u.RemovedStart = u.RemovedEnd = CursorPosition;
                    u.RemovedEnd = Advance(u.RemovedEnd);

                    var nextLine = Lines[pos.mLine + 1];
                    line.InsertRange(line.Count-1, nextLine);
                    RemoveLine(pos.mLine + 1);
                }
                else
                {
                    u.Removed = ""+line[pos.mColumn].character;
                    u.RemovedStart = u.RemovedEnd = CursorPosition;
                    u.RemovedEnd.mColumn++;

                    line.Erase(0, pos.mColumn);
                }

                TextChanged = true;

                Colorize(pos.mLine, 1);
            }

            u.After = State;
            AddUndo(u);
        }

        void BackSpace()
        {
            if (this._readOnly || !Lines.Any())
                return;
            
            UndoRecord u = new UndoRecord(State);

            if (HasSelection)
            {
                u.Removed = GetSelectedText();
                u.RemovedStart = State.SelectionStart;
                u.RemovedEnd = State.SelectionEnd;

                DeleteSelection();
            }
            else
            {
                var pos = CursorPosition;
                
                if (State.CursorPosition.mColumn == 0)
                {
                    if (State.CursorPosition.mLine == 0)
                        return;

                    u.Removed = "\n";
                    u.RemovedStart = u.RemovedEnd = pos;
                    Advance(u.RemovedEnd);

                    var line = Lines[State.CursorPosition.mLine];
                    var prevLine = Lines[State.CursorPosition.mLine - 1];
                    var prevSize = prevLine.Count;
                    prevLine.AddRange(line);
                    RemoveLine(State.CursorPosition.mLine);
                    CursorPosition = new Coordinates(CursorPosition.mLine-1, prevSize);
                }
                else
                {
                    var line = Lines[State.CursorPosition.mLine];

                    u.Removed = line[pos.mColumn - 1].character + "";
                    u.RemovedStart = u.RemovedEnd = CursorPosition;
                    u.RemovedStart = new Coordinates(u.RemovedStart.mColumn, u.RemovedStart.mLine);
                    
                    if (State.CursorPosition.mColumn < line.Count)
                        line.Erase(0, State.CursorPosition.mColumn);
                }

                TextChanged = true;

                EnsureCursorVisible();
                Colorize(State.CursorPosition.mLine, 1);
            }

            u.After = State;
            AddUndo(u);
        }

        string GetSelectedText()
        {
            return GetText(State.SelectionStart, State.SelectionEnd);
        }


    public void DeleteRange(Coordinates start, Coordinates end)
        {
            if (!(end >= start))
            {
                throw new ArgumentOutOfRangeException("negative range. doi.");
            }

            if (_readOnly)
            {
                return;
            }

            if (end == start)
                return;

            if (start.mLine == end.mLine)
            {
                var line = Lines[start.mLine];
                if (end.mColumn >= line.Count)
                    //tgr : used to be based on a pointer aiming at line.begin
                    line.Erase(start.mColumn, line.End);
                else
                    //tgr : used to be based on a pointer aiming at line.begin
                    line.Erase(start.mColumn, end.mColumn);
            }
            else
            {
                var firstLine = Lines[start.mLine];
                var lastLine = Lines[end.mLine];

                firstLine.Erase(start.mColumn, firstLine.Count);
                lastLine.Erase(0, end.mColumn);

                if (start.mLine < end.mLine)
                    //todo : firstline end?
                    firstLine.InsertRange(firstLine.Count, lastLine);

                if (start.mLine < end.mLine)
                    RemoveLine(start.mLine + 1, end.mLine + 1);
            }

            TextChanged = true;
        }
        
        public int InsertTextAt(Coordinates where, string newValues)
        {
            if (_readOnly)
                return -1;

            Coordinates w = where;

            int l = -1;

            foreach(char c in newValues)
            {
                l = InsertTextAt(w, c);
                w = new Coordinates(w.mLine, w.mColumn+1);
            }

            return l;
        }

        public int InsertTextAt(Coordinates where, char newValue)
        {
            if (_readOnly)
                return -1;

            int totalLines = 0;
            var chr = newValue;
            while (chr != '\0')
            {
                if (!Lines.Any())
                    Lines.Add(new Line());

                if (chr == '\r')
                {
                    // skip
                }
                else if (chr == '\n')
                {
                    if (where.mColumn < (int) Lines[where.mLine].Count)
                    {
                        var newLine = InsertLine(where.mLine + 1);
                        var line = Lines[where.mLine];
                        newLine.Insert(where.mColumn, line.End);
                        line.Erase(where.mColumn, line.Count);
                    }
                    else
                    {
                        InsertLine(where.mLine + 1);
                    }
                    ++where.mLine;
                    where.mColumn = 0;
                    ++totalLines;
                }
                else
                {
                    var line = Lines[where.mLine];

                    //tgr : original used a pointer to line.begin
                    line.Insert(where.mColumn, new Glyph(chr, PaletteIndex.Default));
                    ++where.mColumn;
                }
                //chr = *(++newValue); //wtf?

                TextChanged = true;
                return 1; //why does this return an int??
            }

            return totalLines;
        }

        public Coordinates ScreenPosToCoordinates(Vector2 position)
        {
            var origin = ImGui.GetCursorScreenPos();
            Vector2 local = new Vector2(position.X - origin.X, position.Y - origin.Y);

            int lineNo = Math.Max(0, (int) Math.Floor(local.Y / CharAdvance.Y));
            int columnCoord = Math.Max(0, (int) Math.Floor(local.X / CharAdvance.X) - TextStart);

            int column = 0;

            if (lineNo >= 0 && lineNo < Lines.Count)
            {
                var line = Lines[lineNo];
                var distance = 0;
                while (distance < columnCoord && column < (int) line.Count)
                {
                    if (line[column].character == '\t')
                    {
                        distance = (distance / TabSize) * TabSize + TabSize;
                    }
                    else
                    {
                        ++distance;
                    }
                    ++column;
                }
            }

            return new Coordinates(lineNo, column);

        }

        Line InsertLine(int index)
        {
            if (_readOnly)
                return null;

            var l = new Line();

            Lines.Insert(index, l);

            return l;
        }


        void RemoveLine(int start, int end)
        {
            if (_readOnly)
                return;

            Lines.RemoveRange(start, end);
        }

        void RemoveLine(int idx)
        {
            if (_readOnly)
                return;

            Lines.RemoveAt(idx);
        }

        //tgr : original used colorindex of glyphs, but that is not how i define a 'word'.
        public Coordinates FindWordStart(Coordinates from)
        {
            if (from.mLine >= Lines.Count)
                return from;

            var line = Lines[from.mLine];

            int colD = Math.Max(0,Math.Min(from.mColumn, Lines[from.mLine].Count-1));
            while (colD > 0)
            {
                if (line[colD].character == ' ')
                    --colD;
                else
                    break;
            }

            return new Coordinates(from.mLine, colD);
        }

        //tgr : original used colorindex of glyphs, but that is not how i define a 'word'.
        public Coordinates FindWordEnd(Coordinates from)
        {
            if (from.mLine >= Lines.Count)
                return from;

            var line = Lines[from.mLine];

            int colD = from.mColumn;
            while (colD < line.Count)
            {
                if (line[colD].character == ' ')
                    ++colD;
                else
                    break;
            }

            return new Coordinates(from.mLine, colD);
        }

        bool IsOnWordBoundary(Coordinates at)
        {
            if (at.mLine >= Lines.Count || at.mColumn == 0)
                return true;

            var line = Lines[at.mLine];
            if (at.mColumn >= line.Count)
                return true;

            return line[at.mColumn].ColorIndex != line[at.mColumn - 1].ColorIndex;
        }

        public string GetWordUnderCursor()
        {
            var c = CursorPosition;
            return GetWordAt(c);
        }

        public string GetWordAt(Coordinates at)
        {
            var start = FindWordStart(at);
            var end = FindWordEnd(at);

            string r = "";

            Coordinates coo = start;

            //todo : is this a +1?
            while (coo < end)
            {
                r.Append(Lines[coo.mLine][coo.mColumn].character);
                coo = Advance(coo);
            }

            return r;
        }

        private System.Numerics.Vector4 ColorConvertU32ToFloat4(uint inp)
        {
            //BGRA packed
            int IM_COL32_R_SHIFT = 16;
            int IM_COL32_G_SHIFT = 8;
            int IM_COL32_B_SHIFT = 0;
            int IM_COL32_A_SHIFT = 24;
            uint IM_COL32_A_MASK = 0xFF000000;
            
            //RGBA packed
//            int IM_COL32_R_SHIFT = 0;
//            int IM_COL32_G_SHIFT = 8;
//            int IM_COL32_B_SHIFT = 16;
//            int IM_COL32_A_SHIFT = 24;
//            uint IM_COL32_A_MASK = 0xFF000000;

            const float s = 1.0f / 255f;
            return new System.Numerics.Vector4(
                ((inp >> IM_COL32_R_SHIFT) & 0xFF) *s,
                ((inp >> IM_COL32_G_SHIFT) &0xFF) *s,
                ((inp >> IM_COL32_B_SHIFT) &0x00) *s,
                ((inp >> IM_COL32_A_SHIFT) &0xFF) *s
            );
        }
        

        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            if (InputBuffer == null)
                InputBuffer = "";
            Console.Write("Char typed: " + e.KeyChar);
            ImGui.AddInputCharacter(e.KeyChar);
            InputBuffer = InputBuffer + e.KeyChar;
        }

        private static unsafe void SetOpenTKKeyMappings()
        {
            IO io = ImGui.GetIO();
            io.KeyMap[GuiKey.Tab] = (int)Key.Tab;
            io.KeyMap[GuiKey.LeftArrow] = (int)Key.Left;
            io.KeyMap[GuiKey.RightArrow] = (int)Key.Right;
            io.KeyMap[GuiKey.UpArrow] = (int)Key.Up;
            io.KeyMap[GuiKey.DownArrow] = (int)Key.Down;
            io.KeyMap[GuiKey.PageUp] = (int)Key.PageUp;
            io.KeyMap[GuiKey.PageDown] = (int)Key.PageDown;
            io.KeyMap[GuiKey.Home] = (int)Key.Home;
            io.KeyMap[GuiKey.End] = (int)Key.End;
            io.KeyMap[GuiKey.Delete] = (int)Key.Delete;
            io.KeyMap[GuiKey.Backspace] = (int)Key.BackSpace;
            io.KeyMap[GuiKey.Enter] = (int)Key.Enter;
            io.KeyMap[GuiKey.Escape] = (int)Key.Escape;
            io.KeyMap[GuiKey.A] = (int)Key.A;
            io.KeyMap[GuiKey.C] = (int)Key.C;
            io.KeyMap[GuiKey.V] = (int)Key.V;
            io.KeyMap[GuiKey.X] = (int)Key.X;
            io.KeyMap[GuiKey.Y] = (int)Key.Y;
            io.KeyMap[GuiKey.Z] = (int)Key.Z;
        }


        private unsafe void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            ImGui.GetIO().KeysDown[(int)e.Key] = true;
            UpdateModifiers(e);
        }

        private unsafe void OnKeyUp(object sender, KeyboardKeyEventArgs e)
        {
            ImGui.GetIO().KeysDown[(int)e.Key] = false;
            UpdateModifiers(e);
        }

        private static unsafe void UpdateModifiers(KeyboardKeyEventArgs e)
        {
            IO io = ImGui.GetIO();
            io.AltPressed = e.Alt;
            io.CtrlPressed = e.Control;
            io.ShiftPressed = e.Shift;
        }

        #region caret movement

        //tgr : i see a lot of stupid shit going on in here. this needs improving badly.

        public void MoveUp(int amount, bool select)
        {
            var oldPos = CursorPosition;
            CursorPosition = new Coordinates(Math.Max(0, CursorPosition.mLine - amount), _cursorPosition.mColumn);
            if (oldPos == CursorPosition) return;
            if (@select)
            {
                if (oldPos == InteractiveStart)
                    InteractiveStart = CursorPosition;
                else if (oldPos == InteractiveEnd)
                    InteractiveEnd = CursorPosition;
                else
                {
                    InteractiveStart = CursorPosition;
                    InteractiveEnd = oldPos;
                }
            }
            else
                InteractiveStart = InteractiveEnd = CursorPosition;

            SetSelection(InteractiveStart, InteractiveEnd, SelectionMode.Normal);

            EnsureCursorVisible();
        }

        public void MoveDown(int amount, bool select)
        {
            var oldPos = CursorPosition;
            CursorPosition = new Coordinates(Math.Min(Lines.Count-1, CursorPosition.mLine + amount), _cursorPosition.mColumn);

            if (CursorPosition == oldPos) return;
            if (select)
            {
                if (oldPos == InteractiveEnd)
                    InteractiveEnd = CursorPosition;
                else if (oldPos == InteractiveStart)
                    InteractiveStart = CursorPosition;
                else
                {
                    InteractiveStart = oldPos;
                    InteractiveEnd = CursorPosition;
                }
            }
            else
                InteractiveStart = InteractiveEnd = CursorPosition;

            SetSelection(InteractiveStart, InteractiveEnd, SelectionMode.Normal);

            EnsureCursorVisible();
        }

        public void MoveLeft(int amount, bool select, bool wordMode)
        {
            if (!Lines.Any())
                return;

            var oldPos = CursorPosition;
            
            while (amount-- > 0)
            {
                if (CursorPosition.mColumn == 0)
                {
                    if (CursorPosition.mLine > 0)
                    {
                        //todo : is  -1 on row correct?
                        CursorPosition = new Coordinates(_cursorPosition.mLine - 1, Lines[_cursorPosition.mLine-1].Count-1);
                    }
                }
                else
                {
                    CursorPosition = new Coordinates(CursorPosition.mLine, Math.Max(0,CursorPosition.mColumn-1));
                    if (wordMode)
                    {
                        FindWordStart(CursorPosition);
                    }
                }
            }
            
            if (select)
            {
                if (oldPos == InteractiveStart)
                    InteractiveStart = CursorPosition;
                else if (oldPos == InteractiveEnd)
                    InteractiveEnd = CursorPosition;
                else
                {
                    InteractiveStart = CursorPosition;
                    InteractiveEnd = oldPos;
                }
            }
            else
                InteractiveStart = InteractiveEnd = CursorPosition;

            SetSelection(InteractiveStart, InteractiveEnd, select && wordMode ? SelectionMode.Word : SelectionMode.Normal);

            EnsureCursorVisible();
        }

        public void MoveRight(int amount, bool select, bool wordMode)
        {
            var oldPos = CursorPosition;

            if (!Lines.Any())
                return;

            while (amount-- > 0)
            {
                var line = Lines[CursorPosition.mLine];

                if (CursorPosition.mColumn >= line.Count)
                {
                    if (CursorPosition.mLine < Lines.Count - 1)
                    {
                        CursorPosition = new Coordinates(
                            Math.Max(0, Math.Min(Lines.Count - 1, CursorPosition.mLine + 1)),
                            0
                        );
                    }
                }
                else
                {
                    CursorPosition = new Coordinates(CursorPosition.mLine, Math.Max(0, Math.Min(line.Count, CursorPosition.mColumn + 1)));
                    
                    if (wordMode)
                        CursorPosition = FindWordEnd(CursorPosition);
                }
            }

            if (@select)
            {
                if (oldPos == InteractiveEnd)
                    InteractiveEnd = CursorPosition;
                else if (oldPos == InteractiveStart)
                    InteractiveStart = CursorPosition;
                else
                {
                    InteractiveStart = oldPos;
                    InteractiveEnd = CursorPosition;
                }
            }
            else
                InteractiveStart = InteractiveEnd = CursorPosition;

            SetSelection(InteractiveStart, InteractiveEnd, @select && wordMode ? SelectionMode.Word : SelectionMode.Normal);

            EnsureCursorVisible();
        }

        public void MoveTop(bool select)
        {
            var oldPos = CursorPosition;
            CursorPosition = new Coordinates(0, 0);

            if (CursorPosition == oldPos)
                return;

            if (select)
            {
                InteractiveEnd = oldPos;
                InteractiveStart = CursorPosition;
            }
            else
            {
                InteractiveStart = InteractiveEnd = CursorPosition;
            }

            SetSelection(InteractiveStart, InteractiveEnd, SelectionMode.Normal);
        }
        public void MoveBottom(bool select)
        {
            var oldPos = CursorPosition;
            CursorPosition = new Coordinates(Lines.Count-1, Lines.LastOrDefault()?.Count ?? 1 -1);

            if (CursorPosition == oldPos)
                return;

            if (select)
            {
                InteractiveEnd = oldPos;
                InteractiveStart = CursorPosition;
            }
            else
                InteractiveStart = InteractiveEnd = CursorPosition;

            SetSelection(InteractiveStart, InteractiveEnd, SelectionMode.Normal);
        }

        public void MoveHome(bool select)
        {
            var oldPos = CursorPosition;
            CursorPosition = new Coordinates(CursorPosition.mLine, 0);

            if (CursorPosition == oldPos)
                return;

            if (select)
            {
                if (oldPos == InteractiveStart)
                    InteractiveStart = CursorPosition;
                else if (oldPos == InteractiveEnd)
                    InteractiveEnd = CursorPosition;
                else
                {
                    InteractiveStart = CursorPosition;
                    InteractiveEnd = oldPos;
                }
            }
            else
                InteractiveStart = InteractiveEnd = CursorPosition;
            SetSelection(InteractiveStart, InteractiveEnd, SelectionMode.Normal);
        }
        public void MoveEnd(bool select)
        {
            var oldPos = CursorPosition;
            //todo : is -1 correct here?
            CursorPosition = new Coordinates(CursorPosition.mLine, Lines[oldPos.mLine].Count-1);

            if (CursorPosition == oldPos)
                return;
            
            if (select)
            {
                if (oldPos == InteractiveStart)
                    InteractiveStart = CursorPosition;
                else if (oldPos == InteractiveEnd)
                    InteractiveEnd = CursorPosition;
                else
                {
                    InteractiveStart = CursorPosition;
                    InteractiveEnd = oldPos;
                }
            }
            else
                InteractiveStart = InteractiveEnd = CursorPosition;
            SetSelection(InteractiveStart, InteractiveEnd, SelectionMode.Normal);
        }

        public void EnsureCursorVisible()
        {
            if (!WithinRender)
            {
                ScrollToCursor = true;
                return;
            }

            float scrollX = ImGuiNative.igGetScrollX();
            float scrollY = ImGuiNative.igGetScrollY();

            var height = ImGuiNative.igGetWindowHeight();
            var width = ImGuiNative.igGetWindowWidth();

            var top = 1 + (int)Math.Ceiling(scrollY / CharAdvance.Y);
            var bottom = (int)Math.Ceiling((scrollY + height) / CharAdvance.Y);

            var left = (int)Math.Ceiling(scrollX / CharAdvance.X);
            var right = (int)Math.Ceiling((scrollX + width) / CharAdvance.X);

            var pos = CursorPosition;
            var len = TextDistanceToLineStart(pos);

            if (pos.mLine < top)
                ImGuiNative.igSetScrollY(Math.Max(0, (pos.mLine -1) * CharAdvance.Y));
            if (pos.mLine > bottom - 4)
                ImGuiNative.igSetScrollY(Math.Max(0, (pos.mLine + 4) * CharAdvance.Y - height));
            if (len + TextStart < left + 4)
                ImGuiNative.igSetScrollX(Math.Max(0, (len + TextStart - 4) * CharAdvance.X));
            if (len + TextStart > right - 4)
                ImGuiNative.igSetScrollX(Math.Max(0, (len + TextStart + 4) * CharAdvance.X - width));
        }

        int TextDistanceToLineStart(Coordinates from)
        {
            var line = Lines[Math.Min(Math.Max(0,from.mLine), Lines.Count-1)];
            var len = 0;
            for (var it = 0; it<line.Count-1 && it<from.mColumn-1; ++it) //tgr : wtf? unsigned
                len = line[it].character == '\t' ? (len / TabSize) * TabSize + TabSize : len + 1;
            return len;
        }

    #endregion

        public void Render(string title, Vector2 size, bool border)
        {
            WithinRender = true;
            TextChanged = false;
            
            ImGui.BeginChild(title, size, border, WindowFlags.HorizontalScrollbar | WindowFlags.NoMove);
            //I NEVER THOUGHT THIS WOULD BE THE WORST FUCKING PART GOD DAMMIT :(

            //push allow keyboard focus. whatever the fuck that means.
            
            //for some reason reference where to find CTRL, ALD and shift?


            IO io = ImGui.GetIO();

            //todo : i have no idea what the fuck is up with this.
            //we're supposed to get the width of the "X" character for this, which is supposedly the widest
            //but it makes no sense looking at the rest of the code.
            var xadv = 1; 

            //todo : why the heck are all these values pow2?
            CharAdvance = new Vector2(ImGuiNative.igGetFontSize() * xadv, ImGuiNative.igGetFontSize() * 1);


            if (ImGui.IsWindowFocused(FocusedFlags.RootAndChildWindows)) //?
            {
                if (ImGui.IsWindowHovered(HoveredFlags.Default))
                {
                    ImGui.MouseCursor = MouseCursorKind.TextInput;
                }
                io.WantCaptureKeyboard = true;
                io.WantTextInput = true;

                if (!_readOnly)
                {
                    if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.Z)))
                    {
                        if (io.CtrlPressed && io.ShiftPressed)
                        {
                            Redo(1);
                        }
                        else if (io.ShiftPressed)
                        {
                            Undo(1);
                        }
                    }

                    if (io.CtrlPressed && ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.Y)))
                    {
                        Redo(1);
                    }
                }

                
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.UpArrow)))
                {
                    MoveUp(1, io.ShiftPressed);
                }

                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.LeftArrow)))
                {
                    MoveLeft(1, io.ShiftPressed, false);
                }

                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.RightArrow)))
                {
                    MoveRight(1, io.ShiftPressed, io.CtrlPressed);
                }

                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.DownArrow)))
                {
                    MoveDown(1, io.ShiftPressed);
                }

                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.Home)) && io.CtrlPressed)
                {
                    MoveTop(io.ShiftPressed);
                }

                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.Home)) && !io.CtrlPressed)
                {
                    MoveHome(io.ShiftPressed);
                }
                
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.End)) && io.CtrlPressed)
                {
                    MoveBottom(io.ShiftPressed);
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.End)) && !io.CtrlPressed)
                {
                    MoveEnd(io.ShiftPressed);
                }

                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.Delete)))
                {
                    Delete();
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.Backspace)))
                {
                    BackSpace();
                }
                if (io.CtrlPressed && ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.C)))
                {
                    Copy();
                }
                if (io.CtrlPressed && ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.V)))
                {
                    Paste();
                }
                if (io.CtrlPressed && ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.X)))
                {
                    Cut();
                }
                if (io.CtrlPressed && ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.A)))
                {
                    SelectAll();
                }


                if (!_readOnly && !String.IsNullOrEmpty(InputBuffer))
                {

                    //todo : ghokay so the input buffer is fucked. we need to keep track of this manually. :(
                    foreach (char cr in InputBuffer)
                    {
                        EnterCharacter(cr);
                    }
                    InputBuffer = "";
                }
            }
            
            if (ImGui.IsWindowHovered(HoveredFlags.Default))
            {
                if (!io.ShiftPressed && !io.AltPressed)
                {
                    var clicked = ImGui.IsMouseClicked(0);
                    var doubleclicked = ImGui.IsMouseDoubleClicked(0);
                    
                    //todo : fuck triple clicking. seriously.

                    if (doubleclicked)
                    {
                        if (!io.CtrlPressed)
                        {
                            CursorPosition = InteractiveStart = InteractiveEnd = ScreenPosToCoordinates(ImGuiNET.ImGui.GetMousePos());
                            Selectionmode = Selectionmode == SelectionMode.Line ? SelectionMode.Normal : 
                                            SelectionMode.Word;

                            SetSelection(InteractiveStart, InteractiveEnd, Selectionmode);
                        }
                    }
                    else if (clicked)
                    {
                        CursorPosition = InteractiveStart = InteractiveEnd = ScreenPosToCoordinates(ImGuiNET.ImGui.GetMousePos());
                        Selectionmode = io.CtrlPressed ? SelectionMode.Word : SelectionMode.Normal;

                        SetSelection(InteractiveStart, InteractiveEnd, Selectionmode);
                    }
                    //todo : wtf is a lock threshold?
                    else if (ImGui.IsMouseDragging(0, 25) && ImGuiNET.ImGui.IsMouseDown(0))
                    {
                        io.WantCaptureMouse = true;
                        CursorPosition = InteractiveEnd = ScreenPosToCoordinates(ImGui.GetMousePos());
                        SetSelection(InteractiveStart, InteractiveEnd, Selectionmode);
                    }
                }
            }

            ColorizeInternal();

            string buffer ="";
            var contentSize = ImGui.GetWindowContentRegionMax();
            var drawList = ImGui.GetWindowDrawList(); //todo : no idea if the assignments are gonna work like this.

            int appendIndex = 0;
            int longest = TextStart;

            Vector2 cursorScreenPos = ImGui.GetCursorScreenPos();
            float scrollX = ImGuiNative.igGetScrollX();
            float scrollY = ImGuiNative.igGetScrollY();

            int lineNo = (int) Math.Floor(scrollY / CharAdvance.Y);
            var lineMax = Math.Max(0, Math.Min(Lines.Count - 1, lineNo + Math.Floor(scrollY + contentSize.Y) / CharAdvance.Y));

            if (Lines.Any())
            {
                while (lineNo <= lineMax)
                {
                    Vector2 lineStartScreenPos = new Vector2(cursorScreenPos.X, cursorScreenPos.Y + lineNo * CharAdvance.Y);
                    Vector2 textScreenPos = new Vector2(lineStartScreenPos.X + CharAdvance.X * TextStart, lineStartScreenPos.Y);

                    var line = Lines[lineNo];

                    longest = Math.Max(TextStart + TextDistanceToLineStart(new Coordinates(lineNo, line.Count)), longest);
                    int columnNo = 0;
                    Coordinates lineStartCoord = new Coordinates(lineNo, 0);
                    Coordinates lineEndCoord = new Coordinates(lineNo, line.Count);

                    int sstart = -1;
                    int ssend = -1;


                    //todo : i'm assuming we are working with columns here.
                    if (SelectionStart <= SelectionEnd)
                    {
                        sstart = SelectionStart > lineStartCoord ? TextDistanceToLineStart(SelectionStart) : 0;   
                    }
                    if(SelectionEnd > lineStartCoord)
                    {
                        ssend = TextDistanceToLineStart(SelectionEnd < lineEndCoord ? SelectionEnd : lineEndCoord);
                    }

                    if (SelectionEnd.mLine > lineNo)
                    {
                        ++ssend;
                    }

                    if (sstart != -1 && ssend != -1 && sstart < ssend)
                    {
                        Vector2 vstart = new Vector2(lineStartScreenPos.X +(CharAdvance.X) * (sstart + TextStart), lineStartScreenPos.Y);
                        Vector2 vend = new Vector2(lineStartScreenPos.X +(CharAdvance.X) * (ssend + TextStart), lineStartScreenPos.Y + CharAdvance.Y);
                        drawList.AddRectFilled(vstart, vend, Palette[PaletteIndex.Selection], 0);

                    }

                    char[] buf = new char[16];

                    var start = new Vector2(lineStartScreenPos.X + scrollX, lineStartScreenPos.Y);

                    //breakpoint rendering used to be here. i dont care about them.
                    //errors too, but fuck em.
//                    var errorIt = ErrorMarkers.find(lineNo + 1);
//                    if (errorIt != ErrorMarkers.End)
//                    {
//                        var end = new Vector2(lineStartScreenPos.X + contentSize.X + 2.0f * scrollX,
//                            lineStartScreenPos.Y + CharAdvance.Y);
//
//                        drawList.AddRectFilled(start, end, Palette[PaletteIndex.ErrorMarker], 0);
//
//                        if (ImGui.IsMouseHoveringRect(lineStartScreenPos, end, true)) //todo : clip?
//                        {
//                            ImGui.SetTooltip($"Error at line {errorIt.first} \n-----\n{errorIt.str}");
//                        }
//                    }

                    drawList.AddText(new Vector2(lineStartScreenPos.X, lineStartScreenPos.Y), lineNo.ToString(), Palette[PaletteIndex.LineNumber]); 

                    if(CursorPosition.mLine == lineNo)
                    {
                        var focused = ImGui.IsWindowFocused(FocusedFlags.RootWindow);


                        if (!HasSelection)
                        {
                            var end = new Vector2(start.X + contentSize.X + scrollX, start.Y + CharAdvance.Y);
                            drawList.AddRectFilled(start, end, Palette[focused ? PaletteIndex.CurrentLineFill : PaletteIndex.CurrentLineFillInactive], 0);
                            drawList.AddRect(start, end, Palette[PaletteIndex.CurrentLineEdge], 1.0f, 0, 1);
                        }
                        
                        //todo : the original made the selected line do an animation. i don't care about that.
                    }

                    appendIndex = 0;
                    var prevColor = line.Empty
                        ? PaletteIndex.Default
                        : (line[0].MultiLinecomment ? PaletteIndex.MultiLineComment : line[0].ColorIndex);


                    foreach(Glyph glyph in line)
                    {
                        var color = glyph.MultiLinecomment ? PaletteIndex.MultiLineComment : glyph.ColorIndex;

                        if (color != prevColor && buffer.Length > 0)
                        {
                            drawList.AddText(textScreenPos, buffer, Palette[prevColor]);
                            textScreenPos.X += CharAdvance.X * buffer.Length;
                            buffer = "";
                            prevColor = color;
                        }
                        buffer += glyph.character;
                        ++columnNo;
                    }
                    appendIndex = 0;
                    drawList.AddText(textScreenPos, buffer, Palette[prevColor]);
                    textScreenPos.X += CharAdvance.X * buffer.Length;
                    buffer = "";

                    lineStartScreenPos.Y += CharAdvance.Y;
                    textScreenPos.X = lineStartScreenPos.X + CharAdvance.X * TextStart;
                    textScreenPos.Y = lineStartScreenPos.Y;
                    ++lineNo;
                }


                var id = GetWordAt(ScreenPosToCoordinates(ImGui.GetMousePos()));
                if (!String.IsNullOrEmpty(id) && LanguageDefinition.Identifiers.Contains(id))
                {
                    if (id != LanguageDefinition.Identifiers.Last())
                    {
                        ImGui.SetTooltip(id);
                    }
                    //todo : dont get it. dont want to.
//                    else if(LanguageDefinition.mProcIdentifiers.ContainsKey(id))
//                    {
//                        var pi = LanguageDefinition.mProcIdentifiers[id];
//                        if (pi != LanguageDefinition.mProcIdentifiers.LastOrDefault().Value)
//                        {
//                            ImGui.SetTooltip(pi.Declaration);
//                        }
//                    }
                }

            }


            ImGui.Dummy(new Vector2((longest + 2) * CharAdvance.X, Lines.Count * CharAdvance.Y));

            if (ScrollToCursor)
            {
                EnsureCursorVisible();
                ImGuiNative.igSetWindowFocus();
                ScrollToCursor = false;
            }

            //ImGui.PopAllowKeyboardFocus();
            ImGui.EndChild();
            //ImGui.PopStyleVar();
            //ImGui.PopStyleColor();

            WithinRender = false;
        }

        public void EnterCharacter(char c)
        {
            if (!_readOnly)
                return;

            UndoRecord u = new UndoRecord(State);
            
            if (HasSelection)
            {
                u.Removed = GetSelectedText();
                u.RemovedStart = State.SelectionStart;
                u.RemovedEnd = State.SelectionEnd;
                DeleteSelection();
            }

            var coord = CursorPosition;
            u.AddedStart = coord;

            if (!Lines.Any())
                Lines.Add(new Line());

            if (c == '\n')
            {
                InsertLine(coord.mLine + 1);
                Line line = Lines[coord.mLine];
                line.Insert(coord.mColumn, new Glyph(c, PaletteIndex.Default));
                State.CursorPosition = new Coordinates(coord.mLine + 1, 0);
            }
            else
            {
                Line line = Lines[coord.mLine];
                if (Overwrite && line.Count > coord.mColumn)
                    line[coord.mColumn] = new Glyph(c, PaletteIndex.Default);
                else
                    line.Insert(coord.mColumn, new Glyph(c, PaletteIndex.Default));

                State.CursorPosition = new Coordinates(coord.mLine, State.CursorPosition.mColumn+1);
            }

            TextChanged = true;

            u.Added = c + "";
            u.AddedEnd = CursorPosition;
            u.After = State;

            AddUndo(u);

            Colorize(coord.mLine - 1, 3);
            EnsureCursorVisible();
        }
        
        public void SelectAll()
        {
            SetSelection(
                new Coordinates(0, 0),
                new Coordinates(Lines.Count, Lines.Last()?.Count - 1 ?? 0),
                Selectionmode = SelectionMode.Normal
            );
        }

        public void Copy()
        {
            if (HasSelection)
            {
                ImGuiNET.ImGuiNative.igSetClipboardText(GetSelectedText());
            }
        }

        public void Cut()
        {
            if (_readOnly)
            {
                Copy();
            }
            else
            {
                if (!HasSelection)
                    return;

                UndoRecord u = new UndoRecord(State)
                {
                    Removed = GetSelectedText(),
                    RemovedStart = State.SelectionStart,
                    RemovedEnd = State.SelectionEnd
                };

                Copy();
                DeleteSelection();

                u.After = State;
                AddUndo(u);
            }
        }

        public void Paste()
        {
            var clipText = ImGuiNative.igGetClipboardText();
            if (!String.IsNullOrEmpty(clipText))
            {
                UndoRecord u = new UndoRecord(State)
                {
                    Added = clipText,
                    AddedStart = State.CursorPosition,
                    AddedEnd = State.CursorPosition
                };

                InsertText(clipText);

                u.After = State;
                AddUndo(u);
            }
        }

        public void InsertText(string newText)
        {
            if (_readOnly)
                return;

            var pos = CursorPosition;
            var start = HasSelection ? State.SelectionStart : pos;
            int totalLines = pos.mLine - start.mLine;

            totalLines += InsertTextAt(pos, newText);

            SetSelection(pos, pos, SelectionMode.Normal);
            CursorPosition = pos;
            Colorize(start.mLine - 1, totalLines + 2);
        }


        public bool CanUndo()
        {
            return UndoIndex > 0;
        }

        public bool CanRedo()
        {
            return UndoIndex < (int)UndoBuffer.Count;
        }

        public void Undo(int steps)
        {
            while (CanUndo() && steps-- > 0)
                UndoBuffer[--UndoIndex].Undo(this);
        }


        public void Redo(int Steps)
        {
            while (CanRedo() && Steps-- > 0)
                UndoBuffer[UndoIndex++].Redo(this);
        }

        public void AddUndo(UndoRecord aValue)
        {
            //todo : or is this shit supposed to FIFO?
            UndoBuffer.Add(aValue);
            ++UndoIndex;
        }

    

        //unsafe 
        
    }
}

