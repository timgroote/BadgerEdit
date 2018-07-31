using System;
using System.Collections.Generic;
using System.Linq;

namespace BadgerEdit
{
    public class SelectionRange
    {
        private IntVector[] extents = new IntVector[2];
        
        public IntVector OtherEnd
        {
            get => extents[1];
            set => extents[1] = value;
        }

        public IntVector OneEnd
        {
            get => extents[0];
            set => extents[0] = value;
        }

        public IntVector Start
        {
            get { return extents.OrderBy(itm => itm.Y).ThenBy(itm => itm.X).FirstOrDefault(); }
        }

        public IntVector End
        {
            get { return extents.OrderByDescending(itm => itm.Y).ThenByDescending(itm => itm.X).FirstOrDefault(); }
        }

        public bool Complete => OtherEnd != null && OneEnd != null;

        public SelectionRange()
        {
            extents = new IntVector[2];
        }

        public SelectionRange(IntVector a, IntVector b)
        {
            extents = new[]{a,b};
        }
        
        public string GetText(ref List<Line> lines)
        {
            if (Start == null || End == null)
                return String.Empty;

            return Start.Y == End.Y ? 
                lines[Start.Y].GetText(Start.X, End.X) : 
                String.Join("",GetGlyphs(ref lines).Select(g => g.Character));
        }

        public List<Glyph> GetGlyphs(ref List<Line> lines)
        {
            List<Glyph> buffer = new List<Glyph>();

            for (int ln = Start.Y; ln < End.Y; ln++)
            {
                bool firstLn = ln == Start.Y;
                bool lastLn = ln == End.Y;

                if (firstLn)
                {
                    buffer.AddRange(lines[ln].Skip(Start.X));
                    buffer.Add(new Glyph('\n'));
                }
                else if (lastLn)
                {
                    buffer.AddRange(lines[ln].Take(End.X));
                    buffer.Add(new Glyph('\n'));
                }
                else
                {
                    buffer.AddRange(lines[ln]);
                    buffer.Add(new Glyph('\n'));
                }

            }
            return buffer;
        }

        public bool ContainsCoordinates(int x, int y)
        {
            if (Start == null || End == null)
                return false;

            if (y == Start.Y && Start.Y == End.Y)
                return x >= Start.X && x <= End.X;

            if (Start.Y == y)
                return x >= Start.X;

            if (End.Y == y)
                return x <= End.X;
            
            return y >= Start.Y && y <= End.Y;
        }

        public string AsString()
        {
            return $"START {Start?.AsString() ?? "NULL"} - END {End?.AsString() ?? "NULL"} ";
        }
    }
}
