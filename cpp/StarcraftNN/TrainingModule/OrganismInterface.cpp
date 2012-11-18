#include "OrganismInterface.h"

OrganismInterface::OrganismInterface() {
  _unitCount = 3;
  for(int i = 0; i < _unitCount; i++)
    _lastAttack.push_back(0);
}

void OrganismInterface::updateUnits(Organism* organism) {
  _allies = getAllies();
  _enemies = getEnemies();
}