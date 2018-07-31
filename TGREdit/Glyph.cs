using System;

namespace TGREdit
{
    public class Glyph
    {
        public char character;
        public PaletteIndex ColorIndex;
        public bool MultiLinecomment;

        public Glyph(Char c, PaletteIndex colorIndex = PaletteIndex.Default, bool multiLineComment =false)
        {
            character = c;
            MultiLinecomment = multiLineComment;
            ColorIndex = colorIndex;
        }
    }
}