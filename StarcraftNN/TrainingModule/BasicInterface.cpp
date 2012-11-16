#include "BasicInterface.h"

BasicInterface::BasicInterface() : OrganismInterface() {
}

void BasicInterface::sendInputs(Organism* organism) {
  Network* network = organism->net;
  std::vector<float> sense_values;
  int sensorCount = _unitCount * 2;
  sense_values.resize(sensorCount);
  int sensor = 0;
  BWAPI::Position squadPosition = getCentroid(_allies);
  for(int i = 0; i < _allies.size(); i++) {
    BWAPI::Unit* unit = _allies[i];
    float distance = squadPosition.getDistance(unit->getPosition());
    sense_values[sensor++] = distance;
    BWAPI::Position difference = unit->getPosition() - squadPosition;
    float angle = tanf(difference.y() / difference.x());
    sense_values[sensor++] = angle;
  }
  network->load_sensors(sense_values);
  network->activate();
}

void BasicInterface::applyOutputs(Organism* organism) {
  std::vector<NNode*> outputs = organism->net->outputs;
  std::vector<ScoredUnit> _sunits;
  for(int i = 0; i < _unitCount; i++) {
    for(int j = 0; j < _unitCount; j++) {
      double attack = outputs[i * _unitCount + j]->activation;
      _sunits.push_back(ScoredUnit(_enemies[j], attack));
    }
    sort(_sunits.begin(), _sunits.end(), ScoredUnit::compare);
    
    BWAPI::Unit* ally = _allies[i];
    if(unitExists(ally)) {
      for(int j = 0; j < _sunits.size(); j++) {
        BWAPI::Unit* enemy = _sunits[j].unit;
        if(unitExists(enemy)) {
          if(_lastAttack[i] != enemy) {
            ally->attack(enemy);
            _lastAttack[i] = enemy;
          }
          break;
        }
      }
    }
  }
}

Genome* BasicInterface::createGenome() {
  Genome* g = new Genome(1, _unitCount * 2, _unitCount * _unitCount, 5, 100, true, .5);
  return g;
}