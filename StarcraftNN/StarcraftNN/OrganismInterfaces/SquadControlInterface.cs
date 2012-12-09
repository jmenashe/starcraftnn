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
    public abstract class SquadControlInterface : IOrganismInterface
    {
        private static int SquadSize = 3;
        private int _maxSquads;
        Position _centroid;
        private List<Unit> _allies, _enemies;
        List<List<Unit>> _squads, _binnedEnemies;
        int _startAllyHealth, _startEnemyHealth;
        private Dictionary<Unit, Action> _lastAction;
        private Dictionary<Unit, Position> _currentPositions = new Dictionary<Unit,Position>();
        protected static readonly int ThetaBins = 12;
        private static double[] DistanceRanges = { 0, 50, 300, double.PositiveInfinity };

        public abstract int UnitCount { get; }

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

        private enum ActionType
        {
            AttackAir ,
            AttackShort,
            AttackLong,
            AttackAirBonus,
            Move,
            None
        }

        protected struct Position
        {
            public Position(int x, int y) { this.x = x; this.y = y; }
            public int x;
            public int y;
            public double distanceTo(Position other)
            {
                return Math.Sqrt(Math.Pow(this.x - other.x, 2) + Math.Pow(this.y - other.y, 2));
            }
            public static Position operator-(Position left, Position right)
            {
                Position difference = new Position(left.x - right.x, left.y - right.y);
                return difference;
            }
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

        public SquadControlInterface()
        {
            _maxSquads = (int)Math.Ceiling((double)UnitCount / SquadSize);
            _lastAction = new Dictionary<Unit, Action>();
            this.Decoder = new NeatGenomeDecoder(SharpNeat.Decoders.NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(10, true));
        }

        public NeatGenomeFactory CreateGenomeFactory()
        {
            NeatGenomeParameters param = new NeatGenomeParameters();
            param.AddConnectionMutationProbability = 0.1;
            param.AddNodeMutationProbability = 0.1;
            param.ConnectionWeightMutationProbability = 0.8;
            /*
             input:
                per squad:
                    squad health%, count, properties
                per bin:
                    enemy health%, count, properties
            output: 
                per squad:
                    target bin
            */
            NeatGenomeFactory factory = new NeatGenomeFactory(_maxSquads * 5 + DistanceBins * ThetaBins * 5, _maxSquads * DistanceBins * ThetaBins, param);
            return factory;
        }

        protected void Input(IBlackBox blackbox)
        {
            int sensor = 0;
            foreach (var squad in _squads)
            {
                int squadHP = squad.Sum(x => x.getHitPoints());
                int squadMaxHP = squad.Sum(x => x.getType().maxHitPoints());
                blackbox.InputSignalArray[sensor++] = squad.Count;
                blackbox.InputSignalArray[sensor++] = squad.Count (x => Utils.isShortRange(x));
                blackbox.InputSignalArray[sensor++] = squad.Count (x => Utils.isAir(x));
                blackbox.InputSignalArray[sensor++] = squad.Count (x => Utils.hasAttackAirBonus(x));
                blackbox.InputSignalArray[sensor++] = squad.Count == 0 ? 0 : (double)squadHP / squadMaxHP;
            }
            foreach (var enemies in _binnedEnemies)
            {
                int enemyHP = enemies.Sum(x => x.getHitPoints());
                int enemyMaxHP = enemies.Sum(x => x.getType().maxHitPoints());
                blackbox.InputSignalArray[sensor++] = enemies.Count;
                blackbox.InputSignalArray[sensor++] = enemies.Count (x => Utils.isShortRange(x));
                blackbox.InputSignalArray[sensor++] = enemies.Count (x => Utils.isAir(x));
                blackbox.InputSignalArray[sensor++] = enemies.Count (x => Utils.hasAttackAirBonus(x));
                blackbox.InputSignalArray[sensor++] = enemies.Count == 0 ? 0 : (double)enemyHP / enemyMaxHP;
            }
        }

        protected void Activate(IBlackBox blackbox)
        {
            blackbox.Activate();
            int signal = 0;
            List<ScoredBin> sbins = new List<ScoredBin>();
            foreach (var squad in _squads)
            {
                for (int bin = 0; bin < DistanceBins * ThetaBins; ++bin)
                {
                    ScoredBin sbin;
                    sbin.bin = bin;
                    sbin.score = blackbox.OutputSignalArray[signal++];
                    sbins.Add(sbin);
                }
                sbins.OrderByDescending(x => x.score).ToList();
                int targetBin = sbins[0].bin;
                if (_binnedEnemies[targetBin].Count > 0)
                    squadAttack (squad, targetBin);
                else
                    squadMove (squad, targetBin);
            }
        }

        //override this method
        //squad attacks enemies based on ??? rules or pre-trained genome
        //each unit needs to choose attack type and enemy, if it is not already attacking
        protected virtual void squadAttack (List<Unit> squad, int bin)
        {
            var enemies = _binnedEnemies[bin];
            foreach (var ally in squad)
            {
                if (!ally.isAttacking())
                {
                    var target = enemies[0];
                    if (Utils.isAir(target))
                        if (Utils.hasAttackAirBonus(ally))
                            _lastAction[ally].Type = ActionType.AttackAirBonus;
                        else
                            _lastAction[ally].Type = ActionType.AttackAir;
                    else
                        if (Utils.isShortRange(ally))
                            _lastAction[ally].Type = ActionType.AttackShort;
                        else
                            _lastAction[ally].Type = ActionType.AttackLong;
                    ally.attack(target);
                    _lastAction[ally].Target = target;
                }
            }
        }

        protected void squadMove (List<Unit> squad, int bin)
        {
            double d0, d1, t0, t1;
            getLogPolarBounds(bin, out d0, out d1, out t0, out t1);
            double tAvg = (t0 + t1) / 2, dAvg = (d0 + d1) /2;
            int dx = (int)(dAvg * Math.Cos(tAvg));
            int dy = (int)(dAvg * Math.Sin(tAvg));
            var movePosition = new Position(_centroid.x + dx, _centroid.y + dy);
            foreach (var ally in squad)
            {
                if (_lastAction[ally].Type != ActionType.Move || !ally.isMoving())
                {
                    ally.move(new BwapiPosition(movePosition.x, movePosition.y));
                    _lastAction[ally].Type = ActionType.Move;
                }
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

        protected List<List<Unit>> getUnitsInBins(Position position, bool allies = false)
        {
            IEnumerable<Unit> allUnits = allies? _allies : _enemies;
            List<List<Unit>> binnedUnits = new List<List<Unit>>();
            for (int bin = 0; bin < DistanceBins * ThetaBins; bin++)
            {
                double d0, d1, t0, t1;
                getLogPolarBounds(bin, out d0, out d1, out t0, out t1);
                List<Unit> last = new List<Unit>();
                binnedUnits.Add(last);
                foreach (var unit in allUnits.Where(x => x.exists()))
                {
                    double distance = position.distanceTo(_currentPositions[unit]);
                    Position difference = _currentPositions[unit] - position;
                    double theta = Math.Atan2(difference.y, difference.x);
                    if (theta <= 0)
                        theta += 2*Math.PI;
                    if (d0 <= distance && d1 > distance && t0 <= theta && t1 > theta)
                        last.Add(unit);
                }
            }
            return binnedUnits;
        }

        protected void formSquads(Position center)
        {
            var binnedAllies = getUnitsInBins(center, true);
            if (_squads != null)
                _squads.Clear();
            _squads = new List<List<Unit>>();
            List<Unit> squad = new List<Unit>();
            _squads.Add(squad);
            foreach (var bin in binnedAllies)
                foreach (var ally in bin)
                {
                    squad.Add(ally);
                    if (squad.Count == SquadSize && _squads.Count < _maxSquads)
                    {
                        squad = new List<Unit>();
                        _squads.Add(squad);
                    }
                }
            while (_squads.Count < _maxSquads)
                _squads.Add(new List<Unit>());
        }

        public void InputActivate(NeatGenome genome)
        {
            var blackbox = this.Decoder.Decode(genome);
            _currentPositions.Clear();
            foreach (var unit in _allies.Union(_enemies))
            { 
                var bpos = unit.getPosition();
                _currentPositions.Add(unit, new Position(bpos.xConst(), bpos.yConst()));
            }
            _centroid.x = (int) _allies.Average(x => _currentPositions[x].x);
            _centroid.y = (int) _allies.Average(x => _currentPositions[x].y);
            _binnedEnemies = getUnitsInBins(_centroid);
            this.formSquads(_centroid);
            this.Input(blackbox);
            this.Activate(blackbox);
        }

        public virtual void UpdateState(IEnumerable<Unit> allies, IEnumerable<Unit> enemies)
        {
            _allies = allies.OrderBy(x => x.getType().getID()).ToList();
            _enemies = enemies.OrderBy(x => x.getType().getID()).ToList();
            _startAllyHealth = Utils.getAllies().Sum(x => x.getType().maxHitPoints());
            _startEnemyHealth = _enemies.Sum(x => x.getType().maxHitPoints());
            _lastAction.Clear();
            //_maxSquads = (int)Math.Ceiling((double)_allies.Count / SquadSize);
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

        public virtual double ComputeFitness(int frameCount)
        {
            double allyHealth = Utils.getAllies().Sum(x => x.getHitPoints());
            double enemyHealth = Utils.getEnemies().Sum(x => x.getHitPoints());
            double score = Math.Pow (2 + ((allyHealth/_startAllyHealth) - (enemyHealth/_startEnemyHealth)), 3);
            //Console.WriteLine ("{0:f} {1:f} {2:f} {3:f}\t{4:f}", allyHealth, _startAllyHealth, enemyHealth, _startEnemyHealth, score);
            Debug.Assert(!double.IsNaN(score) && score > 0);
            return score;
        }
    }
}
