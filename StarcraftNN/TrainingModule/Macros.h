#define RESET_UNIT_NAME "Terran Vulture"
#define ITERATE_ALLY_UNITS(i)  std::set<BWAPI::Unit*>::const_iterator i = BWAPI::Broodwar->self()->getUnits().begin(); i != BWAPI::Broodwar->self()->getUnits().end(); ++i
#define ITERATE_ENEMY_UNITS(i) std::set<BWAPI::Unit*>::const_iterator i = BWAPI::Broodwar->enemy()->getUnits().begin(); i != BWAPI::Broodwar->enemy()->getUnits().end(); ++i