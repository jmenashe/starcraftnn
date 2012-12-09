using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarcraftNN.OrganismInterfaces.Squads
{
    public class Goliath2Wraith2Squad : SquadInterface
    {
        public Goliath2Wraith2Squad() { }
        public Goliath2Wraith2Squad(List<SWIG.BWAPI.Unit> allies, List<SWIG.BWAPI.Unit> enemies) : base(allies, enemies) { }
    }
}
