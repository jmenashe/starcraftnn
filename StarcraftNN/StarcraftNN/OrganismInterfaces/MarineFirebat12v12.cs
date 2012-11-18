using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using SharpNeat.Genomes.Neat;
using SharpNeat.Decoders.Neat;

namespace StarcraftNN.OrganismInterfaces
{
    public class MarineFirebat12v12 : IOrganismInterface
    {
        private List<Unit> _allies, _enemies;
        private List<Action> _lastAction;
        private static int FirebatCount = 6;
        private static int MarineCount = 6;
        private static int UnitCount = FirebatCount + MarineCount;
        private static double MaxDistance = 750;

        public string SaveFile
        {
            get
            {
                return "marineFirebat12v12";
            }
        }

        private struct ScoredUnit
        {
            public Unit unit;
            public double score;
            public int index;
        }

        private enum ActionTypes
        {
            Attack,
            Move,
            None
        }

        private class Action
        {
            public ActionTypes Type { get; set; }
            public int Arg1 { get; set; }
            public int Arg2 { get; set; }
        }

        public MarineFirebat12v12()
        {
            _lastAction = new List<Action>();
            for (int i = 0; i < UnitCount; i++)
                _lastAction.Add(new Action { Type = ActionTypes.None });
        }

        public NeatGenomeFactory CreateGenomeFactory()
        {
            NeatGenomeFactory factory = new NeatGenomeFactory(UnitCount * (UnitCount * 2 + 1), UnitCount * (UnitCount + 3));
            return factory;
        }

        public void InputActivate(NeatGenome genome)
        {
            int sensor = 0;
            var allyPosition = Utils.getCentroid(_allies);
            NeatGenomeDecoder decoder = new NeatGenomeDecoder(SharpNeat.Decoders.NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(1));
            var blackbox = decoder.Decode(genome);
            foreach (var enemy in _enemies) 
            {
                foreach (var ally in _allies)
                {
                    Position difference = enemy.getPosition().opSubtract(ally.getPosition());
                    double distance = ally.getPosition().getDistance(enemy.getPosition());
                    distance /= MaxDistance;
                    double angle = Math.Atan2(difference.yConst(), difference.xConst());
                    if (angle < 0) angle += 2 * Math.PI;
                    angle /= 2 * Math.PI;
                    blackbox.InputSignalArray[sensor++] = distance;
                    blackbox.InputSignalArray[sensor++] = angle;
                }
                blackbox.InputSignalArray[sensor++] = (double)enemy.getHitPoints() / enemy.getType().maxHitPoints();
            }
            blackbox.Activate();
            for (int i = 0; i < UnitCount; i++)
            {
                Unit ally = _allies[i];
                if (!ally.exists()) continue;
                List<ScoredUnit> scoredUnits = new List<ScoredUnit>();
                for (int j = 0; j < UnitCount; j++)
                {
                    ScoredUnit su = new ScoredUnit();
                    su.score = blackbox.OutputSignalArray[i * UnitCount + j];
                    su.unit = _enemies[j];
                    su.index = j;
                    scoredUnits.Add(su);
                }
                double moveScore = blackbox.OutputSignalArray[i * UnitCount + UnitCount];
                int moveX = (int)blackbox.OutputSignalArray[i * UnitCount + UnitCount + 1] * 1000 - 500;
                int moveY = (int)blackbox.OutputSignalArray[i * UnitCount + UnitCount + 2] * 1000 - 500;
                scoredUnits.Sort((x, y) => { return y.score.CompareTo(x.score); });
                if (moveScore >= scoredUnits[0].score)
                {
                    if (_lastAction[i].Type != ActionTypes.Move || !ally.isMoving())
                    {
                        _lastAction[i].Type = ActionTypes.Move;
                        Position target = new Position(ally.getPosition().xConst() + moveX, ally.getPosition().yConst() + moveY);
                        ally.move(target);
                        _lastAction[i].Type = ActionTypes.Move;
                        _lastAction[i].Arg1 = moveX;
                        _lastAction[i].Arg2 = moveY;
                        continue;
                    }
                }
                foreach (var su in scoredUnits)
                {
                    Unit enemy = su.unit;
                    if (enemy.exists())
                    {
                        if (_lastAction[i].Type != ActionTypes.Attack || _lastAction[i].Arg1 != su.index)
                        {
                            ally.attack(enemy);
                            _lastAction[i].Type = ActionTypes.Attack;
                            _lastAction[i].Arg1 = su.index;
                        }
                        break;
                    }
                }
            }
        }

        public void UpdateState()
        {
            _allies = Utils.getAllies().OrderBy(x => x.getType().getID()).ToList();
            _enemies = Utils.getEnemies().OrderBy(x => x.getType().getID()).ToList();
            foreach (var a in _lastAction)
                a.Type = ActionTypes.None;
        }
    }
}
