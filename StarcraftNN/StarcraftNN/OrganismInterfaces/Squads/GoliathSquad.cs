using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;

namespace StarcraftNN.OrganismInterfaces.Squads
{
    public class GoliathSquad : SquadInterface
    {
        public GoliathSquad() { }
        public GoliathSquad(List<Unit> allies, List<Unit> enemies) : base(allies, enemies) { }
    }
}
