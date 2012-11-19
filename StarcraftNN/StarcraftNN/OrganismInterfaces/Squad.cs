using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using SharpNeat.Genomes.Neat;

namespace StarcraftNN.OrganismInterfaces
{
    public class Squad
    {
        public List<Unit> Allies
        {
            get;
            private set;
        }

        public List<Unit> Enemies
        {
            get;
            private set;
        }

        public NeatGenome Genome
        {
            get;
            private set;
        }

        protected IOrganismInterface Interface
        {
            get;
            set;
        }

        public Squad(NeatGenome genome, IOrganismInterface iface)
        {
            this.Allies = new List<Unit>();
            this.Enemies = new List<Unit>();
            this.Genome = genome;
            this.Interface = iface;
        }

        public void Delegate()
        {
            this.Interface.UpdateState();
            this.Interface.InputActivate(this.Genome);
        }
    }
}
