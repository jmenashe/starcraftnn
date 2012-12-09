using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarcraftNN.OrganismInterfaces
{
    public class Squads12v12 : SquadControlInterface
    {
        public override int UnitCount
        {
            get { return 12; }
        }
    }
}
