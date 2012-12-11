using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unit = SWIG.BWAPI.Unit;
using CenteredClusters = System.Collections.Generic.Dictionary<StarcraftNN.OrganismInterfaces.Position, StarcraftNN.OrganismInterfaces.KMeans.Cluster>;

namespace StarcraftNN.OrganismInterfaces
{
    public class KMeans
    {
        Random _random;
        double _epsilon = 0;

        public KMeans()
        {
            _random = new Random();
        }

        public class Cluster
        {
            public List<Unit> units = new List<Unit>();
            public List<Position> positions = new List<Position>();
            public void Clear() { units.Clear(); positions.Clear(); }
        }

        public List<UnitGroup> ComputeClusters(List<Unit> units, int clusterCount)
        {
            if (units.Count == 0)
                return new List<UnitGroup>();
            if (units.Count < clusterCount)
                return new List<UnitGroup> { units };
            List<Position> positions = units.Select(x => x.getPosition()).Select(x => new Position(x.xConst(), x.yConst())).ToList();
            return ComputeClusters(positions, units, clusterCount);
        }

        public List<UnitGroup> ComputeClusters(List<Position> positions, List<Unit> units, int clusterCount)
        {
            HashSet<int> centerIndexes = new HashSet<int>();
            while (centerIndexes.Count < clusterCount)
                centerIndexes.Add(_random.Next() % positions.Count);
            CenteredClusters clusters = new CenteredClusters();
            foreach (var i in centerIndexes)
                clusters.Add(positions[i], new Cluster());
            do
            {
                assign(clusters, positions, units);
            } while (update(clusters) < _epsilon);
            var groups = new List<UnitGroup>();
            foreach (var kvp in clusters)
                groups.Add(clusters[kvp.Key].units);
            return groups;
        }

        protected void assign(CenteredClusters clusters, List<Position> positions, List<Unit> units)
        {
            foreach (var kvp in clusters)
                clusters[kvp.Key].Clear();
            for (int i = 0; i < positions.Count; i++)
            {
                Position nearestCenter = null;
                double minDistance = double.MaxValue;
                foreach (var kvp in clusters)
                {
                    double distance = positions[i].distanceTo(kvp.Key);
                    if (distance < minDistance)
                    {
                        nearestCenter = kvp.Key;
                        minDistance = distance;
                    }
                }
                clusters[nearestCenter].positions.Add(positions[i]);
                clusters[nearestCenter].units.Add(units[i]);
            }
        }

        protected double update(CenteredClusters clusters)
        {
            double moveDistance = 0;
            foreach (var kvp in clusters)
            {
                var centroid = getCentroid(clusters[kvp.Key].positions);
                moveDistance += centroid.distanceTo(kvp.Key);
                kvp.Key.x = centroid.x;
                kvp.Key.y = centroid.y;
            }
            return moveDistance;
        }

        protected Position getCentroid(List<Position> positions)
        {
            var x = positions.Average(p => p.x);
            var y = positions.Average(p => p.y);
            return new Position((int)x, (int)y);
        }
    }
}
