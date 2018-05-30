using System;

namespace Assets.Scripts
{
    [Serializable]
    public struct Point
    {
        public Point(int x, int y)
        {
            X = x;
            Y = y;
            Z = 0;
        }

        public Point(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int X;
        public int Y;
        public int Z;
    }
}