using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;

namespace StarcraftNN.OrganismInterfaces.Squads
{
    public class MarineFirebatGoliathWraithSquadController : SquadControllerInterface
    {
        protected override int SquadCount
        {
            get { return 6; }
        }

        protected override void formSquads(List<UnitGroup> enemyGroups)
        {
            var marines = _allies.Where(x => x.getType().getID() == bwapi.UnitTypes_Terran_Marine.getID()).ToList();
            var firebats = _allies.Where(x => x.getType().getID() == bwapi.UnitTypes_Terran_Firebat.getID()).ToList();
            var goliaths = _allies.Where(x => x.getType().getID() == bwapi.UnitTypes_Terran_Goliath.getID()).ToList();
            var wraiths = _allies.Where(x => x.getType().getID() == bwapi.UnitTypes_Terran_Wraith.getID()).ToList();
            _squads = new List<ISquad>();
            for (int i = 0; i < 4; i++)
            {
                var group = new UnitGroup();
                group.Add(marines[2 * i]);
                group.Add(marines[2 * i + 1]);
                group.Add(firebats[i]);
                var squad = new Marine2Firebat1Squad(group, enemyGroups[i]);
                _squads.Add(squad);
            }
            for (int i = 0; i < 2; i++)
            {
                var group = new UnitGroup();
                group.Add(goliaths[2 * i]);
                group.Add(goliaths[2 * i + 1]);
                group.Add(wraiths[2 * i]);
                group.Add(wraiths[2 * i + 1]);
                var squad = new Goliath2Wraith2Squad(group, enemyGroups[4 + i]);
                _squads.Add(squad);
            }
        }
    }
}
