using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using SharpNeat.Genomes.Neat;
using SharpNeat.Decoders.Neat;
using SharpNeat.Phenomes;
using StarcraftNN;

namespace StarcraftNN.OrganismInterfaces
{
    public abstract class IndividualControlInterface : IOrganismInterface
    {
        private List<Unit> _allies, _enemies;
        int _startEnemyCount;
        private Dictionary<Unit, Action> _lastAction;
        private static int ThetaBins = 6;
        private static int DistanceBins = 3;
        private static int DistanceRange = 50;


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
                    // ally/enemy hit points over max
                    // ally/enemy count out of all
                    // ally count / enemy count
                    // has short range
                    // has long range
            // output (all per bin): 
                // move to bin
                // attack short range
                // attack long range
            NeatGenomeFactory factory = new NeatGenomeFactory(DistanceBins * ThetaBins * 7 + 1, DistanceBins * ThetaBins * 3, param);
            return factory;
        }

        protected IBlackBox Input(Unit ally, NeatGenome genome)
        {
            int sensor = 0;
            var blackbox = this.Decoder.Decode(genome);
            var binnedAllies = getUnitsInBins(ally, true);
            var binnedEnemies = getUnitsInBins(ally, false);
            blackbox.InputSignalArray[sensor++] = Utils.isShortRange(ally) ? 1 : -1;
            for (int bin = 0; bin < ThetaBins * DistanceBins; bin++)
            {
                var allies = binnedAllies[bin];
                var enemies = binnedEnemies[bin];
                int allyHP = allies.Sum(x => x.getHitPoints());
                int allyMaxHP = allies.Sum(x => x.getType().maxHitPoints());
                int enemyHP = enemies.Sum(x => x.getHitPoints());
                int enemyMaxHP = enemies.Sum(x => x.getType().maxHitPoints());
                bool hasShortRange = enemies.Any(x => Utils.isShortRange(x));
                bool hasLongRange = enemies.Any(x => Utils.isLongRange(x));
                blackbox.InputSignalArray[sensor++] = (double)allyHP / allyMaxHP;
                blackbox.InputSignalArray[sensor++] = (double)enemyHP / enemyMaxHP;
                blackbox.InputSignalArray[sensor++] = (double)allies.Count / _allies.Count;
                blackbox.InputSignalArray[sensor++] = (double)enemies.Count / _enemies.Count;
                blackbox.InputSignalArray[sensor++] = (double)allies.Count / enemies.Count;
                blackbox.InputSignalArray[sensor++] = hasShortRange ? 1 : -1;
                blackbox.InputSignalArray[sensor++] = hasLongRange ? 1 : -1;
            }
            return blackbox;
        }

        protected void Activate(Unit ally, IBlackBox blackbox)
        {
            blackbox.Activate();
            double maxScore = 0;
            int maxBin = 0, maxIndex = 0;
            for (int bin = 0; bin < ThetaBins * DistanceBins; bin++)
            {
                for (int i = 0; i < 3; i++)
                {
                    double score = blackbox.InputSignalArray[3 * bin + i];
                    if (score > maxScore)
                    {
                        maxBin = bin;
                        maxIndex = i;
                        maxScore = score;
                    }
                }
            }
            var enemyBins = getUnitsInBins(ally, false);
            var enemies = enemyBins[maxBin];
            Unit weakest;
            switch(maxIndex)
            {
                case 0:
                    if (_lastAction[ally].Type != ActionTypes.Move || !ally.isMoving())
                    {
                        Position center = getCenter(ally, maxBin);
                        ally.move(center);
                        _lastAction[ally].Type = ActionTypes.Move;
                    }
                    break;
                case 1:
                case 2:
                    if (_lastAction[ally].Type != ActionTypes.Attack)
                    {
                        weakest = enemies
                            .Where(x => maxIndex != 1 ||  Utils.isShortRange(x))
                            .Where(x => maxIndex != 2 || Utils.isLongRange(x))
                            .Where(x => x.getHitPoints() == enemyBins[maxBin].Min(y => y.getHitPoints())).First();
                        if (_lastAction[ally].Target != weakest || !ally.isAttacking())
                            ally.attack(weakest);
                        _lastAction[ally].Type = ActionTypes.Attack;
                        _lastAction[ally].Target = weakest;
                    }
                    break;
            }
        }

        protected void getLogPolarBounds(int bin, out double d0, out double d1, out double t0, out double t1)
        {
            //distance bins: 0-50, 50-150, 150-350, 350-750
            d0 = 0;
            d1 = DistanceRange;
            double dRange = DistanceRange;
            int b = 0;
            for (int dbin = 0; dbin < DistanceBins; dbin++)
            {
                for (int tbin = 0; tbin < ThetaBins; tbin++)
                {
                    if (b == bin)
                    {
                        double tRange = Math.PI / (ThetaBins / 2);
                        t0 = (dbin - (double)ThetaBins / 2) * tRange;
                        t1 = ((dbin + 1) - (double)ThetaBins / 2) * tRange;
                        return;
                    }
                    b++;
                }
                d0 = d1; d1 += (dRange *= 2);
            }
            throw new Exception(string.Format("Bins must be between zero and {0}.", DistanceBins * ThetaBins));
        }

        protected Position getCenter(Unit ally, int bin)
        {
            double d0, d1, t0, t1;
            getLogPolarBounds(bin, out d0, out d1, out t0, out t1);
            double dAvg = (d0 + d1) / 2;
            double tAvg = (t0 + t1) / 2;

            int dx = (int)(dAvg * Math.Cos(tAvg));
            int dy = (int)(dAvg * Math.Sin(tAvg));
            Position center = new Position(ally.getPosition().xConst() + dx, ally.getPosition().yConst() + dy);
            return center;
        }

        protected List<List<Unit>> getUnitsInBins(Unit referenceUnit, bool allies)
        {
            IEnumerable<Unit> allUnits = allies ? Utils.getAllies() : Utils.getEnemies();
            List<List<Unit>> binnedUnits = new List<List<Unit>>();
            for (int bin = 0; bin < DistanceBins * ThetaBins; bin++)
            {
                binnedUnits.Add(new List<Unit>());
                foreach (var unit in allUnits)
                {
                    double d0, d1, t0, t1;
                    getLogPolarBounds(bin, out d0, out d1, out t0, out t1);

                    double distance = referenceUnit.getPosition().getDistance(unit.getPosition());
                    Position difference = unit.getPosition().opSubtract(referenceUnit.getPosition());
                    double theta = Math.Atan2(difference.yConst(), difference.xConst());
                    if (d0 <= distance && d1 > distance && t0 <= theta && t1 > theta)
                        binnedUnits.Last().Add(unit);
                }
            }
            return binnedUnits;
        }

        public void InputActivate(NeatGenome genome)
        {
            foreach (var ally in _allies)
            {
                var blackbox = this.Input(ally, genome);
                this.Activate(ally, blackbox);
            }
        }

        public void UpdateState(IEnumerable<Unit> allies, IEnumerable<Unit> enemies)
        {
            _allies = allies.OrderBy(x => x.getType().getID()).ToList();
            _enemies = enemies.OrderBy(x => x.getType().getID()).ToList();
            _startEnemyCount = _enemies.Count;
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
            double minimum = -(_startEnemyCount * _startEnemyCount) * maxScale;
            double score = Utils.getAllies().Count - Utils.getEnemies().Count;
            score *= Math.Abs(score);
            score *= 200.0 / frameCount;
            score -= minimum;
            return score;
        }
    }
}
