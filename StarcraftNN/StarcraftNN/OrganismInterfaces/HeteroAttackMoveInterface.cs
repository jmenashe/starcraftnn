using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using SharpNeat.Genomes.Neat;
using SharpNeat.Decoders.Neat;
using SharpNeat.Phenomes;
using System.Diagnostics;

namespace StarcraftNN.OrganismInterfaces
{
    public abstract class HeteroAttackMoveInterface : ISquad
    {
        private List<Unit> _allies, _enemies;
        private List<Action> _lastAction;
        private static double MaxDistance = 750;

        public abstract int UnitCount { get; }

        public string SaveFile
        {
            get { return this.GetType().Name; }
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
            public bool Arg2 { get; set; }
        }

        public NeatGenomeDecoder Decoder
        {
            get;
            protected set;
        }

        public HeteroAttackMoveInterface()
        {
            _lastAction = new List<Action>();
            for (int i = 0; i < UnitCount; i++)
                _lastAction.Add(new Action { Type = ActionTypes.None });
            this.Decoder = new NeatGenomeDecoder(SharpNeat.Decoders.NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(10));
        }

        public NeatGenomeFactory CreateGenomeFactory()
        {
            NeatGenomeParameters param = new NeatGenomeParameters();
            param.AddConnectionMutationProbability = 0.1;
            param.AddNodeMutationProbability = 0.1;
            param.ConnectionWeightMutationProbability = 0.8;
            NeatGenomeFactory factory = new NeatGenomeFactory(UnitCount * (UnitCount * 2 + 2), UnitCount * (UnitCount + 4), param);
            return factory;
        }

        protected IBlackBox Input(NeatGenome genome)
        {
            int sensor = 0;
            var allyPosition = Utils.getCentroid(_allies);
            var blackbox = this.Decoder.Decode(genome);
            foreach (var ally in _allies)
            {
                double health;
                if (ally.exists())
                    health = (double)ally.getHitPoints() / ally.getType().maxHitPoints();
                else health = 0;
                blackbox.InputSignalArray[sensor++] = health;
            }
            foreach (var enemy in _enemies)
            {
                double health;
                if (enemy.exists())
                    health = (double)enemy.getHitPoints() / enemy.getType().maxHitPoints();
                else health = 0;
                blackbox.InputSignalArray[sensor++] = health;
            }
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
            }
            return blackbox;
        }

        protected void Activate(IBlackBox blackbox)
        {
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
                double moveDistance = blackbox.OutputSignalArray[i * UnitCount + UnitCount + 1] * 100;
                int moveTheta = (int)(blackbox.OutputSignalArray[i * UnitCount + UnitCount + 2] * Math.PI);
                bool moveFlip = blackbox.OutputSignalArray[i * UnitCount + UnitCount + 3] > 0.5;
                scoredUnits.Sort((x, y) => { return y.score.CompareTo(x.score); });
                if (moveScore >= scoredUnits[0].score)
                {
                    if (_lastAction[i].Type != ActionTypes.Move || !ally.isMoving())
                    {
                        _lastAction[i].Type = ActionTypes.Move;
                        if (moveFlip)
                            moveTheta = -moveTheta;
                        int dx = (int)(moveDistance * Math.Cos(moveTheta));
                        int dy = (int)(moveDistance * Math.Sin(moveTheta));
                        Position target = new Position(ally.getPosition().xConst() + dx, ally.getPosition().yConst() + dy);
                        ally.move(target);
                        _lastAction[i].Type = ActionTypes.Move;
                        _lastAction[i].Arg1 = moveTheta;
                        _lastAction[i].Arg2 = moveFlip;
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

        public void InputActivate(NeatGenome genome)
        {
            var blackbox = this.Input(genome);
            this.Activate(blackbox);
        }

        public void UpdateState(IEnumerable<Unit> allies, IEnumerable<Unit> enemies)
        {
            _allies = allies.OrderBy(x => x.getType().getID()).ToList();
            _enemies = enemies.OrderBy(x => x.getType().getID()).ToList();
            foreach (var a in _lastAction)
                a.Type = ActionTypes.None;
        }

        public void UpdateState()
        {
            this.UpdateState(Utils.getAllies(), Utils.getEnemies());
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
