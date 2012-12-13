using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using BwapiPosition = SWIG.BWAPI.Position;
using SharpNeat.Genomes.Neat;
using SharpNeat.Decoders.Neat;
using SharpNeat.Phenomes;
using System.Diagnostics;

namespace StarcraftNN.OrganismInterfaces
{
    public class SquadInterface : UnitGroup, ISquad
    {
        #region Subclasses
        private enum ActionType
        {
            AttackAir,
            AttackShort,
            AttackLong,
            AttackAirBonus,
            Move,
            None
        }

        private struct ScoredAction
        {
            public ActionType action;
            public double score;
        }

        private struct ScoredBin
        {
            public int bin;
            public double score;
        }

        private class Action
        {
            public ActionType Type { get; set; }
            public Unit Target { get; set; }
        }
        #endregion

        protected UnitGroup _enemies;
        protected NeatGenome _genome;
        protected PolarBinManager _polarbins;
        private Dictionary<Unit, Action> _lastAction;
        static Dictionary<string, NeatGenome> _genomes = new Dictionary<string, NeatGenome>();

        private NeatGenomeDecoder _decoder;
        public NeatGenomeDecoder Decoder
        {
            get
            {
                if(_decoder == null)
                    _decoder = new NeatGenomeDecoder(SharpNeat.Decoders.NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(10, true));
                return _decoder;
            }
        }

        public List<double> DistanceRanges = new List<double> { 0, 100, double.PositiveInfinity };
        public int ThetaBins
        {
            get
            {
                return 5;
            }
        }
        public int DistanceBins { get { return DistanceRanges.Count - 1; } }


        public SquadInterface() 
        {
            _lastAction = new Dictionary<Unit, Action>();
        }

        public SquadInterface(List<Unit> allies, List<Unit> enemies) : base(allies) 
        {
            _lastAction = new Dictionary<Unit, Action>();
            UpdateState(allies, enemies);
        }

        public void UpdateState(IEnumerable<Unit> allies, IEnumerable<Unit> enemies)
        {
            _units = allies.OrderBy(x => x.getType().getID()).ToList();
            _enemies = enemies.OrderBy(x => x.getType().getID()).ToList();
            _polarbins = new PolarBinManager(_units, _enemies, DistanceRanges, ThetaBins);
            foreach (var ally in allies)
                _lastAction[ally] = new Action { Type = ActionType.None };
        }

        public int Size
        {
            get { return 3; }
        }

        public void Move(double theta)
        {
            if (!_units.Any(x => x.exists())) return;
            double distance = 200;
            int dx = (int)(Math.Cos(theta) * distance);
            int dy = (int)(Math.Sin(theta) * distance);
            BwapiPosition centroid = Utils.getCentroid(this);
            for(int i = 0; i < _units.Count; i++)
            {
                var ally = _units[i];
                var position = ally.getPosition();
                ally.move(new BwapiPosition(position.xConst() + dx, position.yConst() + dy));
            }
        }

        protected NeatGenome loadGenome()
        {
            if (!_genomes.ContainsKey(this.SaveFile))
                _genomes[this.SaveFile] = Utils.loadBestGenome(this.SaveFile, this.CreateGenomeFactory());
            return _genomes[this.SaveFile];
        }

        public void Delegate()
        {
            if (_genome == null)
                _genome = loadGenome();
            this.InputActivate(_genome);
        }

        public NeatGenomeFactory CreateGenomeFactory()
        {
            var param = new NeatGenomeParameters();
            param.AddConnectionMutationProbability = 0.1;
            param.AddNodeMutationProbability = 0.1;
            param.ConnectionWeightMutationProbability = 0.8;
            // input: 
            // single: is short range
            // per bin:
            // enemy hit points over max in bin
            // enemy count out of all
            // has short range
            // output (all per bin): 
            // move to bin
            // attack short range
            // attack long range
            var factory = new NeatGenomeFactory(1 + DistanceBins * ThetaBins * 6, this.Size * (6 + DistanceBins * ThetaBins), param);
            return factory;
        }

        public void InputActivate(NeatGenome genome)
        {
            if (!_units.Any(x => x.exists())) return;
            _polarbins.UpdatePositions();
            var blackbox = this.Decoder.Decode(genome);
            this.Input(blackbox);
            this.Activate(blackbox);
        }

        protected void Input(IBlackBox blackbox)
        {
            int sensor = 0;
            var centroid = Utils.getCentroid(_units);
            var binnedEnemies = _polarbins.GetEnemiesInBins(centroid);
            var binnedAllies = _polarbins.GetAlliesInBins(centroid);
            var allEnemyCount = _enemies.Count(x => x.exists());
            var allAllyCount = _units.Count(x => x.exists());
            blackbox.InputSignalArray[sensor++] = (double)this.HitPoints / this.MaxHitPoints;
            for (int bin = 0; bin < ThetaBins * DistanceBins; bin++)
            {
                var enemies = binnedEnemies[bin];
                int enemyHP = enemies.Sum(x => x.getHitPoints());
                int enemyMaxHP = enemies.Sum(x => x.getType().maxHitPoints());
                bool hasShortRange = enemies.Any(x => Utils.isShortRange(x));
                bool hasAir = enemies.Any(x => Utils.isAir(x));
                bool hasAttackAirBonus = enemies.Any(x => Utils.hasAttackAirBonus(x));
                bool hasMachine = enemies.Any(x => Utils.isMachine(x));
                blackbox.InputSignalArray[sensor++] = enemies.Count == 0 ? 0 : (double)enemyHP / enemyMaxHP;
                blackbox.InputSignalArray[sensor++] = allEnemyCount == 0 ? 0 : (double)enemies.Count / allEnemyCount;
                blackbox.InputSignalArray[sensor++] = hasShortRange ? 1 : -1;
                blackbox.InputSignalArray[sensor++] = hasAir ? 1 : -1;
                blackbox.InputSignalArray[sensor++] = hasAttackAirBonus ? 1 : -1;
                blackbox.InputSignalArray[sensor++] = hasMachine ? 1 : -1;
            }
        }

        protected void Activate(IBlackBox blackbox)
        {
            blackbox.Activate();
            foreach (var ally in _units)
            {
                List<ScoredBin> sbins = new List<ScoredBin>();
                int signal = 0;
                List<ScoredAction> sactions = new List<ScoredAction>();
                double moveDistance = blackbox.OutputSignalArray[signal++];
                for (int i = 0; i < 5; i++)
                {
                    ScoredAction sa;
                    sa.score = blackbox.OutputSignalArray[signal++];
                    sa.action = (ActionType)i;

                    sactions.Add(sa);
                }
                sactions = sactions.OrderByDescending(x => x.score).ToList();
                for (int bin = 0; bin < ThetaBins * DistanceBins; bin++)
                {
                    ScoredBin sbin;
                    sbin.bin = bin;
                    sbin.score = blackbox.OutputSignalArray[signal++];
                    sbins.Add(sbin);
                }
                var enemyBins = _polarbins.GetEnemiesInBins(ally);
                sbins = sbins.OrderByDescending(x => x.score).ToList();
                ActionType action = sactions[0].action;
                foreach (var sbin in sbins)
                {
                    var enemies = enemyBins[sbin.bin];
                    if (enemies.Count == 0) continue;
                    switch (action)
                    {
                        case ActionType.None: break;
                        case ActionType.Move:
                            if (_lastAction[ally].Type != ActionType.Move || !ally.isMoving())
                            {
                                Position center = _polarbins.GetMovePosition(ally, sbin.bin, moveDistance);
                                ally.move(new BwapiPosition(center.x, center.y));
                                _lastAction[ally].Type = ActionType.Move;
                            }
                            break;
                        default:
                            if (_lastAction[ally].Type != action || !ally.isAttacking())
                            {
                                IEnumerable<Unit> subgroup = null;
                                switch (action)
                                {
                                    case ActionType.AttackShort:
                                        subgroup = enemies.Where(x => Utils.isShortRange(x));
                                        break;
                                    case ActionType.AttackLong:
                                        subgroup = enemies.Where(x => Utils.isLongRange(x));
                                        break;
                                    case ActionType.AttackAir:
                                        subgroup = enemies.Where(x => Utils.isAir(x));
                                        break;
                                    case ActionType.AttackAirBonus:
                                        subgroup = enemies.Where(x => Utils.hasAttackAirBonus(x));
                                        break;
                                }
                                Debug.Assert(subgroup != null);
                                if (!subgroup.Any()) break;
                                var selection = subgroup
                                    .Where(x => x.getID() == subgroup.Min(y => y.getID())).Single();
                                if (_lastAction[ally].Target != selection || !ally.isAttacking())
                                    ally.attack(selection);
                                _lastAction[ally].Type = action;
                                _lastAction[ally].Target = selection;
                            }
                            break;
                    }
                    break;
                }
            }
        }

        public void UpdateState()
        {
            UpdateState(Utils.getAllies(), Utils.getEnemies());
        }

        public string SaveFile
        {
            get { return this.GetType().Name; }
        }

        public double ComputeFitness(int frameCount)
        {
            return Utils.computeStandardFitness(_units, _enemies, frameCount);
        }
    }
}
