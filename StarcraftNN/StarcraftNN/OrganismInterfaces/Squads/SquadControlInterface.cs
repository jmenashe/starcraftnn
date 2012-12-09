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
    public abstract class SquadControlInterface : IOrganismInterface
    {
        protected List<Unit> _allies, _enemies;
        protected List<ISquad> _squads;
        protected List<UnitGroup> _enemyGroups;
        protected Dictionary<Unit, Action> _lastAction;
        private KMeans _kmeans = new KMeans();
        PolarBinManager _polarbins;

        protected abstract int SquadSize { get; }
        protected abstract int SquadCount { get; }
        protected abstract List<double> DistanceRanges { get; }
        protected abstract int ThetaBins { get; }

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

        public SquadControlInterface()
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
            NeatGenomeFactory factory = new NeatGenomeFactory(this.SquadCount * 5 + DistanceBins * ThetaBins * 5, this.SquadCount * DistanceBins * ThetaBins, param);
            return factory;
        }

        protected void Input(IBlackBox blackbox)
        {
            int sensor = 0;
            foreach (var squad in _squads)
            {
                blackbox.InputSignalArray[sensor++] = (double)squad.HitPoints / squad.MaxHitPoints;
            }
        }

        protected void Activate(IBlackBox blackbox)
        {
            blackbox.Activate();
        }

        protected void groupEnemies()
        {
            _enemyGroups = _kmeans.ComputeClusters(_polarbins.EnemyPositions, _enemies, this.SquadCount);
        }

        protected abstract void formSquads();

        public void InputActivate(NeatGenome genome)
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
            formSquads();
        }

        public void UpdateState()
        {
            this.UpdateState(Utils.getAllies(), Utils.getEnemies());
        }

        public double ComputeFitness(int frameCount)
        {
            double maxScale = 1;
            double minimum = -(_enemies.Count * _enemies.Count) * maxScale;
            int allyScore = _allies.Count(x => x.exists());
            int enemyScore = _enemies.Count(x => x.exists());
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
            Debug.Assert(!double.IsNaN(score));
            return score;
        }
    }
}
