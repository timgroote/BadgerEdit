using System;

namespace BadgerEdit
{
    public class IntVector
    {
        public int X { get; set; }
        public int Y { get; set; }

        public IntVector(int x, int y)
        {
            X = x;
            Y = y;
        }
        public IntVector()
        {
            X = 0;
            Y = 0;
        }

        public static bool operator ==(IntVector a, IntVector b)
        {
            return a?.X == b?.X && a?.Y == b?.Y;
        }

        public static bool operator !=(IntVector a, IntVector b)
        {
            return !(a == b);
        }

        public static IntVector operator +(IntVector a, IntVector b)
        {
            return new IntVector(a.X + b.X, a.Y + b.Y);
        }
        public static IntVector operator *(IntVector a, IntVector b)
        {
            return new IntVector(a.X * b.X, a.Y * b.Y);
        }
        public static IntVector operator -(IntVector a, IntVector b)
        {
            return new IntVector(a.X - b.X, a.Y - b.Y);
        }
        public static IntVector operator /(IntVector a, IntVector b)
        {
            return new IntVector(a.X / b.X, a.Y / b.Y);
        }

        public string AsString()
        {
            return $"x:{X}-y:{Y}";
        }
    }

    public static class IntVectorUtils
    {
        public static IntVector ClampPositive(this IntVector intVector)
        {
            if (intVector.X > 0 && intVector.Y > 0)
                return intVector;

            return new IntVector(Math.Max(0, intVector.X), Math.Max(0, intVector.Y));
        }

        public static IntVector ClampValue(this IntVector intVector, IntVector other)
        {
            return intVector.ClampX(other.X).ClampY(other.Y);
        }

        public static IntVector ClampX(this IntVector intVector, int xMax)
        {
            return new IntVector(Math.Min(intVector.X, xMax), intVector.Y);
        }
        public static IntVector ClampY(this IntVector intVector, int yMax)
        {
            return new IntVector(intVector.Y, Math.Min(intVector.Y, yMax));
        }
    }
}