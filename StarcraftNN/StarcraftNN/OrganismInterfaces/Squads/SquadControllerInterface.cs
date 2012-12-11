using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using SharpNeat.Genomes.Neat;
using SharpNeat.Decoders.Neat;
using SharpNeat.Phenomes;
using StarcraftNN;
using System.Diagnostics;
using BwapiPosition = SWIG.BWAPI.Position;
using System.Threading.Tasks;

namespace StarcraftNN.OrganismInterfaces
{
    public abstract class SquadControllerInterface : IOrganismInterface
    {
        protected List<Unit> _allies, _enemies;
        protected List<ISquad> _squads;
        protected List<UnitGroup> _enemyGroups;
        protected Dictionary<Unit, Action> _lastAction;
        protected KMeans _kmeans = new KMeans();
        protected PolarBinManager _polarbins;

        protected abstract int SquadCount { get; }
        protected List<double> DistanceRanges = new List<double> { 0, 100, 300, 1000, double.PositiveInfinity };
        protected int ThetaBins = 12;

        protected int DistanceBins
        {
            get
            {
                return DistanceRanges.Count - 1;
            }
        }

        public string SaveFile
        {
            get { return this.GetType().Name; }
        }

        protected enum ActionType
        {
            AttackAir,
            AttackShort,
            AttackLong,
            AttackAirBonus,
            Move,
            None
        }

        protected struct ScoredBin
        {
            public int bin;
            public double score;
        }

        protected class Action
        {
            public ActionType Type { get; set; }
            public Unit Target { get; set; }
        }

        public NeatGenomeDecoder Decoder
        {
            get;
            protected set;
        }

        public SquadControllerInterface()
        {
            _lastAction = new Dictionary<Unit, Action>();
            this.Decoder = new NeatGenomeDecoder(SharpNeat.Decoders.NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(10, true));
        }

        public NeatGenomeFactory CreateGenomeFactory()
        {
            NeatGenomeParameters param = new NeatGenomeParameters();
            param.AddConnectionMutationProbability = 0.1;
            param.AddNodeMutationProbability = 0.1;
            param.ConnectionWeightMutationProbability = 0.8;
            NeatGenomeFactory factory = new NeatGenomeFactory(this.SquadCount * 2, this.SquadCount * 2, param);
            return factory;
        }

        protected void Input(IBlackBox blackbox)
        {
            int sensor = 0;
            for (int i = 0; i < this.SquadCount; i++ )
            {
                ISquad squad = _squads[i];
                int gi = Math.Min(_enemyGroups.Count - 1, i);
                UnitGroup enemyGroup = _enemyGroups[gi];
                blackbox.InputSignalArray[sensor++] = (double)squad.HitPoints / squad.MaxHitPoints;
                blackbox.InputSignalArray[sensor++] = (double)enemyGroup.HitPoints / enemyGroup.MaxHitPoints;
            }
        }

        protected void Activate(IBlackBox blackbox)
        {
            blackbox.Activate();
            int signal = 0;
            for (int i = 0; i < this.SquadCount; i++)
            {
                double moveScore = blackbox.OutputSignalArray[signal++];
                double delegateScore = blackbox.OutputSignalArray[signal++];
                if (delegateScore > 0.5)
                    _squads[i].Delegate();
                else
                    _squads[i].Move(moveScore * 2 * Math.PI);
            }
        }

        protected virtual void groupEnemies()
        {
            _enemyGroups = _kmeans.ComputeClusters(_polarbins.EnemyPositions, _enemies, this.SquadCount);
        }

        protected abstract void formSquads(List<UnitGroup> enemyGroups);

        public virtual void InputActivate(NeatGenome genome)
        {
            var blackbox = this.Decoder.Decode(genome);
            this.Input(blackbox);
            this.Activate(blackbox);
        }

        public virtual void UpdateState(IEnumerable<Unit> allies, IEnumerable<Unit> enemies)
        {
            _allies = allies.OrderBy(x => x.getType().getID()).ToList();
            _enemies = enemies.OrderBy(x => x.getType().getID()).ToList();
            _polarbins = new PolarBinManager(_allies, _enemies, DistanceRanges, ThetaBins);
            groupEnemies();
            formSquads(_enemyGroups);
        }

        public void UpdateState()
        {
            this.UpdateState(Utils.getAllies(), Utils.getEnemies());
        }

        public double ComputeFitness(int frameCount)
        {
            double minimum = -(_enemies.Count * _enemies.Count);
            int allyScore = _allies.Count(x => x.exists());
            int enemyScore = _enemies.Count(x => x.exists());
            if (allyScore == enemyScore) // This can happen if the squads all just run away
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
            Debug.Assert(!double.IsNaN(score));
            return score;
        }
    }
}
