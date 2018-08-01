using System;
using System.Collections.Generic;
using System.Linq;

namespace BadgerEdit
{
    public class Line : List<Glyph>
    {
        public Line():base()
        {
            
        }

        public Line(string text):base()
        {
            AddRange(text.Select(ch => new Glyph(ch)));
        }

        public override string ToString()
        {
            return String.Join("", this.Select(n => n.Character));
        }

        public String GetText(int from = -1, int until = 0)
        {
            if (from < 0 && until == 0)
                return this.ToString();

            if (until == 0)
            {
                return ToString().Substring(from);
            }

            return ToString().Substring(from, from + until);
        }

        public void InsertGlyphs(int idx, params Glyph[] glyphs)
        {
            int localIndex = idx;
            foreach (Glyph t in glyphs)
            {
                Insert(localIndex, t);
                localIndex++;
            }
        }

        public void RemoveGlyphs(int idx, int count)
        {
            for (int k = count; k > 0; k--)
            {
                RemoveAt(Math.Max(0,Math.Min(Count, idx+k)-1));
            }
        }

        public Line Concatenate(Line otherLine)
        {
            AddRange(otherLine);
            return this;
        }

        public Glyph End => this.LastOrDefault();
        public Glyph Start => this.FirstOrDefault();
    }
}