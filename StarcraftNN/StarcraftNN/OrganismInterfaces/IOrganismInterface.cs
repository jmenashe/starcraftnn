using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Genomes.Neat;

namespace StarcraftNN.OrganismInterfaces
{
    public interface IOrganismInterface
    {
        NeatGenomeFactory CreateGenomeFactory();
        void InputActivate(NeatGenome genome);
        void UpdateState();
        string SaveFile { get; }
    }
}
