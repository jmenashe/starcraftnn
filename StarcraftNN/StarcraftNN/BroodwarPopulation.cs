using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using StarcraftNN.OrganismInterfaces;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Core;
using System.Threading;

namespace StarcraftNN
{
    public class BroodwarPopulation : IGenomeListEvaluator<NeatGenome>
    {
        private IOrganismInterface _iface;
        NeatEvolutionAlgorithm<NeatGenome> _algorithm;
        private int _currentGenome;
        private static readonly int PopulationSize = 100;

        public ulong EvaluationCount
        {
            get;
            private set;
        }

        public bool StopConditionSatisfied
        {
            get
            {
                return true;
            }
        }

        public BroodwarPopulation(IOrganismInterface iface)
        {
            _iface = iface;
            NeatGenomeFactory factory = _iface.CreateGenomeFactory();
            _algorithm = new NeatEvolutionAlgorithm<NeatGenome>();
            _algorithm.Initialize(this, factory, PopulationSize);
            _algorithm.UpdateScheme = new UpdateScheme(1);
            _algorithm.SingleThreaded = true;
        }
        public void PerformStep()
        {
            NeatGenome genome = _algorithm.GenomeList[_currentGenome];
            _iface.InputActivate(genome);
        }

        public void BeginIteration()
        {
            _iface.UpdateState();
        }

        public void EndIteration(double fitness)
        {
            NeatGenome genome = _algorithm.GenomeList[_currentGenome];
            genome.EvaluationInfo.SetFitness(fitness);
            Console.WriteLine("Fitness: {0:f}", genome.EvaluationInfo.Fitness);
            _currentGenome++;
            if (_currentGenome == BroodwarPopulation.PopulationSize)
            {
                _algorithm.UpdateStats();
                Console.WriteLine("Epoch, Avg Fitness: {0:f}, Max Fitness: {0:f}", _algorithm.Statistics._meanFitness, _algorithm.Statistics._maxFitness);
                _algorithm.StartContinue();
                _currentGenome = 0;
            }
        }

        public void Evaluate(IList<NeatGenome> genomeList)
        {
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
