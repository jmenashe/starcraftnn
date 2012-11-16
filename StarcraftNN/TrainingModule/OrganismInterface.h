#pragma once

#include <organism.h>
#include <BWAPI.h>
#include <math.h>
#include <stdarg.h>
#include "Macros.h"
#include "Utils.h"
#include "ScoredUnit.h"

using namespace NEAT;

class OrganismInterface {
public:
  OrganismInterface();
  virtual Genome* createGenome() = 0;
  virtual void sendInputs(Organism*) = 0;
  virtual void applyOutputs(Organism*) = 0;
  virtual void updateUnits(Organism*);
protected:
  std::vector<BWAPI::Unit*> _allies, _enemies;
  std::vector<BWAPI::Unit*> _lastAttack;
  int _unitCount;
};
