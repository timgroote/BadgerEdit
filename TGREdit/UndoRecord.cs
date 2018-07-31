using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace TGREdit
{
    public class UndoRecord
    {
        public UndoRecord(string added, Coordinates addedStart, Coordinates addedEnd, string removed, Coordinates removedStart, Coordinates removedEnd, EditorState before, EditorState after)
        {
            Added = added;
            AddedStart = addedStart;
            AddedEnd = addedEnd;
            Removed = removed;
            RemovedStart = removedStart;
            RemovedEnd = removedEnd;
            Before = before;
            After = after;
        }

        public UndoRecord(EditorState baseState)
        {
            Before = baseState;
        }

        public string Added { get; set; }
        public Coordinates AddedStart { get; set; }
        public Coordinates AddedEnd { get; set; }

        public string Removed { get; set; }
        public Coordinates RemovedStart;
        public Coordinates RemovedEnd;

        public EditorState Before;
        public EditorState After;

        public void Undo(CodeEditor editor)
        {
            if (!String.IsNullOrEmpty(Added))
            {
                editor.DeleteRange(AddedStart, AddedEnd);
                editor.Colorize(AddedStart.mLine - 1, AddedEnd.mLine + 2);
            }

            if (!String.IsNullOrEmpty(Removed))
            {
                var start = RemovedStart;
                editor.InsertTextAt(start, Removed);
                editor.Colorize(RemovedStart.mLine - 1, RemovedEnd.mLine - RemovedStart.mLine + 2);
            }

            editor.State = Before;

        }

        public void Redo(CodeEditor editor)
        {
            if (!String.IsNullOrEmpty(Removed))
            {
                editor.DeleteRange(RemovedStart, RemovedEnd);
                editor.Colorize(RemovedStart.mLine - 1, RemovedEnd.mLine - RemovedStart.mLine + 1);
            }

            if (!String.IsNullOrEmpty(Added))
            {
                var start = AddedStart;
                editor.InsertTextAt(start, Added);
                editor.Colorize(AddedStart.mLine - 1, AddedEnd.mLine - AddedStart.mLine + 1);
            }

            editor.State = After;
        }
    };

}
