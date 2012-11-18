using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarcraftNN.OrganismInterfaces
{
    public class MarineFirebat3v3 : HeteroAttackMoveInterface
    {
        public override int UnitCount
        {
            get { return 3; }
        }
    }
}
