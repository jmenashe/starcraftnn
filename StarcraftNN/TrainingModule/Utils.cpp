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