using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using BwapiPosition = SWIG.BWAPI.Position;
using SharpNeat.Genomes.Neat;
using SharpNeat.Decoders.Neat;

namespace StarcraftNN.OrganismInterfaces
{
    public class BasicInterface : IOrganismInterface
    {
        private List<Unit> _allies, _enemies, _lastAttack;
        private static int UnitCount = 3;

        public string SaveFile
        {
            get
            {
                return "basic";
            }
        }

        private struct ScoredUnit
        {
            public Unit unit;
            public double score;
        }

        public BasicInterface()
        {
            _lastAttack = new List<Unit>();
            for (int i = 0; i < BasicInterface.UnitCount; i++)
                _lastAttack.Add(null);
        }

        public NeatGenomeFactory CreateGenomeFactory()
        {
            NeatGenomeFactory factory = new NeatGenomeFactory(UnitCount * 2, UnitCount * 3);
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
                BwapiPosition difference = enemy.getPosition().opSubtract(allyPosition);
                double distance = allyPosition.getDistance(enemy.getPosition());
                double angle = Math.Tan(difference.yConst() / difference.xConst());
                blackbox.InputSignalArray[sensor++] = distance;
                blackbox.InputSignalArray[sensor++] = angle;
            }
            blackbox.Activate();
            for (int i = 0; i < BasicInterface.UnitCount; i++)
            {
                Unit ally = _allies[i];
                List<ScoredUnit> scoredUnits = new List<ScoredUnit>();
                for (int j = 0; j < BasicInterface.UnitCount; j++)
                {
                    ScoredUnit su = new ScoredUnit();
                    su.score = blackbox.OutputSignalArray[i * BasicInterface.UnitCount + j];
                    su.unit = _enemies[j];
                    scoredUnits.Add(su);
                }
                scoredUnits.Sort((x, y) => { return y.score.CompareTo(x.score); });
                if (ally.exists())
                {
                    foreach (var enemy in _enemies)
                    {
                        if (enemy.exists())
                        {
                            if (_lastAttack[i] != enemy)
                            {
                                _lastAttack[i] = enemy;
                                ally.attack(enemy);
                            }
                            break;
                        }
                    }
                }
            }
        }

        public void UpdateState()
        {
            _allies = Utils.getAllies();
            _enemies = Utils.getEnemies();
        }

        public double ComputeFitness(int frameCount)
        {
            double maxScale = 4;
            double minimum = -(UnitCount * UnitCount) * maxScale;
            double score = Utils.getAllies().Count - Utils.getEnemies().Count;
            score *= Math.Abs(score);
            score *= 200.0 / frameCount;
            score -= minimum;
            return score;
        }
    }
}
