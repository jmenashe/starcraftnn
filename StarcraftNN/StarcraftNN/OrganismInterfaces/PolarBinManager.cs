using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using StarcraftNN;
using BwapiPosition = SWIG.BWAPI.Position;
using System.Diagnostics;

namespace StarcraftNN.OrganismInterfaces
{
    public class PolarBinManager
    {
        public int DistanceBins
        {
            get;
            private set;
        }
        public int ThetaBins
        {
            get;
            private set;
        }
        private List<Unit> _allies, _enemies;
        private List<double> _distances;
        private Dictionary<Unit, Position> _currentPositions = new Dictionary<Unit, Position>();

        public List<Position> AllyPositions
        {
            get;
            private set;
        }

        public List<Position> EnemyPositions
        {
            get;
            private set;
        }

        public Position Centroid
        {
            get;
            private set;
        }

        public PolarBinManager(List<Unit> allies, List<Unit> enemies, List<double> distances, int thetaBins)
        {
            _allies = allies;
            _enemies = enemies;
            _distances = distances;
            this.DistanceBins = _distances.Count - 1;
            this.ThetaBins = thetaBins;
            this.Centroid = new Position(0, 0);
            UpdatePositions();
            Debug.Assert(this.DistanceBins > 0 && this.ThetaBins > 0);
        }

        public void UpdatePositions()
        {
            _currentPositions.Clear();
            this.AllyPositions = new List<Position>();
            this.EnemyPositions = new List<Position>();
            foreach (var unit in _allies)
            {
                var bpos = unit.getPosition();
                var p = new Position(bpos.xConst(), bpos.yConst());
                _currentPositions.Add(unit, p);
                this.AllyPositions.Add(p);
            }
            foreach (var unit in _enemies)
            {
                var bpos = unit.getPosition();
                var p = new Position(bpos.xConst(), bpos.yConst());
                _currentPositions.Add(unit, p);
                this.EnemyPositions.Add(p);
            }
            this.Centroid.x = (int)_allies.Average(x => _currentPositions[x].x);
            this.Centroid.y = (int)_allies.Average(x => _currentPositions[x].y);
        }

        public Position GetMovePosition(Unit ally, int bin, double moveDistance)
        {
            double d0, d1, t0, t1;
            GetLogPolarBounds(bin, out d0, out d1, out t0, out t1);
            double distance = Math.Pow(10, moveDistance * 3);
            double tAvg = (t0 + t1) / 2;

            int dx = (int)(distance * Math.Cos(tAvg));
            int dy = (int)(distance * Math.Sin(tAvg));
            Position allyPos = _currentPositions[ally];
            Position center = new Position(allyPos.x + dx, allyPos.y + dy);
            return center;
        }

        public void GetLogPolarBounds(int bin, out double d0, out double d1, out double t0, out double t1)
        {
            Debug.Assert(0 <= bin && bin < DistanceBins * ThetaBins);
            d0 = 0;

            int tbin = bin % ThetaBins;
            double tRange = 2 * Math.PI / ThetaBins;
            t0 = tRange * tbin;
            t1 = tRange * (tbin + 1);

            int dbin = (bin - tbin) / ThetaBins;
            d0 = _distances[dbin];
            d1 = _distances[dbin + 1];
        }

        public List<List<Unit>> GetAlliesInBins(Unit ally)
        {
            return GetUnitsInBins(_currentPositions[ally], true);
        }

        public List<List<Unit>> GetAlliesInBins(Position position)
        {
            return GetUnitsInBins(position, true);
        }

        public List<List<Unit>> GetEnemiesInBins(Unit ally)
        {
            return GetUnitsInBins(_currentPositions[ally], false);
        }

        public List<List<Unit>> GetEnemiesInBins(Position position)
        {
            return GetUnitsInBins(position, false);
        }

        protected List<List<Unit>> GetUnitsInBins(Position position, bool allies)
        {
            IEnumerable<Unit> allUnits = allies ? _allies : _enemies;
            List<List<Unit>> binnedUnits = new List<List<Unit>>();
            for (int bin = 0; bin < DistanceBins * ThetaBins; bin++)
            {
                double d0, d1, t0, t1;
                GetLogPolarBounds(bin, out d0, out d1, out t0, out t1);
                List<Unit> last = new List<Unit>();
                binnedUnits.Add(last);
                foreach (var unit in allUnits.Where(x => x.exists()))
                {
                    double distance = position.distanceTo(_currentPositions[unit]);
                    Position difference = _currentPositions[unit] - position;
                    double theta = Math.Atan2(difference.y, difference.x);
                    if (theta <= 0)
                        theta += 2 * Math.PI;
                    if (d0 <= distance && d1 > distance && t0 <= theta && t1 > theta)
                        last.Add(unit);
                }
            }
            return binnedUnits;
        }
    }
}
