namespace TGREdit
{
    public struct Coordinates
    {
        public int mLine;

        public int mColumn;

        public Coordinates(int line = 0, int col = 0)
        {
            mLine = line;
            mColumn = col;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Coordinates))
                return false;

            Coordinates castObj = (Coordinates) obj;

            return castObj.mLine == mLine &&
                   castObj.mColumn == mColumn;
        }

        public static bool operator ==(Coordinates ca, Coordinates cb)
        {
            return ca.Equals(cb);
        }

        public static bool operator !=(Coordinates ca, Coordinates cb)
        {
            return !ca.Equals(cb);
        }

        public static bool operator <(Coordinates ca, Coordinates cb)
        {
            if (ca.mLine != cb.mLine)
                return ca.mLine < cb.mLine;
            return ca.mColumn < cb.mColumn;
        }

        public static bool operator >(Coordinates ca, Coordinates cb)
        {
            if (ca.mLine != cb.mLine)
                return ca.mLine > cb.mLine;
            return ca.mColumn > cb.mColumn;
        }

        public static bool operator <=(Coordinates ca, Coordinates cb)
        {
            if (ca.mLine != cb.mLine)
                return ca.mLine <= cb.mLine;
            return ca.mColumn <= cb.mColumn;
        }

        public static bool operator >=(Coordinates ca, Coordinates cb)
        {
            if (ca.mLine != cb.mLine)
                return ca.mLine >= cb.mLine;
            return ca.mColumn >= cb.mColumn;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}