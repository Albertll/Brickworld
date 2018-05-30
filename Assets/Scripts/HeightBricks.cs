using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts
{
    [Serializable]
    public class HeightBricks
    {
        public int X;
        public int Y;

        public IList<Brick> Bricks = new List<Brick>();

        public HeightBricks(int height = 0)
        {
            Bricks.Add(new Brick(height, 0));
        }

        public IEnumerable<Brick> SortedBricks
        {
            get { return Bricks.OrderBy(b => b.Z); }
        }

        public int Height => SortedBricks.First().Z;
    }
}