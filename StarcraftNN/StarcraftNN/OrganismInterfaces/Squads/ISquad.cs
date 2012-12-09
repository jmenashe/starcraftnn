using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;

namespace StarcraftNN.OrganismInterfaces
{
    public interface ISquad : IOrganismInterface
    {
        int HitPoints { get; }
        int MaxHitPoints { get; }
        int Size { get; }
        void Move(double theta);
        void Delegate();
    }
}
