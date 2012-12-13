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

namespace StarcraftNN.OrganismInterfaces
{
    public abstract class IndividualControlInterface : IOrganismInterface
    {
        private List<Unit> _allies, _enemies;
        int _startEnemyHealth;
        private Dictionary<Unit, Action> _lastAction;
        private Dictionary<Unit, Position> _currentPositions = new Dictionary<Unit,Position>();
        protected static readonly int ThetaBins = 12;
        private static List<double> DistanceRanges = new List<double>{ 0, 50, 300, double.PositiveInfinity };
        public int DistanceBins { get { return DistanceRanges.Count - 1; } }
        private PolarBinManager _polarbins;

        public string SaveFile
        {
            get { return this.GetType().Name; }
        }

        private enum ActionType
        {
            AttackAir ,
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

        public NeatGenomeDecoder Decoder
        {
            get;
            protected set;
        }

        public IndividualControlInterface()
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
            NeatGenomeFactory factory = new NeatGenomeFactory(3 + DistanceBins * ThetaBins * 8, 6 + DistanceBins * ThetaBins, param);
            return factory;
        }

        protected void Input(Unit ally, IBlackBox blackbox)
        {
            int sensor = 0;
            var binnedEnemies = _polarbins.GetEnemiesInBins(ally);
            var binnedAllies = _polarbins.GetAlliesInBins(ally);
            var allEnemyCount = _enemies.Count(x => x.exists());
            var allAllyCount = _allies.Count(x => x.exists());
            blackbox.InputSignalArray[sensor++] = Utils.isShortRange(ally) ? 1 : -1;
            blackbox.InputSignalArray[sensor++] = Utils.isAir(ally) ? 1 : -1;
            blackbox.InputSignalArray[sensor++] = Utils.hasAttackAirBonus(ally) ? 1 : -1;
            for (int bin = 0; bin < ThetaBins * DistanceBins; bin++)
            {
                var enemies = binnedEnemies[bin];
                var allies = binnedAllies[bin];
                int enemyHP = enemies.Sum(x => x.getHitPoints());
                int enemyMaxHP = enemies.Sum(x => x.getType().maxHitPoints());
                int allyHP = allies.Sum(x => x.getHitPoints());
                int allyMaxHP = allies.Sum(x => x.getType().maxHitPoints());
                bool hasShortRange = enemies.Any(x => Utils.isShortRange(x));
                bool hasAir = enemies.Any(x => Utils.isAir(x));
                bool hasAttackAirBonus = enemies.Any(x => Utils.hasAttackAirBonus(x));
                bool hasMachine = enemies.Any(x => Utils.isMachine(x));
                blackbox.InputSignalArray[sensor++] = enemies.Count == 0 ? 0 : (double)enemyHP / enemyMaxHP;
                blackbox.InputSignalArray[sensor++] = allEnemyCount == 0 ? 0 : (double)enemies.Count / allEnemyCount;
                blackbox.InputSignalArray[sensor++] = allies.Count == 0 ? 0 : (double)allyHP / allyMaxHP;
                blackbox.InputSignalArray[sensor++] = allAllyCount == 0 ? 0 : (double)allies.Count / allAllyCount;
                blackbox.InputSignalArray[sensor++] = hasShortRange ? 1 : -1;
                blackbox.InputSignalArray[sensor++] = hasAir ? 1 : -1;
                blackbox.InputSignalArray[sensor++] = hasAttackAirBonus ? 1 : -1;
                blackbox.InputSignalArray[sensor++] = hasMachine ? 1 : -1;
            }
        }

        protected void Activate(Unit ally, IBlackBox blackbox)
        {
            blackbox.Activate();
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
                                case  ActionType.AttackShort:
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

        public void InputActivate(NeatGenome genome)
        {
            var blackbox = this.Decoder.Decode(genome);
            _polarbins.UpdatePositions();
            _currentPositions.Clear();
            foreach (var unit in _allies.Union(_enemies))
            { 
                var bpos = unit.getPosition();
                _currentPositions.Add(unit, new Position(bpos.xConst(), bpos.yConst()));
            }
            foreach (var ally in _allies)
            {
                this.Input(ally, blackbox);
                this.Activate(ally, blackbox);
            }
        }

        public void UpdateState(IEnumerable<Unit> allies, IEnumerable<Unit> enemies)
        {
            _allies = allies.OrderBy(x => x.getType().getID()).ToList();
            _enemies = enemies.OrderBy(x => x.getType().getID()).ToList();
            _polarbins = new PolarBinManager(_allies, _enemies, DistanceRanges, ThetaBins);
            _startEnemyHealth = _enemies.Sum(x => x.getType().maxHitPoints());
            _lastAction.Clear();
            foreach (var ally in allies)
            {
                _lastAction[ally] = new Action();
                _lastAction[ally].Type = ActionType.None;
            }
        }

        public void UpdateState()
        {
            this.UpdateState(Utils.getAllies(), Utils.getEnemies());
        }

        public double ComputeFitness(int frameCount)
        {
            return Utils.computeStandardFitness(_allies, _enemies, frameCount);
        }
    }
}
