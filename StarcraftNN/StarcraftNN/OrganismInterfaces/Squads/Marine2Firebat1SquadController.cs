using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarcraftNN.OrganismInterfaces.Squads
{
    public class Marine2Firebat1SquadController : SquadControllerInterface
    {
        protected override int SquadCount
        {
            get { return 1; }
        }

        protected override void formSquads(List<UnitGroup> enemyGroups)
        {
            _squads = new List<ISquad>();
            Marine2Firebat1Squad squad = new Marine2Firebat1Squad(_allies, enemyGroups[0]);
            _squads.Add(squad);
        }
    }
}
