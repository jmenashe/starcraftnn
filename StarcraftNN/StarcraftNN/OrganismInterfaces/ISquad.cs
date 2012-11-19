using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;

namespace StarcraftNN.OrganismInterfaces
{
    interface ISquad : IOrganismInterface
    {
        void UpdateState(IEnumerable<Unit> allies, IEnumerable<Unit> enemies);
    }
}
