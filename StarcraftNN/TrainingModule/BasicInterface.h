#pragma once

#include "OrganismInterface.h"

using namespace NEAT;

class BasicInterface : public OrganismInterface {
public:
  BasicInterface();
  Genome* createGenome();
  void sendInputs(Organism*);
  void applyOutputs(Organism*);
};
