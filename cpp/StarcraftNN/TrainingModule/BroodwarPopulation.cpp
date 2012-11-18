#include "BroodwarPopulation.h"

BroodwarPopulation::BroodwarPopulation(OrganismInterface* iface) : _iface(iface), _population(0) {
  _popsize = 5;
  if(!load()) {
    BWAPI::Broodwar->printf("creating new population");
    Genome* g = _iface->createGenome();
    _population = new Population(g, _popsize);
  }
  _currentOrganism = 0;
  _generation = 1;
}

BroodwarPopulation::~BroodwarPopulation(void) {
}

void BroodwarPopulation::beginIteration() {
  _iface->updateUnits(_population->organisms[_currentOrganism]);
}

void BroodwarPopulation::performStep() {
  if(!_population) {
    BWAPI::Broodwar->printf("population not initialized");
    return;
  }
  if(_population->organisms.size() <= _currentOrganism) {
    BWAPI::Broodwar->printf("%i organisms available, but current is %i", _population->organisms.size(), _currentOrganism);
    return;
  }
  Organism* organism = _population->organisms[_currentOrganism];
  _iface->sendInputs(organism);
  _iface->applyOutputs(organism);
}

void BroodwarPopulation::endIteration(double fitness) {
  if(_population->organisms.size() <= _currentOrganism) return;
  Organism* organism = _population->organisms[_currentOrganism];
  organism->fitness = fitness;
  _currentOrganism++;
  if(_currentOrganism == _popsize) {
    _generation++;
    _population->epoch(_generation);
    _currentOrganism = 0;
    _popsize =  _population->organisms.size();
    BWAPI::Broodwar->printf("epoch, organisms: %i", _popsize);
  }
}

bool BroodwarPopulation::save() {
  char * base_path = getenv("USERPROFILE");
  if(!base_path) return false;
  std::string popfile = std::string(base_path) + "\\" + POPULATION_FILE;
  std::ofstream o(popfile.c_str());
  _population->print_to_file_by_species(o);
  o.close();
  return true;
}

bool BroodwarPopulation::load() {
  char * base_path = getenv("USERPROFILE");
  if(!base_path) return false;
  std::string popfile = std::string(base_path) + "\\" + POPULATION_FILE;
  std::ifstream i(popfile.c_str());
  bool valid = i.good();
  i.close();
  if(!valid) return false;
  BWAPI::Broodwar->printf("file is good!: %s", popfile.c_str());
  if(_population) delete _population;
  _population = new Population(popfile.c_str());
  for(int i = 0; i < _population->organisms.size(); i++) {
    BWAPI::Broodwar->printf("organism recovered w/ fitness %2.2f", _population->organisms[i]->fitness);
  }
  return true;
}