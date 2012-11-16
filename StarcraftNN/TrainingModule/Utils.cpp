#include "Utils.h"

std::vector<BWAPI::Unit*> getAllies() {
  std::vector<BWAPI::Unit*> units;
  for(ITERATE_ALLY_UNITS(i)){
    BWAPI::Unit* unit = *i;
    if(unit->getType().getName() != RESET_UNIT_NAME && unit->getType().canMove())
      units.push_back(unit);
  }
  return units;
}

std::vector<BWAPI::Unit*> getEnemies() {
  std::vector<BWAPI::Unit*> units;
  for(ITERATE_ENEMY_UNITS(i)){
    BWAPI::Unit* unit = *i;
    if(unit->getType().canMove())
      units.push_back(unit);
  }
  return units;
}

bool unitExists(BWAPI::Unit* unit) {
  unit = BWAPI::Broodwar->getUnit(unit->getID());
  return unit != NULL;
}

BWAPI::Position getCentroid(std::vector<BWAPI::Unit*> units) {
  int x = 0, y = 0;
  for(int i = 0; i < units.size(); i++) {
    x += units[i]->getPosition().x();
    y += units[i]->getPosition().y();
  }
  return BWAPI::Position(x,y);
}