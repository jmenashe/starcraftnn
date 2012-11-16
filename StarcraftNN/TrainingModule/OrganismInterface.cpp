#include "OrganismInterface.h"

OrganismInterface::OrganismInterface(void) {
  UNIT_COUNT = 3;
  for(int i = 0; i < UNIT_COUNT; i++)
    _lastAttack.push_back(0);
}

OrganismInterface::~OrganismInterface(void) {
  
}

BWAPI::Position OrganismInterface::getCentroid(std::vector<BWAPI::Unit*> units) {
  int x = 0, y = 0;
  for(int i = 0; i < units.size(); i++) {
    x += units[i]->getPosition().x();
    y += units[i]->getPosition().y();
  }
  return BWAPI::Position(x,y);
}

void OrganismInterface::updateUnits() {
  _allies = getAllies();
  _enemies = getEnemies();
}

void OrganismInterface::sendInputs(Organism* organism) {
  Network* network = organism->net;
  std::vector<float> sense_values;
  sense_values.resize(Sensors::SENSOR_COUNT);
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
  _organism = organism;
}

void OrganismInterface::applyOutputs() {
  std::vector<NNode*> outputs = _organism->net->outputs;
  std::vector<ScoredUnit> _sunits;
  for(int i = 0; i < UNIT_COUNT; i++) {
    for(int j = 0; j < UNIT_COUNT; j++) {
      double attack = outputs[i * UNIT_COUNT + j]->activation;
      _sunits.push_back(ScoredUnit(_enemies[j], attack));
    }
    sort(_sunits.begin(), _sunits.end(), ScoredUnit::compare);
    
    BWAPI::Unit* ally = _allies[i];
    if(unitExists(ally)) {
      for(int j = 0; j < _sunits.size(); j++) {
        BWAPI::Unit* enemy = _sunits[j].unit;
        if(unitExists(enemy)) {
          if(_lastAttack[i] != enemy) {
            //BWAPI::Broodwar->printf("%i attacks %i (scores: %2.2f, %2.2f, %2.2f)", i, enemy, _sunits[0].score, _sunits[1].score, _sunits[2].score);
            ally->attack(enemy);
            _lastAttack[i] = enemy;
          }
          break;
        }
      }
    }
  }
}

Genome* OrganismInterface::createGenome() {
  Genome* g = new Genome(1, Sensors::SENSOR_COUNT, Effectors::EFFECTOR_COUNT, 5, 100, true, .5);
  return g;
}