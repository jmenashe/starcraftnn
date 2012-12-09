using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarcraftNN.OrganismInterfaces.Squads
{
    public class Marine2Firebat1Squad : SquadInterface
    {
        public Marine2Firebat1Squad() { }
        public Marine2Firebat1Squad(List<SWIG.BWAPI.Unit> allies, List<SWIG.BWAPI.Unit> enemies) : base(allies, enemies) { }
    }
}
