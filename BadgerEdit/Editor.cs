using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace BadgerEdit
{

    /// <summary>
    /// ▄▄▄▄·  ▄▄▄· ·▄▄▄▄   ▄▄ • ▄▄▄ .▄▄▄  ▄▄▄ .·▄▄▄▄  ▪  ▄▄▄▄▄
    ///▐█ ▀█▪▐█ ▀█ ██▪ ██ ▐█ ▀ ▪▀▄.▀·▀▄ █·▀▄.▀·██▪ ██ ██ •██  
    ///▐█▀▀█▄▄█▀▀█ ▐█· ▐█▌▄█ ▀█▄▐▀▀▪▄▐▀▀▄ ▐▀▀▪▄▐█· ▐█▌▐█· ▐█.▪
    ///██▄▪▐█▐█ ▪▐▌██. ██ ▐█▄▪▐█▐█▄▄▌▐█•█▌▐█▄▄▌██. ██ ▐█▌ ▐█▌·
    ///·▀▀▀▀  ▀  ▀ ▀▀▀▀▀• ·▀▀▀▀  ▀▀▀ .▀  ▀ ▀▀▀ ▀▀▀▀▀• ▀▀▀ ▀▀▀ 
    /// 
    ///     _.---.._             _.---...__
    ///.-'   /\   \          .'  /\     /
    ///`.   (  )   \        /   (  )   /
    ///  `.  \/   .'\      /`.   \/  .'
    ///    ``---''   )    (   ``---''
    ///            .';.--.;`.
    ///          .' /_...._\ `.
    ///        .'   `.a  a.'   `.
    ///       (        \/        )
    ///        `.___..-'`-..___.'
    ///           \          /
    ///            `-.____.-'
    ///
    ///
    /// 
    /// You will not be forgotten, old friend.
    /// </summary>

    public class Editor
    {
        public List<Line> Lines = new List<Line>() {new Line()};

        private IntVector _caretPosition = new IntVector();
        private IntVector _selectionAnchor = new IntVector();

        public IntVector CaretPosition
        {
            get => _caretPosition;
            set => _caretPosition = value.ClampY(Lines.Count).ClampX(Lines[value.Y].Count).ClampPositive();
        }

        public SelectionRange Selection {
            get
            {
                if (_caretPosition == _selectionAnchor)
                    return null;

                return new SelectionRange(_selectionAnchor, _caretPosition);
            }
        }

        public Line CurrentLine => Lines[CaretPosition.Y];
        public int LineNo => CaretPosition.Y;
        public int ColNo => CaretPosition.X;

        #region handling keypresses

        public void CharKey(char character)
        {
            CurrentLine.InsertGlyphs(ColNo, new Glyph(character));
            if (character == '\n')
            {
                Line newLine = new Line();
                newLine.AddRange(CurrentLine.Skip(CaretPosition.X+1));
                var remnant = CurrentLine.Take(CaretPosition.X).ToList();
                CurrentLine.Clear();
                CurrentLine.AddRange(remnant);
                Lines.Insert(LineNo+1, newLine);
                Move(MoveDirective.Down);
                Move(MoveDirective.Home);
                resetSelection();
            }
            else
            {
                Move(MoveDirective.Right);
                resetSelection();
            }
        }

        private void resetSelection()
        {
            _selectionAnchor = new IntVector(CaretPosition.X, CaretPosition.Y);
        }

        public void Delete()
        {
            if (Selection != null)
            {
                DeleteSelection();
                return;
            }

            if (ColNo < CurrentLine.Count)
            {
                CurrentLine.RemoveGlyphs(ColNo, 1);
            }
            else if (Lines.Count > LineNo + 1)
            {
                //remove newline if present
                if (CurrentLine.End?.Character == '\n')
                {
                    CurrentLine.RemoveAt(CurrentLine.Count);
                }
                //concat next line into current
                CurrentLine.Concatenate(Lines[LineNo + 1]);
                //remove next line
                Lines.RemoveAt(LineNo + 1);
            }
            resetSelection();
        }

        public void Backspace()
        {
            if (Selection != null)
            {
                DeleteSelection();
                return;
            }

            if (ColNo > 0)
            {
                CurrentLine.RemoveGlyphs(Math.Max(0, ColNo - 1), 1);
            }
            else if (LineNo > 0)
            {   
                var prevLine = Lines[LineNo - 1];
                //remove newLine on previous line
                if (prevLine.End?.Character == '\n')
                {
                    prevLine.RemoveAt(prevLine.Count);
                }
                //concat current line into previous.
                Lines[LineNo - 1] = prevLine.Concatenate(CurrentLine);

                //remove current line
                Lines.Remove(CurrentLine);
            }

            Move(MoveDirective.Left);
            resetSelection();
        }
        
        public void Move(MoveDirective directive, bool maintainSelection = false)
        {
            directive.Execute(Lines, CaretPosition);
            //Console.Out.WriteLine("caret : " + CaretPosition.AsString());
            if (!maintainSelection)
            {
                //Console.Out.WriteLine("lose selection");
                resetSelection();
            }
            //Console.Out.WriteLine("anchor : " + _selectionAnchor.AsString());
            //Console.Out.WriteLine("Selection : " + Selection?.AsString() ?? "NULL");
        }
        #endregion

        public void SelectAll()
        {
            _selectionAnchor = new IntVector(0,0);
            _caretPosition = new IntVector(Lines.LastOrDefault()?.Count ?? 0, Math.Max(0,Lines.Count-1));
        }

        public void Copy()
        {
            ImGuiNET.ImGuiNative.igSetClipboardText(Selection?.GetText(ref Lines) ?? "");
        }

        public void Cut()
        {
            Copy();

            if (Selection == null)
                return;

            DeleteSelection();
        }

        private void DeleteSelection()
        {
            if (Selection == null)
                return;

            if (Selection.Start.Y == Selection.End.Y)
            {
                Lines[Selection.Start.Y].RemoveGlyphs(Selection.Start.X, Selection.End.X);
                return;
            }


            Line StartLine = Lines[Selection.Start.Y];
            Line EndLine = Lines[Selection.End.Y];

            Line reconstructionLine = new Line();
            reconstructionLine.AddRange(StartLine.Take(Selection.Start.X));
            if (reconstructionLine.Any(c => c.Character == '\n'))
            {
                reconstructionLine.RemoveAll(itm => itm.Character == '\n');
            }
            reconstructionLine.AddRange(EndLine.Skip(Selection.End.X));
            
            //insert the reconstruction line
            this.Lines.Insert( Selection.Start.Y, reconstructionLine);

            //remove the lines that were hit
            for(int i = Selection.End.Y + 1; i > 0; i--)
                Lines.RemoveAt(i);

            CaretPosition = Selection.Start;
            resetSelection();
        }

        public void Paste()
        {
            var clipText = ImGuiNative.igGetClipboardText();
            if (!String.IsNullOrEmpty(clipText))
            {
                InsertText(clipText);
            }
        }

        private void InsertText(string clipText)
        {
            string[] linesToInsert = clipText.Split('\n');

            int lnIndex = 0;

            string trail = "";

            foreach (string ln in linesToInsert)
            {
                if (lnIndex == 0)
                {
                    //pick up trail from current line in case that ends up on a different line later.
                    trail = CurrentLine.GetText(CaretPosition.X);
                    CurrentLine.RemoveGlyphs(CaretPosition.X, trail.Length);
                    //insert at current line
                    CurrentLine.Concatenate(new Line(ln));
                    for(int i=0; i < ln.Length; i++)
                        Move(MoveDirective.Right);
                }
                else
                {
                    //insert a totally new line after current caret position
                    Lines.Insert(CaretPosition.Y, new Line(ln));
                    //move down and to end
                    Move(MoveDirective.Down);
                    Move(MoveDirective.End);
                }
                lnIndex++;
            }

            //append trail to current position if it exists.
            if(!String.IsNullOrEmpty(trail))
                CurrentLine.Concatenate(new Line(trail));

        }
    }
}