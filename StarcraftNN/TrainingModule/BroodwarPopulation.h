#pragma once

#include <fstream>
#include <windows.h>
#include "OrganismInterface.h"

#define POPULATION_SIZE 5
#define POPULATION_FILE "population"

using namespace NEAT;

class BroodwarPopulation {
public:
  BroodwarPopulation(OrganismInterface*);
  ~BroodwarPopulation();
  void performStep();
  void beginIteration();
  void endIteration(double);
  bool save();
  bool load();
private:
  OrganismInterface* _iface;
  Population* _population;
  int _currentOrganism, _generation;
};
