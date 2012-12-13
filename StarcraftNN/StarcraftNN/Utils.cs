using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using System.Diagnostics;
using System.IO;
using SharpNeat.Genomes.Neat;
using System.Xml;

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
            var e = bwapi.Broodwar
                .enemies().SelectMany(x => x.getUnits())
                .Where(x => x.getType().canMove())
                .ToList();
            return e;
        }

        public static Position getCentroid(IEnumerable<Unit> units)
        {
            int avgX = (int)units.Where(x => x.exists()).Average(x => x.getPosition().xConst());
            int avgY = (int)units.Where(x => x.exists()).Average(x => x.getPosition().yConst());
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

        public static double computeStandardFitness(List<Unit> allies, List<Unit> enemies, int frameCount)
        {
            double minimum = -(enemies.Count * enemies.Count);
            int allyScore = allies.Count(x => x.exists());
            int enemyScore = enemies.Count(x => x.exists());
            if (enemyScore == 0 && allyScore == 0)
                return 0;
            double score = allyScore - enemyScore;
            score *= Math.Abs(score);
            if (frameCount == 0)
                score = 0;
            else
            {
                score -= minimum;
                score /= Math.Abs(minimum);
            }
            if (double.IsNaN(score) || double.IsInfinity(score) || score < 0)
            {
                Console.WriteLine("Warning: score of {0} from {1} allies, {2} enemies", score, allies.Count, enemies.Count);
                score = 0;
            }
            return score;
        }

        public static NeatGenome loadBestGenome(string name, NeatGenomeFactory factory)
        {
            string directory = Path.Combine(Environment.GetEnvironmentVariable("STARCRAFT_RESULTS"), Environment.MachineName);
            string path = Path.Combine(directory, name + ".xml");
            if (!File.Exists(path))
                return null;
            XmlDocument document = new XmlDocument();
            document.Load(path);
            List<NeatGenome> genomes = NeatGenomeXmlIO.LoadCompleteGenomeList(document, true, factory);
            var genome = genomes.OrderByDescending(x => x.EvaluationInfo.Fitness).First();
            Console.WriteLine("Loaded best genome for {0}", name);
            return genome;
        }
    }
}
