using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using SharpNeat.Genomes.Neat;
using SharpNeat.Decoders.Neat;
using SharpNeat.Phenomes;

namespace StarcraftNN.OrganismInterfaces
{
    public abstract class MarineFirebatSquads12v12 : IOrganismInterface
    {
        private List<Unit> _allies, _enemies;
        private List<Action> _lastAction;
        private List<Squad> _squads;
        private static double MaxDistance = 750;
        private static int MarinesPerSquad = 2;
        private static int FirebatsPerSquad = 2;
        private static int SquadCount = 4;

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
            Delegate,
            Retreat,
            None
        }

        private class Action
        {
            public ActionTypes Type { get; set; }
            public int Arg1 { get; set; }
            public bool Arg2 { get; set; }
        }

        public NeatGenomeDecoder Decoder
        {
            get;
            protected set;
        }

        public MarineFirebatSquads12v12()
        {
            _lastAction = new List<Action>();
            for (int i = 0; i < SquadCount; i++)
                _lastAction.Add(new Action { Type = ActionTypes.None });
            this.Decoder = new NeatGenomeDecoder(SharpNeat.Decoders.NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(10));
        }

        public NeatGenomeFactory CreateGenomeFactory()
        {
            NeatGenomeParameters param = new NeatGenomeParameters();
            param.AddConnectionMutationProbability = 0.1;
            param.AddNodeMutationProbability = 0.1;
            param.ConnectionWeightMutationProbability = 0.8;
            NeatGenomeFactory factory = new NeatGenomeFactory(SquadCount * (SquadCount * 2 + 2), SquadCount * (SquadCount + 1), param);
            return factory;
        }

        protected IBlackBox Input(NeatGenome genome)
        {
            var blackbox = this.Decoder.Decode(genome);
            return blackbox;
        }

        protected void Activate(IBlackBox blackbox)
        {

        }

        public void InputActivate(NeatGenome genome)
        {
            var blackbox = this.Input(genome);
            this.Activate(blackbox);
        }

        public void UpdateState()
        {
        }

        public double ComputeFitness(int frameCount)
        {
            return 0;
        }
    }
}
