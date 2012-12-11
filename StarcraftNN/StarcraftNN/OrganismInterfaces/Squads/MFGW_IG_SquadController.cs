using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;

namespace StarcraftNN.OrganismInterfaces.Squads
{
    public class MFGW_IG_SquadController : SquadControllerInterface
    {
        int _frames;
        protected override int SquadCount
        {
            get { return 6; }
        }

        protected bool groupsEmpty()
        {
            foreach (var g in _enemyGroups)
            {
                bool empty = true;
                foreach (var u in g)
                    if (u.exists())
                    {
                        empty = false;
                        break;
                    }
                if (empty) return true;
            }
            return false;
        }

        protected override void groupEnemies()
        {
            _enemyGroups = new List<UnitGroup>();
            var longrange = _enemies.Where(x => Utils.isLongRange(x)).ToList();
            var shortrange = _enemies.Where(x => Utils.isShortRange(x)).ToList();
            var airbonus = _enemies.Where(x => Utils.hasAttackAirBonus(x)).ToList();
            var flyers = _enemies.Where(x => Utils.isAir(x)).ToList();
            if (shortrange.Count > 0)
            {
                var shortgroups = _kmeans.ComputeClusters(shortrange, 2);
                if (shortgroups.Count > 0)
                    _enemyGroups.AddRange(shortgroups);
            }
            if (_enemyGroups.Count < 4)
            {
                var longgroups = _kmeans.ComputeClusters(longrange, 2);
                _enemyGroups.AddRange(longgroups);
            }
            if (flyers.Count > 0)
            {
                var airgroups = _kmeans.ComputeClusters(flyers, 2);
                if (airgroups.Count > 0)
                    _enemyGroups.AddRange(airgroups);
            }
            if (_enemyGroups.Count < this.SquadCount && airbonus.Count > 0)
            {
                var abgroups = _kmeans.ComputeClusters(airbonus, 2);
                if (abgroups.Count > 0)
                    _enemyGroups.AddRange(abgroups);
            }
        }

        public override void UpdateState(IEnumerable<Unit> allies, IEnumerable<Unit> enemies)
        {
            _frames = 0;
            base.UpdateState(allies, enemies);
        }

        public override void InputActivate(SharpNeat.Genomes.Neat.NeatGenome genome)
        {
            _frames++;
            if (_frames % 100 == 0 || groupsEmpty()) // regroup every 100 frames or when groups are empty
                groupEnemies();
            base.InputActivate(genome);
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
