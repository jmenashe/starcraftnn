#include "OrganismInterface.h"

OrganismInterface::OrganismInterface(void) {
  for(int i = 0; i < 3; i++)
    _lastAttack.push_back(-1);
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
  //BWAPI::Broodwar->printf("applying outputs");
  std::vector<NNode*> outputs = _organism->net->outputs;
  for(int i = 0; i < 3; i++) {
    double attack1 = outputs[i * 3]->activation;
    double attack2 = outputs[i * 3 + 1]->activation;
    double attack3 = outputs[i * 3 + 2]->activation;
    int max = maxIndex(3, attack1, attack2, attack3);
    BWAPI::Broodwar->printf("attacks are %2.2f, %2.2f, %2.2f (max %i)", attack1, attack2, attack3, max);
    BWAPI::Unit* ally = _allies[i];
    if(_lastAttack[i] != max) {
      //ally->attack(_enemies[max]);
      _lastAttack[i] = max;
    }
  }
}

int OrganismInterface::maxIndex(int count, ... ) {
  va_list values;
  va_start(values, count); 
  double max = 0;
  int index = -1;
  for(int i = 0; i < count; ++i ) {
    double val = va_arg(values, double);
    if(max < val) {
      max = val;
      index = i;
    }
  }
  return index;
}

Genome* OrganismInterface::createGenome() {
  Genome* g = new Genome(1, Sensors::SENSOR_COUNT, Effectors::EFFECTOR_COUNT, 1, 100, true, .5);
  return g;
}