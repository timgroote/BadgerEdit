using System.Collections.Generic;
using System.Linq;

namespace TGREdit
{
    public class Line : List<Glyph>
    {
        public bool Empty => Count == 0;
        public Glyph End => this.Last();
        public Glyph Begin => this.First();

        public Dictionary<int, string> ErrorMarkers { get; set; }

        public Line()
        {
            
        }
        public Line(string content)
        {
            AddRange(content.Select(ct => new Glyph(ct, PaletteIndex.Default)));
        }

        //todo : this is a bad idea.
        public void Erase(int p0, Glyph end)
        {
            RemoveRange(p0, IndexOf(end));
        }
        public void Erase(int p0, int p1)
        {
            RemoveRange(p0, p1);
        }
    }
}