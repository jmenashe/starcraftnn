using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;

namespace StarcraftNN
{
    static class Utils
    {
        public static List<Unit> getAllies()
        {
            return bwapi.Broodwar
                .self().getUnits()
                .Where(x => x.getType().canMove())
                .Where(x => x.getType().getID() != RoundManager.ResetUnitTypeID)
                .ToList();
        }
        public static List<Unit> getEnemies()
        {
            return bwapi.Broodwar
                .enemies().SelectMany(x => x.getUnits())
                .Where(x => x.getType().canMove())
                .ToList();
        }

        public static Position getCentroid(IEnumerable<Unit> units)
        {
            int avgX = (int)units.Average(x => x.getPosition().xConst());
            int avgY = (int)units.Average(x => x.getPosition().yConst());
            return new Position(avgX, avgY);
        }

        public static bool isShortRange(Unit unit)
        {
            return unit.getType().getID().In(
                bwapi.UnitTypes_Terran_SCV.getID(),
                bwapi.UnitTypes_Terran_Firebat.getID(),
                bwapi.UnitTypes_Protoss_Zealot.getID(),
                bwapi.UnitTypes_Zerg_Zergling.getID()
            );
        }

        public static bool isLongRange(Unit unit)
        {
            return unit.getType().getID().In(
                bwapi.UnitTypes_Terran_Marine.getID(),
                bwapi.UnitTypes_Terran_Ghost.getID(),
                bwapi.UnitTypes_Terran_Wraith.getID(),
                bwapi.UnitTypes_Terran_Goliath.getID()
            );
        }

        public static bool isAir(Unit unit)
        {
            return unit.getType().getID().In(
                bwapi.UnitTypes_Terran_Battlecruiser.getID(),
                bwapi.UnitTypes_Terran_Wraith.getID(),
                bwapi.UnitTypes_Terran_Valkyrie.getID()
            );
        }

        public static bool hasAttackAirBonus(Unit unit)
        {
            return unit.getType().getID().In(
                bwapi.UnitTypes_Terran_Valkyrie.getID(),
                bwapi.UnitTypes_Terran_Goliath.getID()
            );
        }

        public static bool isMachine(Unit unit)
        {
            return unit.getType().getID().In(
                bwapi.UnitTypes_Terran_Battlecruiser.getID(),
                bwapi.UnitTypes_Terran_Wraith.getID(),
                bwapi.UnitTypes_Terran_Valkyrie.getID(),
                bwapi.UnitTypes_Terran_Siege_Tank_Siege_Mode.getID(),
                bwapi.UnitTypes_Terran_Siege_Tank_Tank_Mode.getID(),
                bwapi.UnitTypes_Terran_SCV.getID(),
                bwapi.UnitTypes_Terran_Goliath.getID()
            );
        }
    }
}
