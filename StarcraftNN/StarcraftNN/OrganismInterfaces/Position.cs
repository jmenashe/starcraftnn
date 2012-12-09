using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarcraftNN.OrganismInterfaces
{
    public class Position
    {
        public Position(int x, int y) { this.x = x; this.y = y; }
        public int x;
        public int y;
        public double distanceTo(Position other)
        {
            return Math.Sqrt(Math.Pow(this.x - other.x, 2) + Math.Pow(this.y - other.y, 2));
        }
        public static Position operator -(Position left, Position right)
        {
            Position difference = new Position(left.x - right.x, left.y - right.y);
            return difference;
        }

        public static implicit operator Position(SWIG.BWAPI.Position p)
        {
            return new Position(p.xConst(), p.yConst());
        }
    }
}
