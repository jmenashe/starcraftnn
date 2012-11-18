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
using System.IO;
using System.Xml;

namespace StarcraftNN
{
    public class BroodwarPopulation : IGenomeListEvaluator<NeatGenome>
    {
        private IOrganismInterface _iface;
        NeatEvolutionAlgorithm<NeatGenome> _algorithm;
        private NeatGenome _currentGenome;
        private static readonly int PopulationSize = 20;
        uint _generation = 0;

        public string SaveFile
        {
            get;
            private set;
        }

        public ulong EvaluationCount
        {
            get;
            private set;
        }

        public bool StopConditionSatisfied
        {
            get
            {
                return false;
            }
        }

        public BroodwarPopulation(IOrganismInterface iface)
        {
            _iface = iface;
            NeatGenomeFactory factory = _iface.CreateGenomeFactory();
            this.SaveFile = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "sc_populations", _iface.SaveFile);
            _algorithm = new NeatEvolutionAlgorithm<NeatGenome>();
            _algorithm.UpdateScheme = new UpdateScheme(1);
            _algorithm.UpdateEvent += (sender, e) =>
            {
                if (_algorithm.CurrentGeneration != _generation)
                {
                    _generation = _algorithm.CurrentGeneration;
                    Console.WriteLine("Epoch, Avg Fitness: {0:f}, Max Fitness: {1:f}", _algorithm.Statistics._meanFitness, _algorithm.Statistics._maxFitness);
                    Save();
                }
            };
            Thread t = new Thread(() =>
                {
                    if (File.Exists(this.SaveFile))
                    {
                        var genomes = Load(factory);
                        _algorithm.Initialize(this, factory, genomes);
                    }
                    else _algorithm.Initialize(this, factory, PopulationSize);
                    _algorithm.StartContinue();
                });
            t.Start();
        }
        public void PerformStep()
        {
            while (_currentGenome.EvaluationInfo.IsEvaluated)
                Thread.Sleep(250);
            _iface.InputActivate(_currentGenome);
        }

        public void BeginIteration()
        {
            _iface.UpdateState();
        }

        public void EndIteration(double fitness)
        {
            _currentGenome.EvaluationInfo.SetFitness(fitness);
            Console.WriteLine("Fitness: {0:f}", _currentGenome.EvaluationInfo.Fitness);
        }

        public void Evaluate(IList<NeatGenome> genomeList)
        {
            foreach (var genome in genomeList)
            {
                _currentGenome = genome;
                while (!_currentGenome.EvaluationInfo.IsEvaluated)
                    Thread.Sleep(250);
            }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public List<NeatGenome> Load(NeatGenomeFactory factory)
        {
            XmlDocument document = new XmlDocument();
            document.Load(this.SaveFile);
            List<NeatGenome> genomes = NeatGenomeXmlIO.LoadCompleteGenomeList(document, true, factory);
            return genomes;
        }

        public void Save()
        {
            XmlDocument document = NeatGenomeXmlIO.SaveComplete(_algorithm.GenomeList, true);
            Directory.CreateDirectory(Path.GetDirectoryName(this.SaveFile));
            document.Save(this.SaveFile);
        }
    }
}
