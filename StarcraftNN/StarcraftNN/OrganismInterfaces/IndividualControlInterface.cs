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

namespace StarcraftNN.OrganismInterfaces
{
    public abstract class IndividualControlInterface : IOrganismInterface
    {
        private List<Unit> _allies, _enemies;
        int _startEnemyHealth;
        private Dictionary<Unit, Action> _lastAction;
        protected static readonly int ThetaBins = 9;
        private static double[] DistanceRanges = { 0, 30, 100, 500, double.PositiveInfinity };

        protected static int DistanceBins
        {
            get
            {
                return DistanceRanges.Length - 1;
            }
        }


        public string SaveFile
        {
            get { return this.GetType().Name; }
        }

        private enum ActionTypes
        {
            AttackShort,
            AttackLong,
            Move,
            None
        }

        private struct ScoredAction
        {
            public ActionTypes action;
            public double score;
        }

        private struct ScoredBin
        {
            public int bin;
            public double score;
        }

        private class Action
        {
            public ActionTypes Type { get; set; }
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
            NeatGenomeFactory factory = new NeatGenomeFactory(DistanceBins * ThetaBins * 3 + 1, 4 + DistanceBins * ThetaBins, param);
            return factory;
        }

        protected void Input(Unit ally, IBlackBox blackbox)
        {
            int sensor = 0;
            var binnedEnemies = getUnitsInBins(ally);
            var allEnemyCount = _enemies.Where(x => x.exists()).Count();
            blackbox.InputSignalArray[sensor++] = Utils.isShortRange(ally) ? 1 : -1;
            for (int bin = 0; bin < ThetaBins * DistanceBins; bin++)
            {
                var enemies = binnedEnemies[bin];
                int enemyHP = enemies.Sum(x => x.getHitPoints());
                int enemyMaxHP = enemies.Sum(x => x.getType().maxHitPoints());
                bool hasShortRange = enemies.Any(x => Utils.isShortRange(x));
                blackbox.InputSignalArray[sensor++] = enemies.Count == 0 ? 0 : (double)enemyHP / enemyMaxHP;
                blackbox.InputSignalArray[sensor++] = (double)enemies.Count / allEnemyCount;
                blackbox.InputSignalArray[sensor++] = hasShortRange ? 1 : -1;
            }
        }

        protected void Activate(Unit ally, IBlackBox blackbox)
        {
            blackbox.Activate();
            List<ScoredBin> sbins = new List<ScoredBin>();
            int signal = 0;
            List<ScoredAction> sactions = new List<ScoredAction>();
            double moveDistance = blackbox.OutputSignalArray[signal++];
            for (int i = 0; i < 3; i++)
            {
                ScoredAction sa;
                sa.score = blackbox.OutputSignalArray[signal++];
                switch(i)
                {
                    case 0: sa.action = ActionTypes.Move; break;
                    case 1: sa.action = ActionTypes.AttackLong; break;
                    case 2: sa.action = ActionTypes.AttackShort; break;
                    default: sa.action = ActionTypes.None; break;
                }
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
            var enemyBins = getUnitsInBins(ally);
            sbins = sbins.OrderByDescending(x => x.score).ToList();
            ActionTypes action = sactions[0].action;
            foreach (var sbin in sbins)
            {
                var enemies = enemyBins[sbin.bin];
                if (enemies.Count == 0) continue;
                switch (action)
                {
                    case ActionTypes.Move:
                        if (_lastAction[ally].Type != ActionTypes.Move || !ally.isMoving())
                        {
                            Position center = getMovePosition(ally, sbin.bin, moveDistance);
                            ally.move(center);
                            _lastAction[ally].Type = ActionTypes.Move;
                        }
                        break;
                    case ActionTypes.AttackLong:
                    case ActionTypes.AttackShort:
                        if (_lastAction[ally].Type != action)
                        {
                            IEnumerable<Unit> subgroup = null;
                            switch (action)
                            {
                                case  ActionTypes.AttackShort:
                                    subgroup = enemies.Where(x => Utils.isShortRange(x));
                                    break;
                                case ActionTypes.AttackLong:
                                    subgroup = enemies.Where(x => Utils.isLongRange(x));
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

        protected void getLogPolarBounds(int bin, out double d0, out double d1, out double t0, out double t1)
        {
            Debug.Assert(0 <= bin && bin < DistanceBins * ThetaBins);
            d0 = 0;

            int tbin = bin % ThetaBins;
            double tRange = 2 * Math.PI / ThetaBins;
            t0 = tRange * tbin;
            t1 = tRange * (tbin + 1);

            int dbin = (bin - tbin) / ThetaBins;
            d0 = DistanceRanges[dbin];
            d1 = DistanceRanges[dbin + 1];
        }

        protected Position getMovePosition(Unit ally, int bin, double moveDistance)
        {
            double d0, d1, t0, t1;
            getLogPolarBounds(bin, out d0, out d1, out t0, out t1);
            double distance = Math.Pow(10, moveDistance * 3);
            double tAvg = (t0 + t1) / 2;

            int dx = (int)(distance * Math.Cos(tAvg));
            int dy = (int)(distance * Math.Sin(tAvg));
            Position center = new Position(ally.getPosition().xConst() + dx, ally.getPosition().yConst() + dy);
            return center;
        }

        protected List<List<Unit>> getUnitsInBins(Unit referenceUnit, bool allies = false)
        {
            IEnumerable<Unit> allUnits = allies ? _allies : _enemies;
            List<List<Unit>> binnedUnits = new List<List<Unit>>();
            for (int bin = 0; bin < DistanceBins * ThetaBins; bin++)
            {
                List<Unit> last = new List<Unit>();
                binnedUnits.Add(last);
                foreach (var unit in allUnits.Where(x => x.exists()))
                {
                    double d0, d1, t0, t1;
                    getLogPolarBounds(bin, out d0, out d1, out t0, out t1);

                    double distance = referenceUnit.getPosition().getDistance(unit.getPosition());
                    Position difference = unit.getPosition().opSubtract(referenceUnit.getPosition());
                    double theta = Math.Atan2(difference.yConst(), difference.xConst());
                    if (d0 <= distance && d1 > distance && t0 <= theta && t1 > theta)
                        last.Add(unit);
                }
            }
            return binnedUnits;
        }

        public void InputActivate(NeatGenome genome)
        {
            var blackbox = this.Decoder.Decode(genome);
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
            _startEnemyHealth = _enemies.Sum(x => x.getType().maxHitPoints());
            _lastAction.Clear();
            foreach (var ally in allies)
            {
                _lastAction[ally] = new Action();
                _lastAction[ally].Type = ActionTypes.None;
            }
        }

        public void UpdateState()
        {
            this.UpdateState(Utils.getAllies(), Utils.getEnemies());
        }

        public double ComputeFitness(int frameCount)
        {
            double maxScale = 4;
            double minimum = -(_startEnemyHealth * _startEnemyHealth) * maxScale;
            double score = _allies.Sum(x => x.getHitPoints()) - _enemies.Sum(x => x.getHitPoints());
            score *= Math.Abs(score);
            score *= 200.0 / frameCount;
            score -= minimum;
            return score;
        }
    }
}
