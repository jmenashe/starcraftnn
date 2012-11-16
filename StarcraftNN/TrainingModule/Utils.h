#include "Macros.h"
#include <stdio.h>
#include <BWAPI.h>

std::vector<BWAPI::Unit*> getAllies();
std::vector<BWAPI::Unit*> getEnemies();
bool unitExists(BWAPI::Unit*);
BWAPI::Position getCentroid(std::vector<BWAPI::Unit*>);