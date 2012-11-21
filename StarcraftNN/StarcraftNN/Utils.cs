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
                bwapi.UnitTypes_Terran_Vulture.getID(),
                bwapi.UnitTypes_Protoss_Zealot.getID(),
                bwapi.UnitTypes_Zerg_Zergling.getID()
            );
        }

        public static bool isLongRange(Unit unit)
        {
            return unit.getType().getID().In(
                bwapi.UnitTypes_Terran_Marine.getID(),
                bwapi.UnitTypes_Terran_Ghost.getID(),
                bwapi.UnitTypes_Terran_Wraith.getID()
            );
        }
    }
}
