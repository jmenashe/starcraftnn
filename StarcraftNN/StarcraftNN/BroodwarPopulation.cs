﻿using System;
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
        private static readonly int PopulationSize = 50;
        private static readonly int FitnessTrials = 5;
        private List<double> _fitnessResults = new List<double>();
        uint _generation = 0;

        public bool EnableEvolution
        {
            get;
            set;
        }

        public string GenomeFile
        {
            get;
            private set;
        }

        public string StatsFile
        {
            get;
            private set;
        }

        public string IterationResultsFile
        {
            get;
            private set;
        }

        public ulong EvaluationCount
        {
            get;
            private set;
        }

        public uint Iteration
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

        private string _saveDirectory = Path.Combine(Environment.GetEnvironmentVariable("STARCRAFT_RESULTS"), Environment.MachineName);

        public BroodwarPopulation(IOrganismInterface iface, bool evolve)
        {
            this.EnableEvolution = evolve;
            _iface = iface;
            NeatGenomeFactory factory = _iface.CreateGenomeFactory();
            this.GenomeFile = Path.Combine(_saveDirectory, _iface.SaveFile + ".xml");
            this.StatsFile = Path.Combine(_saveDirectory, _iface.SaveFile + ".csv");
            this.IterationResultsFile = Path.Combine(_saveDirectory, _iface.SaveFile + "_results.csv");
            if (!Directory.Exists(_saveDirectory))
                Directory.CreateDirectory(_saveDirectory);
            _algorithm = new NeatEvolutionAlgorithm<NeatGenome>();
            _algorithm.UpdateScheme = new UpdateScheme(1);
            _algorithm.UpdateEvent += (sender, e) =>
            {
                if (_algorithm.CurrentGeneration != _generation)
                {
                    _generation = _algorithm.CurrentGeneration;
                    Console.WriteLine("-------------Epoch, Avg Fitness: {0:f}, Max Fitness: {1:f}", _algorithm.Statistics._meanFitness, _algorithm.Statistics._maxFitness);
                    WriteStats();
                    SaveGenomes();
                }
            };
            Thread t = new Thread(() =>
                {
                    if (File.Exists(this.GenomeFile))
                    {
                        if (this.EnableEvolution)
                        {
                            var genomes = Load(factory);
                            Console.WriteLine("Loading genomes from file");
                            _algorithm.Initialize(this, factory, genomes);
                        }
                        else
                        {
                            _currentGenome = Utils.loadBestGenome(iface.SaveFile, factory);
                        }
                    }
                    else if (this.EnableEvolution)
                        _algorithm.Initialize(this, factory, PopulationSize);
                    else
                        _currentGenome = factory.CreateGenome(0);
                    if(this.EnableEvolution)
                        _algorithm.StartContinue();
                });
            t.Start();
        }
        public void PerformStep()
        {
            if (this.EnableEvolution)
            {
                while (_currentGenome.EvaluationInfo.IsEvaluated)
                {
                    Thread.Sleep(250);
                }
            }
            _iface.InputActivate(_currentGenome);
        }

        public void BeginIteration()
        {
            _iface.UpdateState();
        }

        public void EndIteration(int frameCount)
        {
            if (frameCount == 0) return;
            double fitness = _iface.ComputeFitness(frameCount);
            _fitnessResults.Add(fitness);
            using (var w = new StreamWriter(this.IterationResultsFile, true))
                w.WriteLine("{0},{1}", this.Iteration, fitness);
            if (this.EnableEvolution && _fitnessResults.Count == FitnessTrials)
            {
                _currentGenome.EvaluationInfo.SetFitness(_fitnessResults.Average());
                Console.WriteLine("Fitness Avg: {0:f3} Max: {1:f3} Min: {2:f3}", _currentGenome.EvaluationInfo.Fitness, _fitnessResults.Max(), _fitnessResults.Min());
            }
            this.Iteration++;
        }

        public void Evaluate(IList<NeatGenome> genomeList)
        {
            foreach (var genome in genomeList)
            {
                _fitnessResults.Clear();
                _currentGenome = genome;
                while (!_currentGenome.EvaluationInfo.IsEvaluated)
                    Thread.Sleep(250);
            }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        protected List<NeatGenome> Load(NeatGenomeFactory factory)
        {
            XmlDocument document = new XmlDocument();
            document.Load(this.GenomeFile);
            List<NeatGenome> genomes = NeatGenomeXmlIO.LoadCompleteGenomeList(document, true, factory);
            return genomes;
        }

        protected void SaveGenomes()
        {
            XmlDocument document = NeatGenomeXmlIO.SaveComplete(_algorithm.GenomeList, true);
            Directory.CreateDirectory(Path.GetDirectoryName(this.GenomeFile));
            document.Save(this.GenomeFile);
        }

        protected void WriteStats()
        {
            if (!File.Exists(this.StatsFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(this.StatsFile));
                using (StreamWriter writer = new StreamWriter(this.StatsFile))
                {
                    writer.WriteLine(
                        "MaxFitness,MeanFitness,StdDevFitness," + 
                        "MaxComplexity,MeanComplexity,StdDevComplexity," + 
                        "MeanSpeciesChampFitness,StdDevSpeciesChampFitness," + 
                        "MinSpeciesSize,MaxSpeciesSize,SpeciesCount," + 
                        "Generation");
                }
            }
            using (StreamWriter writer = new StreamWriter(this.StatsFile, true))
            {
                writer.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                    _algorithm.Statistics._maxFitness, _algorithm.Statistics._meanFitness, _algorithm.Statistics._stdDevFitness,
                    _algorithm.Statistics._maxComplexity, _algorithm.Statistics._meanComplexity, _algorithm.Statistics._stdDevComplexity,
                    _algorithm.Statistics._meanSpecieChampFitness, _algorithm.Statistics._stdDevSpecieChampFitness,
                    _algorithm.Statistics._minSpecieSize, _algorithm.Statistics._maxSpecieSize, _algorithm.Statistics._speciesCount,
                    _algorithm.Statistics._generation
                );
            }
        }
    }
}
