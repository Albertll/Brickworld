using System;

namespace Assets.Scripts
{
    [Serializable]
    public class Brick
    {
        public Brick(int z, int type)
        {
            Z = z;
            Type = type;
        }
        public int Z { get; }

        public int Type { get; }
    }
}