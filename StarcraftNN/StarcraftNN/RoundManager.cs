using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWIG.BWAPI;
using StarcraftNN.OrganismInterfaces;

namespace StarcraftNN
{
    class RoundManager
    {
        private bool _resetting = false, _completionSignaled = false, _initialized = false;
        private int _resetLocation = 0, _maxAllyUnits, _maxEnemyUnits;
        private Position _resetPosition, _targetPosition;
        private int _frameCount, _completionSignalFrame;
        private BroodwarPopulation _population;
        private static readonly int _locationCounts = 8;
        public static readonly int ResetUnitTypeID = bwapi.UnitTypes_Terran_Vulture.getID();

        public Unit ResetUnit
        {
            get
            {
                return bwapi.Broodwar.self().getUnits().Where(x => x.getType().getID() == RoundManager.ResetUnitTypeID).Single();
            }
        }
        public RoundManager(BroodwarPopulation population) 
        {
            _maxAllyUnits = Utils.getAllies().Count;
            _maxEnemyUnits = Utils.getEnemies().Count;
            _resetPosition = this.ResetUnit.getPosition();
            _population = population;
        }

        public void HandleFrame()
        {
            if (!_initialized)
            {
                beginRound();
                _initialized = true;
            }
            _frameCount++;
            if(!_resetting && _population != null && _frameCount % 5 == 0)
            {
                _population.PerformStep();
            }
        }

        public void HandleUnitCreate(Unit unit)
        {
            if (_completionSignaled && _resetting && Utils.getAllies().Count == _maxAllyUnits)
            {
                beginRound();
            }
        }

        public void HandleUnitDestroy(Unit unit)
        {
            if (_resetting) return;
            int allies = Utils.getAllies().Count;
            int enemies = Utils.getEnemies().Count;
            if(allies == 0 || enemies == 0)
                endRound();
        }

        private void endRound()
        {
            _resetting = true;
            _completionSignaled = false;
            _population.EndIteration(_frameCount);
            signalCompletion();
        }

        private void beginRound()
        {
            _resetting = false;
            _frameCount = 0;
            _population.BeginIteration();
        }

        private void signalCompletion()
        {
            int dx = 0, dy = 0, dist = 125;
            switch(_resetLocation % _locationCounts)
            {
                case 0: dx = -dist; dy = -dist; break;
                case 1: dx = 0; dy = -dist; break; 
                case 2: dx = dist; dy = -dist; break;
                case 3: dx = dist; dy = 0; break;
                case 4: dx = dist; dy = dist; break;
                case 5: dx = 0; dy = dist; break; 
                case 6: dx = -dist; dy = dist; break; 
                case 7: dx = -dist; dy = 0; break;
            }
            _targetPosition = new Position(_resetPosition.xConst() + dx, _resetPosition.yConst() + dy);
            this.ResetUnit.move(_targetPosition);
            _completionSignaled = true;
            _resetLocation++;
            _completionSignalFrame = _frameCount;
        }

        private bool completionSignalComplete()
        {
            double distance = _targetPosition.getDistance(this.ResetUnit.getPosition());
            if (distance < .1)
            {
                return true;
            }
            return false;
        }

        public void HandleMatchEnd()
        {
            _population.EndIteration(_frameCount);
        }
    }
}
